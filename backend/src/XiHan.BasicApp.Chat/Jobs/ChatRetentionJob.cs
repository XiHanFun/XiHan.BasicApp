// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlSugar;
using XiHan.BasicApp.Chat.Domain.Entities;
using XiHan.BasicApp.Chat.Domain.Services;
using XiHan.BasicApp.Contracts.Files;
using XiHan.BasicApp.Runtime.MultiTenancy;
using XiHan.Framework.MultiTenancy.Abstractions.Sharding;
using XiHan.Framework.Persistence.Abstractions.UnitOfWork;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;
using XiHan.Framework.Scheduling.Abstractions.Jobs;
using XiHan.Framework.Scheduling.Abstractions.Schedules;

namespace XiHan.BasicApp.Chat.Jobs;

/// <summary>
/// 聊天的保留清理
/// </summary>
/// <remarks>
/// 内建作业（每天 4 点）。保留天数读平台的 <c>chat.policy</c>（没建即 365 天，不合法直接失败、不按错误的保留期删数据）；
/// 平台与目录里的每个租户逐个作用域删除（独立库的租户先绑定分片，清的是它自己的库），一个作用域失败只记日志、计入失败，不影响其他：
/// 过期的消息按发出时间逐批（每批 <see cref="BatchSize"/> 条，一批一个工作单元）连同表情回应删除，这批消息的附件文件里不再被留存消息引用、也不是会话头像的，经 Files 一并删除；
/// 结束早于保留期的通话连同参与者删除。不删会话与成员。每个作用域一次至多 <see cref="MaxBatchesPerSide"/> 批，剩下的下次接着删
/// </remarks>
/// <param name="policy">聊天策略</param>
/// <param name="sides">要清理的各侧</param>
/// <param name="scopes">作用域工厂</param>
/// <param name="clock">时间源</param>
/// <param name="logger">日志</param>
public sealed partial class ChatRetentionJob(ChatPolicy policy, TenantSides sides, IServiceScopeFactory scopes, TimeProvider clock, ILogger<ChatRetentionJob> logger) : IJob
{
    /// <summary>
    /// 作业名
    /// </summary>
    public const string Name = "chat.messages.cleanup";

    /// <summary>
    /// 一批处理的消息或通话条数
    /// </summary>
    public const int BatchSize = 500;

    /// <summary>
    /// 一个作用域一次至多处理的批数
    /// </summary>
    public const int MaxBatchesPerSide = 200;

    /// <summary>
    /// 核对会话头像时一条查询带的文件数
    /// </summary>
    private const int AvatarChunk = 50;

    /// <summary>
    /// 调度：每天 4 点
    /// </summary>
    public static JobSchedule Schedule { get; } = JobSchedule.FromCron("0 4 * * *");

