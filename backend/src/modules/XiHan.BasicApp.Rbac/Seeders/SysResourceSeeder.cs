#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysResourceSeeder
// Guid:2b3c4d5e-6f78-9012-bcde-112233445566
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026-01-07 12:10:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.Rbac.Seeders;

/// <summary>
/// 系统资源种子数据
/// </summary>
public class SysResourceSeeder : DataSeederBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public SysResourceSeeder(ISqlSugarDbContext dbContext, ILogger<SysResourceSeeder> logger, IServiceProvider serviceProvider)
        : base(dbContext, logger, serviceProvider)
    {
    }

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => 2;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "系统资源种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        if (await HasDataAsync<SysResource>(r => true))
        {
            Logger.LogInformation("系统资源数据已存在，跳过种子数据");
            return;
        }

        var resources = new List<SysResource>
        {
            // 系统管理模块
            new() { ParentId = null, ResourceCode = "system", ResourceName = "系统管理", ResourceType = ResourceType.Menu, ResourcePath = "/system", Icon = "setting", Description = "系统管理模块", RequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 1 },
            
            // 用户管理
            new() { ParentId = null, ResourceCode = "user", ResourceName = "用户管理", ResourceType = ResourceType.Menu, ResourcePath = "/system/user", Icon = "user", Description = "用户管理功能", RequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 10 },
            new() { ParentId = null, ResourceCode = "user_api", ResourceName = "用户API", ResourceType = ResourceType.Api, ResourcePath = "/api/users", Description = "用户管理API接口", RequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 11 },
            
            // 角色管理
            new() { ParentId = null, ResourceCode = "role", ResourceName = "角色管理", ResourceType = ResourceType.Menu, ResourcePath = "/system/role", Icon = "team", Description = "角色管理功能", RequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 20 },
            new() { ParentId = null, ResourceCode = "role_api", ResourceName = "角色API", ResourceType = ResourceType.Api, ResourcePath = "/api/roles", Description = "角色管理API接口", RequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 21 },
            
            // 权限管理
            new() { ParentId = null, ResourceCode = "permission", ResourceName = "权限管理", ResourceType = ResourceType.Menu, ResourcePath = "/system/permission", Icon = "safety", Description = "权限管理功能", RequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 30 },
            new() { ParentId = null, ResourceCode = "permission_api", ResourceName = "权限API", ResourceType = ResourceType.Api, ResourcePath = "/api/permissions", Description = "权限管理API接口", RequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 31 },
            
            // 菜单管理
            new() { ParentId = null, ResourceCode = "menu", ResourceName = "菜单管理", ResourceType = ResourceType.Menu, ResourcePath = "/system/menu", Icon = "menu", Description = "菜单管理功能", RequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 40 },
            new() { ParentId = null, ResourceCode = "menu_api", ResourceName = "菜单API", ResourceType = ResourceType.Api, ResourcePath = "/api/menus", Description = "菜单管理API接口", RequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 41 },
            
            // 部门管理
            new() { ParentId = null, ResourceCode = "department", ResourceName = "部门管理", ResourceType = ResourceType.Menu, ResourcePath = "/system/department", Icon = "apartment", Description = "部门管理功能", RequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 50 },
            new() { ParentId = null, ResourceCode = "department_api", ResourceName = "部门API", ResourceType = ResourceType.Api, ResourcePath = "/api/departments", Description = "部门管理API接口", RequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 51 },
            
            // 租户管理
            new() { ParentId = null, ResourceCode = "tenant", ResourceName = "租户管理", ResourceType = ResourceType.Menu, ResourcePath = "/system/tenant", Icon = "cluster", Description = "租户管理功能", RequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 60 },
            
            // 日志管理
            new() { ParentId = null, ResourceCode = "log", ResourceName = "日志管理", ResourceType = ResourceType.Menu, ResourcePath = "/system/log", Icon = "file-text", Description = "系统日志管理", RequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 70 },
            new() { ParentId = null, ResourceCode = "login_log", ResourceName = "登录日志", ResourceType = ResourceType.Menu, ResourcePath = "/system/log/login", Icon = "login", Description = "登录日志查询", RequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 71 },
            new() { ParentId = null, ResourceCode = "operation_log", ResourceName = "操作日志", ResourceType = ResourceType.Menu, ResourcePath = "/system/log/operation", Icon = "history", Description = "操作日志查询", RequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 72 },
            
            // 系统监控
            new() { ParentId = null, ResourceCode = "monitor", ResourceName = "系统监控", ResourceType = ResourceType.Menu, ResourcePath = "/system/monitor", Icon = "dashboard", Description = "系统监控功能", RequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 80 },
            
            // 配置管理
            new() { ParentId = null, ResourceCode = "config", ResourceName = "配置管理", ResourceType = ResourceType.Menu, ResourcePath = "/system/config", Icon = "tool", Description = "系统配置管理", RequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 90 },
            new() { ParentId = null, ResourceCode = "dict", ResourceName = "字典管理", ResourceType = ResourceType.Menu, ResourcePath = "/system/dict", Icon = "book", Description = "数据字典管理", RequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 91 }
        };

        await BulkInsertAsync(resources);
        Logger.LogInformation($"成功初始化 {resources.Count} 个系统资源");
    }
}
