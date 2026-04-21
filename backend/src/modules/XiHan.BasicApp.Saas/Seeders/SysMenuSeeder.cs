#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysMenuSeeder
// Guid:6f789012-3456-7890-0123-556677889900
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/07 12:50:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.Saas.Seeders;

/// <summary>
/// 系统菜单种子数据。
/// </summary>
public class SysMenuSeeder : DataSeederBase
{
    public SysMenuSeeder(
        ISqlSugarClientResolver clientResolver,
        ILogger<SysMenuSeeder> logger,
        IServiceProvider serviceProvider)
        : base(clientResolver, logger, serviceProvider)
    {
    }

    public override int Order => SaasSeedOrder.Menus;

    public override string Name => "[Saas]系统菜单种子数据";

    protected override async Task SeedInternalAsync()
    {
        var permissionCodes = new[]
        {
            "tenant:read",
            "user:read",
            "role:read",
            "permission:read",
            "menu:read",
            "department:read",
            "constraint_rule:read",
            "config:read",
            "dict:read",
            "notification:read",
            "message:read",
            "email:read",
            "sms:read",
            "oauth_app:read",
            "user_session:read",
            "review:read",
            "task:read",
            "file:read",
            "cache:read",
            "monitor:read",
            "access_log:read",
            "operation_log:read",
            "exception_log:read",
            "audit_log:read",
            "login_log:read",
            "task_log:read"
        };

        var permissions = await DbClient
            .Queryable<SysPermission>()
            .Where(permission => permissionCodes.Contains(permission.PermissionCode))
            .ToListAsync();
        var permissionMap = permissions.ToDictionary(permission => permission.PermissionCode, StringComparer.OrdinalIgnoreCase);

        var templates = BuildTemplates(permissionMap);
        var menuCodes = templates.Select(item => item.MenuCode).ToArray();
        var existingMenus = await DbClient
            .Queryable<SysMenu>()
            .Where(menu => menuCodes.Contains(menu.MenuCode))
            .ToListAsync();

        var existingMap = existingMenus.ToDictionary(menu => menu.MenuCode, StringComparer.OrdinalIgnoreCase);
        var toInsert = templates
            .Where(template => !existingMap.ContainsKey(template.MenuCode))
            .ToList();

        if (toInsert.Count > 0)
        {
            await BulkInsertAsync(toInsert);
        }

        await NormalizeMenuHierarchyAsync(templates.Select(item => item.MenuCode).ToArray());

        var toEnableIds = existingMenus
            .Where(menu =>
                menu.TenantId == SaasSeedDefaults.PlatformTenantId
                && menu.IsGlobal
                && menu.Status != YesOrNo.Yes)
            .Select(menu => menu.BasicId)
            .ToArray();

        if (toEnableIds.Length > 0)
        {
            await DbClient
                .Updateable<SysMenu>()
                .SetColumns(menu => menu.Status == YesOrNo.Yes)
                .Where(menu => toEnableIds.Contains(menu.BasicId))
                .ExecuteCommandAsync();
        }

        Logger.LogInformation(
            "系统菜单模板种子完成：新增 {InsertCount} 项，启用 {EnableCount} 项",
            toInsert.Count,
            toEnableIds.Length);
    }

