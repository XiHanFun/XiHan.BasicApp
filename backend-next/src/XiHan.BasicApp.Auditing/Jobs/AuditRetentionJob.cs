// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Auditing.Domain.Entities;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Settings;
using XiHan.Framework.Auditing.Database.Trail;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.MultiTenancy.Abstractions.Tenants;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Persistence.Abstractions.UnitOfWork;
using XiHan.Framework.Primitives;
using XiHan.Framework.Scheduling.Abstractions.Jobs;

namespace XiHan.BasicApp.Auditing.Jobs;

/// <summary>
/// 日志保留清理（内建作业，方案 5.12）：删除早于保留期的日志。保留天数读平台参数 <c>auditing.retention-days</c>（1 到 3650），没建即用 <see cref="AuditLogOptions.DefaultRetentionDays"/>，
/// 参数值不合法直接失败（不按错误的保留期删数据）。七类分表日志按平台与目录里的每个租户逐个作用域、逐批（每批 <see cref="BatchSize"/> 条，一批一个工作单元）删除；
/// 框架的操作审计表不分租户，单独一遍。一个作用域或一类失败只记日志、计入失败，不影响其他。
/// </summary>
/// <param name="configs">参数读取。</param>
/// <param name="options">日志选项。</param>
/// <param name="tenants">租户目录。</param>
/// <param name="scopes">作用域工厂。</param>
/// <param name="clock">时间源。</param>
/// <param name="logger">日志。</param>
public sealed partial class AuditRetentionJob(IConfigValueReader configs, AuditLogOptions options, ITenantStore tenants, IServiceScopeFactory scopes, TimeProvider clock, ILogger<AuditRetentionJob> logger) : IJob
{
    /// <summary>
    /// 作业名。
    /// </summary>
    public const string Name = "auditing.retention";

    /// <summary>
    /// 一批删除的条数。
    /// </summary>
    public const int BatchSize = 1000;

    /// <summary>
    /// 保留天数的上限（十年）。
    /// </summary>
    public const int MaxRetentionDays = 3650;

    private static readonly DateTimeOffset _earliest = new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// 七类分表日志各自的清理。
    /// </summary>
    private static readonly Func<AuditRetentionJob, string, DateTimeOffset, CancellationToken, Task<long>>[] _purges =
    [
        static (job, tenant, cutoff, ct) => job.PurgeAsync<SysAccessLog>(tenant, cutoff, ct),
        static (job, tenant, cutoff, ct) => job.PurgeAsync<SysExceptionLog>(tenant, cutoff, ct),
        static (job, tenant, cutoff, ct) => job.PurgeAsync<SysLoginLog>(tenant, cutoff, ct),
        static (job, tenant, cutoff, ct) => job.PurgeAsync<SysDiffLog>(tenant, cutoff, ct),
        static (job, tenant, cutoff, ct) => job.PurgeAsync<SysPermissionChangeLog>(tenant, cutoff, ct),
        static (job, tenant, cutoff, ct) => job.PurgeAsync<SysConditionHit>(tenant, cutoff, ct),
        static (job, tenant, cutoff, ct) => job.PurgeAsync<SysOpenApiLog>(tenant, cutoff, ct),
    ];

