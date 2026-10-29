// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Domain.Services;
using XiHan.BasicApp.Access.Infrastructure;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Seeding;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.BasicApp.Runtime.Seeding;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;
using XiHan.Framework.Security.Abstractions.Permissions;

namespace XiHan.BasicApp.Access.Seeds;

/// <summary>
/// 平台角色种子。
/// </summary>
/// <remarks>
/// 号段 100 + 模块偏移 10，在平台身份种子之后。
/// 内建平台角色 <c>admin</c>（全局授予 <c>*</c>）并把 <c>superadmin</c> 指派进去。运营数据只首写：已有的角色不改名、不改授予。
/// </remarks>
/// <param name="roles">角色仓储。</param>
/// <param name="members">指派仓储。</param>
/// <param name="service">角色域服务。</param>
/// <param name="grants">角色授予。</param>
/// <param name="accounts">账号域服务（找超级管理员）。</param>
/// <param name="ids">标识生成器。</param>
public sealed class PlatformRoleSeeder(
    IRepository<SysRole, Guid> roles,
    IRepository<SysUserRole, Guid> members,
    RoleService service,
    RoleGrants grants,
    UserAccountService accounts,
    IIdGenerator<Guid> ids) : ISeeder
{
    /// <summary>
    /// 内建平台管理员角色码。
    /// </summary>
    public const string AdminRoleCode = "admin";

    /// <inheritdoc />
    public string Name => "access.platform";

    /// <inheritdoc />
    public int Order => SeedOrders.PlatformIdentity + SeedOrders.ModuleOffset;

    /// <inheritdoc />
    public async ValueTask<Result> SeedAsync(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        SysRole admin;
        if ((await service.FindByCodeAsync(TenantIds.Platform, AdminRoleCode, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var found))
        {
            admin = found;
        }
        else
        {
            admin = new SysRole { Id = ids.NewId(), OwnerTenantId = TenantIds.Platform, Code = AdminRoleCode, Name = "Administrator", Description = "Built-in platform administrator with every permission.", IsBuiltIn = true, DataScope = DataScope.All };
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

        if (!(await accounts.FindByNameAsync(TenantIds.Platform, SeedOptions.SuperAdminUserName, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var superAdmin))
        {
            return Result.Success();
        }

        var userId = superAdmin.Id;
        var roleId = admin.Id;
        var assigned = await members.ListAsync(EntityQuery.All<SysUserRole>().Where(new Specification<SysUserRole>(m => m.UserId == userId && m.RoleId == roleId)).Page(0, 1), context.CancellationToken).ConfigureAwait(false);
        return assigned.Count > 0
            ? Result.Success()
            : await members.AddAsync(new SysUserRole { Id = ids.NewId(), OwnerTenantId = TenantIds.Platform, UserId = userId, RoleId = roleId }, context.CancellationToken).ConfigureAwait(false);
    }
}
