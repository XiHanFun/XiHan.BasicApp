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
        UserSecurity.Lock,
        UserSecurity.LoginPolicy,
        UserSession.Read,
        UserSession.Revoke,
        UserStatistics.Read,
        PasswordHistory.Read,
        ExternalLogin.Read,
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
        ConstraintRule.Delete
    ];

    /// <summary>
    /// 租户权限码
    /// </summary>
    public static class Tenant
    {
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
    }

    /// <summary>
    /// 租户成员权限码
    /// </summary>
    public static class TenantMember
    {
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
    }

    /// <summary>
    /// 租户版本权限码
    /// </summary>
    public static class TenantEditionPermission
    {
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
    }

    /// <summary>
    /// 资源定义权限码
    /// </summary>
    public static class Resource
    {
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
    }

    /// <summary>
    /// 部门权限码
    /// </summary>
    public static class Department
    {
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
    }

    /// <summary>
    /// 用户权限码
    /// </summary>
    public static class User
    {
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
    }

    /// <summary>
    /// 用户安全权限码
    /// </summary>
    public static class UserSecurity
    {
        /// <summary>
        /// 查看用户安全状态
        /// </summary>
        public const string Read = "saas:user-security:read";

        /// <summary>
        /// 重置用户密码
        /// </summary>
        public const string ResetPassword = "saas:user-security:reset-password";

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
        /// 查看用户会话
        /// </summary>
        public const string Read = "saas:user-session:read";

        /// <summary>
        /// 撤销用户会话
        /// </summary>
        public const string Revoke = "saas:user-session:revoke";
    }

    /// <summary>
    /// 用户统计权限码
    /// </summary>
    public static class UserStatistics
    {
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
        /// 查看第三方登录绑定
        /// </summary>
        public const string Read = "saas:external-login:read";
    }

    /// <summary>
    /// 用户部门归属权限码
    /// </summary>
    public static class UserDepartment
    {
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
    }

    /// <summary>
    /// 角色继承权限码
    /// </summary>
    public static class RoleHierarchy
    {
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
    }

    /// <summary>
    /// 权限委托权限码
    /// </summary>
    public static class PermissionDelegation
    {
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
    }

    /// <summary>
    /// 权限 ABAC 条件权限码
    /// </summary>
    public static class PermissionCondition
    {
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
    }
}
