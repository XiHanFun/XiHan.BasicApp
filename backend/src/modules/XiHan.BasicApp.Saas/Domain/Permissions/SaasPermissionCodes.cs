#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasPermissionCodes
// Guid:8f28fe43-8942-4495-9d38-01557d2bbffd
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/29 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Domain.Permissions;

/// <summary>
/// SaaS 模块权限码
/// </summary>
public static class SaasPermissionCodes
{
    /// <summary>
    /// 模块编码
    /// </summary>
    public const string Module = "saas";

    /// <summary>
    /// 全部权限码
    /// </summary>
    public static IReadOnlyCollection<string> All =>
    [
        Tenant.Read,
        Tenant.Create,
        Tenant.Update,
        Tenant.Status,
        TenantMember.Read,
        TenantMember.Update,
        TenantMember.Status,
        TenantMember.InviteStatus,
        TenantMember.Revoke,
        TenantEdition.Read,
        TenantEdition.Create,
        TenantEdition.Update,
        TenantEdition.Status,
        TenantEdition.Default,
        TenantEditionPermission.Read,
        TenantEditionPermission.Grant,
        TenantEditionPermission.Update,
        TenantEditionPermission.Revoke,
        Permission.Read,
        Permission.Create,
        Permission.Update,
        Permission.Status,
        Permission.Delete,
        Resource.Read,
        Resource.Create,
        Resource.Update,
        Resource.Status,
        Resource.Delete,
        Operation.Read,
        Operation.Create,
        Operation.Update,
        Operation.Status,
        Operation.Delete,
        Menu.Read,
        Menu.Create,
        Menu.Update,
        Menu.Status,
        Menu.Delete,
        Department.Read,
        Department.Create,
        Department.Update,
        Department.Status,
        Department.Delete,
        User.Read,
        User.Create,
        User.Update,
        User.Status,
        User.Delete,
        UserSecurity.Read,
        UserSecurity.ResetPassword,
        UserSecurity.ResetTwoFactor,
        UserSecurity.Lock,
        UserSecurity.LoginPolicy,
        UserSession.Read,
        UserSession.Revoke,
        SessionRole.Read,
        UserStatistics.Read,
        PasswordHistory.Read,
        ExternalLogin.Read,
        OAuthApp.Read,
        OAuthApp.Create,
        OAuthApp.Update,
        OAuthApp.Status,
        OAuthApp.Delete,
        OAuthApp.Secret,
        OAuthCode.Read,
        OAuthToken.Read,
        AccessLog.Read,
        ApiLog.Read,
        DiffLog.Read,
        ExceptionLog.Read,
        LoginLog.Read,
        OperationLog.Read,
        PermissionChangeLog.Read,
        Task.Read,
        Task.Create,
        Task.Update,
        Task.Status,
        Task.RunStatus,
        Task.Delete,
        TaskLog.Read,
        Review.Read,
        Review.Create,
        Review.Update,
        Review.Status,
        Review.Audit,
        Review.Withdraw,
        Review.Delete,
        ReviewLog.Read,
        Config.Read,
        Config.Create,
        Config.Update,
        Config.Status,
        Config.Delete,
        Dict.Read,
        Dict.Create,
        Dict.Update,
        Dict.Status,
        Dict.Delete,
        Version.Read,
        Version.Create,
        Version.Update,
        Version.Upgrade,
        Version.Delete,
        File.Read,
        File.Create,
        File.Update,
        File.Status,
        File.Delete,
        Message.Read,
        Message.Create,
        Message.Update,
        Message.Status,
        Message.Publish,
        Message.Delete,
        MessageTemplate.Read,
        MessageTemplate.Create,
        MessageTemplate.Update,
        MessageTemplate.Status,
        MessageTemplate.Delete,
        Notification.Read,
        Notification.Create,
        Notification.Update,
        Notification.Publish,
        Notification.Delete,
        StorageConfig.Read,
        StorageConfig.Create,
        StorageConfig.Update,
        StorageConfig.Status,
        StorageConfig.Delete,
        Cache.Read,
        Cache.Clear,
        Server.Read,
        UserDepartment.Read,
        UserDepartment.Grant,
        UserDepartment.Update,
        UserDepartment.Status,
        UserDepartment.Revoke,
        Role.Read,
        Role.Create,
        Role.Update,
        Role.Status,
        Role.Delete,
        RoleHierarchy.Read,
        RoleHierarchy.Create,
        RoleHierarchy.Delete,
        RoleDataScope.Read,
        RoleDataScope.Grant,
        RoleDataScope.Update,
        RoleDataScope.Status,
        RoleDataScope.Revoke,
        RolePermission.Read,
        RolePermission.Grant,
        RolePermission.Update,
        RolePermission.Status,
        RolePermission.Revoke,
        UserRole.Read,
        UserRole.Grant,
        UserRole.Update,
        UserRole.Status,
        UserRole.Revoke,
        UserPermission.Read,
        UserPermission.Grant,
        UserPermission.Update,
        UserPermission.Status,
        UserPermission.Revoke,
        PermissionCondition.Read,
        PermissionCondition.Create,
        PermissionCondition.Update,
        PermissionCondition.Status,
        PermissionCondition.Delete,
        UserDataScope.Read,
        UserDataScope.Grant,
        UserDataScope.Update,
        UserDataScope.Status,
        UserDataScope.Revoke,
        FieldLevelSecurity.Read,
        FieldLevelSecurity.Create,
        FieldLevelSecurity.Update,
        FieldLevelSecurity.Status,
        FieldLevelSecurity.Delete,
        PermissionDelegation.Read,
        PermissionDelegation.Create,
        PermissionDelegation.Update,
        PermissionDelegation.Status,
        PermissionDelegation.Revoke,
        PermissionRequest.Read,
        PermissionRequest.Create,
        PermissionRequest.Update,
        PermissionRequest.Status,
        PermissionRequest.Withdraw,
        ConstraintRule.Read,
        ConstraintRule.Create,
        ConstraintRule.Update,
        ConstraintRule.Status,
        ConstraintRule.Delete,
        // 导入导出（逐资源细粒度，渐进登记）
        User.Export,
        OperationLog.Export,
        AccessLog.Export,
        ApiLog.Export,
        LoginLog.Export,
        ExceptionLog.Export,
        DiffLog.Export,
        Config.Import,
        Role.Export,
        Department.Export,
        Permission.Export,
        FieldLevelSecurity.Export,
        PermissionRequest.Export,
        Tenant.Export,
        MessageTemplate.Export,
        Review.Export,
        File.Export,
        OAuthApp.Export,
        Menu.Export,
        Dict.Export,
        Config.Export,
        Task.Export,
        Version.Export,
        UserSession.Export,
        Message.Export,
        Notification.Export,
        TenantEdition.Export,
        StorageConfig.Export,
        ConstraintRule.Export
    ];

