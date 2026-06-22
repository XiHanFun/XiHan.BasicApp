#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasSampleIdentitySeeder
// Guid:7c1e9a64-3d52-4f08-9b21-5e8c2a47d0f9
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/15 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using SqlSugar;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Security.Password;

namespace XiHan.BasicApp.Saas.Infrastructure.Seeders.System;

/// <summary>
/// SaaS 演示身份种子数据（默认租户内的示例角色与账号）
/// </summary>
/// <remarks>
/// 在权限/菜单/组织种子之后运行（Order=35），为默认租户（TenantId=1）补齐一批演示角色与账号：
/// - 角色权限走显式 <see cref="SysRolePermission"/> 绑定（超管才是运行时通配 *）；运行时再受租户版本(Enterprise)白名单门控。
/// - 系统管理员授「全部权限减去平台专属」，与版本白名单口径一致，故授予的权限全部生效。
/// - 账号为默认租户成员（建 <see cref="SysTenantUser"/> 关系），登录后落租户工作台；admin/user/guest 供登录页快捷登录。
/// 幂等：按唯一键存在则跳过/更新，不覆盖已存在账号的密码。
/// </remarks>
public sealed class SaasSampleIdentitySeeder(
    ISqlSugarClientResolver clientResolver,
    ILogger<SaasSampleIdentitySeeder> logger,
    IServiceProvider serviceProvider,
    ICurrentTenant currentTenant,
    IPasswordHasher passwordHasher)
    : DataSeederBase(clientResolver, logger, serviceProvider)
{
    private const string DefaultTenantCode = "default";

    /// <summary>
    /// 平台专属权限（口径对齐 SaasTenantEditionSeeder 的 Enterprise 白名单排除项）：系统管理员授「全部权限减去这些」。
    /// </summary>
    private static readonly HashSet<string> PlatformOnlyCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        SaasPermissionCodes.Tenant.Create,
        SaasPermissionCodes.Tenant.Update,
        SaasPermissionCodes.Tenant.Status,
        SaasPermissionCodes.TenantEdition.Read,
        SaasPermissionCodes.TenantEdition.Create,
        SaasPermissionCodes.TenantEdition.Update,
        SaasPermissionCodes.TenantEdition.Status,
        SaasPermissionCodes.TenantEdition.Default,
        SaasPermissionCodes.TenantEditionPermission.Read,
        SaasPermissionCodes.TenantEditionPermission.Grant,
        SaasPermissionCodes.TenantEditionPermission.Update,
        SaasPermissionCodes.TenantEditionPermission.Revoke,
        SaasPermissionCodes.Resource.Create,
        SaasPermissionCodes.Resource.Update,
        SaasPermissionCodes.Resource.Status,
        SaasPermissionCodes.Resource.Delete,
        SaasPermissionCodes.Operation.Create,
        SaasPermissionCodes.Operation.Update,
        SaasPermissionCodes.Operation.Status,
        SaasPermissionCodes.Operation.Delete,
        SaasPermissionCodes.Menu.Create,
        SaasPermissionCodes.Menu.Update,
        SaasPermissionCodes.Menu.Status,
        SaasPermissionCodes.Menu.Delete,
        SaasPermissionCodes.Cache.Read,
        SaasPermissionCodes.Cache.Clear,
        SaasPermissionCodes.Server.Read,
    };

    /// <summary>
    /// 开发工具权限（代码生成）：平台级开发功能，仅超级管理员可拥有，租户管理员的"全部权限"亦排除之。
    /// </summary>
    private static bool IsDevelopmentToolCode(string code)
    {
        return code.StartsWith("code_gen:", StringComparison.OrdinalIgnoreCase)
            || code.StartsWith("code_gen_api:", StringComparison.OrdinalIgnoreCase);
    }

    private static readonly IReadOnlyList<RoleSeed> RoleSeeds =
    [
        new("tenant_admin", "系统管理员", "租户内最高权限：管理用户/角色/部门/业务/日志等（平台/开发专属除外）",
            DataPermissionScope.All, 10,
            allCodes => allCodes.Where(code => !PlatformOnlyCodes.Contains(code) && !IsDevelopmentToolCode(code))),
        new("normal_user", "普通用户", "普通成员：工作台 + 消息阅读",
            DataPermissionScope.SelfOnly, 20,
            _ =>
            [
                SaasPermissionCodes.UserStatistics.Read,
                SaasPermissionCodes.Notification.Read,
                SaasPermissionCodes.Message.Read,
            ]),
        new("guest", "游客", "只读访客：仅工作台仪表盘",
            DataPermissionScope.SelfOnly, 30,
            _ => [SaasPermissionCodes.UserStatistics.Read]),
        new("auditor", "审计员", "审计与日志查看：全量日志 + 审批查看",
            DataPermissionScope.All, 40,
            _ =>
            [
                SaasPermissionCodes.UserStatistics.Read,
                SaasPermissionCodes.AccessLog.Read,
                SaasPermissionCodes.ApiLog.Read,
                SaasPermissionCodes.OperationLog.Read,
                SaasPermissionCodes.LoginLog.Read,
                SaasPermissionCodes.ExceptionLog.Read,
                SaasPermissionCodes.DiffLog.Read,
                SaasPermissionCodes.PermissionChangeLog.Read,
                SaasPermissionCodes.ReviewLog.Read,
                SaasPermissionCodes.TaskLog.Read,
                SaasPermissionCodes.Review.Read,
            ]),
        new("operator", "运营专员", "内容运营：消息/通知/模板/文件/审批",
            DataPermissionScope.All, 50,
            _ =>
            [
                SaasPermissionCodes.UserStatistics.Read,
                SaasPermissionCodes.Message.Read,
                SaasPermissionCodes.Message.Create,
                SaasPermissionCodes.Message.Update,
                SaasPermissionCodes.Message.Status,
                SaasPermissionCodes.Message.Publish,
                SaasPermissionCodes.Message.Delete,
                SaasPermissionCodes.Notification.Read,
                SaasPermissionCodes.Notification.Create,
                SaasPermissionCodes.Notification.Update,
                SaasPermissionCodes.Notification.Publish,
                SaasPermissionCodes.Notification.Delete,
                SaasPermissionCodes.MessageTemplate.Read,
                SaasPermissionCodes.MessageTemplate.Create,
                SaasPermissionCodes.MessageTemplate.Update,
                SaasPermissionCodes.MessageTemplate.Status,
                SaasPermissionCodes.MessageTemplate.Delete,
                SaasPermissionCodes.File.Read,
                SaasPermissionCodes.File.Create,
                SaasPermissionCodes.File.Update,
                SaasPermissionCodes.File.Status,
                SaasPermissionCodes.File.Delete,
                SaasPermissionCodes.Review.Read,
                SaasPermissionCodes.Review.Create,
                SaasPermissionCodes.Review.Update,
            ]),
    ];

    private static readonly IReadOnlyList<UserSeed> UserSeeds =
    [
        new("admin", "系统管理员", "Admin", "admin@xihan.fun", "Admin@123", "tenant_admin", TenantMemberType.Admin, "tech"),
        new("user", "普通用户", "User", "user@xihan.fun", "User@123", "normal_user", TenantMemberType.Member, "product"),
        new("guest", "游客", "Guest", "guest@xihan.fun", "Guest@123", "guest", TenantMemberType.Guest, "marketing"),
        new("auditor", "审计员", "Auditor", "auditor@xihan.fun", "Auditor@123", "auditor", TenantMemberType.Member, "finance"),
        new("operator", "运营专员", "Operator", "operator@xihan.fun", "Operator@123", "operator", TenantMemberType.Member, "ops"),
        new("zhangsan", "张三", "张三", "zhangsan@xihan.fun", "Demo@123", "normal_user", TenantMemberType.Member, "tech"),
        new("lisi", "李四", "李四", "lisi@xihan.fun", "Demo@123", "normal_user", TenantMemberType.Member, "product"),
        new("wangwu", "王五", "王五", "wangwu@xihan.fun", "Demo@123", "normal_user", TenantMemberType.Member, "hr"),
    ];

    private readonly ICurrentTenant _currentTenant = currentTenant;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;

    /// <summary>
    /// 种子数据优先级（在权限 20 / 版本 21 / 菜单 25 / 组织 30 之后）
    /// </summary>
    public override int Order => 35;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[SaaS]演示身份种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        long tenantId;
        Dictionary<string, long> permissionIdByCode;

        // 平台态解析默认租户 + 全局权限码→ID 映射（权限均为 TenantId=0 全局定义）
        {
            using var platformScope = _currentTenant.Change(null);
            var platformClient = DbClient;
            var tenant = await platformClient.Queryable<SysTenant>()
                .FirstAsync(item => item.TenantCode == DefaultTenantCode);
            if (tenant is null)
            {
                Logger.LogWarning("未找到默认租户，跳过演示身份种子数据");
                return;
            }

            tenantId = tenant.BasicId;
            var permissions = await platformClient.Queryable<SysPermission>()
                .Where(permission => permission.TenantId == 0 && permission.Status == EnableStatus.Enabled)
                .ToListAsync();
            permissionIdByCode = permissions
                .GroupBy(permission => permission.PermissionCode, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First().BasicId, StringComparer.OrdinalIgnoreCase);
        }

        if (permissionIdByCode.Count == 0)
        {
            Logger.LogWarning("平台全局权限不存在，跳过演示身份种子数据");
            return;
        }

        var allCodes = permissionIdByCode.Keys.ToArray();

        // 租户态写入角色 / 角色权限 / 账号 / 安全 / 成员关系 / 用户角色
        using var tenantScope = _currentTenant.Change(tenantId, tenantId.ToString());
        var client = DbClient;

        var roleIdByCode = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        var roleCreated = 0;
        foreach (var roleSeed in RoleSeeds)
        {
            var (roleId, created) = await EnsureRoleAsync(client, tenantId, roleSeed);
            roleIdByCode[roleSeed.Code] = roleId;
            roleCreated += created ? 1 : 0;

            var permissionIds = roleSeed.ResolveCodes(allCodes)
                .Select(code => permissionIdByCode.TryGetValue(code, out var id) ? id : 0)
                .Where(id => id > 0)
                .Distinct()
                .ToList();
            await BindRolePermissionsAsync(client, tenantId, roleId, permissionIds);
        }

        // 部门编码→ID（组织种子 Order 30 已建默认租户部门，在本种子之前）
        var departmentIdByCode = (await client.Queryable<SysDepartment>()
                .Where(department => department.TenantId == tenantId && !department.IsDeleted)
                .ToListAsync())
            .GroupBy(department => department.DepartmentCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().BasicId, StringComparer.OrdinalIgnoreCase);

        var userCreated = 0;
        foreach (var userSeed in UserSeeds)
        {
            var (userId, created) = await EnsureUserAsync(client, tenantId, userSeed);
            userCreated += created ? 1 : 0;
            await EnsureUserSecurityAsync(client, tenantId, userId, userSeed.Password);
            await EnsureMembershipAsync(client, tenantId, userId, userSeed.MemberType);
            if (roleIdByCode.TryGetValue(userSeed.RoleCode, out var roleId))
            {
                await EnsureUserRoleAsync(client, tenantId, userId, roleId);
            }

            if (departmentIdByCode.TryGetValue(userSeed.DepartmentCode, out var departmentId))
            {
                await EnsureUserDepartmentAsync(client, tenantId, userId, departmentId);
            }
        }

        Logger.LogInformation(
            "演示身份种子已就绪（角色 {RoleCount} 个本次新增 {RoleCreated}，账号 {UserCount} 个本次新增 {UserCreated}）",
            RoleSeeds.Count, roleCreated, UserSeeds.Count, userCreated);
    }

    private static async Task<(long RoleId, bool Created)> EnsureRoleAsync(ISqlSugarClient client, long tenantId, RoleSeed seed)
    {
        var existing = await client.Queryable<SysRole>()
            .FirstAsync(role => role.TenantId == tenantId && role.RoleCode == seed.Code);
        if (existing is not null)
        {
            return (existing.BasicId, false);
        }

        var role = new SysRole
        {
            TenantId = tenantId,
            RoleCode = seed.Code,
            RoleName = seed.Name,
            RoleDescription = seed.Description,
            RoleType = RoleType.Business,
            DataScope = seed.DataScope,
            MaxMembers = 0,
            Status = EnableStatus.Enabled,
            Sort = seed.Sort,
            Remark = "系统初始化演示角色",
        };
        var saved = await client.Insertable(role).ExecuteReturnEntityAsync();
        return (saved.BasicId, true);
    }

    private static async Task BindRolePermissionsAsync(ISqlSugarClient client, long tenantId, long roleId, IReadOnlyCollection<long> permissionIds)
    {
        if (permissionIds.Count == 0)
        {
            return;
        }

        var existingIds = (await client.Queryable<SysRolePermission>()
                .Where(binding => binding.TenantId == tenantId && binding.RoleId == roleId)
                .ToListAsync())
            .Select(binding => binding.PermissionId)
            .ToHashSet();

        var addList = permissionIds
            .Where(permissionId => !existingIds.Contains(permissionId))
            .Select(permissionId => new SysRolePermission
            {
                TenantId = tenantId,
                RoleId = roleId,
                PermissionId = permissionId,
                PermissionAction = PermissionAction.Grant,
                Status = ValidityStatus.Valid,
                GrantReason = "系统初始化演示角色权限",
                Remark = "系统初始化演示角色权限",
            })
            .ToList();

        if (addList.Count > 0)
        {
            _ = await client.Insertable(addList).ExecuteCommandAsync();
        }
    }

    private static async Task<(long UserId, bool Created)> EnsureUserAsync(ISqlSugarClient client, long tenantId, UserSeed seed)
    {
        var existing = await client.Queryable<SysUser>()
            .FirstAsync(user => user.TenantId == tenantId && user.UserName == seed.UserName);
        if (existing is not null)
        {
            return (existing.BasicId, false);
        }

        var user = new SysUser
        {
            TenantId = tenantId,
            UserName = seed.UserName,
            RealName = seed.RealName,
            NickName = seed.NickName,
            Email = seed.Email,
            Status = EnableStatus.Enabled,
            IsActive = true,
            Language = "zh-CN",
            IsSystemAccount = false,
            Remark = "系统初始化演示账号",
        };
        var saved = await client.Insertable(user).ExecuteReturnEntityAsync();
        return (saved.BasicId, true);
    }

    private static async Task EnsureMembershipAsync(ISqlSugarClient client, long tenantId, long userId, TenantMemberType memberType)
    {
        var exists = await client.Queryable<SysTenantUser>()
            .FirstAsync(member => member.TenantId == tenantId && member.UserId == userId);
        if (exists is not null)
        {
            return;
        }

        var member = new SysTenantUser
        {
            TenantId = tenantId,
            UserId = userId,
            MemberType = memberType,
            InviteStatus = TenantMemberInviteStatus.Accepted,
            RespondedTime = DateTimeOffset.UtcNow,
            Status = ValidityStatus.Valid,
            Remark = "系统初始化演示成员关系",
        };
        _ = await client.Insertable(member).ExecuteReturnEntityAsync();
    }

    private static async Task EnsureUserRoleAsync(ISqlSugarClient client, long tenantId, long userId, long roleId)
    {
        var exists = await client.Queryable<SysUserRole>()
            .FirstAsync(userRole => userRole.TenantId == tenantId && userRole.UserId == userId && userRole.RoleId == roleId);
        if (exists is not null)
        {
            return;
        }

        var userRole = new SysUserRole
        {
            TenantId = tenantId,
            UserId = userId,
            RoleId = roleId,
            Status = ValidityStatus.Valid,
            GrantReason = "系统初始化演示角色绑定",
            Remark = "系统初始化演示角色绑定",
        };
        _ = await client.Insertable(userRole).ExecuteReturnEntityAsync();
    }

    private static async Task EnsureUserDepartmentAsync(ISqlSugarClient client, long tenantId, long userId, long departmentId)
    {
        var exists = await client.Queryable<SysUserDepartment>()
            .FirstAsync(relation => relation.TenantId == tenantId && relation.UserId == userId && relation.DepartmentId == departmentId);
        if (exists is not null)
        {
            return;
        }

        var relation = new SysUserDepartment
        {
            TenantId = tenantId,
            UserId = userId,
            DepartmentId = departmentId,
            IsMain = true,
            Status = ValidityStatus.Valid,
            Remark = "系统初始化演示用户部门归属",
        };
        _ = await client.Insertable(relation).ExecuteReturnEntityAsync();
    }

    private async Task EnsureUserSecurityAsync(ISqlSugarClient client, long tenantId, long userId, string password)
    {
        var exists = await client.Queryable<SysUserSecurity>()
            .FirstAsync(security => security.UserId == userId);
        if (exists is not null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var security = new SysUserSecurity
        {
            TenantId = tenantId,
            UserId = userId,
            Password = _passwordHasher.HashPassword(password),
            SecurityStamp = Guid.NewGuid().ToString("N"),
            EmailVerified = true,
            AllowMultiLogin = true,
            MaxLoginDevices = 0,
            LastPasswordChangeTime = now,
            LastSecurityCheckTime = now,
            Remark = "系统初始化演示账号安全记录",
        };
        _ = await client.Insertable(security).ExecuteReturnEntityAsync();
    }

    /// <summary>
    /// 角色种子定义
    /// </summary>
    private sealed record RoleSeed(
        string Code,
        string Name,
        string Description,
        DataPermissionScope DataScope,
        int Sort,
        Func<IReadOnlyCollection<string>, IEnumerable<string>> ResolveCodes);

    /// <summary>
    /// 账号种子定义
    /// </summary>
    private sealed record UserSeed(
        string UserName,
        string RealName,
        string NickName,
        string Email,
        string Password,
        string RoleCode,
        TenantMemberType MemberType,
        string DepartmentCode);
}
