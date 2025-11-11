#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RbacConstants
// Guid:4b2b3c4d-5e6f-7890-abcd-ef12345678a9
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/10/31 5:35:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Constants;

/// <summary>
/// RBAC 常量
/// </summary>
public static class RbacConstants
{
    /// <summary>
    /// 默认密码
    /// </summary>
    public const string DefaultPassword = "123456";

    /// <summary>
    /// 超级管理员角色编码
    /// </summary>
    public const string SuperAdminRoleCode = "super_admin";

    /// <summary>
    /// 管理员角色编码
    /// </summary>
    public const string AdminRoleCode = "admin";

    /// <summary>
    /// 普通用户角色编码
    /// </summary>
    public const string UserRoleCode = "user";

    /// <summary>
    /// 游客角色编码
    /// </summary>
    public const string GuestRoleCode = "guest";

    /// <summary>
    /// 系统用户名
    /// </summary>
    public const string SystemUserName = "system";

    /// <summary>
    /// 匿名用户名
    /// </summary>
    public const string AnonymousUserName = "anonymous";

    /// <summary>
    /// 默认租户编码
    /// </summary>
    public const string DefaultTenantCode = "default";
}

/// <summary>
/// 权限常量
/// </summary>
public static class PermissionConstants
{
    /// <summary>
    /// 权限分隔符
    /// </summary>
    public const string Separator = ":";

    /// <summary>
    /// 全部权限
    /// </summary>
    public const string All = "*:*:*";

    #region 用户权限

    /// <summary>
    /// 用户查看权限
    /// </summary>
    public const string UserView = "system:user:view";

    /// <summary>
    /// 用户创建权限
    /// </summary>
    public const string UserCreate = "system:user:create";

    /// <summary>
    /// 用户编辑权限
    /// </summary>
    public const string UserEdit = "system:user:edit";

    /// <summary>
    /// 用户删除权限
    /// </summary>
    public const string UserDelete = "system:user:delete";

    /// <summary>
    /// 用户导出权限
    /// </summary>
    public const string UserExport = "system:user:export";

    /// <summary>
    /// 用户导入权限
    /// </summary>
    public const string UserImport = "system:user:import";

    /// <summary>
    /// 用户重置密码权限
    /// </summary>
    public const string UserResetPassword = "system:user:resetPassword";

    #endregion

    #region 角色权限

    /// <summary>
    /// 角色查看权限
    /// </summary>
    public const string RoleView = "system:role:view";

    /// <summary>
    /// 角色创建权限
    /// </summary>
    public const string RoleCreate = "system:role:create";

    /// <summary>
    /// 角色编辑权限
    /// </summary>
    public const string RoleEdit = "system:role:edit";

    /// <summary>
    /// 角色删除权限
    /// </summary>
    public const string RoleDelete = "system:role:delete";

    /// <summary>
    /// 角色分配权限
    /// </summary>
    public const string RoleAssign = "system:role:assign";

    #endregion

    #region 菜单权限

    /// <summary>
    /// 菜单查看权限
    /// </summary>
    public const string MenuView = "system:menu:view";

    /// <summary>
    /// 菜单创建权限
    /// </summary>
    public const string MenuCreate = "system:menu:create";

    /// <summary>
    /// 菜单编辑权限
    /// </summary>
    public const string MenuEdit = "system:menu:edit";

    /// <summary>
    /// 菜单删除权限
    /// </summary>
    public const string MenuDelete = "system:menu:delete";

    #endregion

    #region 部门权限

    /// <summary>
    /// 部门查看权限
    /// </summary>
    public const string DepartmentView = "system:department:view";

    /// <summary>
    /// 部门创建权限
    /// </summary>
    public const string DepartmentCreate = "system:department:create";

    /// <summary>
    /// 部门编辑权限
    /// </summary>
    public const string DepartmentEdit = "system:department:edit";

    /// <summary>
    /// 部门删除权限
    /// </summary>
    public const string DepartmentDelete = "system:department:delete";

    #endregion

    #region 租户权限

    /// <summary>
    /// 租户查看权限
    /// </summary>
    public const string TenantView = "system:tenant:view";

    /// <summary>
    /// 租户创建权限
    /// </summary>
    public const string TenantCreate = "system:tenant:create";

    /// <summary>
    /// 租户编辑权限
    /// </summary>
    public const string TenantEdit = "system:tenant:edit";

    /// <summary>
    /// 租户删除权限
    /// </summary>
    public const string TenantDelete = "system:tenant:delete";

    /// <summary>
    /// 租户配置权限
    /// </summary>
    public const string TenantConfig = "system:tenant:config";

    #endregion
}

/// <summary>
/// 缓存键常量
/// </summary>
public static class CacheKeyConstants
{
    /// <summary>
    /// 缓存键前缀
    /// </summary>
    private const string Prefix = "rbac";

    /// <summary>
    /// 用户缓存键前缀
    /// </summary>
    public const string UserPrefix = $"{Prefix}:user";

    /// <summary>
    /// 角色缓存键前缀
    /// </summary>
    public const string RolePrefix = $"{Prefix}:role";

