#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysMenuSeeder
// Guid:6f789012-3456-7890-0123-556677889900
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026-01-07 12:50:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
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
    public SysMenuSeeder(ISqlSugarDbContext dbContext, ILogger<SysMenuSeeder> logger, IServiceProvider serviceProvider)
        : base(dbContext, logger, serviceProvider)
    {
    }

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => 14;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "系统菜单种子数据";

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

        var resources = await DbContext.GetClient().Queryable<SysResource>().ToListAsync();
        if (resources.Count == 0)
        {
            Logger.LogWarning("资源数据不存在，跳过菜单种子数据");
            return;
        }

        // 获取资源ID映射
        var resourceMap = resources.ToDictionary(r => r.ResourceCode, r => r.BasicId);

        var menus = new List<SysMenu>
        {
            // 首页
            new()
            {
                ResourceId = null,
                ParentId = null,
                MenuName = "首页",
                MenuCode = "dashboard",
                MenuType = MenuType.Menu,
                Path = "/dashboard",
                Component = "Dashboard/Index",
                RouteName = "Dashboard",
                Icon = "dashboard",
                Title = "首页",
                IsExternal = false,
                IsCache = true,
                IsVisible = true,
                IsAffix = true,
                Status = YesOrNo.Yes,
                Sort = 1
            },

            // 系统管理（目录）
            new()
            {
                ResourceId = GetResourceId(resourceMap, "system"),
                ParentId = null,
                MenuName = "系统管理",
                MenuCode = "system",
                MenuType = MenuType.Directory,
                Path = "/system",
                Component = null,
                RouteName = null,
                Icon = "setting",
                Title = "系统管理",
                IsExternal = false,
                IsCache = false,
                IsVisible = true,
                IsAffix = false,
                Status = YesOrNo.Yes,
                Sort = 100
            },

            // 用户管理
            new()
            {
                ResourceId = GetResourceId(resourceMap, "user"),
                ParentId = null, // 将在插入后更新
                MenuName = "用户管理",
                MenuCode = "user",
                MenuType = MenuType.Menu,
                Path = "/system/user",
                Component = "System/User/Index",
                RouteName = "SystemUser",
                Icon = "user",
                Title = "用户管理",
                IsExternal = false,
                IsCache = true,
                IsVisible = true,
                IsAffix = false,
                Status = YesOrNo.Yes,
                Sort = 101
            },

            // 角色管理
            new()
            {
                ResourceId = GetResourceId(resourceMap, "role"),
                ParentId = null,
                MenuName = "角色管理",
                MenuCode = "role",
                MenuType = MenuType.Menu,
                Path = "/system/role",
                Component = "System/Role/Index",
                RouteName = "SystemRole",
                Icon = "team",
                Title = "角色管理",
                IsExternal = false,
                IsCache = true,
                IsVisible = true,
                IsAffix = false,
                Status = YesOrNo.Yes,
                Sort = 102
            },

            // 权限管理
            new()
            {
                ResourceId = GetResourceId(resourceMap, "permission"),
                ParentId = null,
                MenuName = "权限管理",
                MenuCode = "permission",
                MenuType = MenuType.Menu,
                Path = "/system/permission",
                Component = "System/Permission/Index",
                RouteName = "SystemPermission",
                Icon = "safety",
                Title = "权限管理",
                IsExternal = false,
                IsCache = true,
                IsVisible = true,
                IsAffix = false,
                Status = YesOrNo.Yes,
                Sort = 103
            },

            // 菜单管理
            new()
            {
                ResourceId = GetResourceId(resourceMap, "menu"),
                ParentId = null,
                MenuName = "菜单管理",
                MenuCode = "menu",
                MenuType = MenuType.Menu,
                Path = "/system/menu",
                Component = "System/Menu/Index",
                RouteName = "SystemMenu",
                Icon = "menu",
                Title = "菜单管理",
                IsExternal = false,
                IsCache = true,
                IsVisible = true,
                IsAffix = false,
                Status = YesOrNo.Yes,
                Sort = 104
            },

            // 部门管理
            new()
            {
                ResourceId = GetResourceId(resourceMap, "department"),
                ParentId = null,
                MenuName = "部门管理",
                MenuCode = "department",
                MenuType = MenuType.Menu,
                Path = "/system/department",
                Component = "System/Department/Index",
                RouteName = "SystemDepartment",
                Icon = "apartment",
                Title = "部门管理",
                IsExternal = false,
                IsCache = true,
                IsVisible = true,
                IsAffix = false,
                Status = YesOrNo.Yes,
                Sort = 105
            },

            // 租户管理
            new()
            {
                ResourceId = GetResourceId(resourceMap, "tenant"),
                ParentId = null,
                MenuName = "租户管理",
                MenuCode = "tenant",
                MenuType = MenuType.Menu,
                Path = "/system/tenant",
                Component = "System/Tenant/Index",
                RouteName = "SystemTenant",
                Icon = "cluster",
                Title = "租户管理",
                IsExternal = false,
                IsCache = true,
                IsVisible = true,
                IsAffix = false,
                Status = YesOrNo.Yes,
                Sort = 106
            },

            // 日志管理（目录）
            new()
            {
                ResourceId = GetResourceId(resourceMap, "log"),
                ParentId = null,
                MenuName = "日志管理",
                MenuCode = "log",
                MenuType = MenuType.Directory,
                Path = "/system/log",
                Component = null,
                RouteName = null,
                Icon = "file-text",
                Title = "日志管理",
                IsExternal = false,
                IsCache = false,
                IsVisible = true,
                IsAffix = false,
                Status = YesOrNo.Yes,
                Sort = 200
            },

            // 登录日志
            new()
            {
                ResourceId = GetResourceId(resourceMap, "login_log"),
                ParentId = null,
                MenuName = "登录日志",
                MenuCode = "login_log",
                MenuType = MenuType.Menu,
                Path = "/system/log/login",
                Component = "System/Log/Login",
                RouteName = "LoginLog",
                Icon = "login",
                Title = "登录日志",
                IsExternal = false,
                IsCache = true,
                IsVisible = true,
                IsAffix = false,
                Status = YesOrNo.Yes,
                Sort = 201
            },

            // 操作日志
            new()
            {
                ResourceId = GetResourceId(resourceMap, "operation_log"),
                ParentId = null,
                MenuName = "操作日志",
                MenuCode = "operation_log",
                MenuType = MenuType.Menu,
                Path = "/system/log/operation",
                Component = "System/Log/Operation",
                RouteName = "OperationLog",
                Icon = "history",
                Title = "操作日志",
                IsExternal = false,
                IsCache = true,
                IsVisible = true,
                IsAffix = false,
                Status = YesOrNo.Yes,
                Sort = 202
            },

            // 系统监控
            new()
            {
                ResourceId = GetResourceId(resourceMap, "monitor"),
                ParentId = null,
                MenuName = "系统监控",
                MenuCode = "monitor",
                MenuType = MenuType.Menu,
                Path = "/system/monitor",
                Component = "System/Monitor/Index",
                RouteName = "SystemMonitor",
                Icon = "dashboard",
                Title = "系统监控",
                IsExternal = false,
                IsCache = true,
                IsVisible = true,
                IsAffix = false,
                Status = YesOrNo.Yes,
                Sort = 300
            },

            // 配置管理
            new()
            {
                ResourceId = GetResourceId(resourceMap, "config"),
                ParentId = null,
                MenuName = "配置管理",
                MenuCode = "config",
                MenuType = MenuType.Menu,
                Path = "/system/config",
                Component = "System/Config/Index",
                RouteName = "SystemConfig",
                Icon = "tool",
                Title = "配置管理",
                IsExternal = false,
                IsCache = true,
                IsVisible = true,
                IsAffix = false,
                Status = YesOrNo.Yes,
                Sort = 400
            },

            // 字典管理
            new()
            {
                ResourceId = GetResourceId(resourceMap, "dict"),
                ParentId = null,
                MenuName = "字典管理",
                MenuCode = "dict",
                MenuType = MenuType.Menu,
                Path = "/system/dict",
                Component = "System/Dict/Index",
                RouteName = "SystemDict",
                Icon = "book",
                Title = "字典管理",
                IsExternal = false,
                IsCache = true,
                IsVisible = true,
                IsAffix = false,
                Status = YesOrNo.Yes,
                Sort = 401
            }
        };

        await BulkInsertAsync(menus);

        // 更新子菜单的 ParentId
        await UpdateMenuParentIdAsync("system", new[] { "user", "role", "permission", "menu", "department", "tenant" });
        await UpdateMenuParentIdAsync("log", new[] { "login_log", "operation_log" });

        Logger.LogInformation($"成功初始化 {menus.Count} 个系统菜单");
    }

    /// <summary>
    /// 获取资源ID
    /// </summary>
    private static long? GetResourceId(Dictionary<string, long> resourceMap, string resourceCode)
    {
        return resourceMap.TryGetValue(resourceCode, out var id) ? id : null;
    }

    /// <summary>
    /// 更新菜单的父级ID
    /// </summary>
    private async Task UpdateMenuParentIdAsync(string parentCode, string[] childCodes)
    {
        var parentMenu = await DbContext.GetClient().Queryable<SysMenu>().FirstAsync(m => m.MenuCode == parentCode);
        if (parentMenu != null)
        {
            await DbContext.GetClient().Updateable<SysMenu>()
                .SetColumns(m => m.ParentId == parentMenu.BasicId)
                .Where(m => childCodes.Contains(m.MenuCode))
                .ExecuteCommandAsync();
        }
    }
}
