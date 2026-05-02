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
using XiHan.Framework.Authentication.Password;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Infrastructure.Seeders;

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
        var tenantId = await EnsureDefaultTenantAsync();
        var role = await EnsureSuperAdminRoleAsync();
        var user = await EnsureSuperAdminUserAsync(tenantId);
        await EnsureSuperAdminSecurityAsync(tenantId, user.BasicId);
        await EnsureSuperAdminMembershipAsync(tenantId, user.BasicId);
        await EnsureSuperAdminUserRoleAsync(tenantId, user.BasicId, role.BasicId);
        Logger.LogInformation("SaaS 基础身份数据已就绪，默认登录账号 {UserName}", SuperAdminUserName);
    }

    private async Task<long> EnsureDefaultTenantAsync()
    {
        using var platformScope = _currentTenant.Change(null);
        var client = DbClient;
        var existingTenant = await client.Queryable<SysTenant>()
            .FirstAsync(tenant => tenant.BasicId == DefaultTenantId);
        if (existingTenant is not null)
        {
            return existingTenant.BasicId;
        }

        var defaultCodeExists = await client.Queryable<SysTenant>()
            .AnyAsync(tenant => tenant.TenantCode == DefaultTenantCode);
        var tenant = new SysTenant
        {
            TenantId = 0,
            TenantCode = defaultCodeExists ? $"{DefaultTenantCode}-{DefaultTenantId}" : DefaultTenantCode,
            TenantName = "默认租户",
            TenantShortName = "默认",
            ConfigStatus = TenantConfigStatus.Configured,
            TenantStatus = TenantStatus.Normal,
            IsolationMode = TenantIsolationMode.Field,
            Sort = 1,
            Remark = "系统初始化默认租户"
        };
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
            return existingRole;
        }

        var role = new SysRole
        {
            TenantId = 0,
            RoleCode = SuperAdminRoleCode,
            RoleName = "超级管理员",
            RoleDescription = "系统初始化超级管理员角色",
            RoleType = RoleType.System,
            IsGlobal = true,
            DataScope = DataPermissionScope.All,
            MaxMembers = 0,
            Status = EnableStatus.Enabled,
            Sort = 1,
            Remark = "系统初始化全局角色"
        };

        var savedRole = await client.Insertable(role).ExecuteReturnEntityAsync();
        Logger.LogInformation("成功初始化超级管理员角色，RoleId={RoleId}", savedRole.BasicId);
        return savedRole;
    }

    private async Task<SysUser> EnsureSuperAdminUserAsync(long tenantId)
    {
        using var tenantScope = _currentTenant.Change(tenantId, tenantId.ToString());
        var client = DbClient;
        var existingUser = await client.Queryable<SysUser>()
            .FirstAsync(user => user.TenantId == tenantId && user.UserName == SuperAdminUserName);
        if (existingUser is not null)
        {
            return existingUser;
        }

        var user = new SysUser
        {
            TenantId = tenantId,
            UserName = SuperAdminUserName,
            RealName = "超级管理员",
            NickName = "超级管理员",
            Email = "superadmin@xihan.fun",
            Status = EnableStatus.Enabled,
            Language = "zh-CN",
            IsSystemAccount = true,
            Remark = "系统初始化超级管理员账号"
        };

        var savedUser = await client.Insertable(user).ExecuteReturnEntityAsync();
        Logger.LogInformation("成功初始化超级管理员账号，UserId={UserId}", savedUser.BasicId);
        return savedUser;
    }

    private async Task EnsureSuperAdminSecurityAsync(long tenantId, long userId)
    {
        using var tenantScope = _currentTenant.Change(tenantId, tenantId.ToString());
        var client = DbClient;
        var exists = await client.Queryable<SysUserSecurity>()
            .AnyAsync(security => security.UserId == userId);
        if (exists)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var security = new SysUserSecurity
        {
            TenantId = tenantId,
            UserId = userId,
            Password = _passwordHasher.HashPassword(SuperAdminPassword),
            LastPasswordChangeTime = now,
            FailedLoginAttempts = 0,
            IsLocked = false,
            TwoFactorEnabled = false,
            TwoFactorMethod = TwoFactorMethod.None,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            EmailVerified = true,
            PhoneVerified = false,
            AllowMultiLogin = true,
            MaxLoginDevices = 0,
            LastSecurityCheckTime = now,
            Remark = "系统初始化超级管理员安全记录"
        };

        _ = await client.Insertable(security).ExecuteReturnEntityAsync();
    }

    private async Task EnsureSuperAdminMembershipAsync(long tenantId, long userId)
    {
        using var tenantScope = _currentTenant.Change(tenantId, tenantId.ToString());
        var client = DbClient;
        var exists = await client.Queryable<SysTenantUser>()
            .AnyAsync(member => member.TenantId == tenantId && member.UserId == userId);
        if (exists)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var member = new SysTenantUser
        {
            TenantId = tenantId,
            UserId = userId,
            MemberType = TenantMemberType.Owner,
            InviteStatus = TenantMemberInviteStatus.Accepted,
            InvitedTime = now,
            RespondedTime = now,
            LastActiveTime = now,
            DisplayName = "超级管理员",
            Status = ValidityStatus.Valid,
            Remark = "系统初始化默认租户所有者"
        };

        _ = await client.Insertable(member).ExecuteReturnEntityAsync();
    }

    private async Task EnsureSuperAdminUserRoleAsync(long tenantId, long userId, long roleId)
    {
        using var tenantScope = _currentTenant.Change(tenantId, tenantId.ToString());
        var client = DbClient;
        var exists = await client.Queryable<SysUserRole>()
            .AnyAsync(userRole => userRole.TenantId == tenantId && userRole.UserId == userId && userRole.RoleId == roleId);
        if (exists)
        {
            return;
        }

        var userRole = new SysUserRole
        {
            TenantId = tenantId,
            UserId = userId,
            RoleId = roleId,
            Status = ValidityStatus.Valid,
            GrantReason = "系统初始化超级管理员角色",
            Remark = "系统初始化角色绑定"
        };

        _ = await client.Insertable(userRole).ExecuteReturnEntityAsync();
    }

    private static void SetBasicId<TEntity>(TEntity entity, long basicId)
        where TEntity : class
    {
        var property = typeof(TEntity).GetProperty(nameof(SysTenant.BasicId));
        var setter = property?.GetSetMethod(true)
            ?? throw new InvalidOperationException($"实体 {typeof(TEntity).Name} 未暴露 BasicId setter。");
        setter.Invoke(entity, [basicId]);
    }
}