    /// <inheritdoc />
    public async ValueTask<Result> RunAsync(JobRun run, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(context);
        var cancellationToken = context.CancellationToken;
        var settings = await policy.ReadAsync(context).ConfigureAwait(false);
        if (!settings.TryGetValue(out var configured))
        {
            return settings.Error!;
        }

        var cutoff = clock.GetUtcNow().AddDays(-configured.RetentionDays);
        var total = new Purged(0, 0, 0, 0, 0);
        var failures = 0;
        var deferred = 0;
        var listedSides = await sides.ListAsync(cancellationToken).ConfigureAwait(false);
        if (!listedSides.TryGetValue(out var sideIds))
        {
            return listedSides.Error!;
        }

        foreach (var side in sideIds)
        {
            try
            {
                var (purged, finished) = await PurgeSideAsync(side, cutoff, cancellationToken).ConfigureAwait(false);
                total += purged;
                if (!finished)
                {
                    deferred++;
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                failures++;
                LogPurgeFailed(logger, exception, side);
            }
        }

        LogPurged(logger, configured.RetentionDays, cutoff, total.Messages, total.Reactions, total.Files, total.Calls, total.Participants, deferred, failures);
        return TenantSides.Outcome(Name, failures);
    }

    /// <summary>
    /// 一个作用域的清理：先逐批删消息，再逐批删通话
    /// </summary>
    /// <returns>删除的条数；批数用满还没删完时 <c>Finished</c> 为假</returns>
    private async Task<(Purged Purged, bool Finished)> PurgeSideAsync(string side, DateTimeOffset cutoff, CancellationToken cancellationToken)
    {
        var purged = new Purged(0, 0, 0, 0, 0);
        var batches = 0;
        while (batches < MaxBatchesPerSide)
        {
            var batch = await InScopeAsync(side, (client, files) => PurgeMessagesAsync(client, files, side, cutoff, cancellationToken), cancellationToken).ConfigureAwait(false);
            purged += batch;
            batches++;
            if (batch.Messages < BatchSize)
            {
                break;
            }
        }

        while (batches < MaxBatchesPerSide)
        {
            var batch = await InScopeAsync(side, (client, _) => PurgeCallsAsync(client, side, cutoff, cancellationToken), cancellationToken).ConfigureAwait(false);
            purged += batch;
            batches++;
            if (batch.Calls < BatchSize)
            {
                return (purged, true);
            }
        }

        return (purged, false);
    }

    /// <summary>
    /// 在独立作用域、带租户的工作单元里处理一批：成功即提交，抛出即回滚
    /// </summary>
    private async Task<Purged> InScopeAsync(string side, Func<ISqlSugarClient, IAttachmentFiles?, Task<Purged>> work, CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        (await scope.ServiceProvider.GetRequiredService<TenantShardScope>().BindTenantAsync(side, cancellationToken).ConfigureAwait(false)).ToResult().ThrowIfFailure();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<SqlSugarUnitOfWork>();
        await unitOfWork.BeginAsync(new UnitOfWorkOptions { TenantId = side }, cancellationToken).ConfigureAwait(false);
        try
        {
            // 附件与消息在同一侧的库里，文件的删除跟这批消息在同一个工作单元里提交
            var purged = await work(unitOfWork.ConnectionFor(typeof(SysChatMessage)), scope.ServiceProvider.GetService<IAttachmentFiles>()).ConfigureAwait(false);
            (await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false)).ThrowIfFailure();
            return purged;
        }
        catch
        {
            if (unitOfWork.State == UnitOfWorkState.Active)
            {
                await unitOfWork.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            }

            throw;
        }
    }

    /// <summary>
    /// 一批过期消息：先删不再用到的附件文件，再删表情回应与消息
    /// </summary>
    /// <remarks>
    /// 转发与聊天记录沿用原来的文件，同一个文件可能挂在多条消息上：只有留存的消息（发出时间不早于保留期）都不再引用、也不是会话头像的才删
    /// 还没轮到的过期消息不算引用，它们随后同样被删。没装 Files 就没有附件文件
    /// </remarks>
    private static async Task<Purged> PurgeMessagesAsync(ISqlSugarClient client, IAttachmentFiles? files, string side, DateTimeOffset cutoff, CancellationToken cancellationToken)
    {
        var expired = await client.Queryable<SysChatMessage>()
            .Where(m => m.TenantId == side && m.CreatedAt < cutoff)
            .OrderBy(m => m.CreatedAt)
            .OrderBy(m => m.Id)
            .Take(BatchSize)
            .Select(m => new SysChatMessage { Id = m.Id, Attachments = m.Attachments })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        if (expired.Count == 0)
        {
            return new Purged(0, 0, 0, 0, 0);
        }

        var removedFiles = 0;
        var attached = expired.SelectMany(static m => ChatAttachmentsJson.Deserialize(m.Attachments)).Select(static a => a.FileId).Distinct().ToList();
        if (files is not null && attached.Count > 0)
        {
            var used = await StillUsedAsync(client, side, cutoff, attached, cancellationToken).ConfigureAwait(false);
            var unused = attached.Where(id => !used.Contains(id)).ToList();
            if (unused.Count > 0)
            {
                removedFiles = await files.DeleteAsync(side, ChatRules.AttachmentSource, unused, cancellationToken).ConfigureAwait(false);
            }
        }

        var ids = expired.Select(static m => m.Id).ToList();
        var reactions = await client.Deleteable<SysChatMessageReaction>()
            .Where(r => r.TenantId == side && ids.Contains(r.MessageId))
            .ExecuteCommandAsync(cancellationToken).ConfigureAwait(false);
        var messages = await client.Deleteable<SysChatMessage>()
            .Where(m => m.TenantId == side && ids.Contains(m.Id))
            .ExecuteCommandAsync(cancellationToken).ConfigureAwait(false);
        return new Purged(messages, reactions, removedFiles, 0, 0);
    }

