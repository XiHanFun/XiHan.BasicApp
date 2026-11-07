// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts.MultiTenancy;
using XiHan.BasicApp.Contracts.Organization;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Contracts.Seeding;
using XiHan.BasicApp.Notification.Domain.Entities;
using XiHan.BasicApp.Notification.Domain.Services;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.BasicApp.Runtime.Seeding;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Notification.Seeds;

/// <summary>
/// 演示通知种子
/// </summary>
/// <remarks>
/// 号段 900 + 模块偏移 40（排在演示组织与演示角色之后），<c>BasicApp:Seed:EnableDemoData</c> 打开才写
/// 照 4.x：平台一条上手指南（全员、横幅、登录弹窗、必读并确认）；演示租户 <c>demo</c> 里一条按部门定向、一条按角色定向（紧急、要确认）
/// 都以草稿写入：发布时才按目标展开收件人，走一遍发布就能看到横幅、弹窗、必读与确认的效果
/// 按标题只首写：写过一次（含已删除的）就不再碰；部门或角色不在（运营改了）即跳过那一条
/// </remarks>
/// <param name="runner">按租户发起调用的运行器</param>
/// <param name="options">种子配置</param>
public sealed class DemoNotificationSeeder(ITenantScopeRunner runner, SeedOptions options) : ISeeder
{
    /// <summary>
    /// 平台上手指南的标题
    /// </summary>
    public const string GuideTitle = "XiHan BasicApp 系统功能总览与上手指南";

    /// <summary>
    /// 演示租户里按部门定向的通知标题
    /// </summary>
    public const string DepartmentTitle = "研发部周会：周五下午三点";

    /// <summary>
    /// 演示租户里按角色定向的通知标题
    /// </summary>
    public const string RoleTitle = "请尽快修改初始密码";

    private const string DemoTenantId = "demo";

    private const string DemoDepartmentCode = "rd";

    private const string DemoRoleCode = "employee";

