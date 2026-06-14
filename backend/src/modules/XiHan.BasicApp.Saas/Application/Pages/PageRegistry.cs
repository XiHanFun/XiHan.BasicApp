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
        Dir("workbench", "工作台", "lucide:layout-dashboard", 10, "/workbench/dashboard"),
        // [1.1] 仪表盘
        Page("workbench.dashboard", "仪表盘", SaasPermissionCodes.UserStatistics.Read, "lucide:gauge", 11, isAffix: true),
        // [1.2] 我的消息
        Page("workbench.inbox", "我的消息", null, "lucide:inbox", 12),

        // [2] 身份权限
        Dir("identity", "身份权限", "lucide:shield-check", 100, "/identity/user"),
        // [2.1] 用户管理
        Page("identity.user", "用户管理", SaasPermissionCodes.User.Read, "lucide:users", 110),
        // [2.2] 角色管理
        Page("identity.role", "角色管理", SaasPermissionCodes.Role.Read, "lucide:shield-user", 120),
        // [2.3] 组织机构
        Page("identity.org", "组织机构", SaasPermissionCodes.Department.Read, "lucide:network", 130),
        // [2.4] 权限管理
        Page("identity.permission", "权限管理", SaasPermissionCodes.Permission.Read, "lucide:key-round", 140),
        // [2.5] 字段安全（字段级读写与脱敏策略）
        Page("identity.field-security", "字段安全", SaasPermissionCodes.FieldLevelSecurity.Read, "lucide:eye-off", 150),
        // [2.6] 授权申请
        Page("identity.authorization", "授权申请", SaasPermissionCodes.PermissionRequest.Read, "lucide:file-key", 160),
        // [2.7] 在线用户（会话实时视图：活跃会话 + SignalR 连接标注，权限复用用户会话码）
        Page("identity.online-user", "在线用户", SaasPermissionCodes.UserSession.Read, "lucide:radio", 170),

        // [3] 租户管理
        Dir("tenant", "租户管理", "lucide:building-2", 200, "/tenant/list"),
        // [3.1] 租户列表
        Page("tenant.list", "租户列表", SaasPermissionCodes.Tenant.Read, "lucide:building", 210),
        // [3.2] 版本套餐
        Page("tenant.edition", "版本套餐", SaasPermissionCodes.TenantEdition.Read, "lucide:package", 220),

        // [4] 消息中心
        Dir("message", "消息中心", "lucide:mail", 300, "/message/notification"),
        // [4.1] 通知公告
        Page("message.notification", "通知公告", SaasPermissionCodes.Notification.Read, "lucide:bell", 310),
        // [4.2] 邮件短信
        Page("message.record", "邮件短信", SaasPermissionCodes.Message.Read, "lucide:send", 320),
        // [4.3] 消息模板
        Page("message.template", "消息模板", SaasPermissionCodes.MessageTemplate.Read, "lucide:file-code-2", 330),

        // [5] 审批规则
        Dir("approval", "审批规则", "lucide:clipboard-check", 400, "/approval/review"),
        // [5.1] 审批中心
        Page("approval.review", "审批中心", SaasPermissionCodes.Review.Read, "lucide:check-check", 410),
        // [5.2] 约束规则
        Page("approval.constraint", "约束规则", SaasPermissionCodes.ConstraintRule.Read, "lucide:shield-alert", 420),

        // [6] 文件存储
        Dir("file", "文件存储", "lucide:folder", 500, "/file/library"),
        // [6.1] 文件管理
        Page("file.library", "文件管理", SaasPermissionCodes.File.Read, "lucide:folder-open", 510),
        // [6.2] 存储配置
        Page("file.storage", "存储配置", SaasPermissionCodes.StorageConfig.Read, "lucide:hard-drive", 520),
        // [6.3] 导出中心（挂入文件存储；我的异步导出任务，任意登录用户可见自己的导出，无需权限码。
        Page("file.export-center", "导出中心", null, "lucide:download", 530, parentCode: "file"),

        // [7] 开放平台
        Dir("openapi", "开放平台", "lucide:blocks", 600, "/openapi/app"),
        // [7.1] 应用管理
        Page("openapi.app", "应用管理", SaasPermissionCodes.OAuthApp.Read, "lucide:badge-check", 610),

        // [8] 系统设置
        Dir("setting", "系统设置", "lucide:settings", 700, "/setting/menu"),
        // [8.1] 菜单管理
        Page("setting.menu", "菜单管理", SaasPermissionCodes.Menu.Read, "lucide:list-tree", 710),
        // [8.2] 字典管理
        Page("setting.dict", "字典管理", SaasPermissionCodes.Dict.Read, "lucide:book-open", 720),
        // [8.3] 参数配置
        Page("setting.config", "参数配置", SaasPermissionCodes.Config.Read, "lucide:sliders-horizontal", 730),
        // [8.4] 任务调度
        Page("setting.job", "任务调度", SaasPermissionCodes.Task.Read, "lucide:timer", 740),
        // [8.5] 缓存管理（平台运维专属权限）
        Page("setting.cache", "缓存管理", SaasPermissionCodes.Cache.Read, "lucide:database-backup", 750),
        // [8.6] 服务监控（平台运维专属权限）
        Page("setting.server", "服务监控", SaasPermissionCodes.Server.Read, "lucide:server", 760),
        // [8.7] 版本管理（系统版本与升级迁移）
        Page("setting.version", "版本管理", SaasPermissionCodes.Version.Read, "lucide:git-branch", 770),
        // [8.8] 日志审计（挂入系统设置作为子目录；log.* code 与路径 /log/* 保持不变，仅用 parentCode 改挂载位置）
        Dir("log", "日志审计", "lucide:file-search", 780, "/log/access", parentCode: "setting"),
        // [8.8.1] 访问日志
        Page("log.access", "访问日志", SaasPermissionCodes.AccessLog.Read, "lucide:globe", 810),
        // [8.8.2] 开放接口日志
        Page("log.api", "开放接口日志", SaasPermissionCodes.ApiLog.Read, "lucide:webhook", 820),
        // [8.8.3] 操作日志
        Page("log.operation", "操作日志", SaasPermissionCodes.OperationLog.Read, "lucide:mouse-pointer-click", 830),
        // [8.8.4] 登录日志
        Page("log.login", "登录日志", SaasPermissionCodes.LoginLog.Read, "lucide:log-in", 840),
        // [8.8.5] 异常日志
        Page("log.exception", "异常日志", SaasPermissionCodes.ExceptionLog.Read, "lucide:triangle-alert", 850),
        // [8.8.6] 数据变更
        Page("log.diff", "数据变更", SaasPermissionCodes.DiffLog.Read, "lucide:file-diff", 860),

        // [9] 关于项目
        Dir("about", "关于项目", "lucide:info", 900, "/about"),
        // [10.1] 关于项目（复用前端静态 /about 页：RouteName=About 令动态路由按名去重跳过，导航直达静态页）
        StaticPage("about.project", "关于项目", "/about", "About", "_core/about/index", "lucide:file-text", 910),
        // [10.2] Github（外链，直接打开）
        Link("about.github", "Github", "https://github.com/XiHanFun/XiHan.BasicApp", "lucide:github", 920),
        // [10.3] Gitee（外链，直接打开）
        Link("about.gitee", "Gitee", "https://gitee.com/XiHanFun/XiHan.BasicApp", "lucide:git-branch", 930),
    ];

    /// <summary>
    /// 所有已登记按钮（ParentCode 必须对应 <see cref="All"/> 中的页面码，种子据此生成按钮节点）
    /// </summary>
    public static IReadOnlyList<ButtonDescriptor> Buttons { get; } =
    [
        // [2.1] 用户管理
        Btn("identity.user.create", "新增", SaasPermissionCodes.User.Create, 1),
        Btn("identity.user.update", "编辑", SaasPermissionCodes.User.Update, 2),
        Btn("identity.user.delete", "删除", SaasPermissionCodes.User.Delete, 3),
        Btn("identity.user.status", "启停", SaasPermissionCodes.User.Status, 4),
        Btn("identity.user.reset-password", "重置密码", SaasPermissionCodes.UserSecurity.ResetPassword, 5),
        Btn("identity.user.export", "导出", SaasPermissionCodes.User.Export, 6),

        // [2.2] 角色管理
        Btn("identity.role.create", "新增", SaasPermissionCodes.Role.Create, 1),
        Btn("identity.role.update", "编辑", SaasPermissionCodes.Role.Update, 2),
        Btn("identity.role.delete", "删除", SaasPermissionCodes.Role.Delete, 3),
        Btn("identity.role.status", "启停", SaasPermissionCodes.Role.Status, 4),
        Btn("identity.role.grant-permission", "分配权限", SaasPermissionCodes.RolePermission.Grant, 5),
        Btn("identity.role.export", "导出", SaasPermissionCodes.Role.Export, 9),

        // [2.3] 组织机构
        Btn("identity.org.create", "新增", SaasPermissionCodes.Department.Create, 1),
        Btn("identity.org.update", "编辑", SaasPermissionCodes.Department.Update, 2),
        Btn("identity.org.delete", "删除", SaasPermissionCodes.Department.Delete, 3),
        Btn("identity.org.status", "启停", SaasPermissionCodes.Department.Status, 4),
        Btn("identity.org.export", "导出", SaasPermissionCodes.Department.Export, 9),

        // [2.4] 权限管理
        Btn("identity.permission.create", "新增", SaasPermissionCodes.Permission.Create, 1),
        Btn("identity.permission.update", "编辑", SaasPermissionCodes.Permission.Update, 2),
        Btn("identity.permission.delete", "删除", SaasPermissionCodes.Permission.Delete, 3),
        Btn("identity.permission.status", "启停", SaasPermissionCodes.Permission.Status, 4),
        Btn("identity.permission.export", "导出", SaasPermissionCodes.Permission.Export, 9),

        // [2.5] 字段安全（字段级读写与脱敏策略）
        Btn("identity.field-security.create", "新增", SaasPermissionCodes.FieldLevelSecurity.Create, 1),
        Btn("identity.field-security.update", "编辑", SaasPermissionCodes.FieldLevelSecurity.Update, 2),
        Btn("identity.field-security.status", "启停", SaasPermissionCodes.FieldLevelSecurity.Status, 3),
        Btn("identity.field-security.delete", "删除", SaasPermissionCodes.FieldLevelSecurity.Delete, 4),
        Btn("identity.field-security.export", "导出", SaasPermissionCodes.FieldLevelSecurity.Export, 9),

        // [2.6] 授权申请
        Btn("identity.authorization.create", "发起申请", SaasPermissionCodes.PermissionRequest.Create, 1),
        Btn("identity.authorization.audit", "审批", SaasPermissionCodes.PermissionRequest.Status, 2),
        Btn("identity.authorization.withdraw", "撤回", SaasPermissionCodes.PermissionRequest.Withdraw, 3),
        Btn("identity.authorization.export", "导出", SaasPermissionCodes.PermissionRequest.Export, 9),

        // [2.7] 在线用户（会话实时视图：活跃会话 + SignalR 连接标注，权限复用用户会话码）
        Btn("identity.online-user.revoke", "强制下线", SaasPermissionCodes.UserSession.Revoke, 1),
        Btn("identity.online-user.export", "导出", SaasPermissionCodes.UserSession.Export, 9),

        // [3.1] 租户列表
        Btn("tenant.list.create", "新增", SaasPermissionCodes.Tenant.Create, 1),
        Btn("tenant.list.update", "编辑", SaasPermissionCodes.Tenant.Update, 2),
        Btn("tenant.list.status", "启停", SaasPermissionCodes.Tenant.Status, 3),
        Btn("tenant.list.export", "导出", SaasPermissionCodes.Tenant.Export, 9),

        // [3.2] 版本套餐
        Btn("tenant.edition.create", "新增", SaasPermissionCodes.TenantEdition.Create, 1),
        Btn("tenant.edition.update", "编辑", SaasPermissionCodes.TenantEdition.Update, 2),
        Btn("tenant.edition.status", "启停", SaasPermissionCodes.TenantEdition.Status, 3),
        Btn("tenant.edition.default", "设为默认", SaasPermissionCodes.TenantEdition.Default, 4),
        Btn("tenant.edition.export", "导出", SaasPermissionCodes.TenantEdition.Export, 9),

        // [4.1] 通知公告
        Btn("message.notification.create", "新增", SaasPermissionCodes.Notification.Create, 1),
        Btn("message.notification.update", "编辑", SaasPermissionCodes.Notification.Update, 2),
        Btn("message.notification.publish", "发布", SaasPermissionCodes.Notification.Publish, 3),
        Btn("message.notification.delete", "删除", SaasPermissionCodes.Notification.Delete, 4),
        Btn("message.notification.export", "导出", SaasPermissionCodes.Notification.Export, 9),

        // [4.2] 邮件短信
        Btn("message.record.delete", "删除", SaasPermissionCodes.Message.Delete, 1),
        Btn("message.record.export", "导出", SaasPermissionCodes.Message.Export, 9),

        // [4.3] 消息模板
        Btn("message.template.create", "新增", SaasPermissionCodes.MessageTemplate.Create, 1),
        Btn("message.template.update", "编辑", SaasPermissionCodes.MessageTemplate.Update, 2),
        Btn("message.template.status", "启停", SaasPermissionCodes.MessageTemplate.Status, 3),
        Btn("message.template.delete", "删除", SaasPermissionCodes.MessageTemplate.Delete, 4),
        Btn("message.template.export", "导出", SaasPermissionCodes.MessageTemplate.Export, 9),

        // [5.1] 审批中心
        Btn("approval.review.audit", "审核", SaasPermissionCodes.Review.Audit, 1),
        Btn("approval.review.withdraw", "撤回", SaasPermissionCodes.Review.Withdraw, 2),
        Btn("approval.review.delete", "删除", SaasPermissionCodes.Review.Delete, 3),
        Btn("approval.review.export", "导出", SaasPermissionCodes.Review.Export, 9),

        // [5.2] 约束规则
        Btn("approval.constraint.create", "新增", SaasPermissionCodes.ConstraintRule.Create, 1),
        Btn("approval.constraint.update", "编辑", SaasPermissionCodes.ConstraintRule.Update, 2),
        Btn("approval.constraint.delete", "删除", SaasPermissionCodes.ConstraintRule.Delete, 3),
        Btn("approval.constraint.status", "启停", SaasPermissionCodes.ConstraintRule.Status, 4),
        Btn("approval.constraint.export", "导出", SaasPermissionCodes.ConstraintRule.Export, 9),

        // [6.1] 文件管理
        Btn("file.library.create", "上传", SaasPermissionCodes.File.Create, 1),
        Btn("file.library.update", "编辑", SaasPermissionCodes.File.Update, 2),
        Btn("file.library.delete", "删除", SaasPermissionCodes.File.Delete, 3),
        Btn("file.library.export", "导出", SaasPermissionCodes.File.Export, 9),

        // [6.2] 存储配置
        Btn("file.storage.create", "新增", SaasPermissionCodes.StorageConfig.Create, 1),
        Btn("file.storage.update", "编辑", SaasPermissionCodes.StorageConfig.Update, 2),
        Btn("file.storage.status", "启停", SaasPermissionCodes.StorageConfig.Status, 3),
        Btn("file.storage.delete", "删除", SaasPermissionCodes.StorageConfig.Delete, 4),
        Btn("file.storage.export", "导出", SaasPermissionCodes.StorageConfig.Export, 9),

        // [7.1] 应用管理
        Btn("openapi.app.create", "新增", SaasPermissionCodes.OAuthApp.Create, 1),
        Btn("openapi.app.update", "编辑", SaasPermissionCodes.OAuthApp.Update, 2),
        Btn("openapi.app.delete", "删除", SaasPermissionCodes.OAuthApp.Delete, 3),
        Btn("openapi.app.status", "启停", SaasPermissionCodes.OAuthApp.Status, 4),
        Btn("openapi.app.secret", "重置密钥", SaasPermissionCodes.OAuthApp.Secret, 5),
        Btn("openapi.app.export", "导出", SaasPermissionCodes.OAuthApp.Export, 9),

        // [8.1] 菜单管理
        Btn("setting.menu.create", "新增", SaasPermissionCodes.Menu.Create, 1),
        Btn("setting.menu.update", "编辑", SaasPermissionCodes.Menu.Update, 2),
        Btn("setting.menu.delete", "删除", SaasPermissionCodes.Menu.Delete, 3),
        Btn("setting.menu.status", "启停", SaasPermissionCodes.Menu.Status, 4),
        Btn("setting.menu.export", "导出", SaasPermissionCodes.Menu.Export, 9),

        // [8.2] 字典管理
        Btn("setting.dict.create", "新增", SaasPermissionCodes.Dict.Create, 1),
        Btn("setting.dict.update", "编辑", SaasPermissionCodes.Dict.Update, 2),
        Btn("setting.dict.delete", "删除", SaasPermissionCodes.Dict.Delete, 3),
        Btn("setting.dict.status", "启停", SaasPermissionCodes.Dict.Status, 4),
        Btn("setting.dict.export", "导出", SaasPermissionCodes.Dict.Export, 9),

        // [8.3] 参数配置
        Btn("setting.config.create", "新增", SaasPermissionCodes.Config.Create, 1),
        Btn("setting.config.update", "编辑", SaasPermissionCodes.Config.Update, 2),
        Btn("setting.config.delete", "删除", SaasPermissionCodes.Config.Delete, 3),
        Btn("setting.config.status", "启停", SaasPermissionCodes.Config.Status, 4),
        Btn("setting.config.import", "导入", SaasPermissionCodes.Config.Import, 5),
        Btn("setting.config.export", "导出", SaasPermissionCodes.Config.Export, 9),

        // [8.4] 任务调度
        Btn("setting.job.create", "新增", SaasPermissionCodes.Task.Create, 1),
        Btn("setting.job.update", "编辑", SaasPermissionCodes.Task.Update, 2),
        Btn("setting.job.delete", "删除", SaasPermissionCodes.Task.Delete, 3),
        Btn("setting.job.status", "启停", SaasPermissionCodes.Task.Status, 4),
        Btn("setting.job.run", "执行", SaasPermissionCodes.Task.RunStatus, 5),
        Btn("setting.job.logs", "执行日志", SaasPermissionCodes.TaskLog.Read, 6),
        Btn("setting.job.export", "导出", SaasPermissionCodes.Task.Export, 9),

        // [8.5] 缓存管理（平台运维专属权限）
        Btn("setting.cache.clear", "清理", SaasPermissionCodes.Cache.Clear, 1),

        // [8.7] 版本管理（系统版本与升级迁移）
        Btn("setting.version.create", "新增", SaasPermissionCodes.Version.Create, 1),
        Btn("setting.version.update", "编辑", SaasPermissionCodes.Version.Update, 2),
        Btn("setting.version.upgrade", "升级", SaasPermissionCodes.Version.Upgrade, 3),
        Btn("setting.version.delete", "删除", SaasPermissionCodes.Version.Delete, 4),
        Btn("setting.version.export", "导出", SaasPermissionCodes.Version.Export, 9),

        // [9.1] 访问日志
        Btn("log.access.export", "导出", SaasPermissionCodes.AccessLog.Export, 1),

        // [9.2] 开放接口日志
        Btn("log.api.export", "导出", SaasPermissionCodes.ApiLog.Export, 1),

        // [9.3] 操作日志
        Btn("log.operation.export", "导出", SaasPermissionCodes.OperationLog.Export, 1),

        // [9.4] 登录日志
        Btn("log.login.export", "导出", SaasPermissionCodes.LoginLog.Export, 1),

        // [9.5] 异常日志
        Btn("log.exception.export", "导出", SaasPermissionCodes.ExceptionLog.Export, 1),

        // [9.6] 数据变更
        Btn("log.diff.export", "导出", SaasPermissionCodes.DiffLog.Export, 1),
    ];

    /// <summary>
    /// 构造目录页（MenuType.Directory）：I18nKey/Path/RouteName/ParentCode 由 Code 派生，Component 恒为 null。
    /// parentCode 显式传入时覆盖派生父级（把目录挂到与 code 前缀不同的父级下，路径/视图保持不变）
    /// </summary>
    private static PageDescriptor Dir(string code, string title, string icon, int sort, string redirect, string? parentCode = null) =>
        new(code, title, I18nOf(code), MenuType.Directory, PathOf(code), RouteOf(code), null, parentCode ?? ParentOf(code), null, icon, sort, redirect);

    /// <summary>
    /// 构造菜单页（MenuType.Menu）：I18nKey/Path/RouteName/Component/ParentCode 由 Code 派生。
    /// parentCode 显式传入时覆盖派生父级（把菜单挂到与 code 前缀不同的父级下，路径/视图保持不变）
    /// </summary>
    private static PageDescriptor Page(string code, string title, string? permissionCode, string icon, int sort, bool isCache = false, bool isAffix = false, string? parentCode = null) =>
        new(code, title, I18nOf(code), MenuType.Menu, PathOf(code), RouteOf(code), PathOf(code)[1..] + "/index", parentCode ?? ParentOf(code), permissionCode, icon, sort, null, isCache, isAffix);

    /// <summary>
    /// 构造页面按钮（MenuType.Button）：ParentCode 由 Code 派生
    /// </summary>
    private static ButtonDescriptor Btn(string code, string title, string permissionCode, int sort) =>
        new(code, title, ParentOf(code)!, permissionCode, sort);

    /// <summary>
    /// 复用前端静态路由页面的菜单项（MenuType.Menu）：path/routeName/component 显式给定（与 src/router/routes.ts
    /// 静态路由对齐，动态路由按 RouteName 去重跳过，仅生成侧边栏入口与导航目标），ParentCode 由 Code 派生
    /// </summary>
    private static PageDescriptor StaticPage(string code, string title, string path, string routeName, string component, string icon, int sort) =>
        new(code, title, I18nOf(code), MenuType.Menu, path, routeName, component, ParentOf(code), null, icon, sort);

    /// <summary>
    /// 外链菜单项（MenuType.Menu + IsExternal）：前端按 ExternalUrl 新标签直接打开、不渲染组件，ParentCode 由 Code 派生
    /// </summary>
    private static PageDescriptor Link(string code, string title, string externalUrl, string icon, int sort) =>
        new(code, title, I18nOf(code), MenuType.Menu, PathOf(code), RouteOf(code), null, ParentOf(code), null, icon, sort, null, false, false, true, externalUrl);

    /// <summary>国际化键：menu.{Code 中 . 与 - 替换为 _}</summary>
    private static string I18nOf(string code) => "menu." + code.Replace('.', '_').Replace('-', '_');

    /// <summary>路由路径：/{Code 中 . 替换为 /}</summary>
    private static string PathOf(string code) => "/" + code.Replace('.', '/');

    /// <summary>父页面码：Code 去掉末段（顶层返回 null）</summary>
    private static string? ParentOf(string code)
    {
        var index = code.LastIndexOf('.');
        return index < 0 ? null : code[..index];
    }

    /// <summary>路由名：Code 按 . 与 - 切分后各段首字母大写拼接</summary>
    private static string RouteOf(string code) =>
        string.Concat(code.Split('.', '-').Select(segment => char.ToUpperInvariant(segment[0]) + segment[1..]));
}
