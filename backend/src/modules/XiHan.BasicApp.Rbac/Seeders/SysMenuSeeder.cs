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
using XiHan.BasicApp.Rbac.Domain.Enums;
using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.Rbac.Seeders;

/// <summary>
/// 系统菜单种子数据
/// </summary>
public class SysMenuSeeder : DataSeederBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public SysMenuSeeder(ISqlSugarClientProvider clientProvider, ILogger<SysMenuSeeder> logger, IServiceProvider serviceProvider)
        : base(clientProvider, logger, serviceProvider)
    {
    }

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => 14;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[Rbac]系统菜单种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        if (await HasDataAsync<SysMenu>(m => true))
        {
            Logger.LogInformation("系统菜单数据已存在，跳过种子数据");
            return;
        }

        var resources = await ClientProvider.GetClient().Queryable<SysResource>().ToListAsync();
        if (resources.Count == 0)
        {
            Logger.LogWarning("资源数据不存在，跳过菜单种子数据");
            return;
        }

        var resourceMap = resources.ToDictionary(r => r.ResourceCode, r => r.BasicId);
        var menus = new List<SysMenu>
        {
            new() { ResourceId = null, ParentId = null, MenuName = "首页", MenuCode = "dashboard", MenuType = MenuType.Menu, Path = "/dashboard", Component = "Dashboard/Index", RouteName = "Dashboard", Icon = "dashboard", Title = "首页", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = true, Status = YesOrNo.Yes, Sort = 1 },

            new() { ResourceId = GetResourceId(resourceMap, "system"), ParentId = null, MenuName = "系统管理", MenuCode = "system", MenuType = MenuType.Directory, Path = "/system", Component = null, RouteName = null, Icon = "setting", Title = "系统管理", IsExternal = false, IsCache = false, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 100 },
            new() { ResourceId = GetResourceId(resourceMap, "user"), ParentId = null, MenuName = "账号管理", MenuCode = "user", MenuType = MenuType.Menu, Path = "/system/user", Component = "System/User/Index", RouteName = "SystemUser", Icon = "user", Title = "账号管理", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 101 },
            new() { ResourceId = GetResourceId(resourceMap, "role"), ParentId = null, MenuName = "角色管理", MenuCode = "role", MenuType = MenuType.Menu, Path = "/system/role", Component = "System/Role/Index", RouteName = "SystemRole", Icon = "team", Title = "角色管理", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 102 },
            new() { ResourceId = GetResourceId(resourceMap, "department"), ParentId = null, MenuName = "机构管理", MenuCode = "department", MenuType = MenuType.Menu, Path = "/system/org", Component = "System/Department/Index", RouteName = "SystemDepartment", Icon = "apartment", Title = "机构管理", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 103 },
            new() { ResourceId = GetResourceId(resourceMap, "permission"), ParentId = null, MenuName = "权限管理", MenuCode = "permission", MenuType = MenuType.Menu, Path = "/system/permission", Component = "System/Permission/Index", RouteName = "SystemPermission", Icon = "safety", Title = "权限管理", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 104 },
            new() { ResourceId = GetResourceId(resourceMap, "notice"), ParentId = null, MenuName = "通知公告", MenuCode = "notice", MenuType = MenuType.Menu, Path = "/system/notice", Component = "System/Notification/Index", RouteName = "SystemNotice", Icon = "notification", Title = "通知公告", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 105 },
            new() { ResourceId = GetResourceId(resourceMap, "oauth_app"), ParentId = null, MenuName = "三方账号", MenuCode = "oauth_app", MenuType = MenuType.Menu, Path = "/system/weChatUser", Component = "System/OAuthApp/Index", RouteName = "SystemOAuthApp", Icon = "link", Title = "三方账号", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 106 },

            new() { ResourceId = GetResourceId(resourceMap, "platform"), ParentId = null, MenuName = "平台管理", MenuCode = "platform", MenuType = MenuType.Directory, Path = "/platform", Component = null, RouteName = null, Icon = "appstore", Title = "平台管理", IsExternal = false, IsCache = false, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 200 },
            new() { ResourceId = GetResourceId(resourceMap, "tenant"), ParentId = null, MenuName = "租户管理", MenuCode = "tenant", MenuType = MenuType.Menu, Path = "/platform/tenant", Component = "System/Tenant/Index", RouteName = "SystemTenant", Icon = "cluster", Title = "租户管理", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 201 },
            new() { ResourceId = GetResourceId(resourceMap, "menu"), ParentId = null, MenuName = "菜单管理", MenuCode = "menu", MenuType = MenuType.Menu, Path = "/platform/menu", Component = "System/Menu/Index", RouteName = "SystemMenu", Icon = "menu", Title = "菜单管理", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 202 },
            new() { ResourceId = GetResourceId(resourceMap, "config"), ParentId = null, MenuName = "参数配置", MenuCode = "config", MenuType = MenuType.Menu, Path = "/platform/config", Component = "System/Config/Index", RouteName = "SystemConfig", Icon = "setting", Title = "参数配置", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 203 },
            new() { ResourceId = GetResourceId(resourceMap, "dict"), ParentId = null, MenuName = "字典管理", MenuCode = "dict", MenuType = MenuType.Menu, Path = "/platform/dict", Component = "System/Dict/Index", RouteName = "SystemDict", Icon = "book", Title = "字典管理", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 204 },
            new() { ResourceId = GetResourceId(resourceMap, "task"), ParentId = null, MenuName = "任务调度", MenuCode = "task", MenuType = MenuType.Menu, Path = "/platform/job", Component = "System/Task/Index", RouteName = "SystemTask", Icon = "clock-circle", Title = "任务调度", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 205 },
            new() { ResourceId = GetResourceId(resourceMap, "monitor"), ParentId = null, MenuName = "系统监控", MenuCode = "monitor", MenuType = MenuType.Menu, Path = "/platform/server", Component = "System/Monitor/Index", RouteName = "SystemMonitor", Icon = "dashboard", Title = "系统监控", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 206 },
            new() { ResourceId = GetResourceId(resourceMap, "file"), ParentId = null, MenuName = "文件管理", MenuCode = "file", MenuType = MenuType.Menu, Path = "/platform/file", Component = "System/File/Index", RouteName = "SystemFile", Icon = "folder-open", Title = "文件管理", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 207 },

            new() { ResourceId = GetResourceId(resourceMap, "log"), ParentId = null, MenuName = "日志管理", MenuCode = "log", MenuType = MenuType.Directory, Path = "/log", Component = null, RouteName = null, Icon = "file-text", Title = "日志管理", IsExternal = false, IsCache = false, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 300 },
            new() { ResourceId = GetResourceId(resourceMap, "access_log"), ParentId = null, MenuName = "访问日志", MenuCode = "access_log", MenuType = MenuType.Menu, Path = "/log/vislog", Component = "System/Log/Access", RouteName = "AccessLog", Icon = "global", Title = "访问日志", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 301 },
            new() { ResourceId = GetResourceId(resourceMap, "operation_log"), ParentId = null, MenuName = "操作日志", MenuCode = "operation_log", MenuType = MenuType.Menu, Path = "/log/oplog", Component = "System/Log/Operation", RouteName = "OperationLog", Icon = "history", Title = "操作日志", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 302 },
            new() { ResourceId = GetResourceId(resourceMap, "exception_log"), ParentId = null, MenuName = "异常日志", MenuCode = "exception_log", MenuType = MenuType.Menu, Path = "/log/exlog", Component = "System/Log/Exception", RouteName = "ExceptionLog", Icon = "warning", Title = "异常日志", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 303 },
            new() { ResourceId = GetResourceId(resourceMap, "audit_log"), ParentId = null, MenuName = "差异日志", MenuCode = "audit_log", MenuType = MenuType.Menu, Path = "/log/difflog", Component = "System/Log/Audit", RouteName = "AuditLog", Icon = "diff", Title = "差异日志", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = YesOrNo.Yes, Sort = 304 }
        };

        await BulkInsertAsync(menus);
        await UpdateMenuParentIdAsync("system", ["user", "role", "department", "permission", "notice", "oauth_app"]);
        await UpdateMenuParentIdAsync("platform", ["tenant", "menu", "config", "dict", "task", "monitor", "file"]);
        await UpdateMenuParentIdAsync("log", ["access_log", "operation_log", "exception_log", "audit_log"]);
        Logger.LogInformation("成功初始化 {Count} 个系统菜单", menus.Count);
    }

    private static long? GetResourceId(Dictionary<string, long> resourceMap, string resourceCode) => resourceMap.TryGetValue(resourceCode, out var id) ? id : null;

    private async Task UpdateMenuParentIdAsync(string parentCode, string[] childCodes)
    {
        var parentMenu = await ClientProvider.GetClient().Queryable<SysMenu>().FirstAsync(m => m.MenuCode == parentCode);
        if (parentMenu == null)
        {
            return;
        }

        await ClientProvider.GetClient().Updateable<SysMenu>().SetColumns(m => m.ParentId == parentMenu.BasicId).Where(m => childCodes.Contains(m.MenuCode)).ExecuteCommandAsync();
    }
}
