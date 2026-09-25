// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Security.Password;

namespace XiHan.BasicApp.Saas.Infrastructure.Seeders;

/// <summary>
/// SaaS 超级管理员：平台的 super_admin 系统角色与 superadmin 账号
/// </summary>
/// <remarks>
/// 超管是平台账号（TenantId = 0），登录落平台，超管身份只在平台成立；它不加入任何租户。
/// 角色是系统角色，定义由这里维护、每次对齐；它的「全部权限」由授权快照整体给出，不写授权行。
/// 账号只在首次创建时写资料与初始密码，之后改过的资料与密码不再覆盖；初始密码由种子写入，标记为需要本人改密
/// （参数「密码设置」开启强制改密后，首次登录即要求修改）。
/// </remarks>
public sealed class SaasSuperAdminSeeder(
    ISqlSugarClientResolver clientResolver,
    ILogger<SaasSuperAdminSeeder> logger,
    IServiceProvider serviceProvider,
    IPasswordHasher passwordHasher)
    : PlatformDataSeederBase(clientResolver, logger, serviceProvider)
{
    /// <summary>
    /// 超管账号
    /// </summary>
    public const string UserName = "superadmin";

    /// <summary>
    /// 超管初始密码（首次登录后请立即修改）
    /// </summary>
    public const string InitialPassword = "SuperAdmin@123";

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => SeedOrders.PlatformIdentity;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[SaaS]超级管理员";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        var role = await EnsureRoleAsync();
        var user = await EnsureUserAsync();
        await EnsureSecurityAsync(user.BasicId);
        await EnsureUserRoleAsync(user.BasicId, role.BasicId);
        Logger.LogInformation("{Seeder}：账号 {UserName} 已就绪", Name, UserName);
    }

    private async Task<SysRole> EnsureRoleAsync()
    {
        var role = await DbClient.Queryable<SysRole>()
            .FirstAsync(item => item.TenantId == 0 && item.RoleCode == SaasRoleCodes.SuperAdmin);
        if (role is null)
        {
            role = new SysRole { TenantId = 0, RoleCode = SaasRoleCodes.SuperAdmin };
            _ = ApplyRole(role);
            return await DbClient.Insertable(role).ExecuteReturnEntityAsync();
        }

        if (ApplyRole(role))
        {
            _ = await DbClient.Updateable(role).ExecuteCommandAsync();
        }

        return role;
    }

    private async Task<SysUser> EnsureUserAsync()
    {
        var user = await DbClient.Queryable<SysUser>()
            .FirstAsync(item => item.TenantId == 0 && item.UserName == UserName);
        if (user is null)
        {
            user = new SysUser
            {
                TenantId = 0,
                UserName = UserName,
                RealName = "超级管理员",
                NickName = "超级管理员",
                Email = "superadmin@xihan.fun",
                IsActive = true,
                Remark = "系统初始化超级管理员账号"
            };
            _ = ApplyUser(user);
            return await DbClient.Insertable(user).ExecuteReturnEntityAsync();
        }

        if (ApplyUser(user))
        {
            _ = await DbClient.Updateable(user).ExecuteCommandAsync();
        }

        return user;
    }

    private async Task EnsureSecurityAsync(long userId)
    {
        if (await DbClient.Queryable<SysUserSecurity>().AnyAsync(security => security.UserId == userId))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        _ = await DbClient.Insertable(new SysUserSecurity
        {
            TenantId = 0,
            UserId = userId,
            Password = passwordHasher.HashPassword(InitialPassword),
            PasswordChangeRequired = true,
            LastPasswordChangeTime = now,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            EmailVerified = true,
            AllowMultiLogin = true,
            MaxLoginDevices = 0,
            LastSecurityCheckTime = now,
            Remark = "系统初始化超级管理员安全记录"
        }).ExecuteCommandAsync();
    }

    private async Task EnsureUserRoleAsync(long userId, long roleId)
    {
        var binding = await DbClient.Queryable<SysUserRole>()
            .FirstAsync(item => item.TenantId == 0 && item.UserId == userId && item.RoleId == roleId);
        if (binding is null)
        {
            _ = await DbClient.Insertable(new SysUserRole
            {
                TenantId = 0,
                UserId = userId,
                RoleId = roleId,
                Status = ValidityStatus.Valid,
                GrantReason = "系统初始化超级管理员角色",
                Remark = "系统初始化角色绑定"
            }).ExecuteCommandAsync();
            return;
        }

        if (SeedValues.SetIfChanged(binding.Status, ValidityStatus.Valid, value => binding.Status = value))
        {
            _ = await DbClient.Updateable(binding).ExecuteCommandAsync();
        }
    }

    /// <summary>
    /// 超管角色的定义：系统角色、全部数据、不限成员数
    /// </summary>
    private static bool ApplyRole(SysRole role)
    {
        var changed = false;
        changed |= SeedValues.SetIfChanged(role.RoleName, "超级管理员", value => role.RoleName = value);
        changed |= SeedValues.SetIfChanged(role.RoleDescription, "平台最高权限，只在平台生效", value => role.RoleDescription = value);
        changed |= SeedValues.SetIfChanged(role.RoleType, RoleType.System, value => role.RoleType = value);
        changed |= SeedValues.SetIfChanged(role.DataScope, DataPermissionScope.All, value => role.DataScope = value);
        changed |= SeedValues.SetIfChanged(role.MaxMembers, 0, value => role.MaxMembers = value);
        changed |= SeedValues.SetIfChanged(role.Status, EnableStatus.Enabled, value => role.Status = value);
        changed |= SeedValues.SetIfChanged(role.Sort, 1, value => role.Sort = value);
        changed |= SeedValues.SetIfChanged(role.Remark, "系统初始化超级管理员角色", value => role.Remark = value);
        return changed;
    }

    /// <summary>
    /// 超管账号的系统属性：平台系统账号、启用（资料只在创建时写）
    /// </summary>
    private static bool ApplyUser(SysUser user)
    {
        var changed = false;
        changed |= SeedValues.SetIfChanged(user.IsSystemAccount, true, value => user.IsSystemAccount = value);
        changed |= SeedValues.SetIfChanged(user.Status, EnableStatus.Enabled, value => user.Status = value);
        return changed;
    }
}
