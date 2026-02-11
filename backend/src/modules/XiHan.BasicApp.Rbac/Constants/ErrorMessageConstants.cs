#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ErrorMessageConstants
// Guid:3a33eb19-9d4d-4b3b-815b-2513e048cefd
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/11/13 03:00:13
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Constants;

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