    /// <summary>
    /// 租户权限码
    /// </summary>
    public static class Tenant
    {
        /// <summary>
        /// 分组编码（资源段）
        /// </summary>
        public const string Group = "tenant";

        /// <summary>
        /// 查看租户
        /// </summary>
        public const string Read = "saas:tenant:read";

        /// <summary>
        /// 创建租户
        /// </summary>
        public const string Create = "saas:tenant:create";

        /// <summary>
        /// 更新租户
        /// </summary>
        public const string Update = "saas:tenant:update";

        /// <summary>
        /// 更新租户状态
        /// </summary>
        public const string Status = "saas:tenant:status";

        /// <summary>
        /// 导出租户
        /// </summary>
        public const string Export = "saas:tenant:export";
    }

    /// <summary>
    /// 租户成员权限码
    /// </summary>
    public static class TenantMember
    {
        /// <summary>
        /// 分组编码（资源段）
        /// </summary>
        public const string Group = "tenant-member";

        /// <summary>
        /// 查看租户成员
        /// </summary>
        public const string Read = "saas:tenant-member:read";

        /// <summary>
        /// 更新租户成员
        /// </summary>
        public const string Update = "saas:tenant-member:update";

        /// <summary>
        /// 更新租户成员状态
        /// </summary>
        public const string Status = "saas:tenant-member:status";

        /// <summary>
        /// 更新租户成员邀请状态
        /// </summary>
        public const string InviteStatus = "saas:tenant-member:invite-status";

        /// <summary>
        /// 撤销租户成员
        /// </summary>
        public const string Revoke = "saas:tenant-member:revoke";
    }

    /// <summary>
    /// 租户版本权限码
    /// </summary>
    public static class TenantEdition
    {
        /// <summary>
        /// 分组编码（资源段）
        /// </summary>
        public const string Group = "tenant-edition";

        /// <summary>
        /// 查看租户版本
        /// </summary>
        public const string Read = "saas:tenant-edition:read";

        /// <summary>
        /// 创建租户版本
        /// </summary>
        public const string Create = "saas:tenant-edition:create";

        /// <summary>
        /// 更新租户版本
        /// </summary>
        public const string Update = "saas:tenant-edition:update";

        /// <summary>
        /// 更新租户版本状态
        /// </summary>
        public const string Status = "saas:tenant-edition:status";

        /// <summary>
        /// 设置默认租户版本
        /// </summary>
        public const string Default = "saas:tenant-edition:default";

        /// <summary>
        /// 导出租户版本
        /// </summary>
        public const string Export = "saas:tenant-edition:export";
    }

    /// <summary>
    /// 租户版本权限码
    /// </summary>
    public static class TenantEditionPermission
    {
        /// <summary>
        /// 分组编码（资源段）
        /// </summary>
        public const string Group = "tenant-edition-permission";

        /// <summary>
        /// 查看租户版本权限
        /// </summary>
        public const string Read = "saas:tenant-edition-permission:read";

        /// <summary>
        /// 授予租户版本权限
        /// </summary>
        public const string Grant = "saas:tenant-edition-permission:grant";

        /// <summary>
        /// 更新租户版本权限
        /// </summary>
        public const string Update = "saas:tenant-edition-permission:update";

        /// <summary>
        /// 撤销租户版本权限
        /// </summary>
        public const string Revoke = "saas:tenant-edition-permission:revoke";
    }

    /// <summary>
    /// 权限定义权限码
    /// </summary>
    public static class Permission
    {
        /// <summary>
        /// 分组编码（资源段）
        /// </summary>
        public const string Group = "permission";

        /// <summary>
        /// 查看权限定义
        /// </summary>
        public const string Read = "saas:permission:read";

        /// <summary>
        /// 创建权限定义
        /// </summary>
        public const string Create = "saas:permission:create";

        /// <summary>
        /// 更新权限定义
        /// </summary>
        public const string Update = "saas:permission:update";

        /// <summary>
        /// 更新权限定义状态
        /// </summary>
        public const string Status = "saas:permission:status";

        /// <summary>
        /// 删除权限定义
        /// </summary>
        public const string Delete = "saas:permission:delete";

        /// <summary>
        /// 导出权限定义
        /// </summary>
        public const string Export = "saas:permission:export";
    }

    /// <summary>
    /// 资源定义权限码
    /// </summary>
    public static class Resource
    {
        /// <summary>
        /// 分组编码（资源段）
        /// </summary>
        public const string Group = "resource";

