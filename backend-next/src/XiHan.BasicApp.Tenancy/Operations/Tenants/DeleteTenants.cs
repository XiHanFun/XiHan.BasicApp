// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Tenancy.Domain.Entities;
using XiHan.BasicApp.Tenancy.Domain.Services;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Tenancy.Operations.Tenants;

/// <summary>
/// 批量删除租户。
/// </summary>
/// <remarks>
/// <c>DELETE /api/tenants?ids=...</c>。还有用户的租户不能删（逐项 <c>TENANCY_CONFLICT_003</c>）；删除即软删业务实体并从租户目录移除。
/// </remarks>
/// <param name="tenants">租户仓储。</param>
/// <param name="users">用户仓储。</param>
/// <param name="service">租户域服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = TenancyPermissions.TenantsDelete)]
public sealed class DeleteTenants(IRepository<SysTenant, Guid> tenants, IRepository<SysUser, Guid> users, TenantService service) : IOperation<TenantIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "tenants.delete";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(TenantIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (TenantService.RequirePlatform(context) is { } denied)
        {
            return denied;
        }

        var found = (await tenants.FindManyAsync(request.Ids, context.CancellationToken).ConfigureAwait(false)).ToDictionary(static t => t.Id);
        var succeeded = new List<Guid>();
        var failed = new List<BatchFailure>();
        foreach (var id in request.Ids.Distinct())
        {
            if (!found.TryGetValue(id, out var tenant))
            {
                failed.Add(new BatchFailure(id, TenancyErrors.TenantNotFound));
                continue;
            }

            var tenantId = tenant.TenantId;
            var count = await users.CountAsync(EntityQuery.All<SysUser>().Where(new Specification<SysUser>(u => u.HomeTenantId == tenantId)), context.CancellationToken).ConfigureAwait(false);
            if (count > 0)
            {
                failed.Add(new BatchFailure(id, TenancyErrors.HasUsers));
                continue;
            }

            var removed = await tenants.RemoveAsync(tenant, context.CancellationToken).ConfigureAwait(false);
            if (removed.IsFailure)
            {
                return removed.Error!;
            }

            var directory = await service.RemoveFromDirectoryAsync(tenantId, context.CancellationToken).ConfigureAwait(false);
            if (directory.IsFailure)
            {
                return directory.Error!;
            }

            succeeded.Add(id);
        }

        return new BatchResult(succeeded, failed);
    }
}
