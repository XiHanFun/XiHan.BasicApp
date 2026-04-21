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
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.Saas.Seeders;

/// <summary>
/// 系统资源种子数据。
/// </summary>
public class SysResourceSeeder : DataSeederBase
{
    public SysResourceSeeder(
        ISqlSugarClientResolver clientResolver,
        ILogger<SysResourceSeeder> logger,
        IServiceProvider serviceProvider)
        : base(clientResolver, logger, serviceProvider)
    {
    }

    public override int Order => SaasSeedOrder.Resources;

    public override string Name => "[Saas]系统资源种子数据";

    protected override async Task SeedInternalAsync()
    {
        var templates = BuildTemplates();
        var resourceCodes = templates.Select(item => item.ResourceCode).ToArray();
        var existingResources = await DbClient
            .Queryable<SysResource>()
            .Where(resource => resourceCodes.Contains(resource.ResourceCode))
            .ToListAsync();

        var existingMap = existingResources.ToDictionary(resource => resource.ResourceCode, StringComparer.OrdinalIgnoreCase);
        var toInsert = templates
            .Where(template => !existingMap.ContainsKey(template.ResourceCode))
            .ToList();

        if (toInsert.Count > 0)
        {
            await BulkInsertAsync(toInsert);
        }

        var toEnableIds = existingResources
            .Where(resource =>
                resource.TenantId == SaasSeedDefaults.PlatformTenantId
                && resource.IsGlobal
                && resource.Status != YesOrNo.Yes)
            .Select(resource => resource.BasicId)
            .ToArray();

        if (toEnableIds.Length > 0)
        {
            await DbClient
                .Updateable<SysResource>()
                .SetColumns(resource => resource.Status == YesOrNo.Yes)
                .Where(resource => toEnableIds.Contains(resource.BasicId))
                .ExecuteCommandAsync();
        }

        Logger.LogInformation(
            "系统资源模板种子完成：新增 {InsertCount} 项，启用 {EnableCount} 项",
            toInsert.Count,
            toEnableIds.Length);
    }

    private static List<SysResource> BuildTemplates()
    {
        return
        [
            Create("tenant", "租户管理", "/api/tenants", "租户生命周期与隔离配置管理接口", ResourceType.Api, 100),
            Create("user", "用户管理", "/api/users", "用户身份、资料与租户成员协作入口", ResourceType.Api, 110),
            Create("role", "角色管理", "/api/roles", "角色、继承关系与数据范围管理接口", ResourceType.Api, 120),
            Create("permission", "权限管理", "/api/permissions", "权限原子点与授权矩阵管理接口", ResourceType.Api, 130),
            Create("menu", "菜单管理", "/api/menus", "菜单模板与导航结构管理接口", ResourceType.Api, 140),
            Create("department", "部门管理", "/api/departments", "组织架构与闭包树管理接口", ResourceType.Api, 150),
            Create("constraint_rule", "约束规则", "/api/constraint-rules", "SSD/DSD 等约束规则管理接口", ResourceType.Api, 160),
            Create("config", "配置中心", "/api/configs", "SaaS 运行配置与租户覆盖接口", ResourceType.Api, 170),
            Create("dict", "字典管理", "/api/dicts", "字典与字典项管理接口", ResourceType.Api, 180),
            Create("notification", "通知公告", "/api/notifications", "通知公告管理接口", ResourceType.Api, 190),
            Create("message", "站内消息", "/api/messages", "站内消息收发与未读管理接口", ResourceType.Api, 200),
            Create("email", "邮件消息", "/api/emails", "邮件投递与审计接口", ResourceType.Api, 210),
            Create("sms", "短信消息", "/api/sms", "短信投递与审计接口", ResourceType.Api, 220),
            Create("oauth_app", "三方应用", "/api/oauth-apps", "OAuth 应用与外部集成接口", ResourceType.Api, 230),
            Create("user_session", "用户会话", "/api/user-sessions", "登录会话与设备策略接口", ResourceType.Api, 240),
            Create("review", "审核流程", "/api/reviews", "审核单据与审批结果接口", ResourceType.Api, 250),
            Create("task", "任务调度", "/api/tasks", "调度任务与触发管理接口", ResourceType.Api, 260),
            Create("file", "文件资源", "/api/files", "文件上传下载与元数据接口", ResourceType.File, 270),
            Create("cache", "缓存管理", "/api/caches", "缓存查询与清理接口", ResourceType.Api, 280),
            Create("monitor", "系统监控", "/api/monitors", "系统监控与节点诊断接口", ResourceType.Api, 290),
            Create("access_log", "访问日志", "/api/access-logs", "访问日志查询接口", ResourceType.Api, 300),
            Create("operation_log", "操作日志", "/api/operation-logs", "操作日志查询接口", ResourceType.Api, 310),
            Create("exception_log", "异常日志", "/api/exception-logs", "异常日志查询接口", ResourceType.Api, 320),
            Create("audit_log", "审计日志", "/api/audit-logs", "审计日志查询接口", ResourceType.Api, 330),
            Create("login_log", "登录日志", "/api/login-logs", "登录日志查询接口", ResourceType.Api, 340),
            Create("task_log", "调度日志", "/api/task-logs", "调度执行日志接口", ResourceType.Api, 350)
        ];
    }

    private static SysResource Create(
        string code,
        string name,
        string path,
        string description,
        ResourceType resourceType,
        int sort)
    {
        return new SysResource
        {
            TenantId = SaasSeedDefaults.PlatformTenantId,
            IsGlobal = true,
            ResourceCode = code,
            ResourceName = name,
            ResourceType = resourceType,
            ResourcePath = path,
            Description = description,
            AccessLevel = ResourceAccessLevel.Authorized,
            Status = YesOrNo.Yes,
            Sort = sort
        };
    }
}
