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
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.Saas.Seeders;

/// <summary>
/// 系统菜单种子数据
/// </summary>
public class SysMenuSeeder : DataSeederBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public SysMenuSeeder(ISqlSugarDbContext dbContext, ILogger<SysMenuSeeder> logger, IServiceProvider serviceProvider)
        : base(dbContext, logger, serviceProvider)
    {
    }

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => 15;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[Saas]系统菜单种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        if (await HasDataAsync<SysMenu>(m => true))
        {
            Logger.LogInformation("系统菜单数据已存在，执行菜单层级校准");
            await EnsureDashboardMenusAsync();
            await NormalizeMenuHierarchyAsync();
            return;
        }

        var resources = await DbContext.GetClient().Queryable<SysResource>().ToListAsync();
        if (resources.Count == 0)
        {
            Logger.LogWarning("资源数据不存在，跳过菜单种子数据");
            return;
        }

        var resourceMap = resources.ToDictionary(r => r.ResourceCode, r => r.BasicId);
        var menus = new List<SysMenu>
        {
            // 工作台
            new() { ResourceId = null, ParentId = null, MenuName = "工作台", MenuCode = "dashboard", MenuType = MenuType.Directory, Path = "/dashboard", Component = null, RouteName = null, Redirect = "/dashboard/workspace", Icon = "layout-dashboard", Title = "工作台", IsExternal = false, IsCache = false, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 1 },
            new() { ResourceId = null, ParentId = null, MenuName = "工作台", MenuCode = "dashboard_workspace", MenuType = MenuType.Menu, Path = "/dashboard/workspace", Component = "Dashboard/Index", RouteName = "DashboardWorkspace", Icon = "layout-dashboard", Title = "工作台", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = true, Status = YesOrNo.Yes, Sort = 11 },
            new() { ResourceId = null, ParentId = null, MenuName = "站内信", MenuCode = "dashboard_inbox", MenuType = MenuType.Menu, Path = "/dashboard/inbox", Component = "Dashboard/Inbox/Index", RouteName = "DashboardInbox", Icon = "inbox", Title = "站内信", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 12 },

            // 系统管理
            new() { ResourceId = GetResourceId(resourceMap, "system"), ParentId = null, MenuName = "系统管理", MenuCode = "system", MenuType = MenuType.Directory, Path = "/system", Component = null, RouteName = null, Redirect = "/system/user", Icon = "settings", Title = "系统管理", IsExternal = false, IsCache = false, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 100 },
            new() { ResourceId = GetResourceId(resourceMap, "user"), ParentId = null, MenuName = "账号管理", MenuCode = "user", MenuType = MenuType.Menu, Path = "/system/user", Component = "System/User/Index", RouteName = "SystemUser", Icon = "user", Title = "账号管理", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 101 },
            new() { ResourceId = GetResourceId(resourceMap, "role"), ParentId = null, MenuName = "角色管理", MenuCode = "role", MenuType = MenuType.Menu, Path = "/system/role", Component = "System/Role/Index", RouteName = "SystemRole", Icon = "users", Title = "角色管理", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 102 },
            new() { ResourceId = GetResourceId(resourceMap, "department"), ParentId = null, MenuName = "机构管理", MenuCode = "department", MenuType = MenuType.Menu, Path = "/system/org", Component = "System/Department/Index", RouteName = "SystemDepartment", Icon = "building-2", Title = "机构管理", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 103 },
            new() { ResourceId = GetResourceId(resourceMap, "notice"), ParentId = null, MenuName = "通知公告", MenuCode = "notice", MenuType = MenuType.Menu, Path = "/system/notice", Component = "System/Notification/Index", RouteName = "SystemNotice", Icon = "bell", Title = "通知公告", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Badge = "New", BadgeType = "success", Status = YesOrNo.Yes, Sort = 104 },
            new() { ResourceId = GetResourceId(resourceMap, "oauth_app"), ParentId = null, MenuName = "三方账号", MenuCode = "oauth_app", MenuType = MenuType.Menu, Path = "/system/weChatUser", Component = "System/OAuthApp/Index", RouteName = "SystemOAuthApp", Icon = "link", Title = "三方账号", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 105 },
            new() { ResourceId = GetResourceId(resourceMap, "user_session"), ParentId = null, MenuName = "会话管理", MenuCode = "user_session", MenuType = MenuType.Menu, Path = "/system/session", Component = "System/UserSession/Index", RouteName = "SystemUserSession", Icon = "shield-check", Title = "会话管理", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 106 },
            new() { ResourceId = GetResourceId(resourceMap, "review"), ParentId = null, MenuName = "审核管理", MenuCode = "review", MenuType = MenuType.Menu, Path = "/system/review", Component = "System/Review/Index", RouteName = "SystemReview", Icon = "clipboard-check", Title = "审核管理", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 107 },
            // 消息中心子目录
            new() { ResourceId = GetResourceId(resourceMap, "messaging"), ParentId = null, MenuName = "消息中心", MenuCode = "messaging", MenuType = MenuType.Directory, Path = "/messaging", Component = null, RouteName = null, Redirect = "/messaging/message", Icon = "message-square", Title = "消息中心", IsExternal = false, IsCache = false, IsVisible = true, IsAffix = false, BadgeDot = true, Status = YesOrNo.Yes, Sort = 108 },
            new() { ResourceId = GetResourceId(resourceMap, "message"), ParentId = null, MenuName = "消息管理", MenuCode = "message", MenuType = MenuType.Menu, Path = "/messaging/message", Component = "System/Message/Index", RouteName = "SystemMessage", Icon = "bell-ring", Title = "消息管理", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 251 },
            new() { ResourceId = GetResourceId(resourceMap, "email"), ParentId = null, MenuName = "邮件管理", MenuCode = "email", MenuType = MenuType.Menu, Path = "/messaging/email", Component = "System/Email/Index", RouteName = "SystemEmail", Icon = "mail", Title = "邮件管理", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 252 },
            new() { ResourceId = GetResourceId(resourceMap, "sms"), ParentId = null, MenuName = "短信管理", MenuCode = "sms", MenuType = MenuType.Menu, Path = "/messaging/sms", Component = "System/Sms/Index", RouteName = "SystemSms", Icon = "message-circle", Title = "短信管理", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 253 },

            // 日志管理
            new() { ResourceId = GetResourceId(resourceMap, "log"), ParentId = null, MenuName = "日志管理", MenuCode = "log", MenuType = MenuType.Directory, Path = "/log", Component = null, RouteName = null, Redirect = "/log/vislog", Icon = "file-text", Title = "日志管理", IsExternal = false, IsCache = false, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 109 },
            new() { ResourceId = GetResourceId(resourceMap, "access_log"), ParentId = null, MenuName = "访问日志", MenuCode = "access_log", MenuType = MenuType.Menu, Path = "/log/vislog", Component = "System/Log/Access", RouteName = "AccessLog", Icon = "globe", Title = "访问日志", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 301 },
            new() { ResourceId = GetResourceId(resourceMap, "operation_log"), ParentId = null, MenuName = "操作日志", MenuCode = "operation_log", MenuType = MenuType.Menu, Path = "/log/oplog", Component = "System/Log/Operation", RouteName = "OperationLog", Icon = "history", Title = "操作日志", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 302 },
            new() { ResourceId = GetResourceId(resourceMap, "exception_log"), ParentId = null, MenuName = "异常日志", MenuCode = "exception_log", MenuType = MenuType.Menu, Path = "/log/exlog", Component = "System/Log/Exception", RouteName = "ExceptionLog", Icon = "alert-triangle", Title = "异常日志", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 303 },
            new() { ResourceId = GetResourceId(resourceMap, "audit_log"), ParentId = null, MenuName = "差异日志", MenuCode = "audit_log", MenuType = MenuType.Menu, Path = "/log/difflog", Component = "System/Log/Audit", RouteName = "AuditLog", Icon = "file-diff", Title = "差异日志", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 304 },
            new() { ResourceId = GetResourceId(resourceMap, "login_log"), ParentId = null, MenuName = "登录日志", MenuCode = "login_log", MenuType = MenuType.Menu, Path = "/log/loginlog", Component = "System/Log/Login", RouteName = "LoginLog", Icon = "log-in", Title = "登录日志", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 305 },
            new() { ResourceId = GetResourceId(resourceMap, "task_log"), ParentId = null, MenuName = "调度日志", MenuCode = "task_log", MenuType = MenuType.Menu, Path = "/log/tasklog", Component = "System/Log/Task", RouteName = "TaskLog", Icon = "calendar-clock", Title = "调度日志", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 306 },
            new() { ResourceId = GetResourceId(resourceMap, "api_log"), ParentId = null, MenuName = "接口日志", MenuCode = "api_log", MenuType = MenuType.Menu, Path = "/log/apilog", Component = "System/Log/Api", RouteName = "ApiLog", Icon = "shield-check", Title = "接口日志", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 307 },

            // 平台管理
            new() { ResourceId = GetResourceId(resourceMap, "platform"), ParentId = null, MenuName = "平台管理", MenuCode = "platform", MenuType = MenuType.Directory, Path = "/platform", Component = null, RouteName = null, Redirect = "/platform/menu", Icon = "layout-grid", Title = "平台管理", IsExternal = false, IsCache = false, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 200 },
            new() { ResourceId = GetResourceId(resourceMap, "tenant"), ParentId = null, MenuName = "租户管理", MenuCode = "tenant", MenuType = MenuType.Menu, Path = "/platform/tenant", Component = "System/Tenant/Index", RouteName = "SystemTenant", Icon = "server", Title = "租户管理", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 201 },
            new() { ResourceId = GetResourceId(resourceMap, "permission"), ParentId = null, MenuName = "权限管理", MenuCode = "permission", MenuType = MenuType.Menu, Path = "/platform/permission", Component = "System/Permission/Index", RouteName = "SystemPermission", Icon = "shield", Title = "权限管理", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 202 },
            new() { ResourceId = GetResourceId(resourceMap, "menu"), ParentId = null, MenuName = "菜单管理", MenuCode = "menu", MenuType = MenuType.Menu, Path = "/platform/menu", Component = "System/Menu/Index", RouteName = "SystemMenu", Icon = "menu", Title = "菜单管理", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 203 },
            new() { ResourceId = GetResourceId(resourceMap, "config"), ParentId = null, MenuName = "参数配置", MenuCode = "config", MenuType = MenuType.Menu, Path = "/platform/config", Component = "System/Config/Index", RouteName = "SystemConfig", Icon = "sliders-horizontal", Title = "参数配置", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 204 },
            new() { ResourceId = GetResourceId(resourceMap, "constraint_rule"), ParentId = null, MenuName = "约束规则", MenuCode = "constraint_rule", MenuType = MenuType.Menu, Path = "/platform/constraint-rule", Component = "System/ConstraintRule/Index", RouteName = "SystemConstraintRule", Icon = "scale", Title = "约束规则", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 2041 },
            new() { ResourceId = GetResourceId(resourceMap, "dict"), ParentId = null, MenuName = "字典管理", MenuCode = "dict", MenuType = MenuType.Menu, Path = "/platform/dict", Component = "System/Dict/Index", RouteName = "SystemDict", Icon = "book-open", Title = "字典管理", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 205 },
            new() { ResourceId = GetResourceId(resourceMap, "task"), ParentId = null, MenuName = "任务调度", MenuCode = "task", MenuType = MenuType.Menu, Path = "/platform/job", Component = "System/Task/Index", RouteName = "SystemTask", Icon = "clock", Title = "任务调度", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 206 },
            new() { ResourceId = GetResourceId(resourceMap, "cache"), ParentId = null, MenuName = "缓存管理", MenuCode = "cache", MenuType = MenuType.Menu, Path = "/platform/cache", Component = "System/Cache/Index", RouteName = "SystemCache", Icon = "database", Title = "缓存管理", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 207 },
            new() { ResourceId = GetResourceId(resourceMap, "file"), ParentId = null, MenuName = "文件管理", MenuCode = "file", MenuType = MenuType.Menu, Path = "/platform/file", Component = "System/File/Index", RouteName = "SystemFile", Icon = "folder-open", Title = "文件管理", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 208 },
            new() { ResourceId = GetResourceId(resourceMap, "monitor"), ParentId = null, MenuName = "系统监控", MenuCode = "monitor", MenuType = MenuType.Menu, Path = "/platform/server", Component = "System/Monitor/Index", RouteName = "SystemMonitor", Icon = "activity", Title = "系统监控", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 209 },

            // 关于系统
            new() { ResourceId = GetResourceId(resourceMap, "about"), ParentId = null, MenuName = "关于系统", MenuCode = "about", MenuType = MenuType.Menu, Path = "/about", Component = "About/Index", RouteName = "About", Icon = "info", Title = "关于系统", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 999 },
        };

        await BulkInsertAsync(menus);
        await NormalizeMenuHierarchyAsync();

        Logger.LogInformation("成功初始化 {Count} 个系统菜单", menus.Count);
    }

    private async Task EnsureDashboardMenusAsync()
    {
        var client = DbContext.GetClient();
        var existingCodes = await client
            .Queryable<SysMenu>()
            .Where(m => m.MenuCode == "dashboard" || m.MenuCode == "dashboard_workspace" || m.MenuCode == "dashboard_inbox")
            .Select(m => m.MenuCode)
            .ToListAsync();
        var existingCodeSet = existingCodes
            .Where(static code => !string.IsNullOrWhiteSpace(code))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var addList = new List<SysMenu>();
        if (!existingCodeSet.Contains("dashboard"))
        {
            addList.Add(new SysMenu { ResourceId = null, ParentId = null, MenuName = "工作台", MenuCode = "dashboard", MenuType = MenuType.Directory, Path = "/dashboard", Component = null, RouteName = null, Redirect = "/dashboard/workspace", Icon = "layout-dashboard", Title = "工作台", IsExternal = false, IsCache = false, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 1 });
        }

        if (!existingCodeSet.Contains("dashboard_workspace"))
        {
            addList.Add(new SysMenu { ResourceId = null, ParentId = null, MenuName = "工作台", MenuCode = "dashboard_workspace", MenuType = MenuType.Menu, Path = "/dashboard/workspace", Component = "Dashboard/Index", RouteName = "DashboardWorkspace", Icon = "layout-dashboard", Title = "工作台", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = true, Status = YesOrNo.Yes, Sort = 11 });
        }

        if (!existingCodeSet.Contains("dashboard_inbox"))
        {
            addList.Add(new SysMenu { ResourceId = null, ParentId = null, MenuName = "站内信", MenuCode = "dashboard_inbox", MenuType = MenuType.Menu, Path = "/dashboard/inbox", Component = "Dashboard/Inbox/Index", RouteName = "DashboardInbox", Icon = "inbox", Title = "站内信", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 12 });
        }

        if (addList.Count > 0)
        {
            await BulkInsertAsync(addList);
            Logger.LogInformation("补充新增 {Count} 个工作台菜单", addList.Count);
        }

        await client.Updateable<SysMenu>()
            .SetColumns(m => m.MenuName == "工作台")
            .SetColumns(m => m.MenuType == MenuType.Directory)
            .SetColumns(m => m.Path == "/dashboard")
            .SetColumns(m => m.Component == null)
            .SetColumns(m => m.RouteName == null)
            .SetColumns(m => m.Redirect == "/dashboard/workspace")
            .SetColumns(m => m.Icon == "layout-dashboard")
            .SetColumns(m => m.Title == "工作台")
            .SetColumns(m => m.IsCache == false)
            .SetColumns(m => m.IsVisible == true)
            .SetColumns(m => m.IsAffix == false)
            .SetColumns(m => m.Status == YesOrNo.Yes)
            .SetColumns(m => m.Sort == 1)
            .Where(m => m.MenuCode == "dashboard")
            .ExecuteCommandAsync();

        await client.Updateable<SysMenu>()
            .SetColumns(m => m.MenuName == "工作台")
            .SetColumns(m => m.MenuType == MenuType.Menu)
            .SetColumns(m => m.Path == "/dashboard/workspace")
            .SetColumns(m => m.Component == "Dashboard/Index")
            .SetColumns(m => m.RouteName == "DashboardWorkspace")
            .SetColumns(m => m.Redirect == null)
            .SetColumns(m => m.Icon == "layout-dashboard")
            .SetColumns(m => m.Title == "工作台")
            .SetColumns(m => m.IsCache == true)
            .SetColumns(m => m.IsVisible == true)
            .SetColumns(m => m.IsAffix == true)
            .SetColumns(m => m.Status == YesOrNo.Yes)
            .SetColumns(m => m.Sort == 11)
            .Where(m => m.MenuCode == "dashboard_workspace")
            .ExecuteCommandAsync();

        await client.Updateable<SysMenu>()
            .SetColumns(m => m.MenuName == "站内信")
            .SetColumns(m => m.MenuType == MenuType.Menu)
            .SetColumns(m => m.Path == "/dashboard/inbox")
            .SetColumns(m => m.Component == "Dashboard/Inbox/Index")
            .SetColumns(m => m.RouteName == "DashboardInbox")
            .SetColumns(m => m.Redirect == null)
            .SetColumns(m => m.Icon == "inbox")
            .SetColumns(m => m.Title == "站内信")
            .SetColumns(m => m.IsCache == true)
            .SetColumns(m => m.IsVisible == true)
            .SetColumns(m => m.IsAffix == false)
            .SetColumns(m => m.Status == YesOrNo.Yes)
            .SetColumns(m => m.Sort == 12)
            .Where(m => m.MenuCode == "dashboard_inbox")
            .ExecuteCommandAsync();
    }

    private static long? GetResourceId(Dictionary<string, long> resourceMap, string resourceCode) => resourceMap.TryGetValue(resourceCode, out var id) ? id : null;

    private async Task NormalizeMenuHierarchyAsync()
    {
        // 顶级菜单：日志管理与平台管理保持同级
        await SetMenuAsRootAsync("dashboard");
        await SetMenuAsRootAsync("system");
        await SetMenuAsRootAsync("messaging");
        await SetMenuAsRootAsync("log");
        await SetMenuAsRootAsync("platform");
        await SetMenuAsRootAsync("about");

        await UpdateMenuParentIdAsync("dashboard", ["dashboard_workspace", "dashboard_inbox"]);
        await UpdateMenuParentIdAsync("messaging", ["message", "email", "sms"]);
        await UpdateMenuParentIdAsync("log", ["access_log", "operation_log", "exception_log", "audit_log", "login_log", "task_log", "api_log"]);
        await UpdateMenuParentIdAsync("system", ["user", "role", "department", "notice", "oauth_app", "user_session", "review"]);
        await UpdateMenuParentIdAsync("platform", ["tenant", "permission", "menu", "config", "constraint_rule", "dict", "task", "monitor", "cache", "file"]);
    }

    private async Task SetMenuAsRootAsync(string menuCode)
    {
        await DbContext.GetClient()
            .Updateable<SysMenu>()
            .SetColumns(menu => menu.ParentId == null)
            .Where(menu => menu.MenuCode == menuCode)
            .ExecuteCommandAsync();
    }

    private async Task UpdateMenuParentIdAsync(string parentCode, string[] childCodes)
    {
        var parentMenu = await DbContext.GetClient().Queryable<SysMenu>().FirstAsync(m => m.MenuCode == parentCode);
        if (parentMenu == null)
        {
            return;
        }

        await DbContext.GetClient().Updateable<SysMenu>().SetColumns(m => m.ParentId == parentMenu.BasicId).Where(m => childCodes.Contains(m.MenuCode)).ExecuteCommandAsync();
    }
}
