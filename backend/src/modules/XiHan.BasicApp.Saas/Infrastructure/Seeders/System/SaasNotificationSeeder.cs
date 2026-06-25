#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasNotificationSeeder
// Guid:b2c3d4e5-6f7a-8b9c-0d1e-2f3a4b5c6d7e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/12 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Infrastructure.Seeders.System;

/// <summary>
/// SaaS 系统通知种子数据
/// </summary>
/// <remarks>
/// 仅植入一条平台级「系统功能总览与上手指南」公告（TenantId=0、全员、已发布、Markdown 正文），
/// 让新装系统的消息中心/登录欢迎弹窗一开始就有真实可读的内容，像一个正在运行的系统。
/// 幂等：按标题判存在即跳过，不覆盖运营后续编辑（遵循通知「发布后不可编辑/删除」语义）。
/// </remarks>
public sealed class SaasNotificationSeeder(
    ISqlSugarClientResolver clientResolver,
    ILogger<SaasNotificationSeeder> logger,
    IServiceProvider serviceProvider,
    ICurrentTenant currentTenant)
    : DataSeederBase(clientResolver, logger, serviceProvider)
{
    private const string AnnouncementTitle = "XiHan BasicApp 系统功能总览与上手指南";

    private readonly ICurrentTenant _currentTenant = currentTenant;

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => 26;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[SaaS]系统通知种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        using var platformScope = _currentTenant.Change(null);
        var client = DbClient;

        var exists = await client.Queryable<SysNotification>()
            .Where(notification => notification.TenantId == 0 && notification.Title == AnnouncementTitle && !notification.IsDeleted)
            .AnyAsync();
        if (exists)
        {
            Logger.LogInformation("平台系统公告已存在，跳过种子数据");
            return;
        }

        // 发件人取平台超级管理员（查不到则置 null，不阻断主流程）
        var senderId = await client.Queryable<SysUser>()
            .Where(user => user.TenantId == 0 && user.UserName == "superadmin" && !user.IsDeleted)
            .Select(user => user.BasicId)
            .FirstAsync();

        var now = DateTimeOffset.UtcNow;
        var announcement = new SysNotification
        {
            TenantId = 0,
            NotificationType = NotificationType.System,
            Priority = NotificationPriority.High,
            ContentFormat = NotificationContentFormat.Markdown,
            Title = AnnouncementTitle,
            Content = SystemOverviewContent,
            Icon = "lucide:book-open",
            TargetType = NotificationTargetType.All,
            TargetValue = null,
            NeedConfirm = false,
            IsMandatory = false,
            IsBanner = false,
            // 登录后弹一次欢迎指南（每用户仅弹一次，由 SysUserNotification.PopupShownTime 记录）
            IsPopup = true,
            IsPublished = true,
            SendUserId = senderId == 0 ? null : senderId,
            SendTime = now,
            Remark = "系统初始化内置公告"
        };

        _ = await client.Insertable(announcement).ExecuteReturnEntityAsync();
        Logger.LogInformation("成功初始化平台系统公告（系统功能总览与上手指南）");
    }

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

        - **功能权限（RBAC）**：以权限码（如 `user:read`、`role:create`）控制菜单与操作按钮的可见与可用。
        - **数据范围（ABAC）**：角色可配置数据可见范围 —— 全部 / 本部门 / 本部门及子部门 / 仅本人，越权请求在服务端被拦截。
        - **字段级安全（FLS）**：对敏感字段按角色控制可读与脱敏展示（如手机号、邮箱掩码），未授权字段不下发、不可导出。
        - **授权申请与审计**：高敏权限可走申请审批流程，所有授权变更留痕。

        ## 四、菜单与导航

        - 菜单结构由系统统一维护（页面与操作按钮单一事实源），按当前用户权限码动态过滤，无权限的入口不显示。

        ## 五、数据字典与系统配置

        - **数据字典**：枚举与业务字典集中维护（状态、类型、性别、优先级等），前端下拉与标签统一取数。
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
        - 该模块仅对超级管理员可见。

        ## 十、登录安全与会话

        - **登录方式**：账号密码登录，并预留第三方 OAuth 登录入口。
        - **会话与令牌**：访问令牌 + 刷新令牌机制，支持多设备登录策略与设备数限制。
        - **开放平台**：内置 OAuth 应用管理，第一方前端通过统一客户端获取令牌。

        ## 十一、审计与日志

        - 操作日志、登录日志、差异日志等全链路留痕，支持按用户 / 时间 / 结果检索，满足合规审计。

        ---

        ## 快速上手

        1. **首登改密**：使用初始管理员账号登录后，请立即在「个人中心」修改默认密码。
        2. **建组织、配角色**：在「身份权限」中搭建部门、创建角色并分配权限码与数据范围。
        3. **建用户**：创建成员账号并归属部门、绑定角色。
        4. **按需配置**：在「系统设置」中调整字典、参数、存储与定时任务。

        > 如需帮助，请查阅「关于」中的文档与仓库地址，或联系系统管理员。
        """;
}