    /// <summary>
    /// 系统功能总览的正文（Markdown）
    /// </summary>
    /// <remarks>
    /// 只写当前真实存在并可用的能力
    /// </remarks>
    private const string GuideContent =
        """
        # 欢迎使用 XiHan BasicApp

        XiHan BasicApp 是一套面向 B2B 的多租户中后台基础平台，提供从身份权限到业务支撑的完整底座。本指南带您快速了解系统的各项能力与上手路径。

        ---

        ## 一、平台与租户

        - **多租户隔离**：平台与业务租户并存，业务租户可选共享库或独立数据库；平台维护的全局数据（如菜单、字典、消息模板）对租户只读共享，业务数据归属各自租户。
        - **版本套餐**：内置免费版 / 基础版 / 专业版 / 企业版四档，按版本限制成员席位、存储容量与可用功能白名单，白名单外的权限运行时不生效。
        - **租户管理**：开通（独立库租户先初始化数据库）、开通管理员、停用与到期、席位与存储配额调整、切换版本；平台人员可作为运维支持成员入驻租户。

        ## 二、组织与身份

        - **组织架构**：支持多级部门树，闭包表维护层级路径，部门可设负责人，成员带岗位、工号与职级。
        - **用户管理**：账号、资料、部门归属、多角色绑定，账号启停，以及重置密码、重置双因素、锁定与登录策略等安全设置。
        - **角色管理**：按业务岗位定义角色，绑定权限码与数据范围，支持角色继承；约束规则可配互斥与基数。

        ## 三、权限体系（RBAC + 数据范围 + 字段级安全）

        - **功能权限（RBAC）**：以权限码（如 `identity.users.read`、`access.roles.create`）控制菜单与操作按钮的可见与可用，接口在服务端实时校验。
        - **数据范围**：角色可配置数据可见范围 —— 全部 / 本部门 / 本部门及下级 / 仅本人 / 自定义部门，成员还能单独覆盖，由服务端收窄可见数据。
        - **字段级安全（FLS）**：对敏感字段按角色、用户或部门控制读取方式（如手机号、邮箱掩码或隐藏），服务端脱敏后再下发，导出同一口径。
        - **授权申请与审计**：用户可申请角色或权限，审批通过后自动授权；角色授权、用户角色、用户直授、权限委托与角色继承的变更写入权限变更日志。

        ## 四、菜单与导航

        - 页面菜单由各能力包登记生成，按当前用户权限码动态过滤，无权限的入口不显示；平台可调整展示、启停，并另建目录与外链。

        ## 五、数据字典与系统配置

        - **数据字典**：业务字典集中维护，字典项支持树形层级；代码生成可把字段绑定到字典。
        - **系统配置**：参数集中管理，平台定义全局值、租户可按需覆盖；敏感参数加密存储、读取时不回显明文。

        ## 六、文件与存储

        - **存储配置**：支持本地存储与 S3 兼容存储（S3、MinIO、阿里云 OSS、腾讯云 COS 按服务地址接入），访问密钥加密保存，统一管理文件落盘与访问。
        - **文件管理**：上传（支持秒传与分片）、签名下载与预览，按文件类型与标签归类，不再被用到的内容随文件一起从存储里删除。

        ## 七、定时任务调度

        - 代码登记的作业与运行时作业，支持 Cron 与固定间隔，可暂停恢复、立即执行、改计划；可查看下次触发时间与运行历史。

        ## 八、消息中心

        - **站内通知**：系统公告、安全通知、业务通知、待办与紧急通知五类，支持优先级、有效期与跳转。
        - **触达方式**：顶部横幅、登录弹窗、强制阅读（必读拦截）与可选确认，按全员 / 角色 / 部门 / 指定用户定向下发。
        - **消息模板**：邮件、短信等外发渠道使用统一模板（登录验证码、重置密码、注册欢迎等），按占位符渲染，租户可覆盖平台模板。

        ## 九、代码生成（开发工具）

        - 面向开发者：按数据表生成后端能力包（实体、操作、查询、权限、页面登记与文案）和前端页面，内置 Scriban 模板，复制为自有模板后可定制。
        - 代码生成是平台功能，只在平台里可用。

        ## 十、登录安全与会话

        - **登录方式**：账号密码（可开启图形验证码与双因素验证）、邮箱或手机验证码，第三方 OAuth 登录按配置开放。
        - **会话与令牌**：访问令牌 + 刷新令牌机制，可查看登录设备、按会话下线，可限制同时登录的设备数。
        - **开放平台**：内置 OAuth 应用管理，第三方应用通过标准授权端点（`/api/oauth2/authorize`、`/api/oauth2/token`）接入。

        ## 十一、审计与日志

        - 操作、登录、访问、异常、数据变更、开放接口与权限变更等日志全链路留痕，可按 TraceId 串联追踪，支持按用户 / 时间 / 结果检索，满足合规审计。

        ---

        ## 快速上手

        1. **首登改密**：使用初始管理员账号登录后，请立即在「个人中心 › 安全设置」修改初始密码。
        2. **建组织、配角色**：在「部门」页面搭建组织机构，在「角色」页面创建角色并分配权限码与数据范围。
        3. **建用户**：在「用户」页面创建成员账号并归属部门、绑定角色。
        4. **按需配置**：在「字典管理」「参数配置」「任务调度」「存储配置」页面按需调整。

        > 如需帮助，请查阅「帮助中心」中的文档与仓库地址，或联系系统管理员。
        """;

    /// <inheritdoc />
    public string Name => "notification.demo";

    /// <inheritdoc />
    public int Order => SeedOrders.Demo + (SeedOrders.ModuleOffset * 4);

    /// <inheritdoc />
    public async ValueTask<Result> SeedAsync(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!options.EnableDemoData)
        {
            return Result.Success();
        }

