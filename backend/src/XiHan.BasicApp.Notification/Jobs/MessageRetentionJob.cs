// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.BasicApp.Contracts.Settings;
using XiHan.BasicApp.Notification.Domain.Entities;
using XiHan.BasicApp.Runtime.MultiTenancy;
using XiHan.Framework.Persistence.Abstractions.UnitOfWork;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;
using XiHan.Framework.Scheduling.Abstractions.Jobs;
using XiHan.Framework.Scheduling.Abstractions.Schedules;

namespace XiHan.BasicApp.Notification.Jobs;

/// <summary>
/// 发送记录的保留清理
/// </summary>
/// <remarks>
/// 内建作业（每天 3 点半）。保留天数读平台参数 <c>notification.retention-days</c>（1 到 3650，缺省 180），不合法直接失败，不按错误的保留期删数据
/// 邮件、短信与机器人消息兼作发件箱，只删已结束的（已发送、失败、已取消）：排队中与发送中的还要发，再老也不动；Telegram 出站审计按时间删
/// 四张表都在平台库、跨侧一遍删完，连同软删除的行；逐批（每批 <see cref="BatchSize"/> 条，一批一个工作单元）物理删除，一类失败只记日志、计入失败，不影响其他
/// </remarks>
/// <param name="readers">参数读取（Settings 实现）</param>
/// <param name="scopes">作用域工厂</param>
/// <param name="clock">时间源</param>
/// <param name="logger">日志</param>
public sealed partial class MessageRetentionJob(IEnumerable<IConfigValueReader> readers, IServiceScopeFactory scopes, TimeProvider clock, ILogger<MessageRetentionJob> logger) : IJob
{
    /// <summary>
    /// 作业名
    /// </summary>
    public const string Name = "notification.messages.cleanup";

    /// <summary>
    /// 保留天数参数的键
    /// </summary>
    public const string RetentionDaysConfigKey = "notification.retention-days";

    /// <summary>
    /// 缺省保留天数
    /// </summary>
    public const int DefaultRetentionDays = 180;

    /// <summary>
    /// 保留天数的上限（十年）
    /// </summary>
    public const int MaxRetentionDays = 3650;

    /// <summary>
    /// 一批删除的条数
    /// </summary>
    public const int BatchSize = 1000;

    /// <summary>
    /// 调度：每天 3 点半
    /// </summary>
    public static JobSchedule Schedule { get; } = JobSchedule.FromCron("30 3 * * *");

    /// <summary>
    /// 保留天数内建参数的元数据
    /// </summary>
    public static ConfigDescriptor RetentionDaysConfig { get; } = new(
        RetentionDaysConfigKey,
        "发送记录保留天数",
        "消息通知",
        ConfigDataType.Number,
        "180",
        "邮件、短信与机器人消息的发送记录（只算已结束的）与 Telegram 出站审计保留的天数（1–3650 的整数），保留清理作业删掉更早的；只认平台的，写错即清理失败",
        Order: 420);

    /// <inheritdoc />
    public async ValueTask<Result> RunAsync(JobRun run, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(context);
        var days = await RetentionDaysAsync(context).ConfigureAwait(false);
        if (!days.TryGetValue(out var retention))
        {
            return days.Error!;
        }

        var cutoff = clock.GetUtcNow().AddDays(-retention);
        var cancellationToken = context.CancellationToken;
        var removed = 0L;
        var failures = 0;
        var purges = new (string Table, Func<Task<long>> Purge)[]
        {
            ("sys_email", () => PurgeAsync<SysEmail>(e => e.CreatedAt < cutoff && (e.Status == MessageSendStatus.Success || e.Status == MessageSendStatus.Failed || e.Status == MessageSendStatus.Cancelled), cancellationToken)),
            ("sys_sms", () => PurgeAsync<SysSms>(s => s.CreatedAt < cutoff && (s.Status == MessageSendStatus.Success || s.Status == MessageSendStatus.Failed || s.Status == MessageSendStatus.Cancelled), cancellationToken)),
            ("sys_bot_message", () => PurgeAsync<SysBotMessage>(b => b.CreatedAt < cutoff && (b.Status == MessageSendStatus.Success || b.Status == MessageSendStatus.Failed || b.Status == MessageSendStatus.Cancelled), cancellationToken)),
            ("sys_telegram_message", () => PurgeAsync<SysTelegramMessage>(t => t.CreatedAt < cutoff, cancellationToken)),
        };
        foreach (var (table, purge) in purges)
        {
            try
            {
                removed += await purge().ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                failures++;
                LogPurgeFailed(logger, exception, table);
            }
        }

        LogPurged(logger, retention, cutoff, removed, failures);
        return TenantSides.Outcome(Name, failures);
    }

    /// <summary>
    /// 读取保留天数
    /// </summary>
    /// <remarks>
    /// 没装 Settings 或参数没建即用缺省值；建了必须是 1 到 <see cref="MaxRetentionDays"/> 的整数
    /// </remarks>
    private async ValueTask<Result<int>> RetentionDaysAsync(InvocationContext context)
    {
        if (readers.FirstOrDefault() is not { } reader)
        {
            return DefaultRetentionDays;
        }

        var configured = await reader.GetAsync(RetentionDaysConfigKey, context).ConfigureAwait(false);
        if (!configured.TryGetValue(out var value))
        {
            return configured.Error!;
        }

        if (!value.TryGetValue(out var text))
        {
            return DefaultRetentionDays;
        }

        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var days) && days is >= 1 and <= MaxRetentionDays
            ? days
            : Error.Validation(NotificationErrors.RetentionInvalid, NotificationMessages.RetentionInvalid, MessageArguments.Of(("value", text)));
    }

    /// <summary>
    /// 一类记录的清理：逐批取出到期的主键再物理删除，取不满一批即结束
    /// </summary>
    private async Task<long> PurgeAsync<TEntity>(Expression<Func<TEntity, bool>> due, CancellationToken cancellationToken)
        where TEntity : AppEntity, new()
    {
        var removed = 0L;
        while (true)
        {
            await using var scope = scopes.CreateAsyncScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<SqlSugarUnitOfWork>();
            await unitOfWork.BeginAsync(new UnitOfWorkOptions(), cancellationToken).ConfigureAwait(false);
            int batch;
            try
            {
                var client = unitOfWork.ConnectionFor(typeof(TEntity));
                var ids = await client.Queryable<TEntity>().ClearFilter().Where(due).OrderBy(e => e.Id).Take(BatchSize).Select(e => e.Id).ToListAsync(cancellationToken).ConfigureAwait(false);
                batch = ids.Count == 0 ? 0 : await client.Deleteable<TEntity>().In(ids).ExecuteCommandAsync(cancellationToken).ConfigureAwait(false);
                (await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false)).ThrowIfFailure();
            }
            catch
            {
                if (unitOfWork.State == UnitOfWorkState.Active)
                {
                    await unitOfWork.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                }

                throw;
            }

            removed += batch;
            if (batch < BatchSize)
            {
                return removed;
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Notification retention ({Days} days, before {Cutoff:O}) removed {Removed} records with {Failures} failures.")]
    private static partial void LogPurged(ILogger logger, int days, DateTimeOffset cutoff, long removed, int failures);

    [LoggerMessage(Level = LogLevel.Error, Message = "Notification retention failed for table '{Table}'.")]
    private static partial void LogPurgeFailed(ILogger logger, Exception exception, string table);
}
