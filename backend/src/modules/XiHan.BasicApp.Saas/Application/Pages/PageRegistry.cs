#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PageRegistry
// Guid:b3e17a92-4f05-4d1c-8e63-c2a958f70d84
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;

namespace XiHan.BasicApp.Saas.Application.Pages;

/// <summary>
/// 页面描述符（单一事实源条目）
/// </summary>
/// <param name="Code">页面唯一码</param>
/// <param name="Title">页面标题</param>
/// <param name="MenuType">菜单类型</param>
/// <param name="Path">路由路径</param>
/// <param name="RouteName">路由名称</param>
/// <param name="Component">前端组件路径（目录项为 null）</param>
/// <param name="ParentCode">父页面码（顶层为 null）</param>
/// <param name="PermissionCode">关联权限码（目录项为 null）</param>
/// <param name="Icon">图标标识</param>
/// <param name="Sort">排序值</param>
/// <param name="Redirect">重定向路径（目录项指向首个子项）</param>
/// <param name="IsCache">是否缓存组件</param>
/// <param name="IsAffix">是否固定标签页</param>
public sealed record PageDescriptor(
    string Code,
    string Title,
    MenuType MenuType,
    string Path,
    string RouteName,
    string? Component,
    string? ParentCode,
    string? PermissionCode,
    string Icon,
    int Sort,
    string? Redirect = null,
    bool IsCache = false,
    bool IsAffix = false);