        /// <summary>
        /// 查看资源定义
        /// </summary>
        public const string Read = "saas:resource:read";

        /// <summary>
        /// 创建资源定义
        /// </summary>
        public const string Create = "saas:resource:create";

        /// <summary>
        /// 更新资源定义
        /// </summary>
        public const string Update = "saas:resource:update";

        /// <summary>
        /// 更新资源定义状态
        /// </summary>
        public const string Status = "saas:resource:status";

        /// <summary>
        /// 删除资源定义
        /// </summary>
        public const string Delete = "saas:resource:delete";
    }

    /// <summary>
    /// 操作定义权限码
    /// </summary>
    public static class Operation
    {
        /// <summary>
        /// 分组编码（资源段）
        /// </summary>
        public const string Group = "operation";

        /// <summary>
        /// 查看操作定义
        /// </summary>
        public const string Read = "saas:operation:read";

        /// <summary>
        /// 创建操作定义
        /// </summary>
        public const string Create = "saas:operation:create";

        /// <summary>
        /// 更新操作定义
        /// </summary>
        public const string Update = "saas:operation:update";

        /// <summary>
        /// 更新操作定义状态
        /// </summary>
        public const string Status = "saas:operation:status";

        /// <summary>
        /// 删除操作定义
        /// </summary>
        public const string Delete = "saas:operation:delete";
    }

    /// <summary>
    /// 菜单权限码
    /// </summary>
    public static class Menu
    {
        /// <summary>
        /// 分组编码（资源段）
        /// </summary>
        public const string Group = "menu";

        /// <summary>
        /// 查看菜单
        /// </summary>
        public const string Read = "saas:menu:read";

        /// <summary>
        /// 创建菜单
        /// </summary>
        public const string Create = "saas:menu:create";

        /// <summary>
        /// 更新菜单
        /// </summary>
        public const string Update = "saas:menu:update";

        /// <summary>
        /// 更新菜单状态
        /// </summary>
        public const string Status = "saas:menu:status";

        /// <summary>
        /// 删除菜单
        /// </summary>
        public const string Delete = "saas:menu:delete";

        /// <summary>
        /// 导出菜单
        /// </summary>
        public const string Export = "saas:menu:export";
    }

    /// <summary>
    /// 部门权限码
    /// </summary>
    public static class Department
    {
        /// <summary>
        /// 分组编码（资源段）
        /// </summary>
        public const string Group = "department";

        /// <summary>
        /// 查看部门
        /// </summary>
        public const string Read = "saas:department:read";

        /// <summary>
        /// 创建部门
        /// </summary>
        public const string Create = "saas:department:create";

        /// <summary>
        /// 更新部门
        /// </summary>
        public const string Update = "saas:department:update";

        /// <summary>
        /// 更新部门状态
        /// </summary>
        public const string Status = "saas:department:status";

        /// <summary>
        /// 删除部门
        /// </summary>
        public const string Delete = "saas:department:delete";

        /// <summary>
        /// 导出部门
        /// </summary>
        public const string Export = "saas:department:export";
    }

    /// <summary>
    /// 用户权限码
    /// </summary>
    public static class User
    {
        /// <summary>
        /// 分组编码（资源段）
        /// </summary>
        public const string Group = "user";

        /// <summary>
        /// 查看用户
        /// </summary>
        public const string Read = "saas:user:read";

        /// <summary>
        /// 创建用户
        /// </summary>
        public const string Create = "saas:user:create";

        /// <summary>
        /// 更新用户
        /// </summary>
        public const string Update = "saas:user:update";

        /// <summary>
        /// 更新用户状态
        /// </summary>
        public const string Status = "saas:user:status";

        /// <summary>
        /// 删除用户
        /// </summary>
        public const string Delete = "saas:user:delete";

        /// <summary>
        /// 导出用户
        /// </summary>
        public const string Export = "saas:user:export";
    }

    /// <summary>
    /// 用户安全权限码
    /// </summary>
    public static class UserSecurity
    {
        /// <summary>
        /// 分组编码（资源段）
        /// </summary>
        public const string Group = "user-security";

        /// <summary>
        /// 查看用户安全状态
        /// </summary>
        public const string Read = "saas:user-security:read";

        /// <summary>
        /// 重置用户密码
        /// </summary>
        public const string ResetPassword = "saas:user-security:reset-password";

        /// <summary>
        /// 重置用户双因素认证（OTP）
        /// </summary>
        public const string ResetTwoFactor = "saas:user-security:reset-two-factor";

        /// <summary>
        /// 更新用户锁定状态
        /// </summary>
        public const string Lock = "saas:user-security:lock";

        /// <summary>
        /// 更新用户登录策略
        /// </summary>
        public const string LoginPolicy = "saas:user-security:login-policy";
    }

    /// <summary>
    /// 用户会话权限码
    /// </summary>
    public static class UserSession
    {
        /// <summary>
        /// 分组编码（资源段）
        /// </summary>
        public const string Group = "user-session";

        /// <summary>
        /// 查看用户会话
        /// </summary>
        public const string Read = "saas:user-session:read";

        /// <summary>
        /// 撤销用户会话
        /// </summary>
        public const string Revoke = "saas:user-session:revoke";

        /// <summary>
        /// 导出用户会话
        /// </summary>
        public const string Export = "saas:user-session:export";
    }

    /// <summary>
    /// 会话角色权限码
    /// </summary>
    public static class SessionRole
    {
        /// <summary>
        /// 分组编码（资源段）
        /// </summary>
        public const string Group = "session-role";

        /// <summary>
        /// 查看会话角色
        /// </summary>
        public const string Read = "saas:session-role:read";
    }