    private static List<SysMenu> BuildTemplates(IReadOnlyDictionary<string, SysPermission> permissionMap)
    {
        return
        [
            CreateDirectory("workbench", "工作台", "/workbench", "/workbench/inbox", "layout-dashboard", 100),
            CreateMenu("inbox", "我的消息", "workbench", "/workbench/inbox", "System/Message/Index", "WorkbenchInbox", "inbox", 110, LookupPermissionId(permissionMap, "message:read")),

            CreateDirectory("system", "租户空间", "/system", "/system/user", "settings-2", 200),
            CreateMenu("user", "账号管理", "system", "/system/user", "System/User/Index", "SystemUser", "users", 210, LookupPermissionId(permissionMap, "user:read")),
            CreateMenu("role", "角色管理", "system", "/system/role", "System/Role/Index", "SystemRole", "shield", 220, LookupPermissionId(permissionMap, "role:read")),
            CreateMenu("department", "部门管理", "system", "/system/department", "System/Department/Index", "SystemDepartment", "building-2", 230, LookupPermissionId(permissionMap, "department:read")),
            CreateMenu("notification", "通知公告", "system", "/system/notification", "System/Notification/Index", "SystemNotification", "bell", 240, LookupPermissionId(permissionMap, "notification:read")),
            CreateMenu("oauth_app", "三方应用", "system", "/system/oauth-app", "System/OAuthApp/Index", "SystemOAuthApp", "link", 250, LookupPermissionId(permissionMap, "oauth_app:read")),
            CreateMenu("user_session", "会话管理", "system", "/system/user-session", "System/UserSession/Index", "SystemUserSession", "shield-check", 260, LookupPermissionId(permissionMap, "user_session:read")),
            CreateMenu("review", "审核中心", "system", "/system/review", "System/Review/Index", "SystemReview", "clipboard-check", 270, LookupPermissionId(permissionMap, "review:read")),

            CreateDirectory("platform", "平台管理", "/platform", "/platform/tenant", "server-cog", 300),
            CreateMenu("tenant", "租户管理", "platform", "/platform/tenant", "System/Tenant/Index", "SystemTenant", "server", 310, LookupPermissionId(permissionMap, "tenant:read")),
            CreateMenu("permission", "权限管理", "platform", "/platform/permission", "System/Permission/Index", "SystemPermission", "key-round", 320, LookupPermissionId(permissionMap, "permission:read")),
            CreateMenu("menu", "菜单管理", "platform", "/platform/menu", "System/Menu/Index", "SystemMenu", "panel-left", 330, LookupPermissionId(permissionMap, "menu:read")),
            CreateMenu("constraint_rule", "约束规则", "platform", "/platform/constraint-rule", "System/ConstraintRule/Index", "SystemConstraintRule", "scale", 340, LookupPermissionId(permissionMap, "constraint_rule:read")),
            CreateMenu("config", "运行配置", "platform", "/platform/config", "System/Config/Index", "SystemConfig", "sliders-horizontal", 350, LookupPermissionId(permissionMap, "config:read")),
            CreateMenu("dict", "字典中心", "platform", "/platform/dict", "System/Dict/Index", "SystemDict", "book-marked", 360, LookupPermissionId(permissionMap, "dict:read")),
            CreateMenu("task", "任务调度", "platform", "/platform/task", "System/Task/Index", "SystemTask", "clock-3", 370, LookupPermissionId(permissionMap, "task:read")),
            CreateMenu("file", "文件中心", "platform", "/platform/file", "System/File/Index", "SystemFile", "folder-open", 380, LookupPermissionId(permissionMap, "file:read")),
            CreateMenu("cache", "缓存管理", "platform", "/platform/cache", "System/Cache/Index", "SystemCache", "database-zap", 390, LookupPermissionId(permissionMap, "cache:read")),
            CreateMenu("monitor", "系统监控", "platform", "/platform/monitor", "System/Monitor/Index", "SystemMonitor", "activity", 400, LookupPermissionId(permissionMap, "monitor:read")),

            CreateDirectory("messaging", "消息投递", "/messaging", "/messaging/message", "send", 500),
            CreateMenu("message", "站内消息", "messaging", "/messaging/message", "System/Message/Index", "SystemMessage", "mailbox", 510, LookupPermissionId(permissionMap, "message:read")),
            CreateMenu("email", "邮件管理", "messaging", "/messaging/email", "System/Email/Index", "SystemEmail", "mail", 520, LookupPermissionId(permissionMap, "email:read")),
            CreateMenu("sms", "短信管理", "messaging", "/messaging/sms", "System/Sms/Index", "SystemSms", "message-square-more", 530, LookupPermissionId(permissionMap, "sms:read")),

            CreateDirectory("audit", "审计与日志", "/audit", "/audit/access-log", "scroll-text", 600),
            CreateMenu("access_log", "访问日志", "audit", "/audit/access-log", "System/Log/Access", "AccessLog", "globe", 610, LookupPermissionId(permissionMap, "access_log:read")),
            CreateMenu("operation_log", "操作日志", "audit", "/audit/operation-log", "System/Log/Operation", "OperationLog", "history", 620, LookupPermissionId(permissionMap, "operation_log:read")),
            CreateMenu("exception_log", "异常日志", "audit", "/audit/exception-log", "System/Log/Exception", "ExceptionLog", "triangle-alert", 630, LookupPermissionId(permissionMap, "exception_log:read")),
            CreateMenu("audit_log", "审计日志", "audit", "/audit/audit-log", "System/Log/Audit", "AuditLog", "file-diff", 640, LookupPermissionId(permissionMap, "audit_log:read")),
            CreateMenu("login_log", "登录日志", "audit", "/audit/login-log", "System/Log/Login", "LoginLog", "log-in", 650, LookupPermissionId(permissionMap, "login_log:read")),
            CreateMenu("task_log", "调度日志", "audit", "/audit/task-log", "System/Log/Task", "TaskLog", "calendar-clock", 660, LookupPermissionId(permissionMap, "task_log:read"))
        ];
    }

