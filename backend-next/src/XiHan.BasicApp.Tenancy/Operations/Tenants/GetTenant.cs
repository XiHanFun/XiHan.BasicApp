// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Tenancy.Domain.Services;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Tenancy.Operations.Tenants;

/// <summary>
/// 租户详情：<c>GET /api/tenants/{id}</c>，带管理员登录名与用户数。
/// </summary>
/// <param name="service">租户域服务。</param>
/// <param name="users">用户仓储。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = TenancyPermissions.TenantsRead)]
public sealed class GetTenant(TenantService service, IRepository<SysUser, Guid> users) : IOperation<GetTenantRequest, TenantDetailResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "tenants.get";

    /// <inheritdoc />
    public async ValueTask<Result<TenantDetailResponse>> HandleAsync(GetTenantRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (TenantService.RequirePlatform(context) is { } denied)
        {
            return denied;
        }

        if (!(await service.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var tenant))
        {
            return Error.NotFound(TenancyErrors.TenantNotFound, TenancyMessages.TenantNotFound, MessageArguments.Of(("id", request.Id)));
        }

        var tenantId = tenant.TenantId;
        var count = await users.CountAsync(EntityQuery.All<SysUser>().Where(new Specification<SysUser>(u => u.HomeTenantId == tenantId)), context.CancellationToken).ConfigureAwait(false);
        string? adminUserName = null;
        if (tenant.AdminUserId is { } adminId && (await users.FindAsync(adminId, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var admin))
        {
            adminUserName = admin.UserName;
        }

        return new TenantDetailResponse(TenantResponse.From(tenant), adminUserName, count);
    }
}