    /// <summary>
    /// 用户统计权限码
    /// </summary>
    public static class UserStatistics
    {
        /// <summary>
        /// 分组编码（资源段）
        /// </summary>
        public const string Group = "user-statistics";

        /// <summary>
        /// 查看用户统计
        /// </summary>
        public const string Read = "saas:user-statistics:read";
    }

    /// <summary>
    /// 密码历史权限码
    /// </summary>
    public static class PasswordHistory
    {
        /// <summary>
        /// 分组编码（资源段）
        /// </summary>
        public const string Group = "password-history";

        /// <summary>
        /// 查看密码历史
        /// </summary>
        public const string Read = "saas:password-history:read";
    }

    /// <summary>
    /// 第三方登录绑定权限码
    /// </summary>
    public static class ExternalLogin
    {
        /// <summary>
        /// 分组编码（资源段）
        /// </summary>
        public const string Group = "external-login";

        /// <summary>
        /// 查看第三方登录绑定
        /// </summary>
        public const string Read = "saas:external-login:read";
    }

    /// <summary>
    /// OAuth 应用权限码
    /// </summary>
    public static class OAuthApp
    {
        /// <summary>
        /// 分组编码（资源段）
        /// </summary>
        public const string Group = "oauth-app";

        /// <summary>
        /// 查看 OAuth 应用
        /// </summary>
        public const string Read = "saas:oauth-app:read";

        /// <summary>
        /// 创建 OAuth 应用
        /// </summary>
        public const string Create = "saas:oauth-app:create";

        /// <summary>
        /// 更新 OAuth 应用
        /// </summary>
        public const string Update = "saas:oauth-app:update";

        /// <summary>
        /// 更新 OAuth 应用状态
        /// </summary>
        public const string Status = "saas:oauth-app:status";

        /// <summary>
        /// 删除 OAuth 应用
        /// </summary>
        public const string Delete = "saas:oauth-app:delete";

        /// <summary>
        /// 重置 OAuth 应用密钥
        /// </summary>
        public const string Secret = "saas:oauth-app:secret";

        /// <summary>
        /// 导出 OAuth 应用
        /// </summary>
        public const string Export = "saas:oauth-app:export";
    }

    /// <summary>
    /// OAuth 授权码权限码
    /// </summary>
    public static class OAuthCode
    {
        /// <summary>
        /// 分组编码（资源段）
        /// </summary>
        public const string Group = "oauth-code";

        /// <summary>
        /// 查看 OAuth 授权码
        /// </summary>
        public const string Read = "saas:oauth-code:read";
    }

    /// <summary>
    /// OAuth Token 权限码
    /// </summary>
    public static class OAuthToken
    {
        /// <summary>
        /// 分组编码（资源段）
        /// </summary>
        public const string Group = "oauth-token";

        /// <summary>
        /// 查看 OAuth Token
        /// </summary>
        public const string Read = "saas:oauth-token:read";
    }

    /// <summary>
    /// 访问日志权限码
    /// </summary>
    public static class AccessLog
    {
        /// <summary>
        /// 分组编码（资源段）
        /// </summary>
        public const string Group = "access-log";

        /// <summary>
        /// 查看访问日志
        /// </summary>
        public const string Read = "saas:access-log:read";

        /// <summary>
        /// 导出访问日志
        /// </summary>
        public const string Export = "saas:access-log:export";
    }

    /// <summary>
    /// API 日志权限码
    /// </summary>
    public static class ApiLog
    {
        /// <summary>
        /// 分组编码（资源段）
        /// </summary>
        public const string Group = "api-log";

        /// <summary>
        /// 查看 API 日志
        /// </summary>
        public const string Read = "saas:api-log:read";

        /// <summary>
        /// 导出 API 日志
        /// </summary>
        public const string Export = "saas:api-log:export";
    }

    /// <summary>
    /// 差异日志权限码
    /// </summary>
    public static class DiffLog
    {
        /// <summary>
        /// 分组编码（资源段）
        /// </summary>
        public const string Group = "diff-log";

        /// <summary>
        /// 查看差异日志
        /// </summary>
        public const string Read = "saas:diff-log:read";

        /// <summary>
        /// 导出差异日志
        /// </summary>
        public const string Export = "saas:diff-log:export";
    }

    /// <summary>
    /// 异常日志权限码
    /// </summary>
    public static class ExceptionLog
    {
        /// <summary>
        /// 分组编码（资源段）
        /// </summary>
        public const string Group = "exception-log";

        /// <summary>
        /// 查看异常日志
        /// </summary>
        public const string Read = "saas:exception-log:read";

        /// <summary>
        /// 导出异常日志
        /// </summary>
        public const string Export = "saas:exception-log:export";
    }

    /// <summary>
    /// 登录日志权限码
    /// </summary>
    public static class LoginLog
    {
        /// <summary>
        /// 分组编码（资源段）
        /// </summary>
        public const string Group = "login-log";

        /// <summary>
        /// 查看登录日志
        /// </summary>
        public const string Read = "saas:login-log:read";

        /// <summary>
        /// 导出登录日志
        /// </summary>
        public const string Export = "saas:login-log:export";
    }

    /// <summary>
    /// 操作日志权限码
    /// </summary>
    public static class OperationLog
    {
        /// <summary>
        /// 分组编码（资源段）
        /// </summary>
        public const string Group = "operation-log";

        /// <summary>
        /// 查看操作日志
        /// </summary>
        public const string Read = "saas:operation-log:read";

        /// <summary>
        /// 导出操作日志
        /// </summary>
        public const string Export = "saas:operation-log:export";
    }