        // 先写租户的：那是另起的一次调用，放在这次调用开事务之前，否则两个事务抢同一个库的写锁（SQLite）
        var tenant = await runner.RunAsync(DemoTenantId, SeedTenantAsync, context.CancellationToken).ConfigureAwait(false);
        if (tenant.IsFailure)
        {
            return tenant;
        }

        var guide = new NotificationDraft(
            GuideTitle,
            GuideContent,
            NotificationContentFormat.Markdown,
            NotificationType.System,
            NotificationPriority.High,
            NotificationTargetType.All,
            [],
            "lucide:book-open",
            null,
            null,
            null,
            NeedConfirm: true,
            IsMandatory: true,
            IsBanner: true,
            IsPopup: true,
            null,
            null,
            "演示：全员、横幅、登录弹窗、必读并确认",
            MessageChannels.InApp);
        return await WriteIfMissingAsync(context, guide).ConfigureAwait(false);
    }

    private static async ValueTask<Result> SeedTenantAsync(InvocationContext context)
    {
        var departments = await context.Services.GetRequiredService<IOrganizationDirectory>().DepartmentsAsync(context).ConfigureAwait(false);
        if (departments.FirstOrDefault(static d => d.Code == DemoDepartmentCode) is { } department)
        {
            var draft = new NotificationDraft(
                DepartmentTitle,
                "本周周会改到周五下午三点，地点三楼会议室，请研发部全员参加。",
                NotificationContentFormat.Text,
                NotificationType.Business,
                NotificationPriority.Normal,
                NotificationTargetType.Department,
                [department.Id],
                "lucide:calendar",
                null,
                null,
                null,
                NeedConfirm: false,
                IsMandatory: false,
                IsBanner: false,
                IsPopup: false,
                null,
                null,
                "演示：按部门定向（含下级部门）",
                MessageChannels.InApp);
            var written = await WriteIfMissingAsync(context, draft).ConfigureAwait(false);
            if (written.IsFailure)
            {
                return written;
            }
        }

        var roles = await context.Services.GetRequiredService<IRoleMemberDirectory>().RolesAsync(context).ConfigureAwait(false);
        if (roles.FirstOrDefault(static r => r.Code == DemoRoleCode) is { } role)
        {
            var draft = new NotificationDraft(
                RoleTitle,
                "演示账号的密码都是公开的，请在「个人中心 - 安全设置」修改为只有你知道的密码。",
                NotificationContentFormat.Text,
                NotificationType.Security,
                NotificationPriority.Urgent,
                NotificationTargetType.Role,
                [role.Id],
                "lucide:shield-alert",
                null,
                null,
                null,
                NeedConfirm: true,
                IsMandatory: false,
                IsBanner: false,
                IsPopup: false,
                null,
                null,
                "演示：按角色定向、紧急、需要确认",
                MessageChannels.InApp);
            var written = await WriteIfMissingAsync(context, draft).ConfigureAwait(false);
            if (written.IsFailure)
            {
                return written;
            }
        }

        return Result.Success();
    }

    // 平台一侧的行也落在这张表，框架仓储要求调用带租户，所以直接用工作单元的连接按侧查；连同删掉的一起算
    private static async Task<Result> WriteIfMissingAsync(InvocationContext context, NotificationDraft draft)
    {
        var side = SideRows.SideOf(context);
        var title = draft.Title;
        var written = await context.Services.GetRequiredService<SqlSugarUnitOfWork>().ConnectionFor(typeof(SysNotification)).Queryable<SysNotification>()
            .Where(n => n.TenantId == side && n.Title == title)
            .CountAsync(context.CancellationToken)
            .ConfigureAwait(false);
        if (written > 0)
        {
            return Result.Success();
        }

        var created = await context.Services.GetRequiredService<NotificationService>().CreateAsync(context, draft).ConfigureAwait(false);
        return created.IsFailure ? created.Error! : Result.Success();
    }
}