    private async Task NormalizeMenuHierarchyAsync(string[] menuCodes)
    {
        var menus = await DbClient
            .Queryable<SysMenu>()
            .Where(menu => menuCodes.Contains(menu.MenuCode))
            .ToListAsync();

        var menuMap = menus.ToDictionary(menu => menu.MenuCode, StringComparer.OrdinalIgnoreCase);
        var parentMap = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["workbench"] = null,
            ["inbox"] = "workbench",
            ["system"] = null,
            ["user"] = "system",
            ["role"] = "system",
            ["department"] = "system",
            ["notification"] = "system",
            ["oauth_app"] = "system",
            ["user_session"] = "system",
            ["review"] = "system",
            ["platform"] = null,
            ["tenant"] = "platform",
            ["permission"] = "platform",
            ["menu"] = "platform",
            ["constraint_rule"] = "platform",
            ["config"] = "platform",
            ["dict"] = "platform",
            ["task"] = "platform",
            ["file"] = "platform",
            ["cache"] = "platform",
            ["monitor"] = "platform",
            ["messaging"] = null,
            ["message"] = "messaging",
            ["email"] = "messaging",
            ["sms"] = "messaging",
            ["audit"] = null,
            ["access_log"] = "audit",
            ["operation_log"] = "audit",
            ["exception_log"] = "audit",
            ["audit_log"] = "audit",
            ["login_log"] = "audit",
            ["task_log"] = "audit"
        };

        foreach (var pair in parentMap)
        {
            if (!menuMap.TryGetValue(pair.Key, out var menu))
            {
                continue;
            }

            long? expectedParentId = null;
            if (!string.IsNullOrWhiteSpace(pair.Value) && menuMap.TryGetValue(pair.Value, out var parentMenu))
            {
                expectedParentId = parentMenu.BasicId;
            }

            if (menu.ParentId == expectedParentId)
            {
                continue;
            }

            await DbClient
                .Updateable<SysMenu>()
                .SetColumns(item => item.ParentId == expectedParentId)
                .Where(item => item.BasicId == menu.BasicId)
                .ExecuteCommandAsync();
        }
    }

    private static long? LookupPermissionId(IReadOnlyDictionary<string, SysPermission> permissionMap, string permissionCode)
    {
        return permissionMap.TryGetValue(permissionCode, out var permission) ? permission.BasicId : null;
    }

    private static SysMenu CreateDirectory(
        string code,
        string name,
        string path,
        string redirect,
        string icon,
        int sort)
    {
        return new SysMenu
        {
            TenantId = SaasSeedDefaults.PlatformTenantId,
            IsGlobal = true,
            MenuCode = code,
            MenuName = name,
            MenuType = MenuType.Directory,
            Path = path,
            Redirect = redirect,
            Icon = icon,
            Title = name,
            IsVisible = true,
            IsCache = false,
            IsAffix = false,
            Status = YesOrNo.Yes,
            Sort = sort
        };
    }

    private static SysMenu CreateMenu(
        string code,
        string name,
        string parentCode,
        string path,
        string component,
        string routeName,
        string icon,
        int sort,
        long? permissionId)
    {
        return new SysMenu
        {
            TenantId = SaasSeedDefaults.PlatformTenantId,
            IsGlobal = true,
            ParentId = null,
            PermissionId = permissionId,
            MenuCode = code,
            MenuName = name,
            MenuType = MenuType.Menu,
            Path = path,
            Component = component,
            RouteName = routeName,
            Icon = icon,
            Title = name,
            IsVisible = true,
            IsCache = true,
            IsAffix = false,
            Status = YesOrNo.Yes,
            Sort = sort,
            Metadata = $"{{\"templateParent\":\"{parentCode}\"}}"
        };
    }
}
