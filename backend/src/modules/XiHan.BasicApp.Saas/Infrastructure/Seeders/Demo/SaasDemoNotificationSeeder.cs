// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Seeders;

/// <summary>
/// SaaS 演示通知：平台的上手指南，以及企业版演示租户里按部门、按角色定向的通知
/// </summary>
/// <remarks>
/// 通知都以草稿写入：发布会按目标展开收件人，走一遍发布就能看到横幅、弹窗、必读与确认的效果。
/// 按标题判断写过没有（连同删掉的一起算），已有的不覆盖。
/// </remarks>
public sealed class SaasDemoNotificationSeeder(
    ISqlSugarClientResolver clientResolver,
    ILogger<SaasDemoNotificationSeeder> logger,
    IServiceProvider serviceProvider)
    : DemoDataSeederBase(clientResolver, logger, serviceProvider)
{
    private const string GuideTitle = "XiHan BasicApp 系统功能总览与上手指南";

    /// <summary>
    /// 系统功能总览公告正文（Markdown）。仅描述当前真实存在并可用的模块能力。
    /// </summary>
    private const string SystemOverviewContent =
        """
        # 欢迎使用 XiHan BasicApp

        XiHan BasicApp 是一套面向 B2B 的多租户中后台基础平台，提供从身份权限到业务支撑的完整底座。本指南带您快速了解系统的各项能力与上手路径。

        ---

        ## 一、平台与租户

        - **多租户隔离**：平台租户（系统内置）与业务租户并存，数据按租户隔离；全局数据归属平台租户，业务数据归属各自租户。
        - **版本套餐**：内置免费版 / 基础版 / 专业版 / 企业版四档，按版本控制用户数、存储容量与可用功能白名单。
        - **租户管理**：开通、停用、配额调整、版本升级，以及跨租户成员（外部协作 / 顾问）授权。

        ## 二、组织与身份

        - **组织架构**：支持多级部门树（公司 → 部门 → 子部门），闭包表维护层级路径。
        - **用户管理**：账号、资料、部门归属、多角色绑定，账号启停与安全设置。
        - **角色管理**：按业务岗位定义角色，绑定权限码与数据范围。

        ## 三、权限体系（RBAC + ABAC + 字段级安全）

        - **功能权限（RBAC）**：以权限码（如 `saas:user:read`、`saas:role:create`）控制菜单与操作按钮的可见与可用。
        - **数据范围（ABAC）**：角色可配置数据可见范围 —— 全部 / 本部门 / 本部门及下级 / 仅本人 / 自定义部门，成员还能单独覆盖，越权请求在服务端被拦截。
        - **字段级安全（FLS）**：对敏感字段按角色控制可读与脱敏展示（如手机号、邮箱掩码），未授权字段不下发、不可导出。
        - **授权申请与审计**：高敏权限可走申请审批流程，所有授权变更留痕。

        ## 四、菜单与导航

        - 菜单结构由系统统一维护（页面与操作按钮单一事实源），按当前用户权限码动态过滤，无权限的入口不显示。

        ## 五、数据字典与系统配置

        - **数据字典**：业务字典集中维护，字典项支持树形层级；代码生成可把字段绑定到字典。
        - **系统配置**：平台级参数集中管理，区分内置参数与运营可调参数，敏感参数支持加密存储。

        ## 六、文件与存储

        - **存储配置**：支持本地存储，并预留对象存储（S3 / OSS / MinIO）接入位，统一管理文件落盘与访问。
        - **文件管理**：上传、下载、归类与生命周期清理。

        ## 七、定时任务调度

        - 内置任务调度，支持 Cron 表达式、超时控制、并发开关与失败重试；可视化查看下次触发时间与执行记录。

        ## 八、消息中心

        - **站内通知**：系统公告、安全通知、业务通知、待办与紧急通知五类，支持优先级、有效期与跳转。
        - **触达方式**：顶部横幅、登录弹窗、强制阅读（必读拦截）与可选确认，按角色 / 部门 / 指定用户定向下发。
        - **消息模板**：邮件等外发渠道使用统一模板（登录验证码、找回密码、欢迎信等），支持占位符渲染。

        ## 九、代码生成（开发工具）

        - 面向开发者：按数据表生成实体、仓储、服务、DTO、接口与前端页面代码，内置 Scriban 模板，可定制。
        - 代码生成是平台功能，只在平台里可用。

        ## 十、登录安全与会话

        - **登录方式**：账号密码、邮箱验证码，第三方 OAuth 登录按参数开放。
        - **会话与令牌**：访问令牌 + 刷新令牌机制，支持多设备登录策略与设备数限制。
        - **开放平台**：内置 OAuth 应用管理，第一方前端通过统一客户端获取令牌。

        ## 十一、审计与日志

        - 操作日志、登录日志、差异日志等全链路留痕，支持按用户 / 时间 / 结果检索，满足合规审计。

        ---

        ## 快速上手

        1. **首登改密**：使用初始管理员账号登录后，请立即在「个人中心 - 账号安全」修改初始密码。
        2. **建组织、配角色**：在「身份权限」中搭建部门、创建角色并分配权限码与数据范围。
        3. **建用户**：创建成员账号并归属部门、绑定角色。
        4. **按需配置**：在「系统设置」中调整字典、参数、存储与定时任务。

        > 如需帮助，请查阅「关于」中的文档与仓库地址，或联系系统管理员。
        """;

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => SeedOrders.Demo + 1;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[SaaS]演示通知";

    /// <summary>
    /// 写入演示数据
    /// </summary>
    protected override async Task SeedDemoAsync()
    {
        var added = 0;
        var senderId = await DbClientFor<SysUser>().Queryable<SysUser>()
            .Where(user => user.TenantId == 0 && user.UserName == SaasSuperAdminSeeder.UserName)
            .Select(user => user.BasicId)
            .FirstAsync();
        if (senderId == 0)
        {
            throw new InvalidOperationException($"{Name}：超级管理员不存在，平台身份种子须先执行。");
        }
        added += await InsertIfMissingAsync(0, new SysNotification
        {
            NotificationType = NotificationType.System,
            Priority = NotificationPriority.High,
            ContentFormat = NotificationContentFormat.Markdown,
            Title = GuideTitle,
            Content = SystemOverviewContent,
            Icon = "lucide:book-open",
            TargetType = NotificationTargetType.All,
            NeedConfirm = true,
            IsMandatory = true,
            IsBanner = true,
            IsPopup = true,
            SendUserId = senderId,
            Remark = "演示：全员、横幅、登录弹窗、必读并确认"
        });

        var tenant = await DbClientFor<SysTenant>().Queryable<SysTenant>().FirstAsync(item => item.TenantCode == "demo-enterprise");
        if (tenant is not null)
        {
            using var tenantScope = CurrentTenant.Change(tenant.BasicId, tenant.TenantName);
            var department = await DbClientFor<SysDepartment>().Queryable<SysDepartment>()
                .FirstAsync(item => item.TenantId == tenant.BasicId && item.DepartmentCode == "rd");
            var role = await DbClientFor<SysRole>().Queryable<SysRole>()
                .FirstAsync(item => item.TenantId == tenant.BasicId && item.RoleCode == "employee");
            var owner = await DbClientFor<SysUser>().Queryable<SysUser>()
                .FirstAsync(item => item.TenantId == tenant.BasicId && item.UserName == "owner");
            if (department is not null)
            {
                added += await InsertIfMissingAsync(tenant.BasicId, new SysNotification
                {
                    NotificationType = NotificationType.Business,
                    Priority = NotificationPriority.Normal,
                    ContentFormat = NotificationContentFormat.Text,
                    Title = "研发中心周会：周五下午三点",
                    Content = "本周周会改到周五下午三点，地点三楼会议室，请研发中心（含前端组、后端组）全员参加。",
                    Icon = "lucide:calendar",
                    TargetType = NotificationTargetType.Department,
                    TargetValue = JsonSerializer.Serialize(new[] { department.BasicId }),
                    SendUserId = owner?.BasicId,
                    Remark = "演示：按部门定向（含下级部门）"
                });
            }

            if (role is not null)
            {
                added += await InsertIfMissingAsync(tenant.BasicId, new SysNotification
                {
                    NotificationType = NotificationType.Security,
                    Priority = NotificationPriority.Urgent,
                    ContentFormat = NotificationContentFormat.Text,
                    Title = "请尽快修改初始密码",
                    Content = "演示账号的密码都是公开的，请在「个人中心 - 账号安全」修改为只有你知道的密码。",
                    Icon = "lucide:shield-alert",
                    TargetType = NotificationTargetType.Role,
                    TargetValue = JsonSerializer.Serialize(new[] { role.BasicId }),
                    NeedConfirm = true,
                    SendUserId = owner?.BasicId,
                    Remark = "演示：按角色定向、紧急、需要确认"
                });
            }
        }

        Logger.LogInformation("{Seeder}：新增 {Count} 条草稿通知，已有的不覆盖", Name, added);
    }

    /// <summary>
    /// 按标题写一条草稿通知（已有同标题的不写）
    /// </summary>
    private async Task<int> InsertIfMissingAsync(long tenantId, SysNotification notification)
    {
        var title = notification.Title;
        if (await DbClientFor<SysNotification>().Queryable<SysNotification>()
                .IncludingDeleted()
                .AnyAsync(item => item.TenantId == tenantId && item.Title == title))
        {
            return 0;
        }

        notification.TenantId = tenantId;
        notification.IsPublished = false;
        notification.SendTime = DateTimeOffset.UtcNow;
        _ = await DbClientFor<SysNotification>().Insertable(notification).ExecuteCommandAsync();
        return 1;
    }
}