/// <summary>
/// 页面登记表 — 系统页面的单一事实源，菜单种子数据从此处生成
/// </summary>
public static class PageRegistry
{
    /// <summary>
    /// 所有已登记页面（父目录必须排在子项之前，种子依顺序解析 ParentId）
    /// </summary>
    public static IReadOnlyList<PageDescriptor> All { get; } =
    [
        // ── 工作台 ──────────────────────────────────────────────────
        new(
            "workbench",
            "工作台",
            MenuType.Directory,
            "/workbench",
            "Workbench",
            null,
            null,
            null,
            "lucide:layout-dashboard",
            10,
            "/workbench/dashboard"),
        new(
            "workbench.dashboard",
            "仪表盘",
            MenuType.Menu,
            "/workbench/dashboard",
            "WorkbenchDashboard",
            "workbench/dashboard/index",
            "workbench",
            SaasPermissionCodes.UserStatistics.Read,
            "lucide:gauge",
            11,
            null,
            false,
            true),
        new(
            "workbench.inbox",
            "我的消息",
            MenuType.Menu,
            "/workbench/inbox",
            "WorkbenchInbox",
            "workbench/inbox/index",
            "workbench",
            null,
            "lucide:inbox",
            12),

        // ── 身份权限 ─────────────────────────────────────────────────
        new(
            "identity",
            "身份权限",
            MenuType.Directory,
            "/identity",
            "Identity",
            null,
            null,
            null,
            "lucide:shield-check",
            100,
            "/identity/user"),
        new(
            "identity.user",
            "用户管理",
            MenuType.Menu,
            "/identity/user",
            "IdentityUser",
            "system/user/index",
            "identity",
            SaasPermissionCodes.User.Read,
            "lucide:users",
            110),
        new(
            "identity.role",
            "角色管理",
            MenuType.Menu,
            "/identity/role",
            "IdentityRole",
            "system/role/index",
            "identity",
            SaasPermissionCodes.Role.Read,
            "lucide:shield-user",
            120),
        new(
            "identity.org",
            "组织机构",
            MenuType.Menu,
            "/identity/org",
            "IdentityOrg",
            "system/org/index",
            "identity",
            SaasPermissionCodes.Department.Read,
            "lucide:network",
            130),
        new(
            "identity.permission",
            "权限管理",
            MenuType.Menu,
            "/identity/permission",
            "IdentityPermission",
            "system/permission/index",
            "identity",
            SaasPermissionCodes.Permission.Read,
            "lucide:key-round",
            140),
        new(
            "identity.authorization",
            "授权申请",
            MenuType.Menu,
            "/identity/authorization",
            "IdentityAuthorization",
            "identity/authorization/index",
            "identity",
            SaasPermissionCodes.PermissionRequest.Read,
            "lucide:file-key",
            150),

        // ── 租户管理 ─────────────────────────────────────────────────
        new(
            "tenant",
            "租户管理",
            MenuType.Directory,
            "/tenant",
            "Tenant",
            null,
            null,
            null,
            "lucide:building-2",
            200,
            "/tenant/list"),
        new(
            "tenant.list",
            "租户列表",
            MenuType.Menu,
            "/tenant/list",
            "TenantList",
            "platform/tenant/index",
            "tenant",
            SaasPermissionCodes.Tenant.Read,
            "lucide:building",
            210),
        new(
            "tenant.edition",
            "版本套餐",
            MenuType.Menu,
            "/tenant/edition",
            "TenantEdition",
            "tenant/edition/index",
            "tenant",
            SaasPermissionCodes.TenantEdition.Read,
            "lucide:package",
            220),

        // ── 消息中心 ─────────────────────────────────────────────────
        // 注：SaasPermissionCodes 中无独立 Notification 族，使用语义最近的 Message.Read
        new(
            "message",
            "消息中心",
            MenuType.Directory,
            "/message",
            "Message",
            null,
            null,
            null,
            "lucide:mail",
            300,
            "/message/notification"),
        new(
            "message.notification",
            "通知管理",
            MenuType.Menu,
            "/message/notification",
            "MessageNotification",
            "message/notification/index",
            "message",
            SaasPermissionCodes.Message.Read,
            "lucide:bell",
            310),
        new(
            "message.record",
            "邮件短信",
            MenuType.Menu,
            "/message/record",
            "MessageRecord",
            "system/message/index",
            "message",
            SaasPermissionCodes.Message.Read,
            "lucide:send",
            320),

        // ── 审批规则 ─────────────────────────────────────────────────
        new(
            "approval",
            "审批规则",
            MenuType.Directory,
            "/approval",
            "Approval",
            null,
            null,
            null,
            "lucide:clipboard-check",
            400,
            "/approval/review"),
        new(
            "approval.review",
            "审批中心",
            MenuType.Menu,
            "/approval/review",
            "ApprovalReview",
            "platform/approval/index",
            "approval",
            SaasPermissionCodes.Review.Read,
            "lucide:check-check",
            410),
        new(
            "approval.constraint",
            "约束规则",
            MenuType.Menu,
            "/approval/constraint",
            "ApprovalConstraint",
            "approval/constraint/index",
            "approval",
            SaasPermissionCodes.ConstraintRule.Read,
            "lucide:shield-alert",
            420),

        // ── 文件存储 ─────────────────────────────────────────────────
        // 注：SaasPermissionCodes 中无独立 StorageConfig 族，使用语义最近的 Config.Read
        new(
            "file",
            "文件存储",
            MenuType.Directory,
            "/file",
            "File",
            null,
            null,
            null,
            "lucide:folder",
            500,
            "/file/library"),
        new(
            "file.library",
            "文件库",
            MenuType.Menu,
            "/file/library",
            "FileLibrary",
            "platform/file/index",
            "file",
            SaasPermissionCodes.File.Read,
            "lucide:folder-open",
            510),
        new(
            "file.storage",
            "存储配置",
            MenuType.Menu,
            "/file/storage",
            "FileStorage",
            "file/storage/index",
            "file",
            SaasPermissionCodes.Config.Read,
            "lucide:hard-drive",
            520),

        // ── 开放平台 ─────────────────────────────────────────────────
        new(
            "openapi",
            "开放平台",
            MenuType.Directory,
            "/openapi",
            "Openapi",
            null,
            null,
            null,
            "lucide:blocks",
            600,
            "/openapi/app"),
        new(
            "openapi.app",
            "应用管理",
            MenuType.Menu,
            "/openapi/app",
            "OpenapiApp",
            "platform/app/index",
            "openapi",
            SaasPermissionCodes.OAuthApp.Read,
            "lucide:badge-check",
            610),

        // ── 系统设置 ─────────────────────────────────────────────────
        new(
            "setting",
            "系统设置",
            MenuType.Directory,
            "/setting",
            "Setting",
            null,
            null,
            null,
            "lucide:settings",
            700,
            "/setting/menu"),
        new(
            "setting.menu",
            "菜单管理",
            MenuType.Menu,
            "/setting/menu",
            "SettingMenu",
            "platform/menu/index",
            "setting",
            SaasPermissionCodes.Menu.Read,
            "lucide:list-tree",
            710),
        new(
            "setting.dict",
            "字典管理",
            MenuType.Menu,
            "/setting/dict",
            "SettingDict",
            "platform/dict/index",
            "setting",
            SaasPermissionCodes.Dict.Read,
            "lucide:book-open",
            720),
        new(
            "setting.config",
            "参数配置",
            MenuType.Menu,
            "/setting/config",
            "SettingConfig",
            "platform/config/index",
            "setting",
            SaasPermissionCodes.Config.Read,
            "lucide:sliders-horizontal",
            730),
        new(
            "setting.job",
            "任务调度",
            MenuType.Menu,
            "/setting/job",
            "SettingJob",
            "platform/job/index",
            "setting",
            SaasPermissionCodes.Task.Read,
            "lucide:timer",
            740),
        new(
            "setting.cache",
            "缓存管理",
            MenuType.Menu,
            "/setting/cache",
            "SettingCache",
            "platform/cache/index",
            "setting",
            SaasPermissionCodes.Config.Read,
            "lucide:database-backup",
            750),
        new(
            "setting.server",
            "服务监控",
            MenuType.Menu,
            "/setting/server",
            "SettingServer",
            "platform/server/index",
            "setting",
            SaasPermissionCodes.Config.Read,
            "lucide:server",
            760),
        new(
            "setting.about",
            "关于系统",
            MenuType.Menu,
            "/setting/about",
            "SettingAbout",
            "setting/about/index",
            "setting",
            SaasPermissionCodes.Version.Read,
            "lucide:info",
            770),

        // ── 日志审计 ─────────────────────────────────────────────────
        new(
            "log",
            "日志审计",
            MenuType.Directory,
            "/log",
            "Log",
            null,
            "setting",
            null,
            "lucide:file-search",
            765,
            "/log/access"),
        new(
            "log.access",
            "访问日志",
            MenuType.Menu,
            "/log/access",
            "LogAccess",
            "log/access/index",
            "log",
            SaasPermissionCodes.AccessLog.Read,
            "lucide:globe",
            810),
        new(
            "log.api",
            "接口日志",
            MenuType.Menu,
            "/log/api",
            "LogApi",
            "log/api/index",
            "log",
            SaasPermissionCodes.ApiLog.Read,
            "lucide:webhook",
            820),
        new(
            "log.operation",
            "操作日志",
            MenuType.Menu,
            "/log/operation",
            "LogOperation",
            "log/operation/index",
            "log",
            SaasPermissionCodes.OperationLog.Read,
            "lucide:mouse-pointer-click",
            830),
        new(
            "log.login",
            "登录日志",
            MenuType.Menu,
            "/log/login",
            "LogLogin",
            "log/login/index",
            "log",
            SaasPermissionCodes.LoginLog.Read,
            "lucide:log-in",
            840),
        new(
            "log.exception",
            "异常日志",
            MenuType.Menu,
            "/log/exception",
            "LogException",
            "log/exception/index",
            "log",
            SaasPermissionCodes.ExceptionLog.Read,
            "lucide:triangle-alert",
            850)
    ];
}
