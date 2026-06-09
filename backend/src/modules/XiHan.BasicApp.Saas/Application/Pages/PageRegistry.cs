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
        new("workbench","工作台",MenuType.Directory,"/workbench","Workbench",null,null,null,"lucide:layout-dashboard",10,"/workbench/dashboard"),
        // [1.1] 仪表盘
        new("workbench.dashboard","仪表盘",MenuType.Menu,"/workbench/dashboard","WorkbenchDashboard","workbench/dashboard/index","workbench",SaasPermissionCodes.UserStatistics.Read,"lucide:gauge",11,null,false,true),
        // [1.2] 我的消息
        new("workbench.inbox","我的消息",MenuType.Menu,"/workbench/inbox","WorkbenchInbox","workbench/inbox/index","workbench",null,"lucide:inbox",12),

        // [2] 身份权限
        new("identity","身份权限",MenuType.Directory,"/identity","Identity",null,null,null,"lucide:shield-check",100,"/identity/user"),
        // [2.1] 用户管理
        new("identity.user","用户管理",MenuType.Menu,"/identity/user","IdentityUser","system/user/index","identity",SaasPermissionCodes.User.Read,"lucide:users",110),
        // [2.2] 角色管理
        new("identity.role","角色管理",MenuType.Menu,"/identity/role","IdentityRole","system/role/index","identity",SaasPermissionCodes.Role.Read,"lucide:shield-user",120),
        // [2.3] 组织机构
        new("identity.org","组织机构",MenuType.Menu,"/identity/org","IdentityOrg","system/org/index","identity",SaasPermissionCodes.Department.Read,"lucide:network",130),
        // [2.4] 权限管理
        new("identity.permission","权限管理",MenuType.Menu,"/identity/permission","IdentityPermission","system/permission/index","identity",SaasPermissionCodes.Permission.Read,"lucide:key-round",140),
        // [2.5] 授权申请
        new("identity.authorization","授权申请",MenuType.Menu,"/identity/authorization","IdentityAuthorization","identity/authorization/index","identity",SaasPermissionCodes.PermissionRequest.Read,"lucide:file-key",150),

        // [3] 租户管理
        new("tenant","租户管理",MenuType.Directory,"/tenant","Tenant",null,null,null,"lucide:building-2",200,"/tenant/list"),
        // [3.1] 租户列表
        new("tenant.list","租户列表",MenuType.Menu,"/tenant/list","TenantList","platform/tenant/index","tenant",SaasPermissionCodes.Tenant.Read,"lucide:building",210),
        // [3.2] 版本套餐
        new("tenant.edition","版本套餐",MenuType.Menu,"/tenant/edition","TenantEdition","tenant/edition/index","tenant",SaasPermissionCodes.TenantEdition.Read,"lucide:package",220),

        // [4] 消息中心
        // 注：SaasPermissionCodes 中无独立 Notification 族，使用语义最近的 Message.Read
        new("message","消息中心",MenuType.Directory,"/message","Message",null,null,null,"lucide:mail",300,"/message/notification"),
        // [4.1] 通知管理
        new("message.notification","通知管理",MenuType.Menu,"/message/notification","MessageNotification","message/notification/index","message",SaasPermissionCodes.Message.Read,"lucide:bell",310),
        // [4.2] 邮件短信
        new("message.record","邮件短信",MenuType.Menu,"/message/record","MessageRecord","system/message/index","message",SaasPermissionCodes.Message.Read,"lucide:send",320),

        // [5] 审批规则
        new("approval","审批规则",MenuType.Directory,"/approval","Approval",null,null,null,"lucide:clipboard-check",400,"/approval/review"),
        // [5.1] 审批中心
        new("approval.review","审批中心",MenuType.Menu,"/approval/review","ApprovalReview","platform/approval/index","approval",SaasPermissionCodes.Review.Read,"lucide:check-check",410),
        // [5.2] 约束规则
        new("approval.constraint","约束规则",MenuType.Menu,"/approval/constraint","ApprovalConstraint","approval/constraint/index","approval",SaasPermissionCodes.ConstraintRule.Read,"lucide:shield-alert",420),

        // [6] 文件存储
        // 注：SaasPermissionCodes 中无独立 StorageConfig 族，使用语义最近的 Config.Read
        new("file","文件存储",MenuType.Directory,"/file","File",null,null,null,"lucide:folder",500,"/file/library"),
        // [6.1] 文件管理
        new("file.library","文件管理",MenuType.Menu,"/file/library","FileLibrary","platform/file/index","file",SaasPermissionCodes.File.Read,"lucide:folder-open",510),
        // [6.2] 存储配置
        new("file.storage","存储配置",MenuType.Menu,"/file/storage","FileStorage","file/storage/index","file",SaasPermissionCodes.Config.Read,"lucide:hard-drive",520),

        // [7] 开放平台
        new("openapi","开放平台",MenuType.Directory,"/openapi","Openapi",null,null,null,"lucide:blocks",600,"/openapi/app"),
        // [7.1] 应用管理
        new("openapi.app","应用管理",MenuType.Menu,"/openapi/app","OpenapiApp","platform/app/index","openapi",SaasPermissionCodes.OAuthApp.Read,"lucide:badge-check",610),

        // [8] 系统设置
        new("setting","系统设置",MenuType.Directory,"/setting","Setting",null,null,null,"lucide:settings",700,"/setting/menu"),
        // [8.1] 菜单管理
        new("setting.menu","菜单管理",MenuType.Menu,"/setting/menu","SettingMenu","platform/menu/index","setting",SaasPermissionCodes.Menu.Read,"lucide:list-tree",710),
        // [8.2] 字典管理
        new("setting.dict","字典管理",MenuType.Menu,"/setting/dict","SettingDict","platform/dict/index","setting",SaasPermissionCodes.Dict.Read,"lucide:book-open",720),
        // [8.3] 参数配置
        new("setting.config","参数配置",MenuType.Menu,"/setting/config","SettingConfig","platform/config/index","setting",SaasPermissionCodes.Config.Read,"lucide:sliders-horizontal",730),
        // [8.4] 任务调度
        new("setting.job","任务调度",MenuType.Menu,"/setting/job","SettingJob","platform/job/index","setting",SaasPermissionCodes.Task.Read,"lucide:timer",740),
        // [8.5] 缓存管理
        new("setting.cache","缓存管理",MenuType.Menu,"/setting/cache","SettingCache","platform/cache/index","setting",SaasPermissionCodes.Config.Read,"lucide:database-backup",750),
        // [8.6] 服务监控
        new("setting.server","服务监控",MenuType.Menu,"/setting/server","SettingServer","platform/server/index","setting",SaasPermissionCodes.Config.Read,"lucide:server",760),
        // [8.7] 日志审计
        new("log","日志审计",MenuType.Directory,"/log","Log",null,"setting",null,"lucide:file-search",765,"/log/access"),
        // [8.7.1] 访问日志
        new("log.access","访问日志",MenuType.Menu,"/log/access","LogAccess","log/access/index","log",SaasPermissionCodes.AccessLog.Read,"lucide:globe",810),
        // [8.7.2] 接口日志
        new("log.api","接口日志",MenuType.Menu,"/log/api","LogApi","log/api/index","log",SaasPermissionCodes.ApiLog.Read,"lucide:webhook",820),
        // [8.7.3] 操作日志
        new("log.operation","操作日志",MenuType.Menu,"/log/operation","LogOperation","log/operation/index","log",SaasPermissionCodes.OperationLog.Read,"lucide:mouse-pointer-click",830),
        // [8.7.4] 登录日志
        new("log.login","登录日志",MenuType.Menu,"/log/login","LogLogin","log/login/index","log",SaasPermissionCodes.LoginLog.Read,"lucide:log-in",840),
        // [8.7.5] 异常日志
        new("log.exception","异常日志",MenuType.Menu,"/log/exception","LogException","log/exception/index","log",SaasPermissionCodes.ExceptionLog.Read,"lucide:triangle-alert",850),

        // [9] 关于系统
        new("about","关于系统",MenuType.Menu,"/about","About","/about/index",null,SaasPermissionCodes.Version.Read,"lucide:info",770),
    ];

    /// <summary>
    /// 所有已登记按钮（ParentCode 必须对应 <see cref="All"/> 中的页面码，种子据此生成按钮节点）
    /// </summary>
    public static IReadOnlyList<ButtonDescriptor> Buttons { get; } =
    [
        // [2.1] 用户管理
        new("identity.user.create","新增","identity.user",SaasPermissionCodes.User.Create,1),
        new("identity.user.update","编辑","identity.user",SaasPermissionCodes.User.Update,2),
        new("identity.user.delete","删除","identity.user",SaasPermissionCodes.User.Delete,3),
        new("identity.user.status","启停","identity.user",SaasPermissionCodes.User.Status,4),
        new("identity.user.reset-password","重置密码","identity.user",SaasPermissionCodes.UserSecurity.ResetPassword,5),

        // [2.2] 角色管理
        new("identity.role.create","新增","identity.role",SaasPermissionCodes.Role.Create,1),
        new("identity.role.update","编辑","identity.role",SaasPermissionCodes.Role.Update,2),
        new("identity.role.delete","删除","identity.role",SaasPermissionCodes.Role.Delete,3),
        new("identity.role.status","启停","identity.role",SaasPermissionCodes.Role.Status,4),
        new("identity.role.grant-permission","分配权限","identity.role",SaasPermissionCodes.RolePermission.Grant,5),

        // [2.3] 组织机构
        new("identity.org.create","新增","identity.org",SaasPermissionCodes.Department.Create,1),
        new("identity.org.update","编辑","identity.org",SaasPermissionCodes.Department.Update,2),
        new("identity.org.delete","删除","identity.org",SaasPermissionCodes.Department.Delete,3),
        new("identity.org.status","启停","identity.org",SaasPermissionCodes.Department.Status,4),

        // [2.4] 权限管理
        new("identity.permission.create","新增","identity.permission",SaasPermissionCodes.Permission.Create,1),
        new("identity.permission.update","编辑","identity.permission",SaasPermissionCodes.Permission.Update,2),
        new("identity.permission.delete","删除","identity.permission",SaasPermissionCodes.Permission.Delete,3),
        new("identity.permission.status","启停","identity.permission",SaasPermissionCodes.Permission.Status,4),

        // [2.5] 授权申请
        new("identity.authorization.create","发起申请","identity.authorization",SaasPermissionCodes.PermissionRequest.Create,1),
        new("identity.authorization.audit","审批","identity.authorization",SaasPermissionCodes.PermissionRequest.Status,2),
        new("identity.authorization.withdraw","撤回","identity.authorization",SaasPermissionCodes.PermissionRequest.Withdraw,3),

        // [3.1] 租户列表
        new("tenant.list.create","新增","tenant.list",SaasPermissionCodes.Tenant.Create,1),
        new("tenant.list.update","编辑","tenant.list",SaasPermissionCodes.Tenant.Update,2),
        new("tenant.list.status","启停","tenant.list",SaasPermissionCodes.Tenant.Status,3),

        // [3.2] 版本套餐
        new("tenant.edition.create","新增","tenant.edition",SaasPermissionCodes.TenantEdition.Create,1),
        new("tenant.edition.update","编辑","tenant.edition",SaasPermissionCodes.TenantEdition.Update,2),
        new("tenant.edition.status","启停","tenant.edition",SaasPermissionCodes.TenantEdition.Status,3),
        new("tenant.edition.default","设为默认","tenant.edition",SaasPermissionCodes.TenantEdition.Default,4),

        // [4.1] 通知管理
        new("message.notification.create","新增","message.notification",SaasPermissionCodes.Message.Create,1),
        new("message.notification.update","编辑","message.notification",SaasPermissionCodes.Message.Update,2),
        new("message.notification.publish","发布","message.notification",SaasPermissionCodes.Message.Publish,3),
        new("message.notification.delete","删除","message.notification",SaasPermissionCodes.Message.Delete,4),

        // [4.2] 邮件短信
        new("message.record.delete","删除","message.record",SaasPermissionCodes.Message.Delete,1),

        // [5.1] 审批中心
        new("approval.review.audit","审核","approval.review",SaasPermissionCodes.Review.Audit,1),
        new("approval.review.withdraw","撤回","approval.review",SaasPermissionCodes.Review.Withdraw,2),
        new("approval.review.delete","删除","approval.review",SaasPermissionCodes.Review.Delete,3),

        // [5.2] 约束规则
        new("approval.constraint.create","新增","approval.constraint",SaasPermissionCodes.ConstraintRule.Create,1),
        new("approval.constraint.update","编辑","approval.constraint",SaasPermissionCodes.ConstraintRule.Update,2),
        new("approval.constraint.delete","删除","approval.constraint",SaasPermissionCodes.ConstraintRule.Delete,3),
        new("approval.constraint.status","启停","approval.constraint",SaasPermissionCodes.ConstraintRule.Status,4),

        // [6.1] 文件管理
        new("file.library.create","上传","file.library",SaasPermissionCodes.File.Create,1),
        new("file.library.update","编辑","file.library",SaasPermissionCodes.File.Update,2),
        new("file.library.delete","删除","file.library",SaasPermissionCodes.File.Delete,3),

        // [6.2] 存储配置
        new("file.storage.create","新增","file.storage",SaasPermissionCodes.Config.Create,1),
        new("file.storage.update","编辑","file.storage",SaasPermissionCodes.Config.Update,2),
        new("file.storage.delete","删除","file.storage",SaasPermissionCodes.Config.Delete,3),

        // [7.1] 应用管理
        new("openapi.app.create","新增","openapi.app",SaasPermissionCodes.OAuthApp.Create,1),
        new("openapi.app.update","编辑","openapi.app",SaasPermissionCodes.OAuthApp.Update,2),
        new("openapi.app.delete","删除","openapi.app",SaasPermissionCodes.OAuthApp.Delete,3),
        new("openapi.app.status","启停","openapi.app",SaasPermissionCodes.OAuthApp.Status,4),
        new("openapi.app.secret","重置密钥","openapi.app",SaasPermissionCodes.OAuthApp.Secret,5),

        // [8.1] 菜单管理
        new("setting.menu.create","新增","setting.menu",SaasPermissionCodes.Menu.Create,1),
        new("setting.menu.update","编辑","setting.menu",SaasPermissionCodes.Menu.Update,2),
        new("setting.menu.delete","删除","setting.menu",SaasPermissionCodes.Menu.Delete,3),
        new("setting.menu.status","启停","setting.menu",SaasPermissionCodes.Menu.Status,4),

        // [8.2] 字典管理
        new("setting.dict.create","新增","setting.dict",SaasPermissionCodes.Dict.Create,1),
        new("setting.dict.update","编辑","setting.dict",SaasPermissionCodes.Dict.Update,2),
        new("setting.dict.delete","删除","setting.dict",SaasPermissionCodes.Dict.Delete,3),
        new("setting.dict.status","启停","setting.dict",SaasPermissionCodes.Dict.Status,4),

        // [8.3] 参数配置
        new("setting.config.create","新增","setting.config",SaasPermissionCodes.Config.Create,1),
        new("setting.config.update","编辑","setting.config",SaasPermissionCodes.Config.Update,2),
        new("setting.config.delete","删除","setting.config",SaasPermissionCodes.Config.Delete,3),
        new("setting.config.status","启停","setting.config",SaasPermissionCodes.Config.Status,4),

        // [8.4] 任务调度
        new("setting.job.create","新增","setting.job",SaasPermissionCodes.Task.Create,1),
        new("setting.job.update","编辑","setting.job",SaasPermissionCodes.Task.Update,2),
        new("setting.job.delete","删除","setting.job",SaasPermissionCodes.Task.Delete,3),
        new("setting.job.status","启停","setting.job",SaasPermissionCodes.Task.Status,4),
        new("setting.job.run","执行","setting.job",SaasPermissionCodes.Task.RunStatus,5),
    ];
}
