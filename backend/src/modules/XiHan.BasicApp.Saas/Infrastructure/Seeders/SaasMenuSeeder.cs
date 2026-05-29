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
    private const string SeededMenuRemark = "系统初始化全局菜单";
    private readonly ICurrentTenant _currentTenant = currentTenant;

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => 25;

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
            .Where(permission =>
                permission.TenantId == 0
                && permission.IsGlobal
                && permissionCodes.Contains(permission.PermissionCode)
                && permission.Status == EnableStatus.Enabled)
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
            // IsGlobal 为派生属性（= TenantId == 0）：平台菜单仅需置 TenantId = 0
            TenantId = 0
        };
        _ = ApplyDefinition(menu, definition, parentId, permissionId);
        return menu;
    }

    private static bool ApplyDefinition(SysMenu menu, MenuSeedDefinition definition, long? parentId, long? permissionId)
    {
        var changed = false;
        changed |= SetIfChanged(menu.TenantId, 0, value => menu.TenantId = value);
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
        changed |= SetIfChanged(menu.Remark, SeededMenuRemark, value => menu.Remark = value);
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
                SaasPermissionCodes.UserStatistics.Read,
                "lucide:gauge",
                "仪表盘",
                11,
                null,
                false,
                true),
            new(
                "workbench.inbox",
                "站内信",
                MenuType.Menu,
                "/workbench/inbox",
                "WorkbenchInbox",
                "workbench/inbox/index",
                "workbench",
                null,
                "lucide:inbox",
                "站内信",
                12,
                null,
                true),
            new(
                "workbench.profile",
                "个人中心",
                MenuType.Menu,
                "/workbench/profile",
                "WorkbenchProfile",
                "_core/profile/index",
                "workbench",
                null,
                "lucide:user-round-cog",
                "个人中心",
                13,
                null,
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
                "/system/user"),
            new(
                "system.user",
                "账号管理",
                MenuType.Menu,
                "/system/user",
                "SystemUser",
                "system/user/index",
                "system",
                SaasPermissionCodes.User.Read,
                "lucide:users",
                "账号管理",
                110,
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
                120,
                null,
                true),
            new(
                "system.org",
                "机构管理",
                MenuType.Menu,
                "/system/org",
                "SystemOrg",
                "system/org/index",
                "system",
                SaasPermissionCodes.Department.Read,
                "lucide:network",
                "机构管理",
                130,
                null,
                true),
            new(
                "system.permission",
                "权限中心",
                MenuType.Menu,
                "/system/permission",
                "SystemPermission",
                "system/permission/index",
                "system",
                SaasPermissionCodes.Permission.Read,
                "lucide:key-round",
                "权限中心",
                140,
                null,
                true),
            new(
                "system.message",
                "消息中心",
                MenuType.Menu,
                "/system/message",
                "SystemMessage",
                "system/message/index",
                "system",
                SaasPermissionCodes.Message.Read,
                "lucide:mail",
                "消息中心",
                150,
                null,
                true),

            new(
                "platform",
                "平台管理",
                MenuType.Directory,
                "/platform",
                "Platform",
                null,
                null,
                null,
                "lucide:blocks",
                "平台管理",
                200,
                "/platform/app"),
            new(
                "platform.app",
                "应用管理",
                MenuType.Menu,
                "/platform/app",
                "PlatformApp",
                "platform/app/index",
                "platform",
                SaasPermissionCodes.OAuthApp.Read,
                "lucide:badge-check",
                "应用管理",
                210,
                null,
                true),
            new(
                "platform.tenant",
                "租户管理",
                MenuType.Menu,
                "/platform/tenant",
                "PlatformTenant",
                "platform/tenant/index",
                "platform",
                SaasPermissionCodes.Tenant.Read,
                "lucide:building-2",
                "租户管理",
                220,
                null,
                true),
            new(
                "platform.menu",
                "菜单管理",
                MenuType.Menu,
                "/platform/menu",
                "PlatformMenu",
                "platform/menu/index",
                "platform",
                SaasPermissionCodes.Menu.Read,
                "lucide:list-tree",
                "菜单管理",
                230,
                null,
                true),
            new(
                "platform.config",
                "参数配置",
                MenuType.Menu,
                "/platform/config",
                "PlatformConfig",
                "platform/config/index",
                "platform",
                SaasPermissionCodes.Config.Read,
                "lucide:sliders-horizontal",
                "参数配置",
                240,
                null,
                true),
            new(
                "platform.dict",
                "字典管理",
                MenuType.Menu,
                "/platform/dict",
                "PlatformDict",
                "platform/dict/index",
                "platform",
                SaasPermissionCodes.Dict.Read,
                "lucide:book-open",
                "字典管理",
                250,
                null,
                true),
            new(
                "platform.file",
                "文件管理",
                MenuType.Menu,
                "/platform/file",
                "PlatformFile",
                "platform/file/index",
                "platform",
                SaasPermissionCodes.File.Read,
                "lucide:folder-open",
                "文件管理",
                260,
                null,
                true),
            new(
                "platform.job",
                "任务调度",
                MenuType.Menu,
                "/platform/job",
                "PlatformJob",
                "platform/job/index",
                "platform",
                SaasPermissionCodes.Task.Read,
                "lucide:timer",
                "任务调度",
                270,
                null,
                true),
            new(
                "platform.approval",
                "审批流程",
                MenuType.Menu,
                "/platform/approval",
                "PlatformApproval",
                "platform/approval/index",
                "platform",
                SaasPermissionCodes.Review.Read,
                "lucide:clipboard-check",
                "审批流程",
                280,
                null,
                true),
            new(
                "platform.server",
                "系统监控",
                MenuType.Menu,
                "/platform/server",
                "PlatformServer",
                "platform/server/index",
                "platform",
                SaasPermissionCodes.Config.Read,
                "lucide:server",
                "系统监控",
                290,
                null,
                true),
            new(
                "platform.cache",
                "缓存管理",
                MenuType.Menu,
                "/platform/cache",
                "PlatformCache",
                "platform/cache/index",
                "platform",
                SaasPermissionCodes.Config.Read,
                "lucide:database-backup",
                "缓存管理",
                300,
                null,
                true),

            new(
                "log",
                "日志管理",
                MenuType.Directory,
                "/log",
                "Log",
                null,
                null,
                null,
                "lucide:file-search",
                "日志管理",
                400,
                "/log/login"),
            new(
                "log.login",
                "登录日志",
                MenuType.Menu,
                "/log/login",
                "LogLogin",
                "log/login/index",
                "log",
                SaasPermissionCodes.LoginLog.Read,
                "lucide:log-in",
                "登录日志",
                410,
                null,
                true),
            new(
                "log.access",
                "访问日志",
                MenuType.Menu,
                "/log/access",
                "LogAccess",
                "log/access/index",
                "log",
                SaasPermissionCodes.AccessLog.Read,
                "lucide:activity",
                "访问日志",
                420,
                null,
                true),
            new(
                "log.operation",
                "操作日志",
                MenuType.Menu,
                "/log/operation",
                "LogOperation",
                "log/operation/index",
                "log",
                SaasPermissionCodes.OperationLog.Read,
                "lucide:clipboard-list",
                "操作日志",
                430,
                null,
                true),
            new(
                "log.api",
                "API 日志",
                MenuType.Menu,
                "/log/api",
                "LogApi",
                "log/api/index",
                "log",
                SaasPermissionCodes.ApiLog.Read,
                "lucide:globe",
                "API 日志",
                440,
                null,
                true),
            new(
                "log.exception",
                "异常日志",
                MenuType.Menu,
                "/log/exception",
                "LogException",
                "log/exception/index",
                "log",
                SaasPermissionCodes.ExceptionLog.Read,
                "lucide:bug",
                "异常日志",
                450,
                null,
                true),
            new(
                "log.diff",
                "差异日志",
                MenuType.Menu,
                "/log/diff",
                "LogDiff",
                "log/diff/index",
                "log",
                SaasPermissionCodes.DiffLog.Read,
                "lucide:file-diff",
                "差异日志",
                460)
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
