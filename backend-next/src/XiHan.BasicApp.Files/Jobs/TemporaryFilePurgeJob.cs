// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Files.Domain.Services;
using XiHan.Framework.MultiTenancy.Abstractions.Tenants;
using XiHan.Framework.Persistence.Abstractions.UnitOfWork;
using XiHan.Framework.Primitives;
using XiHan.Framework.Scheduling.Abstractions.Jobs;
using XiHan.Framework.Scheduling.Abstractions.Schedules;

namespace XiHan.BasicApp.Files.Jobs;

/// <summary>
/// 到期临时文件的清理
/// </summary>
/// <remarks>
/// 内建作业，每小时一次（4.x 有临时文件与到期时间、没有清理作业，写法照分片上传的过期处理）：平台与目录里的每个租户逐个作用域、
/// 逐批（每批 <see cref="FileService.PurgeBatchSize"/> 条，一批一个工作单元）删掉过了到期时间的临时文件，存储里不再被引用的对象一起删；
/// 一侧一次至多 <see cref="MaxBatchesPerSide"/> 批，积压的留给下一次。代码生成的产物包与导出中心的结果文件都在其列
/// 一个作用域失败只记日志、计入失败，不影响其他。读取侧本来就按到期拒绝，不等这个作业
/// </remarks>
/// <param name="tenants">租户目录</param>
/// <param name="scopes">作用域工厂</param>
/// <param name="logger">日志</param>
public sealed partial class TemporaryFilePurgeJob(ITenantStore tenants, IServiceScopeFactory scopes, ILogger<TemporaryFilePurgeJob> logger) : IJob
{
    /// <summary>
    /// 作业名
    /// </summary>
    public const string Name = "files.temporary.purge";

    /// <summary>
    /// 一侧一次运行至多处理的批数
    /// </summary>
    public const int MaxBatchesPerSide = 20;

    /// <summary>
    /// 计划：每小时第 45 分钟
    /// </summary>
    public static JobSchedule Schedule { get; } = JobSchedule.FromCron("45 * * * *");

    /// <inheritdoc />
    public async ValueTask<Result> RunAsync(JobRun run, InvocationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(context);
        var sides = new List<string> { TenantIds.Platform };
        sides.AddRange((await tenants.ListAsync(cancellationToken).ConfigureAwait(false)).Select(static t => t.Id));
        var purged = 0;
        var failures = 0;
        var deferred = 0;
        foreach (var side in sides)
        {
            try
            {
                var batches = 0;
                int batch;
                do
                {
                    batch = await InScopeAsync(side, cancellationToken).ConfigureAwait(false);
                    purged += batch;
                    batches++;
                }
                while (batch == FileService.PurgeBatchSize && batches < MaxBatchesPerSide);

                if (batch == FileService.PurgeBatchSize)
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

        LogPurged(logger, purged, deferred, failures);
        return Result.Success();
    }

    /// <summary>
    /// 在独立作用域、带租户的工作单元里处理一批
    /// </summary>
    /// <remarks>
    /// 成功即提交，抛出即回滚
    /// </remarks>
    private async Task<int> InScopeAsync(string side, CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await unitOfWork.BeginAsync(new UnitOfWorkOptions(TenantId: side), cancellationToken).ConfigureAwait(false);
        try
        {
            var count = await scope.ServiceProvider.GetRequiredService<FileService>().PurgeExpiredAsync(side, cancellationToken).ConfigureAwait(false);
            (await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false)).ThrowIfFailure();
            return count;
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

    [LoggerMessage(Level = LogLevel.Information, Message = "Temporary file purge deleted {Purged} expired files; {Deferred} sides have more left for the next run; {Failures} failures.")]
    private static partial void LogPurged(ILogger logger, int purged, int deferred, int failures);

    [LoggerMessage(Level = LogLevel.Error, Message = "Temporary file purge failed for side '{Side}'.")]
    private static partial void LogPurgeFailed(ILogger logger, Exception exception, string side);
}
