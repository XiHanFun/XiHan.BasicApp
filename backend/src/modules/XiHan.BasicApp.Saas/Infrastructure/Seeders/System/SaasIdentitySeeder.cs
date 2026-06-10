#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasIdentitySeeder
// Guid:15cdbbda-19bc-47f1-9701-51692456d1d0
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/02 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Security.Password;

namespace XiHan.BasicApp.Saas.Infrastructure.Seeders.System;

/// <summary>
/// SaaS 基础身份种子数据
/// </summary>
public sealed class SaasIdentitySeeder(
    ISqlSugarClientResolver clientResolver,
    ILogger<SaasIdentitySeeder> logger,
    IServiceProvider serviceProvider,
    ICurrentTenant currentTenant,
    IPasswordHasher passwordHasher)
    : DataSeederBase(clientResolver, logger, serviceProvider)
{
    private const long DefaultTenantId = 1;
    private const string DefaultTenantCode = "default";
    private const string SuperAdminUserName = "superadmin";
    private const string SuperAdminPassword = "SuperAdmin@123";
    private const string SuperAdminRoleCode = "super_admin";

    private readonly ICurrentTenant _currentTenant = currentTenant;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => 10;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[SaaS]基础身份种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        // 超管为平台账号（TenantId=0）：登录后落控制中心（平台态），可管理租户/用户/系统，
        // 并可经 SwitchTenant 进入任意租户；不建立 SysTenantUser 成员关系。
        _ = await EnsureDefaultTenantAsync();
        var role = await EnsureSuperAdminRoleAsync();
        var user = await EnsureSuperAdminUserAsync();
        await EnsureSuperAdminSecurityAsync(user.BasicId);
        await EnsureSuperAdminUserRoleAsync(user.BasicId, role.BasicId);
        Logger.LogInformation("SaaS 基础身份数据已就绪，默认登录账号 {UserName}", SuperAdminUserName);
    }

    /// <summary>
    /// 应用超级管理员角色字段（全局 System 角色，TenantId=0）。
    /// </summary>
    /// <remarks>
    /// 超管的"全部权限"由运行时特判承载：授权快照 <c>AuthorizationSnapshotQueryService</c> 检测到 super_admin 角色时
    /// 注入通配 "*"，故本角色不落具体 RolePermission 行；版本(Edition)门控亦对 "*" 放行。
    /// 如需改为显式权限绑定，应同时移除快照中的 "*" 特判，避免双轨。
    /// </remarks>
    private static bool ApplySuperAdminRole(SysRole role)
    {
        var changed = false;
        changed |= SetIfChanged(role.TenantId, 0, value => role.TenantId = value);
        changed |= SetIfChanged(role.RoleCode, SuperAdminRoleCode, value => role.RoleCode = value);
        changed |= SetIfChanged(role.RoleName, "超级管理员", value => role.RoleName = value);
        changed |= SetIfChanged(role.RoleDescription, "系统初始化超级管理员角色", value => role.RoleDescription = value);
        changed |= SetIfChanged(role.RoleType, RoleType.System, value => role.RoleType = value);
        changed |= SetIfChanged(role.DataScope, DataPermissionScope.All, value => role.DataScope = value);
        changed |= SetIfChanged(role.MaxMembers, 0, value => role.MaxMembers = value);
        changed |= SetIfChanged(role.Status, EnableStatus.Enabled, value => role.Status = value);
        changed |= SetIfChanged(role.Sort, 1, value => role.Sort = value);
        changed |= SetIfChanged(role.Remark, "系统初始化全局角色", value => role.Remark = value);
        return changed;
    }

    private static bool ApplySuperAdminUser(SysUser user, bool overwriteProfile)
    {
        var changed = false;
        changed |= SetIfChanged(user.TenantId, 0, value => user.TenantId = value);
        changed |= SetIfChanged(user.UserName, SuperAdminUserName, value => user.UserName = value);
        changed |= SetIfChanged(user.Status, EnableStatus.Enabled, value => user.Status = value);
        changed |= SetIfChanged(user.Language, string.IsNullOrWhiteSpace(user.Language) ? "zh-CN" : user.Language, value => user.Language = value);
        changed |= SetIfChanged(user.IsSystemAccount, true, value => user.IsSystemAccount = value);
        changed |= SetIfChanged(user.Remark, "系统初始化超级管理员账号", value => user.Remark = value);

        if (overwriteProfile || string.IsNullOrWhiteSpace(user.RealName))
        {
            changed |= SetIfChanged(user.RealName, "超级管理员", value => user.RealName = value);
        }

        if (overwriteProfile || string.IsNullOrWhiteSpace(user.NickName))
        {
            changed |= SetIfChanged(user.NickName, "超级管理员", value => user.NickName = value);
        }

        if (overwriteProfile || string.IsNullOrWhiteSpace(user.Email))
        {
            changed |= SetIfChanged(user.Email, "superadmin@xihan.fun", value => user.Email = value);
        }

        return changed;
    }

    private static bool ApplySuperAdminUserRole(SysUserRole userRole, long userId, long roleId)
    {
        var changed = false;
        changed |= SetIfChanged(userRole.TenantId, 0, value => userRole.TenantId = value);
        changed |= SetIfChanged(userRole.UserId, userId, value => userRole.UserId = value);
        changed |= SetIfChanged(userRole.RoleId, roleId, value => userRole.RoleId = value);
        changed |= SetIfChanged(userRole.Status, ValidityStatus.Valid, value => userRole.Status = value);
        changed |= SetIfChanged(userRole.GrantReason, "系统初始化超级管理员角色", value => userRole.GrantReason = value);
        changed |= SetIfChanged(userRole.Remark, "系统初始化角色绑定", value => userRole.Remark = value);
        return changed;
    }

    private static void SetBasicId<TEntity>(TEntity entity, long basicId)
        where TEntity : class
    {
        var property = typeof(TEntity).GetProperty(nameof(SysTenant.BasicId));
        var setter = property?.GetSetMethod(true)
            ?? throw new InvalidOperationException($"实体 {typeof(TEntity).Name} 未暴露 BasicId setter。");
        setter.Invoke(entity, [basicId]);
    }

    private static bool SetIfChanged<T>(T current, T next, Action<T> setter)
    {
        if (EqualityComparer<T>.Default.Equals(current, next))
        {
            return false;
        }

        setter(next);
        return true;
    }

    private async Task<long> EnsureDefaultTenantAsync()
    {
        using var platformScope = _currentTenant.Change(null);
        var client = DbClient;
        var existingTenant = await client.Queryable<SysTenant>()
            .FirstAsync(tenant => tenant.TenantCode == DefaultTenantCode)
            ?? await client.Queryable<SysTenant>()
                .FirstAsync(tenant => tenant.BasicId == DefaultTenantId);
        if (existingTenant is not null)
        {
            if (ApplyDefaultTenant(existingTenant))
            {
                _ = await client.Updateable(existingTenant).ExecuteCommandAsync();
            }

            return existingTenant.BasicId;
        }

        var tenant = new SysTenant
        {
            TenantCode = DefaultTenantCode
        };
        _ = ApplyDefaultTenant(tenant);
        SetBasicId(tenant, DefaultTenantId);
        _ = await client.Insertable(tenant).ExecuteReturnEntityAsync();
        Logger.LogInformation("成功初始化默认租户，TenantId={TenantId}", DefaultTenantId);
        return DefaultTenantId;
    }

    private async Task<SysRole> EnsureSuperAdminRoleAsync()
    {
        using var platformScope = _currentTenant.Change(null);
        var client = DbClient;
        var existingRole = await client.Queryable<SysRole>()
            .FirstAsync(role => role.TenantId == 0 && role.RoleCode == SuperAdminRoleCode);
        if (existingRole is not null)
        {
            if (ApplySuperAdminRole(existingRole))
            {
                _ = await client.Updateable(existingRole).ExecuteCommandAsync();
            }

            return existingRole;
        }

        var role = new SysRole
        {
            RoleCode = SuperAdminRoleCode
        };
        _ = ApplySuperAdminRole(role);

        var savedRole = await client.Insertable(role).ExecuteReturnEntityAsync();
        Logger.LogInformation("成功初始化超级管理员角色，RoleId={RoleId}", savedRole.BasicId);
        return savedRole;
    }

    private async Task<SysUser> EnsureSuperAdminUserAsync()
    {
        using var platformScope = _currentTenant.Change(null);
        var client = DbClient;
        var existingUser = await client.Queryable<SysUser>()
            .FirstAsync(user => user.TenantId == 0 && user.UserName == SuperAdminUserName);
        if (existingUser is not null)
        {
            if (ApplySuperAdminUser(existingUser, overwriteProfile: false))
            {
                _ = await client.Updateable(existingUser).ExecuteCommandAsync();
            }

            return existingUser;
        }

        var user = new SysUser
        {
            UserName = SuperAdminUserName
        };
        _ = ApplySuperAdminUser(user, overwriteProfile: true);

        var savedUser = await client.Insertable(user).ExecuteReturnEntityAsync();
        Logger.LogInformation("成功初始化超级管理员账号，UserId={UserId}", savedUser.BasicId);
        return savedUser;
    }

    private async Task EnsureSuperAdminSecurityAsync(long userId)
    {
        using var platformScope = _currentTenant.Change(null);
        var client = DbClient;
        var exists = await client.Queryable<SysUserSecurity>()
            .FirstAsync(security => security.UserId == userId);
        if (exists is not null)
        {
            if (ApplySuperAdminSecurity(exists, userId, resetPassword: false))
            {
                _ = await client.Updateable(exists).ExecuteCommandAsync();
            }

            return;
        }

        var security = new SysUserSecurity
        {
            SecurityStamp = Guid.NewGuid().ToString("N")
        };
        _ = ApplySuperAdminSecurity(security, userId, resetPassword: true);

        _ = await client.Insertable(security).ExecuteReturnEntityAsync();
    }

    private async Task EnsureSuperAdminUserRoleAsync(long userId, long roleId)
    {
        using var platformScope = _currentTenant.Change(null);
        var client = DbClient;
        var exists = await client.Queryable<SysUserRole>()
            .FirstAsync(userRole => userRole.TenantId == 0 && userRole.UserId == userId && userRole.RoleId == roleId);
        if (exists is not null)
        {
            if (ApplySuperAdminUserRole(exists, userId, roleId))
            {
                _ = await client.Updateable(exists).ExecuteCommandAsync();
            }

            return;
        }

        var userRole = new SysUserRole
        {
            UserId = userId,
            RoleId = roleId
        };
        _ = ApplySuperAdminUserRole(userRole, userId, roleId);

        _ = await client.Insertable(userRole).ExecuteReturnEntityAsync();
    }

    private bool ApplyDefaultTenant(SysTenant tenant)
    {
        var changed = false;
        changed |= SetIfChanged(tenant.TenantId, 0, value => tenant.TenantId = value);
        changed |= SetIfChanged(tenant.TenantCode, DefaultTenantCode, value => tenant.TenantCode = value);
        changed |= SetIfChanged(tenant.TenantName, "默认租户", value => tenant.TenantName = value);
        changed |= SetIfChanged(tenant.TenantShortName, "默认", value => tenant.TenantShortName = value);
        changed |= SetIfChanged(tenant.ConfigStatus, TenantConfigStatus.Configured, value => tenant.ConfigStatus = value);
        changed |= SetIfChanged(tenant.TenantStatus, TenantStatus.Normal, value => tenant.TenantStatus = value);
        changed |= SetIfChanged(tenant.IsolationMode, TenantIsolationMode.Field, value => tenant.IsolationMode = value);
        changed |= SetIfChanged(tenant.Sort, 1, value => tenant.Sort = value);
        changed |= SetIfChanged(tenant.Remark, "系统初始化默认租户", value => tenant.Remark = value);
        return changed;
    }

    private bool ApplySuperAdminSecurity(SysUserSecurity security, long userId, bool resetPassword)
    {
        var now = DateTimeOffset.UtcNow;
        var changed = false;
        changed |= SetIfChanged(security.TenantId, 0, value => security.TenantId = value);
        changed |= SetIfChanged(security.UserId, userId, value => security.UserId = value);
        changed |= SetIfChanged(security.EmailVerified, true, value => security.EmailVerified = value);
        changed |= SetIfChanged(security.AllowMultiLogin, true, value => security.AllowMultiLogin = value);
        changed |= SetIfChanged(security.MaxLoginDevices, 0, value => security.MaxLoginDevices = value);
        changed |= SetIfChanged(security.Remark, "系统初始化超级管理员安全记录", value => security.Remark = value);

        if (resetPassword || string.IsNullOrWhiteSpace(security.Password))
        {
            security.Password = _passwordHasher.HashPassword(SuperAdminPassword);
            security.LastPasswordChangeTime = now;
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(security.SecurityStamp))
        {
            security.SecurityStamp = Guid.NewGuid().ToString("N");
            changed = true;
        }

        if (!security.LastSecurityCheckTime.HasValue)
        {
            security.LastSecurityCheckTime = now;
            changed = true;
        }

        return changed;
    }
}
