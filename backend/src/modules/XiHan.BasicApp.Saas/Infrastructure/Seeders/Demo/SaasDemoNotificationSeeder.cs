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

        - **多租户隔离**：平台（系统保留）与业务租户并存，业务租户可选字段隔离或独立数据库隔离；平台维护的全局数据（如菜单、全局角色模板）对租户只读共享，业务数据归属各自租户。
        - **版本套餐**：内置免费版 / 基础版 / 专业版 / 企业版四档，按版本限制成员席位、存储容量与可用功能白名单，白名单外的权限运行时不生效。
        - **租户管理**：开通（库隔离租户先初始化数据库）、开通管理员、停用与到期、席位与存储配额调整、切换版本；平台人员可作为支持成员入驻租户。
        - **外部成员**：租户可把其他租户的已有账号以外部协作者、顾问、访客等身份加入或邀请为成员（默认企业版开放），实际权限由分配的角色决定。

        ## 二、组织与身份

        - **组织架构**：支持多级部门树，部门类型覆盖集团、公司、部门、团队等，闭包表维护层级路径。
        - **用户管理**：账号、资料、部门归属、多角色绑定，账号启停，以及重置密码、重置双因素、锁定与登录策略等安全设置。
        - **角色管理**：按业务岗位定义角色，绑定权限码与数据范围，支持角色继承与成员上限。

        ## 三、权限体系（RBAC + 数据范围 + 字段级安全）

        - **功能权限（RBAC）**：以权限码（如 `saas:user:read`、`saas:role:create`）控制菜单与操作按钮的可见与可用，接口在服务端实时校验。
        - **数据范围**：角色可配置数据可见范围 —— 全部 / 本部门 / 本部门及下级 / 仅本人 / 自定义部门，成员还能单独覆盖；由服务端收窄可见数据，目前作用于用户列表与用户导出。
        - **字段级安全（FLS）**：对敏感字段按角色、用户或部门控制读取方式（如手机号、邮箱掩码或隐藏），服务端脱敏后再下发，导出同一口径。
        - **授权申请与审计**：用户可申请角色或权限，审批通过后自动授权；角色授权、用户角色、用户直授、权限委托与角色继承的变更写入权限变更日志。

        ## 四、菜单与导航

        - 菜单结构由系统统一维护（页面与操作按钮单一事实源），按当前用户权限码动态过滤，无权限的入口不显示。

        ## 五、数据字典与系统配置

        - **数据字典**：业务字典集中维护，字典项支持树形层级；代码生成可把字段绑定到字典。
        - **系统配置**：参数集中管理，平台定义全局值、租户可按需覆盖；内置参数不可删除，敏感参数加密存储、读取时不回显明文。

        ## 六、文件与存储

        - **存储配置**：支持本地存储与 S3 兼容存储（MinIO 等按 S3 类型接入）、阿里云 OSS、腾讯云 COS，访问密钥加密保存，统一管理文件落盘与访问。
        - **文件管理**：上传（支持秒传）、下载与预签名链接，按文件类型与标签归类，删除时可选同时删除物理文件。

        ## 七、定时任务调度

        - 内置任务调度，支持立即、延时、固定间隔与 Cron 四种触发，带超时控制、并发开关与失败重试；可查看下次触发时间与执行记录。

        ## 八、消息中心

        - **站内通知**：系统公告、安全通知、业务通知、待办与紧急通知五类，支持优先级、有效期与跳转。
        - **触达方式**：顶部横幅、登录弹窗、强制阅读（必读拦截）与可选确认，按全员 / 角色 / 部门 / 指定用户定向下发。
        - **消息模板**：邮件、短信等外发渠道使用统一模板（登录验证码、重置密码、注册欢迎等），按占位符渲染，租户可覆盖平台模板。

        ## 九、代码生成（开发工具）

        - 面向开发者：按数据表生成实体、仓储、应用与查询服务、DTO、映射，以及前端类型、接口与页面代码，并附菜单与权限种子；内置 Scriban 模板，复制为自有模板后可定制。
        - 代码生成是平台功能，只在平台里可用。

        ## 十、登录安全与会话

        - **登录方式**：账号密码（可开启图形验证码与双因素验证）、邮箱验证码，第三方 OAuth 登录按参数开放。
        - **会话与令牌**：访问令牌 + 刷新令牌机制；同一设备重新登录会替换旧会话，账号已有其它在线会话时登录会发送安全提醒。
        - **开放平台**：内置 OAuth 应用管理，第三方应用通过标准授权端点（/connect/authorize、/connect/token）接入。

        ## 十一、审计与日志

        - 操作、登录、访问、异常、数据变更、开放接口与权限变更等日志全链路留痕，可按 TraceId 串联追踪，支持按用户 / 时间 / 结果检索，满足合规审计。

        ---

        ## 快速上手

        1. **首登改密**：使用初始管理员账号登录后，请立即在「个人中心 › 安全设置」修改初始密码。
        2. **建组织、配角色**：在「身份权限」中搭建组织机构、创建角色并分配权限码与数据范围。
        3. **建用户**：创建成员账号并归属部门、绑定角色。
        4. **按需配置**：在「系统管理」中调整字典、参数与任务调度，在「文件中心 › 存储配置」中配置存储。

        > 如需帮助，请查阅「帮助中心」中的文档与仓库地址，或联系系统管理员。
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
