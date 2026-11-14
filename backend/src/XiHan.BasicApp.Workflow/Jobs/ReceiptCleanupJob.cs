// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Runtime.MultiTenancy;
using XiHan.BasicApp.Workflow.Domain.Entities;
using XiHan.Framework.MultiTenancy.Abstractions.Sharding;
using XiHan.Framework.Persistence.Abstractions.UnitOfWork;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;
using XiHan.Framework.Scheduling.Abstractions.Jobs;
using XiHan.Framework.Scheduling.Abstractions.Schedules;

namespace XiHan.BasicApp.Workflow.Jobs;

/// <summary>
/// 已处理工作流事件回执的清理
/// </summary>
/// <remarks>
/// 内建作业（每天 4 点半）。回执只用来防同一事件被处理两次：事件至少投递一次，重投都在投递重试的时限之内，远短于 <see cref="Retention"/>，过了这么久的回执不会再用到
/// 平台与目录里的每个租户逐个作用域删除（独立库的租户先绑定分片，清的是它自己的库），逐批（每批 <see cref="BatchSize"/> 条，一批一个工作单元）；一个作用域失败只记日志、计入失败，不影响其他
/// </remarks>
/// <param name="sides">要清理的各侧</param>
/// <param name="scopes">作用域工厂</param>
/// <param name="clock">时间源</param>
/// <param name="logger">日志</param>
public sealed partial class ReceiptCleanupJob(TenantSides sides, IServiceScopeFactory scopes, TimeProvider clock, ILogger<ReceiptCleanupJob> logger) : IJob
{
    /// <summary>
    /// 作业名
    /// </summary>
    public const string Name = "workflow.receipts.cleanup";

    /// <summary>
    /// 一批删除的条数
    /// </summary>
    public const int BatchSize = 1000;

    /// <summary>
    /// 回执保留多久（90 天）
    /// </summary>
    public static TimeSpan Retention { get; } = TimeSpan.FromDays(90);

    /// <summary>
    /// 调度：每天 4 点半
    /// </summary>
    public static JobSchedule Schedule { get; } = JobSchedule.FromCron("30 4 * * *");

    /// <inheritdoc />
    public async ValueTask<Result> RunAsync(JobRun run, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(context);
        var cancellationToken = context.CancellationToken;
        var cutoff = clock.GetUtcNow() - Retention;
        var listedSides = await sides.ListAsync(cancellationToken).ConfigureAwait(false);
        if (!listedSides.TryGetValue(out var sideIds))
        {
            return listedSides.Error!;
        }

        var removed = 0L;
        var failures = 0;
        foreach (var side in sideIds)
        {
            try
            {
                removed += await PurgeAsync(side, cutoff, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                failures++;
                LogPurgeFailed(logger, exception, side);
            }
        }

        LogPurged(logger, cutoff, removed, failures);
        return TenantSides.Outcome(Name, failures);
    }

    /// <summary>
    /// 一侧的清理：逐批取出过期回执的主键再删，取不满一批即结束
    /// </summary>
    private async Task<long> PurgeAsync(string side, DateTimeOffset cutoff, CancellationToken cancellationToken)
    {
        var removed = 0L;
        while (true)
        {
            await using var scope = scopes.CreateAsyncScope();
            (await scope.ServiceProvider.GetRequiredService<TenantShardScope>().BindTenantAsync(side, cancellationToken).ConfigureAwait(false)).ToResult().ThrowIfFailure();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<SqlSugarUnitOfWork>();
            await unitOfWork.BeginAsync(new UnitOfWorkOptions { TenantId = side }, cancellationToken).ConfigureAwait(false);
            int batch;
            try
            {
                var client = unitOfWork.ConnectionFor(typeof(SysWorkflowMessageReceipt));
                var ids = await client.Queryable<SysWorkflowMessageReceipt>().Where(r => r.TenantId == side && r.CreatedAt < cutoff).OrderBy(r => r.Id).Take(BatchSize).Select(r => r.Id).ToListAsync(cancellationToken).ConfigureAwait(false);
                batch = ids.Count == 0 ? 0 : await client.Deleteable<SysWorkflowMessageReceipt>().In(ids).ExecuteCommandAsync(cancellationToken).ConfigureAwait(false);
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

    [LoggerMessage(Level = LogLevel.Information, Message = "Workflow receipt cleanup (before {Cutoff:O}) removed {Removed} receipts with {Failures} failures.")]
    private static partial void LogPurged(ILogger logger, DateTimeOffset cutoff, long removed, int failures);

    [LoggerMessage(Level = LogLevel.Error, Message = "Workflow receipt cleanup failed for side '{Side}'.")]
    private static partial void LogPurgeFailed(ILogger logger, Exception exception, string side);
}
