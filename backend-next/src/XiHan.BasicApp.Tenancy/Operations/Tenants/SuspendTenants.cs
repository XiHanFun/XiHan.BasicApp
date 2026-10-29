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
/// 批量停用租户。
/// </summary>
/// <remarks>
/// <c>POST /api/tenants:suspend</c>。目录标记为禁用后，带该租户声明的调用被框架拒绝（<c>MULTITENANCY_DENIED_001</c>）、登录被拒。
/// </remarks>
/// <param name="tenants">租户仓储。</param>
/// <param name="service">租户域服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = TenancyPermissions.TenantsSuspend)]
public sealed class SuspendTenants(IRepository<SysTenant, Guid> tenants, TenantService service) : IOperation<TenantIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "tenants.suspend";

    /// <inheritdoc />
    public ValueTask<Result<BatchResult>> HandleAsync(TenantIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return TenantStatusBatch.RunAsync(request, context, tenants, service, TenantStatus.Suspended);
    }
}
