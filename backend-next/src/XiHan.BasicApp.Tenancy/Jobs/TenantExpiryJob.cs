// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Tenancy.Domain.Entities;
using XiHan.BasicApp.Tenancy.Domain.Services;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Persistence.Abstractions.UnitOfWork;
using XiHan.Framework.Primitives;
using XiHan.Framework.Scheduling.Abstractions.Jobs;
using XiHan.Framework.Scheduling.Abstractions.Schedules;

namespace XiHan.BasicApp.Tenancy.Jobs;

/// <summary>
/// 租户到期处理。
/// </summary>
/// <remarks>
/// 内建作业，每 5 分钟一次：把到期时间已过、仍生效的租户标为到期并同步租户目录（停用），带这个租户声明的调用随之被框架拒绝（<c>MULTITENANCY_DENIED_001</c>）。
/// 到期时间一过 Identity 就拒绝登录、刷新与切入（读目录属性），不等这个作业。一次一个平台工作单元，失败整批回滚、作业记失败。
/// 4.x 同（后台服务每分钟扫一次，不吊销会话、不发通知）；恢复要先延长到期时间（<c>tenants.resume</c>，否则 <see cref="TenancyErrors.TenantExpired"/>）。
/// </remarks>
/// <param name="scopes">作用域工厂。</param>
/// <param name="clock">时间源。</param>
/// <param name="logger">日志。</param>
public sealed partial class TenantExpiryJob(IServiceScopeFactory scopes, TimeProvider clock, ILogger<TenantExpiryJob> logger) : IJob
{
    /// <summary>
    /// 作业名。
    /// </summary>
    public const string Name = "tenancy.tenants.expire";

    /// <summary>
    /// 计划：每 5 分钟。
    /// </summary>
    public static JobSchedule Schedule { get; } = JobSchedule.FromCron("*/5 * * * *");

    /// <inheritdoc />
    public async ValueTask<Result> RunAsync(JobRun run, InvocationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(context);
        var now = clock.GetUtcNow();
        await using var scope = scopes.CreateAsyncScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await unitOfWork.BeginAsync(new UnitOfWorkOptions(TenantId: TenantIds.Platform), cancellationToken).ConfigureAwait(false);
        try
        {
            var tenants = scope.ServiceProvider.GetRequiredService<IRepository<SysTenant, Guid>>();
            var service = scope.ServiceProvider.GetRequiredService<TenantService>();
            var due = await tenants.ListAsync(
                EntityQuery.All<SysTenant>().Where(new Specification<SysTenant>(t => t.Status == TenantStatus.Active && t.ExpiresAt.HasValue && t.ExpiresAt <= now)),
                cancellationToken).ConfigureAwait(false);
            foreach (var tenant in due)
            {
                tenant.Status = TenantStatus.Expired;
                var updated = await tenants.UpdateAsync(tenant, cancellationToken).ConfigureAwait(false);
                if (updated.IsFailure)
                {
                    await unitOfWork.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                    return updated.Error!;
                }

                var synced = await service.SyncDirectoryAsync(tenant, cancellationToken).ConfigureAwait(false);
                if (synced.IsFailure)
                {
                    await unitOfWork.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                    return synced.Error!;
                }
            }

            var committed = await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);
            if (committed.IsFailure)
            {
                return committed;
            }

            LogExpired(logger, due.Count);
            return Result.Success();
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

    [LoggerMessage(Level = LogLevel.Information, Message = "Tenant expiry marked {Expired} tenants expired.")]
    private static partial void LogExpired(ILogger logger, int expired);
}
