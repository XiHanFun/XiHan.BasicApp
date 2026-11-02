// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlSugar;
using XiHan.BasicApp.Chat.Domain.Entities;
using XiHan.BasicApp.Chat.Domain.Services;
using XiHan.BasicApp.Contracts;
using XiHan.Framework.MultiTenancy.Abstractions.Sharding;
using XiHan.Framework.MultiTenancy.Abstractions.Tenants;
using XiHan.Framework.Persistence.Abstractions.UnitOfWork;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;
using XiHan.Framework.Scheduling.Abstractions.Jobs;
using XiHan.Framework.Scheduling.Abstractions.Schedules;

namespace XiHan.BasicApp.Chat.Jobs;

/// <summary>
/// 聊天消息的保留清理。
/// </summary>
/// <remarks>
/// 内建作业（照 4.x 的 <c>chat-retention-cleanup</c>，每天 4 点）。保留天数读平台的 <c>chat.policy</c>（没建即 365 天，不合法直接失败、不按错误的保留期删数据）；
/// 平台与目录里的每个租户逐个作用域删除（独立库的租户先绑定分片，清的是它自己的库）：先删所属消息已过期的表情回应，再删过期的消息；
/// 不删会话、成员，也不删附件文件（照 4.x）。一个作用域失败只记日志、计入失败，不影响其他。
/// </remarks>
/// <param name="policy">聊天策略。</param>
/// <param name="tenants">租户目录。</param>
/// <param name="scopes">作用域工厂。</param>
/// <param name="clock">时间源。</param>
/// <param name="logger">日志。</param>
public sealed partial class ChatRetentionJob(ChatPolicy policy, ITenantStore tenants, IServiceScopeFactory scopes, TimeProvider clock, ILogger<ChatRetentionJob> logger) : IJob
{
    /// <summary>
    /// 作业名。
    /// </summary>
    public const string Name = "chat.messages.cleanup";

    /// <summary>
    /// 调度：每天 4 点。
    /// </summary>
    /// <remarks>
    /// 照 4.x 的 Cron <c>0 4 * * *</c>。
    /// </remarks>
    public static JobSchedule Schedule { get; } = JobSchedule.FromCron("0 4 * * *");

    /// <inheritdoc />
    public async ValueTask<Result> RunAsync(JobRun run, InvocationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(context);
        var settings = await policy.ReadAsync(context).ConfigureAwait(false);
        if (!settings.TryGetValue(out var configured))
        {
            return settings.Error!;
        }

        var cutoff = clock.GetUtcNow().AddDays(-configured.RetentionDays);
        var removedMessages = 0L;
        var removedReactions = 0L;
        var failures = 0;
        var sides = new List<string> { TenantIds.Platform };
        sides.AddRange((await tenants.ListAsync(cancellationToken).ConfigureAwait(false)).Select(static t => t.Id));
        foreach (var side in sides)
        {
            try
            {
                var (reactions, messages) = await PurgeAsync(side, cutoff, cancellationToken).ConfigureAwait(false);
                removedReactions += reactions;
                removedMessages += messages;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                failures++;
                LogPurgeFailed(logger, exception, side);
            }
        }

        LogPurged(logger, configured.RetentionDays, cutoff, removedMessages, removedReactions, failures);
        return Result.Success();
    }

    /// <summary>
    /// 一个作用域的清理：在独立作用域、带租户的工作单元里先删回应再删消息。
    /// </summary>
    /// <remarks>
    /// 回应没有自己的保留期，按「所属消息已过期」删（照 4.x：按回应自己的时间删会漏掉「老消息 + 新回应」，顺序反过来就再也筛不出这些回应）。
    /// </remarks>
    private async Task<(int Reactions, int Messages)> PurgeAsync(string side, DateTimeOffset cutoff, CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        (await scope.ServiceProvider.GetRequiredService<TenantShardScope>().BindTenantAsync(side, cancellationToken).ConfigureAwait(false)).ToResult().ThrowIfFailure();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<SqlSugarUnitOfWork>();
        await unitOfWork.BeginAsync(new UnitOfWorkOptions(TenantId: side), cancellationToken).ConfigureAwait(false);
        try
        {
            var client = unitOfWork.ConnectionFor(typeof(SysChatMessage));
            var reactions = await client.Deleteable<SysChatMessageReaction>()
                .Where(r => r.TenantId == side && SqlFunc.Subqueryable<SysChatMessage>().Where(m => m.Id == r.MessageId && m.CreatedAt < cutoff).Any())
                .ExecuteCommandAsync(cancellationToken).ConfigureAwait(false);
            var messages = await client.Deleteable<SysChatMessage>()
                .Where(m => m.TenantId == side && m.CreatedAt < cutoff)
                .ExecuteCommandAsync(cancellationToken).ConfigureAwait(false);
            (await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false)).ThrowIfFailure();
            return (reactions, messages);
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

    [LoggerMessage(Level = LogLevel.Information, Message = "Chat retention ({Days} days, before {Cutoff:O}) removed {Messages} messages and {Reactions} reactions with {Failures} failures.")]
    private static partial void LogPurged(ILogger logger, int days, DateTimeOffset cutoff, long messages, long reactions, int failures);

    [LoggerMessage(Level = LogLevel.Error, Message = "Chat retention failed for scope '{Scope}'.")]
    private static partial void LogPurgeFailed(ILogger logger, Exception exception, string scope);
}
