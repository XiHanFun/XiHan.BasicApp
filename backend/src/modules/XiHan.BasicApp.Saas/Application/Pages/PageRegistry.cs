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
/// <remarks>
/// 一致性约定（前后端同步的硬规则）：
/// - Component 必须等于 Path 去掉前导斜杠后追加 "/index"（即前端 src/views 目录结构与路由路径一一对应）
/// - I18nKey 命名为 menu.{Code 中 . 与 - 替换为 _}，并在前端 packages/locales/langs/{lang}/menu.ts 中维护双语文案
/// - 静态路由（/about、/workbench/profile、/control-center 等）由前端 src/router/routes.ts 持有，不得登记到本表（避免路径/路由名冲突）
/// </remarks>
/// <param name="Code">页面唯一码</param>
/// <param name="Title">页面标题（中文原文，I18nKey 缺失翻译时的回退展示）</param>
/// <param name="I18nKey">国际化键（menu.* 命名空间）</param>
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
/// <param name="IsExternal">是否外链菜单（直接打开外部地址，不生成可用前端路由）</param>
/// <param name="ExternalUrl">外链地址（IsExternal=true 时使用）</param>
public sealed record PageDescriptor(
    string Code,
    string Title,
    string? I18nKey,
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
    bool IsAffix = false,
    bool IsExternal = false,
    string? ExternalUrl = null);