    /// <inheritdoc />
    public async ValueTask<Result> RunAsync(JobRun run, InvocationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(context);
        var days = await RetentionDaysAsync(context).ConfigureAwait(false);
        if (!days.TryGetValue(out var retention))
        {
            return days.Error!;
        }

        var cutoff = clock.GetUtcNow().AddDays(-retention);
        var removed = 0L;
        var failures = 0;
        var scopesToClean = new List<string> { TenantIds.Platform };
        scopesToClean.AddRange((await tenants.ListAsync(cancellationToken).ConfigureAwait(false)).Select(static t => t.Id));
        foreach (var tenant in scopesToClean)
        {
            foreach (var purge in _purges)
            {
                try
                {
                    removed += await purge(this, tenant, cutoff, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    failures++;
                    LogPurgeFailed(logger, exception, tenant);
                }
            }
        }

        try
        {
            removed += await PurgeOperationAuditsAsync(cutoff, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            failures++;
            LogPurgeFailed(logger, exception, "operation-audit");
        }

        LogPurged(logger, retention, cutoff, removed, failures);
        return Result.Success();
    }

    /// <summary>
    /// 保留天数：参数没建用缺省值；建了必须是 1 到 <see cref="MaxRetentionDays"/> 的整数。
    /// </summary>
    private async ValueTask<Result<int>> RetentionDaysAsync(InvocationContext context)
    {
        var configured = await configs.GetAsync(AuditLogOptions.RetentionDaysConfigKey, context).ConfigureAwait(false);
        if (!configured.TryGetValue(out var value))
        {
            return configured.Error!;
        }

        if (!value.TryGetValue(out var text))
        {
            return options.DefaultRetentionDays;
        }

        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var days) && days is >= 1 and <= MaxRetentionDays
            ? days
            : Error.Validation(AuditingErrors.RetentionInvalid, AuditingMessages.RetentionInvalid, MessageArguments.Of(("value", text)));
    }

    /// <summary>
    /// 一个作用域里一类日志的清理：逐批取出早于截止时间的行再删，取不满一批即结束。
    /// </summary>
    private async Task<long> PurgeAsync<TEntity>(string tenant, DateTimeOffset cutoff, CancellationToken cancellationToken)
        where TEntity : AuditLogRecord
    {
        var before = cutoff.UtcDateTime;
        var query = EntityQuery.All<TEntity>().Where(new Specification<TEntity>(e => e.OccurredAt < before)).Within(_earliest, cutoff).Page(0, BatchSize);
        var removed = 0L;
        while (true)
        {
            var batch = await InScopeAsync(tenant, async (services, ct) =>
            {
                var repository = services.GetRequiredService<IRepository<TEntity, Guid>>();
                var rows = await repository.ListAsync(query, ct).ConfigureAwait(false);
                if (rows.Count > 0)
                {
                    Fold(await repository.RemoveRangeAsync(rows, ct).ConfigureAwait(false));
                }

                return rows.Count;
            }, cancellationToken).ConfigureAwait(false);
            removed += batch;
            if (batch < BatchSize)
            {
                return removed;
            }
        }
    }

    /// <summary>
    /// 框架操作审计表的清理（不分租户）。
    /// </summary>
    private async Task<long> PurgeOperationAuditsAsync(DateTimeOffset cutoff, CancellationToken cancellationToken)
    {
        var query = EntityQuery.All<AuditRecord>().Where(new Specification<AuditRecord>(r => r.Timestamp < cutoff)).Page(0, BatchSize);
        var removed = 0L;
        while (true)
        {
            var batch = await InScopeAsync(null, async (services, ct) =>
            {
                var repository = services.GetRequiredService<IRepository<AuditRecord, Guid>>();
                var rows = await repository.ListAsync(query, ct).ConfigureAwait(false);
                if (rows.Count > 0)
                {
                    Fold(await repository.RemoveRangeAsync(rows, ct).ConfigureAwait(false));
                }

                return rows.Count;
            }, cancellationToken).ConfigureAwait(false);
            removed += batch;
            if (batch < BatchSize)
            {
                return removed;
            }
        }
    }

    /// <summary>
    /// 在独立作用域、带租户的工作单元里执行一批；成功即提交，抛出即回滚。
    /// </summary>
    private async Task<int> InScopeAsync(string? tenant, Func<IServiceProvider, CancellationToken, Task<int>> work, CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await unitOfWork.BeginAsync(new UnitOfWorkOptions(TenantId: tenant), cancellationToken).ConfigureAwait(false);
        try
        {
            var count = await work(scope.ServiceProvider, cancellationToken).ConfigureAwait(false);
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

    /// <summary>
    /// 逐项结果里有失败即抛出（本批回滚）。
    /// </summary>
    private static void Fold(IReadOnlyList<Result> results)
    {
        foreach (var result in results)
        {
            result.ThrowIfFailure();
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Audit retention ({Days} days, before {Cutoff:O}) removed {Removed} logs with {Failures} failures.")]
    private static partial void LogPurged(ILogger logger, int days, DateTimeOffset cutoff, long removed, int failures);

    [LoggerMessage(Level = LogLevel.Error, Message = "Audit retention failed for scope '{Scope}'.")]
    private static partial void LogPurgeFailed(ILogger logger, Exception exception, string scope);
}
