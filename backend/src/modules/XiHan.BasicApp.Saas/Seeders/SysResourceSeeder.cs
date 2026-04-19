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
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.Saas.Seeders;

/// <summary>
/// 系统资源种子数据
/// 资源是"被控制对象"（API/数据/文件），扁平结构，不含 UI（菜单/按钮）
/// </summary>
public class SysResourceSeeder : DataSeederBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public SysResourceSeeder(ISqlSugarClientResolver clientResolver, ILogger<SysResourceSeeder> logger, IServiceProvider serviceProvider)
        : base(clientResolver, logger, serviceProvider)
    {
    }

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => 1;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[Saas]系统资源种子数据";

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

        // 资源只包含 API/数据 等"被控制对象"，菜单/按钮在 SysMenu 中独立维护
        var resources = new List<SysResource>
        {
            // 系统管理 API
            new() { ResourceCode = "user", ResourceName = "用户管理", ResourceType = ResourceType.Api, ResourcePath = "/api/users", Description = "用户管理API接口", AccessLevel = ResourceAccessLevel.Authorized, Status = YesOrNo.Yes, Sort = 100 },
            new() { ResourceCode = "role", ResourceName = "角色管理", ResourceType = ResourceType.Api, ResourcePath = "/api/roles", Description = "角色管理API接口", AccessLevel = ResourceAccessLevel.Authorized, Status = YesOrNo.Yes, Sort = 101 },
            new() { ResourceCode = "department", ResourceName = "机构管理", ResourceType = ResourceType.Api, ResourcePath = "/api/departments", Description = "机构管理API接口", AccessLevel = ResourceAccessLevel.Authorized, Status = YesOrNo.Yes, Sort = 102 },
            new() { ResourceCode = "notice", ResourceName = "通知公告", ResourceType = ResourceType.Api, ResourcePath = "/api/notifications", Description = "通知公告API接口", AccessLevel = ResourceAccessLevel.Authorized, Status = YesOrNo.Yes, Sort = 103 },
            new() { ResourceCode = "oauth_app", ResourceName = "三方账号", ResourceType = ResourceType.Api, ResourcePath = "/api/oauth-apps", Description = "三方账号管理API接口", AccessLevel = ResourceAccessLevel.Authorized, Status = YesOrNo.Yes, Sort = 104 },
            new() { ResourceCode = "user_session", ResourceName = "会话管理", ResourceType = ResourceType.Api, ResourcePath = "/api/user-sessions", Description = "在线会话管理API接口", AccessLevel = ResourceAccessLevel.Authorized, Status = YesOrNo.Yes, Sort = 105 },
            new() { ResourceCode = "review", ResourceName = "审核管理", ResourceType = ResourceType.Api, ResourcePath = "/api/reviews", Description = "审核管理API接口", AccessLevel = ResourceAccessLevel.Authorized, Status = YesOrNo.Yes, Sort = 106 },

            // 平台管理 API
            new() { ResourceCode = "tenant", ResourceName = "租户管理", ResourceType = ResourceType.Api, ResourcePath = "/api/tenants", Description = "租户管理API接口", AccessLevel = ResourceAccessLevel.Authorized, Status = YesOrNo.Yes, Sort = 200 },
            new() { ResourceCode = "permission", ResourceName = "权限管理", ResourceType = ResourceType.Api, ResourcePath = "/api/permissions", Description = "权限管理API接口", AccessLevel = ResourceAccessLevel.Authorized, Status = YesOrNo.Yes, Sort = 201 },
            new() { ResourceCode = "menu", ResourceName = "菜单管理", ResourceType = ResourceType.Api, ResourcePath = "/api/menus", Description = "菜单管理API接口", AccessLevel = ResourceAccessLevel.Authorized, Status = YesOrNo.Yes, Sort = 202 },
            new() { ResourceCode = "config", ResourceName = "参数配置", ResourceType = ResourceType.Api, ResourcePath = "/api/configs", Description = "参数配置API接口", AccessLevel = ResourceAccessLevel.Authorized, Status = YesOrNo.Yes, Sort = 203 },
            new() { ResourceCode = "constraint_rule", ResourceName = "约束规则", ResourceType = ResourceType.Api, ResourcePath = "/api/constraint-rules", Description = "约束规则管理API接口", AccessLevel = ResourceAccessLevel.Authorized, Status = YesOrNo.Yes, Sort = 204 },
            new() { ResourceCode = "dict", ResourceName = "字典管理", ResourceType = ResourceType.Api, ResourcePath = "/api/dicts", Description = "数据字典管理API接口", AccessLevel = ResourceAccessLevel.Authorized, Status = YesOrNo.Yes, Sort = 205 },
            new() { ResourceCode = "task", ResourceName = "任务调度", ResourceType = ResourceType.Api, ResourcePath = "/api/tasks", Description = "任务调度管理API接口", AccessLevel = ResourceAccessLevel.Authorized, Status = YesOrNo.Yes, Sort = 206 },
            new() { ResourceCode = "monitor", ResourceName = "系统监控", ResourceType = ResourceType.Api, ResourcePath = "/api/monitors", Description = "系统监控API接口", AccessLevel = ResourceAccessLevel.Authorized, Status = YesOrNo.Yes, Sort = 207 },
            new() { ResourceCode = "cache", ResourceName = "缓存管理", ResourceType = ResourceType.Api, ResourcePath = "/api/caches", Description = "缓存管理API接口", AccessLevel = ResourceAccessLevel.Authorized, Status = YesOrNo.Yes, Sort = 208 },
            new() { ResourceCode = "region", ResourceName = "行政区域", ResourceType = ResourceType.Api, ResourcePath = "/api/regions", Description = "行政区域管理API接口", AccessLevel = ResourceAccessLevel.Authorized, Status = YesOrNo.Yes, Sort = 209 },
            new() { ResourceCode = "file", ResourceName = "文件管理", ResourceType = ResourceType.File, ResourcePath = "/api/files", Description = "文件管理API接口", AccessLevel = ResourceAccessLevel.Authorized, Status = YesOrNo.Yes, Sort = 210 },
            new() { ResourceCode = "plugin", ResourceName = "动态插件", ResourceType = ResourceType.Api, ResourcePath = "/api/plugins", Description = "动态插件管理API接口", AccessLevel = ResourceAccessLevel.Authorized, Status = YesOrNo.Yes, Sort = 211 },

            // 消息中心 API
            new() { ResourceCode = "message", ResourceName = "消息管理", ResourceType = ResourceType.Api, ResourcePath = "/api/messages", Description = "站内消息管理API接口", AccessLevel = ResourceAccessLevel.Authorized, Status = YesOrNo.Yes, Sort = 250 },
            new() { ResourceCode = "email", ResourceName = "邮件管理", ResourceType = ResourceType.Api, ResourcePath = "/api/emails", Description = "邮件发送管理API接口", AccessLevel = ResourceAccessLevel.Authorized, Status = YesOrNo.Yes, Sort = 251 },
            new() { ResourceCode = "sms", ResourceName = "短信管理", ResourceType = ResourceType.Api, ResourcePath = "/api/sms", Description = "短信发送管理API接口", AccessLevel = ResourceAccessLevel.Authorized, Status = YesOrNo.Yes, Sort = 252 },

            // 日志 API
            new() { ResourceCode = "access_log", ResourceName = "访问日志", ResourceType = ResourceType.Api, ResourcePath = "/api/access-logs", Description = "访问日志API接口", AccessLevel = ResourceAccessLevel.Authorized, Status = YesOrNo.Yes, Sort = 300 },
            new() { ResourceCode = "operation_log", ResourceName = "操作日志", ResourceType = ResourceType.Api, ResourcePath = "/api/operation-logs", Description = "操作日志API接口", AccessLevel = ResourceAccessLevel.Authorized, Status = YesOrNo.Yes, Sort = 301 },
            new() { ResourceCode = "exception_log", ResourceName = "异常日志", ResourceType = ResourceType.Api, ResourcePath = "/api/exception-logs", Description = "异常日志API接口", AccessLevel = ResourceAccessLevel.Authorized, Status = YesOrNo.Yes, Sort = 302 },
            new() { ResourceCode = "audit_log", ResourceName = "差异日志", ResourceType = ResourceType.Api, ResourcePath = "/api/audit-logs", Description = "差异日志API接口", AccessLevel = ResourceAccessLevel.Authorized, Status = YesOrNo.Yes, Sort = 303 },
            new() { ResourceCode = "login_log", ResourceName = "登录日志", ResourceType = ResourceType.Api, ResourcePath = "/api/login-logs", Description = "登录日志API接口", AccessLevel = ResourceAccessLevel.Authorized, Status = YesOrNo.Yes, Sort = 304 },
            new() { ResourceCode = "task_log", ResourceName = "调度日志", ResourceType = ResourceType.Api, ResourcePath = "/api/task-logs", Description = "调度日志API接口", AccessLevel = ResourceAccessLevel.Authorized, Status = YesOrNo.Yes, Sort = 305 },
            new() { ResourceCode = "api_log", ResourceName = "接口日志", ResourceType = ResourceType.Api, ResourcePath = "/api/api-logs", Description = "接口安全日志API接口", AccessLevel = ResourceAccessLevel.Authorized, Status = YesOrNo.Yes, Sort = 306 },
        };

        await BulkInsertAsync(resources);
        Logger.LogInformation("成功初始化 {Count} 个系统资源", resources.Count);
    }
}