/// <summary>
/// 按钮描述符（页面内操作按钮，菜单种子据此生成 MenuType.Button 节点）
/// </summary>
/// <param name="Code">按钮唯一码（建议 {页面码}.{动作}）</param>
/// <param name="Title">按钮标题</param>
/// <param name="ParentCode">所属页面码（对应 PageDescriptor.Code）</param>
/// <param name="PermissionCode">关联权限码</param>
/// <param name="Sort">同页面内排序值</param>
public sealed record ButtonDescriptor(
    string Code,
    string Title,
    string ParentCode,
    string PermissionCode,
    int Sort);

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
        // [1] 工作台
         new("workbench", "工作台", "menu.workbench", MenuType.Directory, "/workbench", "Workbench", null, null, null, "lucide:layout-dashboard", 10, "/workbench/dashboard"),
        // [1.1] 仪表盘
         new("workbench.dashboard", "仪表盘", "menu.workbench_dashboard", MenuType.Menu, "/workbench/dashboard", "WorkbenchDashboard", "workbench/dashboard/index", "workbench", SaasPermissionCodes.UserStatistics.Read, "lucide:gauge", 11, IsAffix: true),
        // [1.2] 我的消息
         new("workbench.inbox", "我的消息", "menu.workbench_inbox", MenuType.Menu, "/workbench/inbox", "WorkbenchInbox", "workbench/inbox/index", "workbench", null, "lucide:inbox", 12),

        // [2] 身份权限
         new("identity", "身份权限", "menu.identity", MenuType.Directory, "/identity", "Identity", null, null, null, "lucide:shield-check", 100, "/identity/user"),
        // [2.1] 用户管理
         new("identity.user", "用户管理", "menu.identity_user", MenuType.Menu, "/identity/user", "IdentityUser", "identity/user/index", "identity", SaasPermissionCodes.User.Read, "lucide:users", 110),
        // [2.2] 角色管理
         new("identity.role", "角色管理", "menu.identity_role", MenuType.Menu, "/identity/role", "IdentityRole", "identity/role/index", "identity", SaasPermissionCodes.Role.Read, "lucide:shield-user", 120),
        // [2.3] 组织机构
         new("identity.org", "组织机构", "menu.identity_org", MenuType.Menu, "/identity/org", "IdentityOrg", "identity/org/index", "identity", SaasPermissionCodes.Department.Read, "lucide:network", 130),
        // [2.4] 权限管理
         new("identity.permission", "权限管理", "menu.identity_permission", MenuType.Menu, "/identity/permission", "IdentityPermission", "identity/permission/index", "identity", SaasPermissionCodes.Permission.Read, "lucide:key-round", 140),
        // [2.5] 字段安全（字段级读写与脱敏策略）
         new("identity.field-security", "字段安全", "menu.identity_field_security", MenuType.Menu, "/identity/field-security", "IdentityFieldSecurity", "identity/field-security/index", "identity", SaasPermissionCodes.FieldLevelSecurity.Read, "lucide:eye-off", 150),
        // [2.6] 授权申请
         new("identity.authorization", "授权申请", "menu.identity_authorization", MenuType.Menu, "/identity/authorization", "IdentityAuthorization", "identity/authorization/index", "identity", SaasPermissionCodes.PermissionRequest.Read, "lucide:file-key", 160),
        // [2.7] 在线用户（会话实时视图：活跃会话 + SignalR 连接标注，权限复用用户会话码）
         new("identity.online-user", "在线用户", "menu.identity_online_user", MenuType.Menu, "/identity/online-user", "IdentityOnlineUser", "identity/online-user/index", "identity", SaasPermissionCodes.UserSession.Read, "lucide:radio", 170),

        // [3] 租户管理
         new("tenant", "租户管理", "menu.tenant", MenuType.Directory, "/tenant", "Tenant", null, null, null, "lucide:building-2", 200, "/tenant/list"),
        // [3.1] 租户列表
         new("tenant.list", "租户列表", "menu.tenant_list", MenuType.Menu, "/tenant/list", "TenantList", "tenant/list/index", "tenant", SaasPermissionCodes.Tenant.Read, "lucide:building", 210),
        // [3.2] 版本套餐
         new("tenant.edition", "版本套餐", "menu.tenant_edition", MenuType.Menu, "/tenant/edition", "TenantEdition", "tenant/edition/index", "tenant", SaasPermissionCodes.TenantEdition.Read, "lucide:package", 220),

        // [4] 消息中心
         new("message", "消息中心", "menu.message", MenuType.Directory, "/message", "Message", null, null, null, "lucide:mail", 300, "/message/notification"),
        // [4.1] 通知公告
         new("message.notification", "通知公告", "menu.message_notification", MenuType.Menu, "/message/notification", "MessageNotification", "message/notification/index", "message", SaasPermissionCodes.Notification.Read, "lucide:bell", 310),
        // [4.2] 邮件短信
         new("message.record", "邮件短信", "menu.message_record", MenuType.Menu, "/message/record", "MessageRecord", "message/record/index", "message", SaasPermissionCodes.Message.Read, "lucide:send", 320),
        // [4.3] 消息模板
         new("message.template", "消息模板", "menu.message_template", MenuType.Menu, "/message/template", "MessageTemplate", "message/template/index", "message", SaasPermissionCodes.MessageTemplate.Read, "lucide:file-code-2", 330),

        // [5] 审批规则
         new("approval", "审批规则", "menu.approval", MenuType.Directory, "/approval", "Approval", null, null, null, "lucide:clipboard-check", 400, "/approval/review"),
        // [5.1] 审批中心
         new("approval.review", "审批中心", "menu.approval_review", MenuType.Menu, "/approval/review", "ApprovalReview", "approval/review/index", "approval", SaasPermissionCodes.Review.Read, "lucide:check-check", 410),
        // [5.2] 约束规则
         new("approval.constraint", "约束规则", "menu.approval_constraint", MenuType.Menu, "/approval/constraint", "ApprovalConstraint", "approval/constraint/index", "approval", SaasPermissionCodes.ConstraintRule.Read, "lucide:shield-alert", 420),

        // [6] 文件存储
         new("file", "文件存储", "menu.file", MenuType.Directory, "/file", "File", null, null, null, "lucide:folder", 500, "/file/library"),
        // [6.1] 文件管理
         new("file.library", "文件管理", "menu.file_library", MenuType.Menu, "/file/library", "FileLibrary", "file/library/index", "file", SaasPermissionCodes.File.Read, "lucide:folder-open", 510),
        // [6.2] 存储配置
         new("file.storage", "存储配置", "menu.file_storage", MenuType.Menu, "/file/storage", "FileStorage", "file/storage/index", "file", SaasPermissionCodes.StorageConfig.Read, "lucide:hard-drive", 520),
        // [6.3] 导出中心（挂入文件存储；我的异步导出任务，任意登录用户可见自己的导出，无需权限码。
         new("file.export-center", "导出中心", "menu.file_export_center", MenuType.Menu, "/file/export-center", "FileExportCenter", "file/export-center/index", "file", null, "lucide:download", 530),

        // [7] 开放平台
         new("openapi", "开放平台", "menu.openapi", MenuType.Directory, "/openapi", "Openapi", null, null, null, "lucide:blocks", 600, "/openapi/app"),
        // [7.1] 应用管理
         new("openapi.app", "应用管理", "menu.openapi_app", MenuType.Menu, "/openapi/app", "OpenapiApp", "openapi/app/index", "openapi", SaasPermissionCodes.OAuthApp.Read, "lucide:badge-check", 610),

        // [8] 系统设置
         new("setting", "系统设置", "menu.setting", MenuType.Directory, "/setting", "Setting", null, null, null, "lucide:settings", 700, "/setting/menu"),
        // [8.1] 菜单管理
         new("setting.menu", "菜单管理", "menu.setting_menu", MenuType.Menu, "/setting/menu", "SettingMenu", "setting/menu/index", "setting", SaasPermissionCodes.Menu.Read, "lucide:list-tree", 710),
        // [8.2] 字典管理
         new("setting.dict", "字典管理", "menu.setting_dict", MenuType.Menu, "/setting/dict", "SettingDict", "setting/dict/index", "setting", SaasPermissionCodes.Dict.Read, "lucide:book-open", 720),
        // [8.3] 参数配置
         new("setting.config", "参数配置", "menu.setting_config", MenuType.Menu, "/setting/config", "SettingConfig", "setting/config/index", "setting", SaasPermissionCodes.Config.Read, "lucide:sliders-horizontal", 730),
        // [8.4] 任务调度
         new("setting.job", "任务调度", "menu.setting_job", MenuType.Menu, "/setting/job", "SettingJob", "setting/job/index", "setting", SaasPermissionCodes.Task.Read, "lucide:timer", 740),
        // [8.5] 缓存管理（平台运维专属权限）
         new("setting.cache", "缓存管理", "menu.setting_cache", MenuType.Menu, "/setting/cache", "SettingCache", "setting/cache/index", "setting", SaasPermissionCodes.Cache.Read, "lucide:database-backup", 750),
        // [8.6] 服务监控（平台运维专属权限）
         new("setting.server", "服务监控", "menu.setting_server", MenuType.Menu, "/setting/server", "SettingServer", "setting/server/index", "setting", SaasPermissionCodes.Server.Read, "lucide:server", 760),
        // [8.7] 版本管理（系统版本与升级迁移）
         new("setting.version", "版本管理", "menu.setting_version", MenuType.Menu, "/setting/version", "SettingVersion", "setting/version/index", "setting", SaasPermissionCodes.Version.Read, "lucide:git-branch", 770),

        // [9] 日志审计
         new("log", "日志审计", "menu.log", MenuType.Directory, "/log", "Log", null, null, null, "lucide:file-search", 800, "/log/access"),
        // [9.1] 访问日志
         new("log.access", "访问日志", "menu.log_access", MenuType.Menu, "/log/access", "LogAccess", "log/access/index", "log", SaasPermissionCodes.AccessLog.Read, "lucide:globe", 810),
        // [9.2] 开放接口日志
         new("log.api", "开放接口日志", "menu.log_api", MenuType.Menu, "/log/api", "LogApi", "log/api/index", "log", SaasPermissionCodes.ApiLog.Read, "lucide:webhook", 820),
        // [9.3] 操作日志
         new("log.operation", "操作日志", "menu.log_operation", MenuType.Menu, "/log/operation", "LogOperation", "log/operation/index", "log", SaasPermissionCodes.OperationLog.Read, "lucide:mouse-pointer-click", 830),
        // [9.4] 登录日志
         new("log.login", "登录日志", "menu.log_login", MenuType.Menu, "/log/login", "LogLogin", "log/login/index", "log", SaasPermissionCodes.LoginLog.Read, "lucide:log-in", 840),
        // [9.5] 异常日志
         new("log.exception", "异常日志", "menu.log_exception", MenuType.Menu, "/log/exception", "LogException", "log/exception/index", "log", SaasPermissionCodes.ExceptionLog.Read, "lucide:triangle-alert", 850),
        // [9.6] 数据变更
         new("log.diff", "数据变更", "menu.log_diff", MenuType.Menu, "/log/diff", "LogDiff", "log/diff/index", "log", SaasPermissionCodes.DiffLog.Read, "lucide:file-diff", 860),

        // [10] 关于项目
         new("about", "关于项目", "menu.about", MenuType.Directory, "/about", "About", null, null, null, "lucide:info", 900, "/about"),
        // [10.1] 关于项目（复用前端静态 /about 页：RouteName=About 令动态路由按名去重跳过，导航直达静态页）
         new("about.project", "项目概览", "menu.about_project", MenuType.Menu, "/about/project", "AboutProject", "_core/about/index", "about", null, "lucide:file-text", 910),
        // [10.2] Github（外链，直接打开）
         new("about.github", "Github", "menu.about_github", MenuType.Menu, "/about/github", "AboutGithub", null, "about", null, "lucide:github", 920, IsExternal: true, ExternalUrl: "https://github.com/XiHanFun/XiHan.BasicApp"),
        // [10.3] Gitee（外链，直接打开）
         new("about.gitee", "Gitee", "menu.about_gitee", MenuType.Menu, "/about/gitee", "AboutGitee", null, "about", null, "lucide:git-branch", 930, IsExternal: true, ExternalUrl: "https://gitee.com/XiHanFun/XiHan.BasicApp"),
    ];

    /// <summary>
    /// 所有已登记按钮（ParentCode 必须对应 <see cref="All"/> 中的页面码，种子据此生成按钮节点）
    /// </summary>
    public static IReadOnlyList<ButtonDescriptor> Buttons { get; } =
    [
        // [2.1] 用户管理
         new("identity.user.create", "新增", "identity.user", SaasPermissionCodes.User.Create, 1),
         new("identity.user.update", "编辑", "identity.user", SaasPermissionCodes.User.Update, 2),
         new("identity.user.delete", "删除", "identity.user", SaasPermissionCodes.User.Delete, 3),
         new("identity.user.status", "启停", "identity.user", SaasPermissionCodes.User.Status, 4),
         new("identity.user.reset-password", "重置密码", "identity.user", SaasPermissionCodes.UserSecurity.ResetPassword, 5),
         new("identity.user.export", "导出", "identity.user", SaasPermissionCodes.User.Export, 6),

        // [2.2] 角色管理
         new("identity.role.create", "新增", "identity.role", SaasPermissionCodes.Role.Create, 1),
         new("identity.role.update", "编辑", "identity.role", SaasPermissionCodes.Role.Update, 2),
         new("identity.role.delete", "删除", "identity.role", SaasPermissionCodes.Role.Delete, 3),
         new("identity.role.status", "启停", "identity.role", SaasPermissionCodes.Role.Status, 4),
         new("identity.role.grant-permission", "分配权限", "identity.role", SaasPermissionCodes.RolePermission.Grant, 5),
         new("identity.role.export", "导出", "identity.role", SaasPermissionCodes.Role.Export, 9),

        // [2.3] 组织机构
         new("identity.org.create", "新增", "identity.org", SaasPermissionCodes.Department.Create, 1),
         new("identity.org.update", "编辑", "identity.org", SaasPermissionCodes.Department.Update, 2),
         new("identity.org.delete", "删除", "identity.org", SaasPermissionCodes.Department.Delete, 3),
         new("identity.org.status", "启停", "identity.org", SaasPermissionCodes.Department.Status, 4),
         new("identity.org.export", "导出", "identity.org", SaasPermissionCodes.Department.Export, 9),

        // [2.4] 权限管理
         new("identity.permission.create", "新增", "identity.permission", SaasPermissionCodes.Permission.Create, 1),
         new("identity.permission.update", "编辑", "identity.permission", SaasPermissionCodes.Permission.Update, 2),
         new("identity.permission.delete", "删除", "identity.permission", SaasPermissionCodes.Permission.Delete, 3),
         new("identity.permission.status", "启停", "identity.permission", SaasPermissionCodes.Permission.Status, 4),
         new("identity.permission.export", "导出", "identity.permission", SaasPermissionCodes.Permission.Export, 9),

        // [2.5] 字段安全（字段级读写与脱敏策略）
         new("identity.field-security.create", "新增", "identity.field-security", SaasPermissionCodes.FieldLevelSecurity.Create, 1),
         new("identity.field-security.update", "编辑", "identity.field-security", SaasPermissionCodes.FieldLevelSecurity.Update, 2),
         new("identity.field-security.status", "启停", "identity.field-security", SaasPermissionCodes.FieldLevelSecurity.Status, 3),
         new("identity.field-security.delete", "删除", "identity.field-security", SaasPermissionCodes.FieldLevelSecurity.Delete, 4),
         new("identity.field-security.export", "导出", "identity.field-security", SaasPermissionCodes.FieldLevelSecurity.Export, 9),

        // [2.6] 授权申请
         new("identity.authorization.create", "发起申请", "identity.authorization", SaasPermissionCodes.PermissionRequest.Create, 1),
         new("identity.authorization.audit", "审批", "identity.authorization", SaasPermissionCodes.PermissionRequest.Status, 2),
         new("identity.authorization.withdraw", "撤回", "identity.authorization", SaasPermissionCodes.PermissionRequest.Withdraw, 3),
         new("identity.authorization.export", "导出", "identity.authorization", SaasPermissionCodes.PermissionRequest.Export, 9),

        // [2.7] 在线用户（会话实时视图：活跃会话 + SignalR 连接标注，权限复用用户会话码）
         new("identity.online-user.revoke", "强制下线", "identity.online-user", SaasPermissionCodes.UserSession.Revoke, 1),
         new("identity.online-user.export", "导出", "identity.online-user", SaasPermissionCodes.UserSession.Export, 9),

        // [3.1] 租户列表
         new("tenant.list.create", "新增", "tenant.list", SaasPermissionCodes.Tenant.Create, 1),
         new("tenant.list.update", "编辑", "tenant.list", SaasPermissionCodes.Tenant.Update, 2),
         new("tenant.list.status", "启停", "tenant.list", SaasPermissionCodes.Tenant.Status, 3),
         new("tenant.list.export", "导出", "tenant.list", SaasPermissionCodes.Tenant.Export, 9),

        // [3.2] 版本套餐
         new("tenant.edition.create", "新增", "tenant.edition", SaasPermissionCodes.TenantEdition.Create, 1),
         new("tenant.edition.update", "编辑", "tenant.edition", SaasPermissionCodes.TenantEdition.Update, 2),
         new("tenant.edition.status", "启停", "tenant.edition", SaasPermissionCodes.TenantEdition.Status, 3),
         new("tenant.edition.default", "设为默认", "tenant.edition", SaasPermissionCodes.TenantEdition.Default, 4),
         new("tenant.edition.export", "导出", "tenant.edition", SaasPermissionCodes.TenantEdition.Export, 9),

        // [4.1] 通知公告
         new("message.notification.create", "新增", "message.notification", SaasPermissionCodes.Notification.Create, 1),
         new("message.notification.update", "编辑", "message.notification", SaasPermissionCodes.Notification.Update, 2),
         new("message.notification.publish", "发布", "message.notification", SaasPermissionCodes.Notification.Publish, 3),
         new("message.notification.delete", "删除", "message.notification", SaasPermissionCodes.Notification.Delete, 4),
         new("message.notification.export", "导出", "message.notification", SaasPermissionCodes.Notification.Export, 9),

        // [4.2] 邮件短信
         new("message.record.delete", "删除", "message.record", SaasPermissionCodes.Message.Delete, 1),
         new("message.record.export", "导出", "message.record", SaasPermissionCodes.Message.Export, 9),

        // [4.3] 消息模板
         new("message.template.create", "新增", "message.template", SaasPermissionCodes.MessageTemplate.Create, 1),
         new("message.template.update", "编辑", "message.template", SaasPermissionCodes.MessageTemplate.Update, 2),
         new("message.template.status", "启停", "message.template", SaasPermissionCodes.MessageTemplate.Status, 3),
         new("message.template.delete", "删除", "message.template", SaasPermissionCodes.MessageTemplate.Delete, 4),
         new("message.template.export", "导出", "message.template", SaasPermissionCodes.MessageTemplate.Export, 9),

        // [5.1] 审批中心
         new("approval.review.audit", "审核", "approval.review", SaasPermissionCodes.Review.Audit, 1),
         new("approval.review.withdraw", "撤回", "approval.review", SaasPermissionCodes.Review.Withdraw, 2),
         new("approval.review.delete", "删除", "approval.review", SaasPermissionCodes.Review.Delete, 3),
         new("approval.review.export", "导出", "approval.review", SaasPermissionCodes.Review.Export, 9),

        // [5.2] 约束规则
         new("approval.constraint.create", "新增", "approval.constraint", SaasPermissionCodes.ConstraintRule.Create, 1),
         new("approval.constraint.update", "编辑", "approval.constraint", SaasPermissionCodes.ConstraintRule.Update, 2),
         new("approval.constraint.delete", "删除", "approval.constraint", SaasPermissionCodes.ConstraintRule.Delete, 3),
         new("approval.constraint.status", "启停", "approval.constraint", SaasPermissionCodes.ConstraintRule.Status, 4),
         new("approval.constraint.export", "导出", "approval.constraint", SaasPermissionCodes.ConstraintRule.Export, 9),

        // [6.1] 文件管理
         new("file.library.create", "上传", "file.library", SaasPermissionCodes.File.Create, 1),
         new("file.library.update", "编辑", "file.library", SaasPermissionCodes.File.Update, 2),
         new("file.library.delete", "删除", "file.library", SaasPermissionCodes.File.Delete, 3),
         new("file.library.export", "导出", "file.library", SaasPermissionCodes.File.Export, 9),

        // [6.2] 存储配置
         new("file.storage.create", "新增", "file.storage", SaasPermissionCodes.StorageConfig.Create, 1),
         new("file.storage.update", "编辑", "file.storage", SaasPermissionCodes.StorageConfig.Update, 2),
         new("file.storage.status", "启停", "file.storage", SaasPermissionCodes.StorageConfig.Status, 3),
         new("file.storage.delete", "删除", "file.storage", SaasPermissionCodes.StorageConfig.Delete, 4),
         new("file.storage.export", "导出", "file.storage", SaasPermissionCodes.StorageConfig.Export, 9),

        // [7.1] 应用管理
         new("openapi.app.create", "新增", "openapi.app", SaasPermissionCodes.OAuthApp.Create, 1),
         new("openapi.app.update", "编辑", "openapi.app", SaasPermissionCodes.OAuthApp.Update, 2),
         new("openapi.app.delete", "删除", "openapi.app", SaasPermissionCodes.OAuthApp.Delete, 3),
         new("openapi.app.status", "启停", "openapi.app", SaasPermissionCodes.OAuthApp.Status, 4),
         new("openapi.app.secret", "重置密钥", "openapi.app", SaasPermissionCodes.OAuthApp.Secret, 5),
         new("openapi.app.export", "导出", "openapi.app", SaasPermissionCodes.OAuthApp.Export, 9),

        // [8.1] 菜单管理
         new("setting.menu.create", "新增", "setting.menu", SaasPermissionCodes.Menu.Create, 1),
         new("setting.menu.update", "编辑", "setting.menu", SaasPermissionCodes.Menu.Update, 2),
         new("setting.menu.delete", "删除", "setting.menu", SaasPermissionCodes.Menu.Delete, 3),
         new("setting.menu.status", "启停", "setting.menu", SaasPermissionCodes.Menu.Status, 4),
         new("setting.menu.export", "导出", "setting.menu", SaasPermissionCodes.Menu.Export, 9),

        // [8.2] 字典管理
         new("setting.dict.create", "新增", "setting.dict", SaasPermissionCodes.Dict.Create, 1),
         new("setting.dict.update", "编辑", "setting.dict", SaasPermissionCodes.Dict.Update, 2),
         new("setting.dict.delete", "删除", "setting.dict", SaasPermissionCodes.Dict.Delete, 3),
         new("setting.dict.status", "启停", "setting.dict", SaasPermissionCodes.Dict.Status, 4),
         new("setting.dict.export", "导出", "setting.dict", SaasPermissionCodes.Dict.Export, 9),

        // [8.3] 参数配置
         new("setting.config.create", "新增", "setting.config", SaasPermissionCodes.Config.Create, 1),
         new("setting.config.update", "编辑", "setting.config", SaasPermissionCodes.Config.Update, 2),
         new("setting.config.delete", "删除", "setting.config", SaasPermissionCodes.Config.Delete, 3),
         new("setting.config.status", "启停", "setting.config", SaasPermissionCodes.Config.Status, 4),
         new("setting.config.import", "导入", "setting.config", SaasPermissionCodes.Config.Import, 5),
         new("setting.config.export", "导出", "setting.config", SaasPermissionCodes.Config.Export, 9),

        // [8.4] 任务调度
         new("setting.job.create", "新增", "setting.job", SaasPermissionCodes.Task.Create, 1),
         new("setting.job.update", "编辑", "setting.job", SaasPermissionCodes.Task.Update, 2),
         new("setting.job.delete", "删除", "setting.job", SaasPermissionCodes.Task.Delete, 3),
         new("setting.job.status", "启停", "setting.job", SaasPermissionCodes.Task.Status, 4),
         new("setting.job.run", "执行", "setting.job", SaasPermissionCodes.Task.RunStatus, 5),
         new("setting.job.logs", "执行日志", "setting.job", SaasPermissionCodes.TaskLog.Read, 6),
         new("setting.job.export", "导出", "setting.job", SaasPermissionCodes.Task.Export, 9),

        // [8.5] 缓存管理（平台运维专属权限）
         new("setting.cache.clear", "清理", "setting.cache", SaasPermissionCodes.Cache.Clear, 1),

        // [8.7] 版本管理（系统版本与升级迁移）
         new("setting.version.create", "新增", "setting.version", SaasPermissionCodes.Version.Create, 1),
         new("setting.version.update", "编辑", "setting.version", SaasPermissionCodes.Version.Update, 2),
         new("setting.version.upgrade", "升级", "setting.version", SaasPermissionCodes.Version.Upgrade, 3),
         new("setting.version.delete", "删除", "setting.version", SaasPermissionCodes.Version.Delete, 4),
         new("setting.version.export", "导出", "setting.version", SaasPermissionCodes.Version.Export, 9),

        // [9.1] 访问日志
         new("log.access.export", "导出", "log.access", SaasPermissionCodes.AccessLog.Export, 1),

        // [9.2] 开放接口日志
         new("log.api.export", "导出", "log.api", SaasPermissionCodes.ApiLog.Export, 1),

        // [9.3] 操作日志
         new("log.operation.export", "导出", "log.operation", SaasPermissionCodes.OperationLog.Export, 1),

        // [9.4] 登录日志
         new("log.login.export", "导出", "log.login", SaasPermissionCodes.LoginLog.Export, 1),

        // [9.5] 异常日志
         new("log.exception.export", "导出", "log.exception", SaasPermissionCodes.ExceptionLog.Export, 1),

        // [9.6] 数据变更
         new("log.diff.export", "导出", "log.diff", SaasPermissionCodes.DiffLog.Export, 1),
    ];
}
