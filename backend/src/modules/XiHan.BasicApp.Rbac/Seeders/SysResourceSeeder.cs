#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysResourceSeeder
// Guid:2b3c4d5e-6f78-9012-bcde-112233445566
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/07 12:10:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Rbac.Domain.Enums;
using XiHan.BasicApp.Rbac.Domain.Entities;
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
    public override int Order => 1;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[Rbac]系统资源种子数据";

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
            new() { ParentId = null, ResourceCode = "system", ResourceName = "系统管理", ResourceType = ResourceType.Menu, ResourcePath = "/system", Icon = "setting", Description = "系统管理目录", IsRequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 1 },
            new() { ParentId = null, ResourceCode = "platform", ResourceName = "平台管理", ResourceType = ResourceType.Menu, ResourcePath = "/platform", Icon = "appstore", Description = "平台管理目录", IsRequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 2 },
            new() { ParentId = null, ResourceCode = "log", ResourceName = "日志管理", ResourceType = ResourceType.Menu, ResourcePath = "/log", Icon = "file-text", Description = "日志管理目录", IsRequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 3 },

            new() { ParentId = null, ResourceCode = "user", ResourceName = "账号管理", ResourceType = ResourceType.Menu, ResourcePath = "/system/user", Icon = "user", Description = "账号管理功能", IsRequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 100 },
            new() { ParentId = null, ResourceCode = "role", ResourceName = "角色管理", ResourceType = ResourceType.Menu, ResourcePath = "/system/role", Icon = "team", Description = "角色管理功能", IsRequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 101 },
            new() { ParentId = null, ResourceCode = "permission", ResourceName = "权限管理", ResourceType = ResourceType.Menu, ResourcePath = "/system/permission", Icon = "safety", Description = "权限管理功能", IsRequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 102 },
            new() { ParentId = null, ResourceCode = "department", ResourceName = "机构管理", ResourceType = ResourceType.Menu, ResourcePath = "/system/org", Icon = "apartment", Description = "机构管理功能", IsRequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 103 },
            new() { ParentId = null, ResourceCode = "notice", ResourceName = "通知公告", ResourceType = ResourceType.Menu, ResourcePath = "/system/notice", Icon = "notification", Description = "通知公告功能", IsRequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 104 },
            new() { ParentId = null, ResourceCode = "oauth_app", ResourceName = "三方账号", ResourceType = ResourceType.Menu, ResourcePath = "/system/weChatUser", Icon = "link", Description = "三方账号管理", IsRequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 105 },

            new() { ParentId = null, ResourceCode = "tenant", ResourceName = "租户管理", ResourceType = ResourceType.Menu, ResourcePath = "/platform/tenant", Icon = "cluster", Description = "租户管理功能", IsRequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 200 },
            new() { ParentId = null, ResourceCode = "menu", ResourceName = "菜单管理", ResourceType = ResourceType.Menu, ResourcePath = "/platform/menu", Icon = "menu", Description = "菜单管理功能", IsRequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 201 },
            new() { ParentId = null, ResourceCode = "config", ResourceName = "参数配置", ResourceType = ResourceType.Menu, ResourcePath = "/platform/config", Icon = "setting", Description = "系统参数配置", IsRequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 202 },
            new() { ParentId = null, ResourceCode = "dict", ResourceName = "字典管理", ResourceType = ResourceType.Menu, ResourcePath = "/platform/dict", Icon = "book", Description = "数据字典管理", IsRequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 203 },
            new() { ParentId = null, ResourceCode = "task", ResourceName = "任务调度", ResourceType = ResourceType.Menu, ResourcePath = "/platform/job", Icon = "clock-circle", Description = "任务调度管理", IsRequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 204 },
            new() { ParentId = null, ResourceCode = "monitor", ResourceName = "系统监控", ResourceType = ResourceType.Menu, ResourcePath = "/platform/server", Icon = "dashboard", Description = "系统监控功能", IsRequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 205 },
            new() { ParentId = null, ResourceCode = "cache", ResourceName = "缓存管理", ResourceType = ResourceType.Menu, ResourcePath = "/platform/cache", Icon = "database", Description = "缓存管理功能", IsRequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 206 },
            new() { ParentId = null, ResourceCode = "region", ResourceName = "行政区域", ResourceType = ResourceType.Menu, ResourcePath = "/platform/region", Icon = "global", Description = "行政区域管理", IsRequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 207 },
            new() { ParentId = null, ResourceCode = "file", ResourceName = "文件管理", ResourceType = ResourceType.Menu, ResourcePath = "/platform/file", Icon = "folder-open", Description = "文件管理功能", IsRequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 208 },
            new() { ParentId = null, ResourceCode = "plugin", ResourceName = "动态插件", ResourceType = ResourceType.Menu, ResourcePath = "/platform/plugin", Icon = "deployment-unit", Description = "动态插件管理", IsRequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 209 },

            new() { ParentId = null, ResourceCode = "access_log", ResourceName = "访问日志", ResourceType = ResourceType.Menu, ResourcePath = "/log/vislog", Icon = "global", Description = "访问日志查询", IsRequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 300 },
            new() { ParentId = null, ResourceCode = "operation_log", ResourceName = "操作日志", ResourceType = ResourceType.Menu, ResourcePath = "/log/oplog", Icon = "history", Description = "操作日志查询", IsRequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 301 },
            new() { ParentId = null, ResourceCode = "exception_log", ResourceName = "异常日志", ResourceType = ResourceType.Menu, ResourcePath = "/log/exlog", Icon = "warning", Description = "异常日志查询", IsRequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 302 },
            new() { ParentId = null, ResourceCode = "audit_log", ResourceName = "差异日志", ResourceType = ResourceType.Menu, ResourcePath = "/log/difflog", Icon = "diff", Description = "差异日志查询", IsRequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 303 },

            new() { ParentId = null, ResourceCode = "user_api", ResourceName = "用户API", ResourceType = ResourceType.Api, ResourcePath = "/api/users", Description = "用户管理API接口", IsRequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 500 },
            new() { ParentId = null, ResourceCode = "role_api", ResourceName = "角色API", ResourceType = ResourceType.Api, ResourcePath = "/api/roles", Description = "角色管理API接口", IsRequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 501 },
            new() { ParentId = null, ResourceCode = "permission_api", ResourceName = "权限API", ResourceType = ResourceType.Api, ResourcePath = "/api/permissions", Description = "权限管理API接口", IsRequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 502 },
            new() { ParentId = null, ResourceCode = "department_api", ResourceName = "机构API", ResourceType = ResourceType.Api, ResourcePath = "/api/departments", Description = "机构管理API接口", IsRequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 503 },
            new() { ParentId = null, ResourceCode = "tenant_api", ResourceName = "租户API", ResourceType = ResourceType.Api, ResourcePath = "/api/tenants", Description = "租户管理API接口", IsRequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 504 },
            new() { ParentId = null, ResourceCode = "menu_api", ResourceName = "菜单API", ResourceType = ResourceType.Api, ResourcePath = "/api/menus", Description = "菜单管理API接口", IsRequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 505 },
            new() { ParentId = null, ResourceCode = "dict_api", ResourceName = "字典API", ResourceType = ResourceType.Api, ResourcePath = "/api/dicts", Description = "字典管理API接口", IsRequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 506 },
            new() { ParentId = null, ResourceCode = "config_api", ResourceName = "配置API", ResourceType = ResourceType.Api, ResourcePath = "/api/configs", Description = "参数配置API接口", IsRequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 507 },
            new() { ParentId = null, ResourceCode = "file_api", ResourceName = "文件API", ResourceType = ResourceType.Api, ResourcePath = "/api/files", Description = "文件管理API接口", IsRequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 508 },
            new() { ParentId = null, ResourceCode = "access_log_api", ResourceName = "访问日志API", ResourceType = ResourceType.Api, ResourcePath = "/api/access-logs", Description = "访问日志API接口", IsRequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 600 },
            new() { ParentId = null, ResourceCode = "operation_log_api", ResourceName = "操作日志API", ResourceType = ResourceType.Api, ResourcePath = "/api/operation-logs", Description = "操作日志API接口", IsRequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 601 },
            new() { ParentId = null, ResourceCode = "exception_log_api", ResourceName = "异常日志API", ResourceType = ResourceType.Api, ResourcePath = "/api/exception-logs", Description = "异常日志API接口", IsRequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 602 },
            new() { ParentId = null, ResourceCode = "audit_log_api", ResourceName = "差异日志API", ResourceType = ResourceType.Api, ResourcePath = "/api/audit-logs", Description = "差异日志API接口", IsRequireAuth = true, IsPublic = false, Status = YesOrNo.Yes, Sort = 603 }
        };

        await BulkInsertAsync(resources);
        await UpdateResourceParentIdAsync("system", ["user", "role", "permission", "department", "notice", "oauth_app"]);
        await UpdateResourceParentIdAsync("platform", ["tenant", "menu", "config", "dict", "task", "monitor", "cache", "region", "file", "plugin"]);
        await UpdateResourceParentIdAsync("log", ["access_log", "operation_log", "exception_log", "audit_log"]);
        Logger.LogInformation("成功初始化 {Count} 个系统资源", resources.Count);
    }

    private async Task UpdateResourceParentIdAsync(string parentCode, string[] childCodes)
    {
        var parent = await DbContext.GetClient().Queryable<SysResource>().FirstAsync(r => r.ResourceCode == parentCode);
        if (parent == null)
        {
            return;
        }

        await DbContext.GetClient().Updateable<SysResource>().SetColumns(r => r.ParentId == parent.BasicId).Where(r => childCodes.Contains(r.ResourceCode)).ExecuteCommandAsync();
    }
}
