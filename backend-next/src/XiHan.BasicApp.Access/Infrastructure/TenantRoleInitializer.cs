// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Domain.Services;
using XiHan.BasicApp.Access.Seeds;
using XiHan.BasicApp.Contracts.MultiTenancy;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;
using XiHan.Framework.Security.Abstractions.Permissions;

namespace XiHan.BasicApp.Access.Infrastructure;

/// <summary>
/// <see cref="ITenantInitializer"/> 的实现，为新租户建内建角色。
/// </summary>
/// <remarks>
/// 为新租户建内建角色 <c>admin</c>（租户侧、<c>*</c>、数据范围全部），并把管理员指派进去。
/// 角色与授予都走租户侧（授予主体 <c>role:&lt;角色 ID&gt;</c>、租户列为该租户），与平台的内建管理员角色同构。幂等：已有即复用。
/// </remarks>
/// <param name="roles">角色仓储。</param>
/// <param name="members">指派仓储。</param>
/// <param name="service">角色域服务。</param>
/// <param name="grants">角色授予。</param>
/// <param name="ids">标识生成器。</param>
public sealed class TenantRoleInitializer(
    IRepository<SysRole, Guid> roles,
    IRepository<SysUserRole, Guid> members,
    RoleService service,
    RoleGrants grants,
    IIdGenerator<Guid> ids) : ITenantInitializer
{
    /// <inheritdoc />
    public async Task<Result> InitializeAsync(string tenantId, Guid adminUserId, InvocationContext context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(context);
        SysRole admin;
        if ((await service.FindByCodeAsync(tenantId, PlatformRoleSeeder.AdminRoleCode, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var found))
        {
            admin = found;
        }
        else
        {
            admin = new SysRole { Id = ids.NewId(), OwnerTenantId = tenantId, Code = PlatformRoleSeeder.AdminRoleCode, Name = "Administrator", Description = "Built-in tenant administrator with every tenant-side permission.", IsBuiltIn = true, DataScope = DataScope.All };
            var added = await roles.AddAsync(admin, context.CancellationToken).ConfigureAwait(false);
            if (added.IsFailure)
            {
                return added;
            }
        }

        var current = await grants.ListAsync(admin, context.CancellationToken).ConfigureAwait(false);
        if (!current.Contains(PermissionGrant.All, StringComparer.Ordinal))
        {
            foreach (var outcome in await grants.GrantAsync(admin, [PermissionGrant.All], context.CancellationToken).ConfigureAwait(false))
            {
                if (outcome.Outcome.IsFailure)
                {
                    return outcome.Outcome;
                }
            }
        }

        var roleId = admin.Id;
        var assigned = await members.ListAsync(EntityQuery.All<SysUserRole>().Where(new Specification<SysUserRole>(m => m.UserId == adminUserId && m.RoleId == roleId)).Page(0, 1), context.CancellationToken).ConfigureAwait(false);
        return assigned.Count > 0
            ? Result.Success()
            : await members.AddAsync(new SysUserRole { Id = ids.NewId(), OwnerTenantId = tenantId, UserId = adminUserId, RoleId = roleId }, context.CancellationToken).ConfigureAwait(false);
    }
}