    /// <summary>
    /// 权限变更日志权限码
    /// </summary>
    public static class PermissionChangeLog
    {
        /// <summary>
        /// 分组编码（资源段）
        /// </summary>
        public const string Group = "permission-change-log";

        /// <summary>
        /// 查看权限变更日志
        /// </summary>
        public const string Read = "saas:permission-change-log:read";
    }

    /// <summary>
    /// 系统任务权限码
    /// </summary>
    public static class Task
    {
        /// <summary>
        /// 分组编码（资源段）
        /// </summary>
        public const string Group = "task";

        /// <summary>
        /// 查看系统任务
        /// </summary>
        public const string Read = "saas:task:read";

        /// <summary>
        /// 创建系统任务
        /// </summary>
        public const string Create = "saas:task:create";

        /// <summary>
        /// 更新系统任务
        /// </summary>
        public const string Update = "saas:task:update";

        /// <summary>
        /// 更新系统任务启停状态
        /// </summary>
        public const string Status = "saas:task:status";

        /// <summary>
        /// 更新系统任务运行状态
        /// </summary>
        public const string RunStatus = "saas:task:run-status";

        /// <summary>
        /// 删除系统任务
        /// </summary>
        public const string Delete = "saas:task:delete";

        /// <summary>
        /// 导出系统任务
        /// </summary>
        public const string Export = "saas:task:export";
    }

    /// <summary>
    /// 任务日志权限码
    /// </summary>
    public static class TaskLog
    {
        /// <summary>
        /// 分组编码（资源段）
        /// </summary>
        public const string Group = "task-log";

        /// <summary>
        /// 查看任务日志
        /// </summary>
        public const string Read = "saas:task-log:read";
    }

    /// <summary>
    /// 系统审查权限码
    /// </summary>
    public static class Review
    {
        /// <summary>
        /// 分组编码（资源段）
        /// </summary>
        public const string Group = "review";

        /// <summary>
        /// 查看系统审查
        /// </summary>
        public const string Read = "saas:review:read";

        /// <summary>
        /// 创建系统审查
        /// </summary>
        public const string Create = "saas:review:create";

        /// <summary>
        /// 更新系统审查
        /// </summary>
        public const string Update = "saas:review:update";

        /// <summary>
        /// 更新系统审查启停状态
        /// </summary>
        public const string Status = "saas:review:status";

        /// <summary>
        /// 审核系统审查
        /// </summary>
        public const string Audit = "saas:review:audit";

        /// <summary>
        /// 撤回系统审查
        /// </summary>
        public const string Withdraw = "saas:review:withdraw";

        /// <summary>
        /// 删除系统审查
        /// </summary>
        public const string Delete = "saas:review:delete";

        /// <summary>
        /// 导出系统审查
        /// </summary>
        public const string Export = "saas:review:export";
    }

    /// <summary>
    /// 审查日志权限码
    /// </summary>
    public static class ReviewLog
    {
        /// <summary>
        /// 分组编码（资源段）
        /// </summary>
        public const string Group = "review-log";

        /// <summary>
        /// 查看审查日志
        /// </summary>
        public const string Read = "saas:review-log:read";
    }

    /// <summary>
    /// 系统配置权限码
    /// </summary>
    public static class Config
    {
        /// <summary>
        /// 分组编码（资源段）
        /// </summary>
        public const string Group = "config";

        /// <summary>
        /// 查看系统配置
        /// </summary>
        public const string Read = "saas:config:read";

        /// <summary>
        /// 创建系统配置
        /// </summary>
        public const string Create = "saas:config:create";

        /// <summary>
        /// 更新系统配置
        /// </summary>
        public const string Update = "saas:config:update";

        /// <summary>
        /// 更新系统配置状态
        /// </summary>
        public const string Status = "saas:config:status";

        /// <summary>
        /// 删除系统配置
        /// </summary>
        public const string Delete = "saas:config:delete";

        /// <summary>
        /// 导入系统配置
        /// </summary>
        public const string Import = "saas:config:import";

        /// <summary>
        /// 导出系统配置
        /// </summary>
        public const string Export = "saas:config:export";
    }

    /// <summary>
    /// 系统字典权限码
    /// </summary>
    public static class Dict
    {
        /// <summary>
        /// 分组编码（资源段）
        /// </summary>
        public const string Group = "dict";

        /// <summary>
        /// 查看系统字典
        /// </summary>
        public const string Read = "saas:dict:read";

        /// <summary>
        /// 创建系统字典
        /// </summary>
        public const string Create = "saas:dict:create";

        /// <summary>
        /// 更新系统字典
        /// </summary>
        public const string Update = "saas:dict:update";

        /// <summary>
        /// 更新系统字典状态
        /// </summary>
        public const string Status = "saas:dict:status";

        /// <summary>
        /// 删除系统字典
        /// </summary>
        public const string Delete = "saas:dict:delete";

        /// <summary>
        /// 导出系统字典
        /// </summary>
        public const string Export = "saas:dict:export";
    }

    /// <summary>
    /// 系统版本权限码
    /// </summary>
    public static class Version
    {
        /// <summary>
        /// 分组编码（资源段）
        /// </summary>
        public const string Group = "version";

        /// <summary>
        /// 查看系统版本
        /// </summary>
        public const string Read = "saas:version:read";

        /// <summary>
        /// 创建系统版本
        /// </summary>
        public const string Create = "saas:version:create";

        /// <summary>
        /// 更新系统版本
        /// </summary>
        public const string Update = "saas:version:update";

        /// <summary>
        /// 更新系统升级状态
        /// </summary>
        public const string Upgrade = "saas:version:upgrade";

