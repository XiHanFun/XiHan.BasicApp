// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Identity;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Tenancy.Domain.Entities;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Tenancy.Operations.Tenants;

/// <summary>
/// 我能进入的租户：<c>tenants.mine</c>（自助操作，只要求已认证）。注册地租户 + 成员租户，只列正常状态的；平台账号另告知能进入平台。
/// 控制中心据此画卡片，切换走 <c>auth.switch-tenant</c>。
/// </summary>
/// <param name="users">用户仓储。</param>
/// <param name="tenants">租户仓储。</param>
/// <param name="members">成员仓储。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class MyTenants(IRepository<SysUser, Guid> users, IRepository<SysTenant, Guid> tenants, IRepository<SysTenantMember, Guid> members) : IOperation<MyTenantsRequest, MyTenantsResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "tenants.mine";

    /// <inheritdoc />
    public async ValueTask<Result<MyTenantsResponse>> HandleAsync(MyTenantsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var security = context.GetSecurity();
        if (!Guid.TryParse(security.Principal.Id, out var userId) || !(await users.FindAsync(userId, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var user))
        {
            return Error.NotFound(IdentityErrors.UserNotFound, IdentityMessages.UserNotFound, MessageArguments.Of(("id", security.Principal.Id)));
        }

        var current = security.Tenant?.Id;
        var memberships = await members.ListAsync(EntityQuery.All<SysTenantMember>().Where(new Specification<SysTenantMember>(m => m.UserId == userId)), context.CancellationToken).ConfigureAwait(false);
        var wanted = memberships.Select(static m => m.TenantId).ToList();
        if (!string.Equals(user.HomeTenantId, TenantIds.Platform, StringComparison.Ordinal))
        {
            wanted.Add(user.HomeTenantId);
        }

        var items = new List<AvailableTenantResponse>();
        if (wanted.Count > 0)
        {
            var ids = wanted.Distinct().ToList();
            var rows = await tenants.ListAsync(
                EntityQuery.All<SysTenant>().Where(new Specification<SysTenant>(t => ids.Contains(t.TenantId) && t.Status == TenantStatus.Active)).OrderBy(static t => t.Order).OrderBy(static t => t.TenantId),
                context.CancellationToken).ConfigureAwait(false);
            var types = memberships.ToDictionary(static m => m.TenantId, static m => m.MemberType);
            foreach (var tenant in rows)
            {
                var isHome = string.Equals(tenant.TenantId, user.HomeTenantId, StringComparison.Ordinal);
                items.Add(new AvailableTenantResponse(tenant.TenantId, tenant.Name, isHome, isHome ? null : types[tenant.TenantId], string.Equals(tenant.TenantId, current, StringComparison.Ordinal)));
            }
        }

        var canAccessPlatform = string.Equals(user.HomeTenantId, TenantIds.Platform, StringComparison.Ordinal);
        return new MyTenantsResponse(canAccessPlatform, canAccessPlatform && current is null, items);
    }
}
