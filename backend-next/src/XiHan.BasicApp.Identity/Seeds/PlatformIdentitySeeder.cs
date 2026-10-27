// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Seeding;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.BasicApp.Runtime.Seeding;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;
using XiHan.Framework.Security.Abstractions.Permissions;
using XiHan.Framework.Security.Database.Grants;

namespace XiHan.BasicApp.Identity.Seeds;

/// <summary>
/// 平台身份种子（号段 100）：初始超级管理员 <c>superadmin</c>（平台账号，密码来自 <c>BasicApp:Seed:SuperAdminPassword</c>，缺省沿用 4.x）与它的全局授予 <c>*</c>。
/// 运营数据只首写：账号已存在时不改密码；授予缺失时补上。角色与授权快照随 Access 包落地后改为经角色授予。
/// </summary>
/// <param name="users">用户仓储。</param>
/// <param name="accounts">账号域服务。</param>
/// <param name="grants">框架授予存储。</param>
/// <param name="ids">标识生成器。</param>
/// <param name="options">种子配置。</param>
public sealed class PlatformIdentitySeeder(
    IRepository<SysUser, Guid> users,
    UserAccountService accounts,
    PermissionGrantRecordStore grants,
    IIdGenerator<Guid> ids,
    SeedOptions options) : ISeeder
{
    /// <inheritdoc />
    public string Name => "identity.platform";

    /// <inheritdoc />
    public int Order => SeedOrders.PlatformIdentity;

    /// <inheritdoc />
    public async ValueTask<Result> SeedAsync(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var existing = await accounts.FindByNameAsync(TenantIds.Platform, SeedOptions.SuperAdminUserName, context.CancellationToken).ConfigureAwait(false);
        SysUser admin;
        if (existing.TryGetValue(out var found))
        {
            admin = found;
        }
        else
        {
            admin = new SysUser { Id = ids.NewId(), UserName = SeedOptions.SuperAdminUserName, DisplayName = "Super Admin", HomeTenantId = TenantIds.Platform, Status = UserStatus.Active };
            var added = await users.AddAsync(admin, context.CancellationToken).ConfigureAwait(false);
            if (added.IsFailure)
            {
                return added;
            }

            var securityResult = await accounts.SecurityOfAsync(admin.Id, context.CancellationToken).ConfigureAwait(false);
            if (!securityResult.TryGetValue(out var security))
            {
                return securityResult.ToResult();
            }

            var set = await accounts.SetPasswordAsync(security, options.SuperAdminPassword, mustChange: false, context.CancellationToken).ConfigureAwait(false);
            if (set.IsFailure)
            {
                return set;
            }
        }

        // 全局授予 *：平台主体没有租户，授予行的租户列为空串
        var subject = GrantSubject.ForUser(admin.Id.ToString("D"));
        var current = await grants.ListAsync(subject, tenantId: null, skip: 0, take: 10, context.CancellationToken).ConfigureAwait(false);
        if (current.Any(static g => string.Equals(g.Permission, PermissionGrant.All, StringComparison.Ordinal)))
        {
            return Result.Success();
        }

        foreach (var outcome in await grants.GrantAsync([new PermissionGrant(subject, PermissionGrant.All)], context.CancellationToken).ConfigureAwait(false))
        {
            if (outcome.Outcome.IsFailure)
            {
                return outcome.Outcome;
            }
        }

        return Result.Success();
    }
}