        /// <summary>
        /// 删除系统版本
        /// </summary>
        public const string Delete = "saas:version:delete";

        /// <summary>
        /// 导出系统版本
        /// </summary>
        public const string Export = "saas:version:export";
    }

    /// <summary>
    /// 系统文件权限码
    /// </summary>
    public static class File
    {
        /// <summary>
        /// 分组编码（资源段）
        /// </summary>
        public const string Group = "file";

        /// <summary>
        /// 查看系统文件
        /// </summary>
        public const string Read = "saas:file:read";

        /// <summary>
        /// 创建系统文件
        /// </summary>
        public const string Create = "saas:file:create";

        /// <summary>
        /// 更新系统文件
        /// </summary>
        public const string Update = "saas:file:update";

        /// <summary>
        /// 更新系统文件状态
        /// </summary>
        public const string Status = "saas:file:status";

        /// <summary>
        /// 删除系统文件
        /// </summary>
        public const string Delete = "saas:file:delete";

        /// <summary>
        /// 导出系统文件
        /// </summary>
        public const string Export = "saas:file:export";
    }

    /// <summary>
    /// 系统消息权限码
    /// </summary>
    public static class Message
    {
        /// <summary>
        /// 分组编码（资源段）
        /// </summary>
        public const string Group = "message";

        /// <summary>
        /// 查看系统消息
        /// </summary>
        public const string Read = "saas:message:read";

        /// <summary>
        /// 创建系统消息
        /// </summary>
        public const string Create = "saas:message:create";

        /// <summary>
        /// 更新系统消息
        /// </summary>
        public const string Update = "saas:message:update";

        /// <summary>
        /// 更新系统消息状态
        /// </summary>
        public const string Status = "saas:message:status";

        /// <summary>
        /// 发布系统通知
        /// </summary>
        public const string Publish = "saas:message:publish";

        /// <summary>
        /// 删除系统消息
        /// </summary>
        public const string Delete = "saas:message:delete";

        /// <summary>
        /// 导出系统消息
        /// </summary>
        public const string Export = "saas:message:export";
    }

    /// <summary>
    /// 消息模板权限码（邮件/短信/通知内容模板管理）
    /// </summary>
    public static class MessageTemplate
    {
        /// <summary>
        /// 分组编码（资源段）
        /// </summary>
        public const string Group = "message-template";

        /// <summary>
        /// 查看消息模板
        /// </summary>
        public const string Read = "saas:message-template:read";

        /// <summary>
        /// 创建消息模板
        /// </summary>
        public const string Create = "saas:message-template:create";

        /// <summary>
        /// 更新消息模板
        /// </summary>
        public const string Update = "saas:message-template:update";

        /// <summary>
        /// 更新消息模板状态
        /// </summary>
        public const string Status = "saas:message-template:status";

        /// <summary>
        /// 删除消息模板
        /// </summary>
        public const string Delete = "saas:message-template:delete";

        /// <summary>
        /// 导出消息模板
        /// </summary>
        public const string Export = "saas:message-template:export";
    }

    /// <summary>
    /// 系统通知权限码（站内通知/公告，独立于邮件短信的 Message 族，支持独立授权）
    /// </summary>
    public static class Notification
    {
        /// <summary>
        /// 分组编码（资源段）
        /// </summary>
        public const string Group = "notification";

        /// <summary>
        /// 查看系统通知
        /// </summary>
        public const string Read = "saas:notification:read";

        /// <summary>
        /// 创建系统通知
        /// </summary>
        public const string Create = "saas:notification:create";

        /// <summary>
        /// 更新系统通知
        /// </summary>
        public const string Update = "saas:notification:update";

        /// <summary>
        /// 发布系统通知
        /// </summary>
        public const string Publish = "saas:notification:publish";

        /// <summary>
        /// 删除系统通知
        /// </summary>
        public const string Delete = "saas:notification:delete";

        /// <summary>
        /// 导出系统通知
        /// </summary>
        public const string Export = "saas:notification:export";
    }

    /// <summary>
    /// 存储配置权限码（对象存储/本地存储等存储后端配置，独立于参数配置的 Config 族）
    /// </summary>
    public static class StorageConfig
    {
        /// <summary>
        /// 分组编码（资源段）
        /// </summary>
        public const string Group = "storage-config";

        /// <summary>
        /// 查看存储配置
        /// </summary>
        public const string Read = "saas:storage-config:read";

        /// <summary>
        /// 创建存储配置
        /// </summary>
        public const string Create = "saas:storage-config:create";

        /// <summary>
        /// 更新存储配置
        /// </summary>
        public const string Update = "saas:storage-config:update";

        /// <summary>
        /// 更新存储配置状态
        /// </summary>
        public const string Status = "saas:storage-config:status";

        /// <summary>
        /// 删除存储配置
        /// </summary>
        public const string Delete = "saas:storage-config:delete";

        /// <summary>
        /// 导出存储配置
        /// </summary>
        public const string Export = "saas:storage-config:export";
    }

    /// <summary>
    /// 缓存管理权限码（平台运维专属：查看/清理分布式缓存）
    /// </summary>
    public static class Cache
    {
        /// <summary>
        /// 分组编码（资源段）
        /// </summary>
        public const string Group = "cache";

        /// <summary>
        /// 查看缓存
        /// </summary>
        public const string Read = "saas:cache:read";

        /// <summary>
        /// 清理缓存
        /// </summary>
        public const string Clear = "saas:cache:clear";
    }

    /// <summary>
    /// 服务监控权限码（平台运维专属：查看服务器与运行时指标）
    /// </summary>
    public static class Server
    {
        /// <summary>
        /// 分组编码（资源段）
        /// </summary>
        public const string Group = "server";