    /// <summary>
    /// 这些文件里仍被留存消息引用或用作会话头像的
    /// </summary>
    /// <remarks>
    /// 逐个文件按文本查留存消息的附件 JSON：JSON 里还有上传方给的文件名，名字里写着别的文件 ID 也算在用，宁可多留一个文件；头像列存的就是文件 ID 的文本
    /// </remarks>
    private static async Task<HashSet<Guid>> StillUsedAsync(ISqlSugarClient client, string side, DateTimeOffset cutoff, List<Guid> fileIds, CancellationToken cancellationToken)
    {
        var used = new HashSet<Guid>();
        foreach (var id in fileIds)
        {
            var marker = id.ToString();
            if (await client.Queryable<SysChatMessage>()
                .Where(m => m.TenantId == side && m.CreatedAt >= cutoff && m.Attachments != null && m.Attachments.Contains(marker))
                .AnyAsync(cancellationToken)
                .ConfigureAwait(false))
            {
                used.Add(id);
            }
        }

        foreach (var chunk in fileIds.Chunk(AvatarChunk))
        {
            var markers = chunk.Select(static id => id.ToString()).ToList();
            var candidates = chunk.ToHashSet();
            var avatars = await client.Queryable<SysChatConversation>()
                .ClearFilter()
                .Where(c => c.TenantId == side && c.Avatar != null && markers.Contains(c.Avatar!))
                .Select(c => c.Avatar)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            used.UnionWith(avatars.Select(static avatar => Guid.TryParse(avatar, out var id) ? id : Guid.Empty).Where(candidates.Contains));
        }

        return used;
    }

    /// <summary>
    /// 一批结束早于保留期的通话，连同参与者
    /// </summary>
    private static async Task<Purged> PurgeCallsAsync(ISqlSugarClient client, string side, DateTimeOffset cutoff, CancellationToken cancellationToken)
    {
        var ids = await client.Queryable<SysChatCall>()
            .ClearFilter()
            .Where(c => c.TenantId == side && c.EndedAt.HasValue && c.EndedAt.Value < cutoff)
            .OrderBy(c => c.Id)
            .Take(BatchSize)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        if (ids.Count == 0)
        {
            return new Purged(0, 0, 0, 0, 0);
        }

        var participants = await client.Deleteable<SysChatCallParticipant>()
            .Where(p => p.TenantId == side && ids.Contains(p.CallId))
            .ExecuteCommandAsync(cancellationToken).ConfigureAwait(false);
        var calls = await client.Deleteable<SysChatCall>()
            .Where(c => c.TenantId == side && ids.Contains(c.Id))
            .ExecuteCommandAsync(cancellationToken).ConfigureAwait(false);
        return new Purged(0, 0, 0, calls, participants);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Chat retention ({Days} days, before {Cutoff:O}) removed {Messages} messages, {Reactions} reactions, {Files} attachment files, {Calls} calls and {Participants} call participants; {Deferred} scopes continue next run, {Failures} failures.")]
    private static partial void LogPurged(ILogger logger, int days, DateTimeOffset cutoff, long messages, long reactions, long files, long calls, long participants, int deferred, int failures);

    [LoggerMessage(Level = LogLevel.Error, Message = "Chat retention failed for scope '{Scope}'.")]
    private static partial void LogPurgeFailed(ILogger logger, Exception exception, string scope);

    /// <summary>
    /// 删除的条数
    /// </summary>
    /// <param name="Messages">消息</param>
    /// <param name="Reactions">表情回应</param>
    /// <param name="Files">附件文件</param>
    /// <param name="Calls">通话</param>
    /// <param name="Participants">通话参与者</param>
    private readonly record struct Purged(long Messages, long Reactions, long Files, long Calls, long Participants)
    {
        /// <summary>
        /// 两份计数相加
        /// </summary>
        public static Purged operator +(Purged left, Purged right)
            => new(left.Messages + right.Messages, left.Reactions + right.Reactions, left.Files + right.Files, left.Calls + right.Calls, left.Participants + right.Participants);
    }
}
