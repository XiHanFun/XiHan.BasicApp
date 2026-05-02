#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasMenuSeeder
// Guid:8d9bf0f9-1040-46fe-8bb7-e5c889f11f2b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/03 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Infrastructure.Seeders;

/// <summary>
/// SaaS 菜单种子数据
/// </summary>
public sealed class SaasMenuSeeder(
    ISqlSugarClientResolver clientResolver,
    ILogger<SaasMenuSeeder> logger,
    IServiceProvider serviceProvider,
    ICurrentTenant currentTenant)
    : DataSeederBase(clientResolver, logger, serviceProvider)
{
    private readonly ICurrentTenant _currentTenant = currentTenant;

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => 22;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[SaaS]系统菜单种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        using var platformScope = _currentTenant.Change(null);
        var client = DbClient;
        var definitions = BuildDefinitions();
        var permissionCodes = definitions
            .Select(definition => definition.PermissionCode)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var permissions = await client.Queryable<SysPermission>()
            .Where(permission => permissionCodes.Contains(permission.PermissionCode) && permission.Status == EnableStatus.Enabled)
            .ToListAsync();
        var permissionMap = permissions
            .GroupBy(permission => permission.PermissionCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.First().BasicId,
                StringComparer.OrdinalIgnoreCase);

        var menuCodes = definitions.Select(definition => definition.MenuCode).ToArray();
        var existingMenus = await client.Queryable<SysMenu>()
            .Where(menu => menu.TenantId == 0 && menuCodes.Contains(menu.MenuCode))
            .ToListAsync();
        var menuMap = existingMenus.ToDictionary(menu => menu.MenuCode, StringComparer.OrdinalIgnoreCase);

        var addCount = 0;
        var updateCount = 0;
        foreach (var definition in definitions)
        {
            if (!TryResolvePermissionId(definition, permissionMap, out var permissionId))
            {
                Logger.LogWarning("菜单 {MenuCode} 依赖权限 {PermissionCode} 不存在，跳过初始化", definition.MenuCode, definition.PermissionCode);
                continue;
            }

            if (!TryResolveParentId(definition, menuMap, out var parentId))
            {
                Logger.LogWarning("菜单 {MenuCode} 依赖父菜单 {ParentCode} 不存在，跳过初始化", definition.MenuCode, definition.ParentCode);
                continue;
            }

            if (menuMap.TryGetValue(definition.MenuCode, out var existing))
            {
                if (ApplyDefinition(existing, definition, parentId, permissionId))
                {
                    _ = await client.Updateable(existing).ExecuteCommandAsync();
                    updateCount++;
                }

                continue;
            }

            var menu = CreateMenu(definition, parentId, permissionId);
            var savedMenu = await client.Insertable(menu).ExecuteReturnEntityAsync();
            menuMap[definition.MenuCode] = savedMenu;
            addCount++;
        }

        if (addCount == 0 && updateCount == 0)
        {
            Logger.LogInformation("SaaS 菜单数据已存在，跳过种子数据");
            return;
        }

        Logger.LogInformation("成功初始化 SaaS 菜单，新增 {AddCount} 个，更新 {UpdateCount} 个", addCount, updateCount);
    }

    private static bool TryResolvePermissionId(
        MenuSeedDefinition definition,
        IReadOnlyDictionary<string, long> permissionMap,
        out long? permissionId)
    {
        permissionId = null;
        if (string.IsNullOrWhiteSpace(definition.PermissionCode))
        {
            return true;
        }

        if (!permissionMap.TryGetValue(definition.PermissionCode, out var resolvedPermissionId))
        {
            return false;
        }

        permissionId = resolvedPermissionId;
        return true;
    }

    private static bool TryResolveParentId(
        MenuSeedDefinition definition,
        IReadOnlyDictionary<string, SysMenu> menuMap,
        out long? parentId)
    {
        parentId = null;
        if (string.IsNullOrWhiteSpace(definition.ParentCode))
        {
            return true;
        }

        if (!menuMap.TryGetValue(definition.ParentCode, out var parent))
        {
            return false;
        }

        parentId = parent.BasicId;
        return true;
    }

    private static SysMenu CreateMenu(MenuSeedDefinition definition, long? parentId, long? permissionId)
    {
        var menu = new SysMenu
        {
            TenantId = 0,
            IsGlobal = true
        };
        _ = ApplyDefinition(menu, definition, parentId, permissionId);
        return menu;
    }

    private static bool ApplyDefinition(SysMenu menu, MenuSeedDefinition definition, long? parentId, long? permissionId)
    {
        var changed = false;
        changed |= SetIfChanged(menu.TenantId, 0, value => menu.TenantId = value);
        changed |= SetIfChanged(menu.IsGlobal, true, value => menu.IsGlobal = value);
        changed |= SetIfChanged(menu.ParentId, parentId, value => menu.ParentId = value);
        changed |= SetIfChanged(menu.PermissionId, permissionId, value => menu.PermissionId = value);
        changed |= SetIfChanged(menu.MenuName, definition.MenuName, value => menu.MenuName = value);
        changed |= SetIfChanged(menu.MenuCode, definition.MenuCode, value => menu.MenuCode = value);
        changed |= SetIfChanged(menu.MenuType, definition.MenuType, value => menu.MenuType = value);
        changed |= SetIfChanged(menu.Path, definition.Path, value => menu.Path = value);
        changed |= SetIfChanged(menu.Component, definition.Component, value => menu.Component = value);
        changed |= SetIfChanged(menu.RouteName, definition.RouteName, value => menu.RouteName = value);
        changed |= SetIfChanged(menu.Redirect, definition.Redirect, value => menu.Redirect = value);
        changed |= SetIfChanged(menu.Icon, definition.Icon, value => menu.Icon = value);
        changed |= SetIfChanged(menu.Title, definition.Title, value => menu.Title = value);
        changed |= SetIfChanged(menu.IsExternal, false, value => menu.IsExternal = value);
        changed |= SetIfChanged(menu.ExternalUrl, null, value => menu.ExternalUrl = value);
        changed |= SetIfChanged(menu.IsCache, definition.IsCache, value => menu.IsCache = value);
        changed |= SetIfChanged(menu.IsVisible, true, value => menu.IsVisible = value);
        changed |= SetIfChanged(menu.IsAffix, definition.IsAffix, value => menu.IsAffix = value);
        changed |= SetIfChanged(menu.Badge, null, value => menu.Badge = value);
        changed |= SetIfChanged(menu.BadgeType, null, value => menu.BadgeType = value);
        changed |= SetIfChanged(menu.BadgeDot, false, value => menu.BadgeDot = value);
        changed |= SetIfChanged(menu.Metadata, null, value => menu.Metadata = value);
        changed |= SetIfChanged(menu.Status, EnableStatus.Enabled, value => menu.Status = value);
        changed |= SetIfChanged(menu.Sort, definition.Sort, value => menu.Sort = value);
        changed |= SetIfChanged(menu.Remark, "系统初始化全局菜单", value => menu.Remark = value);
        return changed;
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

    private static IReadOnlyList<MenuSeedDefinition> BuildDefinitions()
    {
        return
        [
            new(
                "workbench",
                "工作台",
                MenuType.Directory,
                "/workbench",
                "Workbench",
                null,
                null,
                null,
                "lucide:layout-dashboard",
                "工作台",
                10,
                "/workbench/dashboard"),
            new(
                "workbench.dashboard",
                "仪表盘",
                MenuType.Menu,
                "/workbench/dashboard",
                "WorkbenchDashboard",
                "workbench/dashboard/index",
                "workbench",
                null,
                "lucide:gauge",
                "仪表盘",
                11,
                null,
                false,
                true),
            new(
                "system",
                "系统管理",
                MenuType.Directory,
                "/system",
                "System",
                null,
                null,
                null,
                "lucide:settings",
                "系统管理",
                100,
                "/system/tenant"),
            new(
                "system.tenant",
                "租户管理",
                MenuType.Menu,
                "/system/tenant",
                "SystemTenant",
                "system/tenant/index",
                "system",
                SaasPermissionCodes.Tenant.Read,
                "lucide:building-2",
                "租户管理",
                110,
                null,
                true),
            new(
                "system.tenant-edition",
                "租户版本",
                MenuType.Menu,
                "/system/tenant-edition",
                "SystemTenantEdition",
                "system/tenant-edition/index",
                "system",
                SaasPermissionCodes.TenantEdition.Read,
                "lucide:package",
                "租户版本",
                120,
                null,
                true),
            new(
                "system.tenant-edition-permission",
                "版本权限",
                MenuType.Menu,
                "/system/tenant-edition-permission",
                "SystemTenantEditionPermission",
                "system/tenant-edition-permission/index",
                "system",
                SaasPermissionCodes.TenantEditionPermission.Read,
                "lucide:package-check",
                "版本权限",
                130,
                null,
                true),
            new(
                "system.tenant-member",
                "租户成员",
                MenuType.Menu,
                "/system/tenant-member",
                "SystemTenantMember",
                "system/tenant-member/index",
                "system",
                SaasPermissionCodes.TenantMember.Read,
                "lucide:users",
                "租户成员",
                140,
                null,
                true),
            new(
                "system.permission",
                "权限定义",
                MenuType.Menu,
                "/system/permission",
                "SystemPermission",
                "system/permission/index",
                "system",
                SaasPermissionCodes.Permission.Read,
                "lucide:key-round",
                "权限定义",
                210,
                null,
                true),
            new(
                "system.resource",
                "资源定义",
                MenuType.Menu,
                "/system/resource",
                "SystemResource",
                "system/resource/index",
                "system",
                SaasPermissionCodes.Resource.Read,
                "lucide:box",
                "资源定义",
                220,
                null,
                true),
            new(
                "system.operation",
                "操作定义",
                MenuType.Menu,
                "/system/operation",
                "SystemOperation",
                "system/operation/index",
                "system",
                SaasPermissionCodes.Operation.Read,
                "lucide:mouse-pointer-click",
                "操作定义",
                230,
                null,
                true),
            new(
                "system.role",
                "角色管理",
                MenuType.Menu,
                "/system/role",
                "SystemRole",
                "system/role/index",
                "system",
                SaasPermissionCodes.Role.Read,
                "lucide:shield-user",
                "角色管理",
                240,
                null,
                true),
            new(
                "system.role-hierarchy",
                "角色继承",
                MenuType.Menu,
                "/system/role-hierarchy",
                "SystemRoleHierarchy",
                "system/role-hierarchy/index",
                "system",
                SaasPermissionCodes.RoleHierarchy.Read,
                "lucide:git-branch",
                "角色继承",
                250,
                null,
                true),
            new(
                "system.role-permission",
                "角色权限",
                MenuType.Menu,
                "/system/role-permission",
                "SystemRolePermission",
                "system/role-permission/index",
                "system",
                SaasPermissionCodes.RolePermission.Read,
                "lucide:shield-check",
                "角色权限",
                260,
                null,
                true),
            new(
                "system.role-data-scope",
                "角色数据范围",
                MenuType.Menu,
                "/system/role-data-scope",
                "SystemRoleDataScope",
                "system/role-data-scope/index",
                "system",
                SaasPermissionCodes.RoleDataScope.Read,
                "lucide:database-zap",
                "角色数据范围",
                270,
                null,
                true),
            new(
                "system.user-role",
                "用户角色",
                MenuType.Menu,
                "/system/user-role",
                "SystemUserRole",
                "system/user-role/index",
                "system",
                SaasPermissionCodes.UserRole.Read,
                "lucide:user-check",
                "用户角色",
                280,
                null,
                true),
            new(
                "system.user-permission",
                "用户权限",
                MenuType.Menu,
                "/system/user-permission",
                "SystemUserPermission",
                "system/user-permission/index",
                "system",
                SaasPermissionCodes.UserPermission.Read,
                "lucide:user-cog",
                "用户权限",
                290,
                null,
                true),
            new(
                "system.user-data-scope",
                "用户数据范围",
                MenuType.Menu,
                "/system/user-data-scope",
                "SystemUserDataScope",
                "system/user-data-scope/index",
                "system",
                SaasPermissionCodes.UserDataScope.Read,
                "lucide:database",
                "用户数据范围",
                300,
                null,
                true),
            new(
                "system.user-session",
                "用户会话",
                MenuType.Menu,
                "/system/user-session",
                "SystemUserSession",
                "system/user-session/index",
                "system",
                SaasPermissionCodes.UserSession.Read,
                "lucide:monitor-dot",
                "用户会话",
                310,
                null,
                true),
            new(
                "system.permission-condition",
                "权限条件",
                MenuType.Menu,
                "/system/permission-condition",
                "SystemPermissionCondition",
                "system/permission-condition/index",
                "system",
                SaasPermissionCodes.PermissionCondition.Read,
                "lucide:list-filter",
                "权限条件",
                320,
                null,
                true),
            new(
                "system.constraint-rule",
                "约束规则",
                MenuType.Menu,
                "/system/constraint-rule",
                "SystemConstraintRule",
                "system/constraint-rule/index",
                "system",
                SaasPermissionCodes.ConstraintRule.Read,
                "lucide:workflow",
                "约束规则",
                330,
                null,
                true),
            new(
                "system.field-level-security",
                "字段级安全",
                MenuType.Menu,
                "/system/field-level-security",
                "SystemFieldLevelSecurity",
                "system/field-level-security/index",
                "system",
                SaasPermissionCodes.FieldLevelSecurity.Read,
                "lucide:scan-text",
                "字段级安全",
                340,
                null,
                true),
            new(
                "system.oauth-app",
                "OAuth 应用",
                MenuType.Menu,
                "/system/oauth-app",
                "SystemOAuthApp",
                "system/oauth-app/index",
                "system",
                SaasPermissionCodes.OAuthApp.Read,
                "lucide:badge-check",
                "OAuth 应用",
                410,
                null,
                true),
            new(
                "system.message",
                "系统消息",
                MenuType.Menu,
                "/system/message",
                "SystemMessage",
                "system/message/index",
                "system",
                SaasPermissionCodes.Message.Read,
                "lucide:message-square",
                "系统消息",
                420,
                null,
                true),
            new(
                "system.notification",
                "系统通知",
                MenuType.Menu,
                "/system/notification",
                "SystemNotification",
                "system/notification/index",
                "system",
                SaasPermissionCodes.Message.Read,
                "lucide:bell",
                "系统通知",
                430,
                null,
                true),
            new(
                "system.cache",
                "缓存管理",
                MenuType.Menu,
                "/system/cache",
                "SystemCache",
                "system/cache/index",
                "system",
                SaasPermissionCodes.Config.Read,
                "lucide:database-backup",
                "缓存管理",
                510,
                null,
                true),
            new(
                "system.server",
                "系统监控",
                MenuType.Menu,
                "/system/server",
                "SystemServer",
                "system/server/index",
                "system",
                SaasPermissionCodes.Config.Read,
                "lucide:server",
                "系统监控",
                520,
                null,
                true)
        ];
    }

    private sealed record MenuSeedDefinition(
        string MenuCode,
        string MenuName,
        MenuType MenuType,
        string Path,
        string RouteName,
        string? Component,
        string? ParentCode,
        string? PermissionCode,
        string Icon,
        string Title,
        int Sort,
        string? Redirect = null,
        bool IsCache = false,
        bool IsAffix = false);
}