        /// <summary>
        /// 查看服务监控
        /// </summary>
        public const string Read = "saas:server:read";
    }

    /// <summary>
    /// 用户部门归属权限码
    /// </summary>
    public static class UserDepartment
    {
        /// <summary>
        /// 分组编码（资源段）
        /// </summary>
        public const string Group = "user-department";

        /// <summary>
        /// 查看用户部门归属
        /// </summary>
        public const string Read = "saas:user-department:read";

        /// <summary>
        /// 分配用户部门归属
        /// </summary>
        public const string Grant = "saas:user-department:grant";

        /// <summary>
        /// 更新用户部门归属
        /// </summary>
        public const string Update = "saas:user-department:update";

        /// <summary>
        /// 更新用户部门归属状态
        /// </summary>
        public const string Status = "saas:user-department:status";

        /// <summary>
        /// 撤销用户部门归属
        /// </summary>
        public const string Revoke = "saas:user-department:revoke";
    }

    /// <summary>
    /// 角色定义权限码
    /// </summary>
    public static class Role
    {
        /// <summary>
        /// 分组编码（资源段）
        /// </summary>
        public const string Group = "role";

        /// <summary>
        /// 查看角色定义
        /// </summary>
        public const string Read = "saas:role:read";

        /// <summary>
        /// 创建角色定义
        /// </summary>
        public const string Create = "saas:role:create";

        /// <summary>
        /// 更新角色定义
        /// </summary>
        public const string Update = "saas:role:update";

        /// <summary>
        /// 更新角色状态
        /// </summary>
        public const string Status = "saas:role:status";

        /// <summary>
        /// 删除角色定义
        /// </summary>
        public const string Delete = "saas:role:delete";

        /// <summary>
        /// 导出角色
        /// </summary>
        public const string Export = "saas:role:export";
    }

    /// <summary>
    /// 角色继承权限码
    /// </summary>
    public static class RoleHierarchy
    {
        /// <summary>
        /// 分组编码（资源段）
        /// </summary>
        public const string Group = "role-hierarchy";

        /// <summary>
        /// 查看角色继承
        /// </summary>
        public const string Read = "saas:role-hierarchy:read";

        /// <summary>
        /// 创建角色继承
        /// </summary>
        public const string Create = "saas:role-hierarchy:create";

        /// <summary>
        /// 删除角色继承
        /// </summary>
        public const string Delete = "saas:role-hierarchy:delete";
    }

    /// <summary>
    /// 角色数据范围权限码
    /// </summary>
    public static class RoleDataScope
    {
        /// <summary>
        /// 分组编码（资源段）
        /// </summary>
        public const string Group = "role-data-scope";

        /// <summary>
        /// 查看角色数据范围
        /// </summary>
        public const string Read = "saas:role-data-scope:read";

        /// <summary>
        /// 授予角色数据范围
        /// </summary>
        public const string Grant = "saas:role-data-scope:grant";

        /// <summary>
        /// 更新角色数据范围
        /// </summary>
        public const string Update = "saas:role-data-scope:update";

        /// <summary>
        /// 更新角色数据范围状态
        /// </summary>
        public const string Status = "saas:role-data-scope:status";

        /// <summary>
        /// 撤销角色数据范围
        /// </summary>
        public const string Revoke = "saas:role-data-scope:revoke";
    }

    /// <summary>
    /// 角色权限权限码
    /// </summary>
    public static class RolePermission
    {
        /// <summary>
        /// 分组编码（资源段）
        /// </summary>
        public const string Group = "role-permission";

        /// <summary>
        /// 查看角色权限
        /// </summary>
        public const string Read = "saas:role-permission:read";

        /// <summary>
        /// 授予角色权限
        /// </summary>
        public const string Grant = "saas:role-permission:grant";

        /// <summary>
        /// 更新角色权限
        /// </summary>
        public const string Update = "saas:role-permission:update";

        /// <summary>
        /// 更新角色权限状态
        /// </summary>
        public const string Status = "saas:role-permission:status";

        /// <summary>
        /// 撤销角色权限
        /// </summary>
        public const string Revoke = "saas:role-permission:revoke";
    }

    /// <summary>
    /// 用户角色权限码
    /// </summary>
    public static class UserRole
    {
        /// <summary>
        /// 分组编码（资源段）
        /// </summary>
        public const string Group = "user-role";

        /// <summary>
        /// 查看用户角色
        /// </summary>
        public const string Read = "saas:user-role:read";

        /// <summary>
        /// 授予用户角色
        /// </summary>
        public const string Grant = "saas:user-role:grant";

        /// <summary>
        /// 更新用户角色
        /// </summary>
        public const string Update = "saas:user-role:update";

        /// <summary>
        /// 更新用户角色状态
        /// </summary>
        public const string Status = "saas:user-role:status";

        /// <summary>
        /// 撤销用户角色
        /// </summary>
        public const string Revoke = "saas:user-role:revoke";
    }

    /// <summary>
    /// 用户直授权限权限码
    /// </summary>
    public static class UserPermission
    {
        /// <summary>
        /// 分组编码（资源段）
        /// </summary>
        public const string Group = "user-permission";

        /// <summary>
        /// 查看用户直授权限
        /// </summary>
        public const string Read = "saas:user-permission:read";

        /// <summary>
        /// 授予用户直授权限
        /// </summary>
        public const string Grant = "saas:user-permission:grant";

        /// <summary>
        /// 更新用户直授权限
        /// </summary>
        public const string Update = "saas:user-permission:update";

        /// <summary>
        /// 更新用户直授权限状态
        /// </summary>
        public const string Status = "saas:user-permission:status";

