#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionConstants
// Guid:26a84679-8924-47f7-beab-0a4500f47181
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/11/13 2:58:52
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Constants;

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
