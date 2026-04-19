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
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.Saas.Seeders;

/// <summary>
/// 系统菜单种子数据
/// 菜单是纯 UI 结构，通过 PermissionId 绑定所需权限
/// </summary>
public class SysMenuSeeder : DataSeederBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public SysMenuSeeder(ISqlSugarClientResolver clientResolver, ILogger<SysMenuSeeder> logger, IServiceProvider serviceProvider)
        : base(clientResolver, logger, serviceProvider)
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
            await NormalizeMenuHierarchyAsync();
            return;
        }

        // 菜单通过 PermissionId 绑定权限，目录类菜单无需权限（null）
        var menus = new List<SysMenu>
        {
            // 工作台
            new() { ParentId = null, MenuName = "工作台", MenuCode = "workbench", MenuType = MenuType.Directory, Path = "/workbench", Component = null, RouteName = null, Redirect = "/workbench/dashboard", Icon = "workbench", Title = "工作台", IsExternal = false, IsCache = false, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 1 },
            new() { ParentId = null, MenuName = "仪表板", MenuCode = "dashboard", MenuType = MenuType.Menu, Path = "/workbench/dashboard", Component = "Workbench/Dashboard/Index", RouteName = "Dashboard", Icon = "dashboard", Title = "工作台", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = true, Status = YesOrNo.Yes, Sort = 100 },
            new() { ParentId = null, MenuName = "站内信", MenuCode = "inbox", MenuType = MenuType.Menu, Path = "/workbench/inbox", Component = "Workbench/Inbox/Index", RouteName = "Inbox", Icon = "inbox", Title = "站内信", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 200 },

            // 消息中心
            new() { ParentId = null, MenuName = "消息中心", MenuCode = "messaging", MenuType = MenuType.Directory, Path = "/messaging", Component = null, RouteName = null, Redirect = "/messaging/message", Icon = "message-square", Title = "消息中心", IsExternal = false, IsCache = false, IsVisible = true, IsAffix = false, BadgeDot = true, Status = YesOrNo.Yes, Sort = 7000 },
            new() { ParentId = null, MenuName = "消息管理", MenuCode = "message", MenuType = MenuType.Menu, Path = "/messaging/message", Component = "System/Message/Index", RouteName = "SystemMessage", Icon = "bell-ring", Title = "消息管理", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 100 },
            new() { ParentId = null, MenuName = "邮件管理", MenuCode = "email", MenuType = MenuType.Menu, Path = "/messaging/email", Component = "System/Email/Index", RouteName = "SystemEmail", Icon = "mail", Title = "邮件管理", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 200 },
            new() { ParentId = null, MenuName = "短信管理", MenuCode = "sms", MenuType = MenuType.Menu, Path = "/messaging/sms", Component = "System/Sms/Index", RouteName = "SystemSms", Icon = "message-circle", Title = "短信管理", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 300 },

            // 系统管理
            new() { ParentId = null, MenuName = "系统管理", MenuCode = "system", MenuType = MenuType.Directory, Path = "/system", Component = null, RouteName = null, Redirect = "/system/user", Icon = "settings", Title = "系统管理", IsExternal = false, IsCache = false, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 8000 },
            new() { ParentId = null, MenuName = "账号管理", MenuCode = "user", MenuType = MenuType.Menu, Path = "/system/user", Component = "System/User/Index", RouteName = "SystemUser", Icon = "user", Title = "账号管理", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 100 },
            new() { ParentId = null, MenuName = "角色管理", MenuCode = "role", MenuType = MenuType.Menu, Path = "/system/role", Component = "System/Role/Index", RouteName = "SystemRole", Icon = "users", Title = "角色管理", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 200 },
            new() { ParentId = null, MenuName = "机构管理", MenuCode = "department", MenuType = MenuType.Menu, Path = "/system/org", Component = "System/Department/Index", RouteName = "SystemDepartment", Icon = "building-2", Title = "机构管理", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 300 },
            new() { ParentId = null, MenuName = "通知公告", MenuCode = "notice", MenuType = MenuType.Menu, Path = "/system/notice", Component = "System/Notification/Index", RouteName = "SystemNotice", Icon = "bell", Title = "通知公告", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 400 },
            new() { ParentId = null, MenuName = "三方账号", MenuCode = "oauth_app", MenuType = MenuType.Menu, Path = "/system/weChatUser", Component = "System/OAuthApp/Index", RouteName = "SystemOAuthApp", Icon = "link", Title = "三方账号", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 500 },
            new() { ParentId = null, MenuName = "会话管理", MenuCode = "user_session", MenuType = MenuType.Menu, Path = "/system/session", Component = "System/UserSession/Index", RouteName = "SystemUserSession", Icon = "shield-check", Title = "会话管理", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 600 },
            new() { ParentId = null, MenuName = "审核管理", MenuCode = "review", MenuType = MenuType.Menu, Path = "/system/review", Component = "System/Review/Index", RouteName = "SystemReview", Icon = "clipboard-check", Title = "审核管理", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 700 },

            // 平台管理
            new() { ParentId = null, MenuName = "平台管理", MenuCode = "platform", MenuType = MenuType.Directory, Path = "/platform", Component = null, RouteName = null, Redirect = "/platform/menu", Icon = "layout-grid", Title = "平台管理", IsExternal = false, IsCache = false, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 8001 },
            new() { ParentId = null, MenuName = "租户管理", MenuCode = "tenant", MenuType = MenuType.Menu, Path = "/platform/tenant", Component = "System/Tenant/Index", RouteName = "SystemTenant", Icon = "server", Title = "租户管理", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 100 },
            new() { ParentId = null, MenuName = "权限管理", MenuCode = "permission", MenuType = MenuType.Menu, Path = "/platform/permission", Component = "System/Permission/Index", RouteName = "SystemPermission", Icon = "shield", Title = "权限管理", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 200 },
            new() { ParentId = null, MenuName = "约束规则", MenuCode = "constraint_rule", MenuType = MenuType.Menu, Path = "/platform/constraint-rule", Component = "System/ConstraintRule/Index", RouteName = "SystemConstraintRule", Icon = "scale", Title = "约束规则", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 300 },
            new() { ParentId = null, MenuName = "菜单管理", MenuCode = "menu", MenuType = MenuType.Menu, Path = "/platform/menu", Component = "System/Menu/Index", RouteName = "SystemMenu", Icon = "menu", Title = "菜单管理", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 400 },
            new() { ParentId = null, MenuName = "参数配置", MenuCode = "config", MenuType = MenuType.Menu, Path = "/platform/config", Component = "System/Config/Index", RouteName = "SystemConfig", Icon = "sliders-horizontal", Title = "参数配置", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 500 },
            new() { ParentId = null, MenuName = "字典管理", MenuCode = "dict", MenuType = MenuType.Menu, Path = "/platform/dict", Component = "System/Dict/Index", RouteName = "SystemDict", Icon = "book-open", Title = "字典管理", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 600 },
            new() { ParentId = null, MenuName = "任务调度", MenuCode = "task", MenuType = MenuType.Menu, Path = "/platform/job", Component = "System/Task/Index", RouteName = "SystemTask", Icon = "clock", Title = "任务调度", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 700 },
            new() { ParentId = null, MenuName = "缓存管理", MenuCode = "cache", MenuType = MenuType.Menu, Path = "/platform/cache", Component = "System/Cache/Index", RouteName = "SystemCache", Icon = "database", Title = "缓存管理", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 800 },
            new() { ParentId = null, MenuName = "文件管理", MenuCode = "file", MenuType = MenuType.Menu, Path = "/platform/file", Component = "System/File/Index", RouteName = "SystemFile", Icon = "folder-open", Title = "文件管理", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 900 },
            new() { ParentId = null, MenuName = "系统监控", MenuCode = "monitor", MenuType = MenuType.Menu, Path = "/platform/server", Component = "System/Monitor/Index", RouteName = "SystemMonitor", Icon = "activity", Title = "系统监控", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 1000 },

            // 日志管理
            new() { ParentId = null, MenuName = "日志管理", MenuCode = "log", MenuType = MenuType.Directory, Path = "/log", Component = null, RouteName = null, Redirect = "/log/vislog", Icon = "file-text", Title = "日志管理", IsExternal = false, IsCache = false, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 8002 },
            new() { ParentId = null, MenuName = "访问日志", MenuCode = "access_log", MenuType = MenuType.Menu, Path = "/log/vislog", Component = "System/Log/Access", RouteName = "AccessLog", Icon = "globe", Title = "访问日志", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 100 },
            new() { ParentId = null, MenuName = "操作日志", MenuCode = "operation_log", MenuType = MenuType.Menu, Path = "/log/oplog", Component = "System/Log/Operation", RouteName = "OperationLog", Icon = "history", Title = "操作日志", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 200 },
            new() { ParentId = null, MenuName = "异常日志", MenuCode = "exception_log", MenuType = MenuType.Menu, Path = "/log/exlog", Component = "System/Log/Exception", RouteName = "ExceptionLog", Icon = "alert-triangle", Title = "异常日志", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 300 },
            new() { ParentId = null, MenuName = "差异日志", MenuCode = "audit_log", MenuType = MenuType.Menu, Path = "/log/difflog", Component = "System/Log/Audit", RouteName = "AuditLog", Icon = "file-diff", Title = "差异日志", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 400 },
            new() { ParentId = null, MenuName = "登录日志", MenuCode = "login_log", MenuType = MenuType.Menu, Path = "/log/loginlog", Component = "System/Log/Login", RouteName = "LoginLog", Icon = "log-in", Title = "登录日志", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 500 },
            new() { ParentId = null, MenuName = "调度日志", MenuCode = "task_log", MenuType = MenuType.Menu, Path = "/log/tasklog", Component = "System/Log/Task", RouteName = "TaskLog", Icon = "calendar-clock", Title = "调度日志", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 600 },
            new() { ParentId = null, MenuName = "接口日志", MenuCode = "api_log", MenuType = MenuType.Menu, Path = "/log/apilog", Component = "System/Log/Api", RouteName = "ApiLog", Icon = "shield-check", Title = "接口日志", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 700 },

            // 关于系统
            new() { ParentId = null, MenuName = "关于系统", MenuCode = "about", MenuType = MenuType.Menu, Path = "/about", Component = "About/Index", RouteName = "About", Icon = "info", Title = "关于系统", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 9999 },
        };

        await BulkInsertAsync(menus);
        await NormalizeMenuHierarchyAsync();

        Logger.LogInformation("成功初始化 {Count} 个系统菜单", menus.Count);
    }

    /// <summary>
    /// 标准化菜单层级
    /// </summary>
    private async Task NormalizeMenuHierarchyAsync()
    {
        await SetMenuAsRootAsync("workbench");
        await SetMenuAsRootAsync("messaging");
        await SetMenuAsRootAsync("system");
        await SetMenuAsRootAsync("platform");
        await SetMenuAsRootAsync("log");
        await SetMenuAsRootAsync("about");

        await UpdateMenuParentIdAsync("workbench", ["dashboard", "inbox"]);
        await UpdateMenuParentIdAsync("messaging", ["message", "email", "sms"]);
        await UpdateMenuParentIdAsync("system", ["user", "role", "department", "notice", "oauth_app", "user_session", "review"]);
        await UpdateMenuParentIdAsync("platform", ["tenant", "permission", "constraint_rule", "menu", "config", "dict", "task", "cache", "file", "monitor"]);
        await UpdateMenuParentIdAsync("log", ["access_log", "operation_log", "exception_log", "audit_log", "login_log", "task_log", "api_log"]);
    }

    private async Task SetMenuAsRootAsync(string menuCode)
    {
        await DbClient
            .Updateable<SysMenu>()
            .SetColumns(menu => menu.ParentId == null)
            .Where(menu => menu.MenuCode == menuCode)
            .ExecuteCommandAsync();
    }

    private async Task UpdateMenuParentIdAsync(string parentCode, string[] childCodes)
    {
        var parentMenu = await DbClient
            .Queryable<SysMenu>()
            .FirstAsync(m => m.MenuCode == parentCode);

        if (parentMenu == null)
        {
            return;
        }

        await DbClient
            .Updateable<SysMenu>()
            .SetColumns(m => m.ParentId == parentMenu.BasicId)
            .Where(m => childCodes.Contains(m.MenuCode))
            .ExecuteCommandAsync();
    }
}