        /// <summary>
        /// 撤销用户直授权限
        /// </summary>
        public const string Revoke = "saas:user-permission:revoke";
    }

    /// <summary>
    /// 用户数据范围权限码
    /// </summary>
    public static class UserDataScope
    {
        /// <summary>
        /// 分组编码（资源段）
        /// </summary>
        public const string Group = "user-data-scope";

        /// <summary>
        /// 查看用户数据范围
        /// </summary>
        public const string Read = "saas:user-data-scope:read";

        /// <summary>
        /// 授予用户数据范围
        /// </summary>
        public const string Grant = "saas:user-data-scope:grant";

        /// <summary>
        /// 更新用户数据范围
        /// </summary>
        public const string Update = "saas:user-data-scope:update";

        /// <summary>
        /// 更新用户数据范围状态
        /// </summary>
        public const string Status = "saas:user-data-scope:status";

        /// <summary>
        /// 撤销用户数据范围
        /// </summary>
        public const string Revoke = "saas:user-data-scope:revoke";
    }

    /// <summary>
    /// 字段级安全权限码
    /// </summary>
    public static class FieldLevelSecurity
    {
        /// <summary>
        /// 分组编码（资源段）
        /// </summary>
        public const string Group = "field-level-security";

        /// <summary>
        /// 查看字段级安全
        /// </summary>
        public const string Read = "saas:field-level-security:read";

        /// <summary>
        /// 创建字段级安全
        /// </summary>
        public const string Create = "saas:field-level-security:create";

        /// <summary>
        /// 更新字段级安全
        /// </summary>
        public const string Update = "saas:field-level-security:update";

        /// <summary>
        /// 更新字段级安全状态
        /// </summary>
        public const string Status = "saas:field-level-security:status";

        /// <summary>
        /// 删除字段级安全
        /// </summary>
        public const string Delete = "saas:field-level-security:delete";

        /// <summary>
        /// 导出字段级安全
        /// </summary>
        public const string Export = "saas:field-level-security:export";
    }

    /// <summary>
    /// 权限委托权限码
    /// </summary>
    public static class PermissionDelegation
    {
        /// <summary>
        /// 分组编码（资源段）
        /// </summary>
        public const string Group = "permission-delegation";

        /// <summary>
        /// 查看权限委托
        /// </summary>
        public const string Read = "saas:permission-delegation:read";

        /// <summary>
        /// 创建权限委托
        /// </summary>
        public const string Create = "saas:permission-delegation:create";

        /// <summary>
        /// 更新权限委托
        /// </summary>
        public const string Update = "saas:permission-delegation:update";

        /// <summary>
        /// 更新权限委托状态
        /// </summary>
        public const string Status = "saas:permission-delegation:status";

        /// <summary>
        /// 撤销权限委托
        /// </summary>
        public const string Revoke = "saas:permission-delegation:revoke";
    }

    /// <summary>
    /// 权限申请权限码
    /// </summary>
    public static class PermissionRequest
    {
        /// <summary>
        /// 分组编码（资源段）
        /// </summary>
        public const string Group = "permission-request";

        /// <summary>
        /// 查看权限申请
        /// </summary>
        public const string Read = "saas:permission-request:read";

        /// <summary>
        /// 创建权限申请
        /// </summary>
        public const string Create = "saas:permission-request:create";

        /// <summary>
        /// 更新权限申请
        /// </summary>
        public const string Update = "saas:permission-request:update";

        /// <summary>
        /// 更新权限申请状态
        /// </summary>
        public const string Status = "saas:permission-request:status";

        /// <summary>
        /// 撤回权限申请
        /// </summary>
        public const string Withdraw = "saas:permission-request:withdraw";

        /// <summary>
        /// 导出权限申请
        /// </summary>
        public const string Export = "saas:permission-request:export";
    }

    /// <summary>
    /// 权限 ABAC 条件权限码
    /// </summary>
    public static class PermissionCondition
    {
        /// <summary>
        /// 分组编码（资源段）
        /// </summary>
        public const string Group = "permission-condition";

        /// <summary>
        /// 查看权限 ABAC 条件
        /// </summary>
        public const string Read = "saas:permission-condition:read";

        /// <summary>
        /// 创建权限 ABAC 条件
        /// </summary>
        public const string Create = "saas:permission-condition:create";

        /// <summary>
        /// 更新权限 ABAC 条件
        /// </summary>
        public const string Update = "saas:permission-condition:update";

        /// <summary>
        /// 更新权限 ABAC 条件状态
        /// </summary>
        public const string Status = "saas:permission-condition:status";

        /// <summary>
        /// 删除权限 ABAC 条件
        /// </summary>
        public const string Delete = "saas:permission-condition:delete";
    }

    /// <summary>
    /// 约束规则权限码
    /// </summary>
    public static class ConstraintRule
    {
        /// <summary>
        /// 分组编码（资源段）
        /// </summary>
        public const string Group = "constraint-rule";

        /// <summary>
        /// 查看约束规则
        /// </summary>
        public const string Read = "saas:constraint-rule:read";

        /// <summary>
        /// 创建约束规则
        /// </summary>
        public const string Create = "saas:constraint-rule:create";

        /// <summary>
        /// 更新约束规则
        /// </summary>
        public const string Update = "saas:constraint-rule:update";

        /// <summary>
        /// 更新约束规则状态
        /// </summary>
        public const string Status = "saas:constraint-rule:status";

        /// <summary>
        /// 删除约束规则
        /// </summary>
        public const string Delete = "saas:constraint-rule:delete";

        /// <summary>
        /// 导出约束规则
        /// </summary>
        public const string Export = "saas:constraint-rule:export";
    }
}