    /// <summary>
    /// 权限缓存键前缀
    /// </summary>
    public const string PermissionPrefix = $"{Prefix}:permission";

    /// <summary>
    /// 菜单缓存键前缀
    /// </summary>
    public const string MenuPrefix = $"{Prefix}:menu";

    /// <summary>
    /// 部门缓存键前缀
    /// </summary>
    public const string DepartmentPrefix = $"{Prefix}:department";

    /// <summary>
    /// 租户缓存键前缀
    /// </summary>
    public const string TenantPrefix = $"{Prefix}:tenant";

    /// <summary>
    /// 用户权限缓存键
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    public static string UserPermissions(RbacIdType userId) => $"{UserPrefix}:permissions:{userId}";

    /// <summary>
    /// 用户角色缓存键
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    public static string UserRoles(RbacIdType userId) => $"{UserPrefix}:roles:{userId}";

    /// <summary>
    /// 用户菜单缓存键
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    public static string UserMenus(RbacIdType userId) => $"{UserPrefix}:menus:{userId}";

    /// <summary>
    /// 角色菜单缓存键
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <returns></returns>
    public static string RoleMenus(RbacIdType roleId) => $"{RolePrefix}:menus:{roleId}";

    /// <summary>
    /// 角色权限缓存键
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <returns></returns>
    public static string RolePermissions(RbacIdType roleId) => $"{RolePrefix}:permissions:{roleId}";

    /// <summary>
    /// 菜单树缓存键
    /// </summary>
    public const string MenuTree = $"{MenuPrefix}:tree";

    /// <summary>
    /// 部门树缓存键
    /// </summary>
    public const string DepartmentTree = $"{DepartmentPrefix}:tree";
}

/// <summary>
/// 错误消息常量
/// </summary>
public static class ErrorMessageConstants
{
    #region 用户错误消息

    /// <summary>
    /// 用户不存在
    /// </summary>
    public const string UserNotFound = "用户不存在";

    /// <summary>
    /// 用户名已存在
    /// </summary>
    public const string UserNameExists = "用户名已存在";

    /// <summary>
    /// 邮箱已存在
    /// </summary>
    public const string EmailExists = "邮箱已存在";

    /// <summary>
    /// 手机号已存在
    /// </summary>
    public const string PhoneExists = "手机号已存在";

    /// <summary>
    /// 原密码错误
    /// </summary>
    public const string OldPasswordError = "原密码错误";

    /// <summary>
    /// 两次密码不一致
    /// </summary>
    public const string PasswordNotMatch = "两次密码不一致";

    #endregion

    #region 角色错误消息

    /// <summary>
    /// 角色不存在
    /// </summary>
    public const string RoleNotFound = "角色不存在";

    /// <summary>
    /// 角色编码已存在
    /// </summary>
    public const string RoleCodeExists = "角色编码已存在";

    /// <summary>
    /// 角色已分配给用户，无法删除
    /// </summary>
    public const string RoleHasUsers = "角色已分配给用户，无法删除";

    #endregion

    #region 权限错误消息

    /// <summary>
    /// 权限不存在
    /// </summary>
    public const string PermissionNotFound = "权限不存在";

    /// <summary>
    /// 权限编码已存在
    /// </summary>
    public const string PermissionCodeExists = "权限编码已存在";

    #endregion

    #region 菜单错误消息

    /// <summary>
    /// 菜单不存在
    /// </summary>
    public const string MenuNotFound = "菜单不存在";

    /// <summary>
    /// 菜单编码已存在
    /// </summary>
    public const string MenuCodeExists = "菜单编码已存在";

    /// <summary>
    /// 菜单有子菜单，无法删除
    /// </summary>
    public const string MenuHasChildren = "菜单有子菜单，无法删除";

    #endregion

    #region 部门错误消息

    /// <summary>
    /// 部门不存在
    /// </summary>
    public const string DepartmentNotFound = "部门不存在";

    /// <summary>
    /// 部门编码已存在
    /// </summary>
    public const string DepartmentCodeExists = "部门编码已存在";

    /// <summary>
    /// 部门有子部门，无法删除
    /// </summary>
    public const string DepartmentHasChildren = "部门有子部门，无法删除";

    /// <summary>
    /// 部门有用户，无法删除
    /// </summary>
    public const string DepartmentHasUsers = "部门有用户，无法删除";

    #endregion

    #region 租户错误消息

    /// <summary>
    /// 租户不存在
    /// </summary>
    public const string TenantNotFound = "租户不存在";

    /// <summary>
    /// 租户编码已存在
    /// </summary>
    public const string TenantCodeExists = "租户编码已存在";

    /// <summary>
    /// 域名已存在
    /// </summary>
    public const string DomainExists = "域名已存在";

    /// <summary>
    /// 租户已过期
    /// </summary>
    public const string TenantExpired = "租户已过期";

    /// <summary>
    /// 租户已被禁用
    /// </summary>
    public const string TenantDisabled = "租户已被禁用";

    #endregion
}