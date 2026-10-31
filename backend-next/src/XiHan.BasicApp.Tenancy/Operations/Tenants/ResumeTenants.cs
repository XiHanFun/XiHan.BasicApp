// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Tenancy.Domain.Entities;
using XiHan.BasicApp.Tenancy.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Tenancy.Operations.Tenants;

/// <summary>
/// 批量恢复租户：<c>POST /api/tenants:resume</c>。
/// </summary>
/// <param name="tenants">租户仓储。</param>
/// <param name="service">租户域服务。</param>
/// <param name="clock">时间源。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = TenancyPermissions.TenantsSuspend)]
public sealed class ResumeTenants(IRepository<SysTenant, Guid> tenants, TenantService service, TimeProvider clock) : IOperation<TenantIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "tenants.resume";

    /// <inheritdoc />
    public ValueTask<Result<BatchResult>> HandleAsync(TenantIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        // 停用或到期的都能恢复，但要没过到期时间；过了的记为逐项失败（先延长到期时间）
        var now = clock.GetUtcNow();
        return TenantStatusBatch.RunAsync(request, context, tenants, service, TenantStatus.Active, tenant => tenant.ExpiresAt is { } expiresAt && expiresAt <= now ? TenancyErrors.TenantExpired : null);
    }
}
