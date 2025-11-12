#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:CacheKeyConstants
// Guid:86edf339-188c-4181-b106-42a1a032ace4
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/11/13 2:59:18
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Constants;

/// <summary>
/// 缓存键常量
/// </summary>
public static class CacheKeyConstants
{
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
    /// 菜单树缓存键
    /// </summary>
    public const string MenuTree = $"{MenuPrefix}:tree";

    /// <summary>
    /// 部门树缓存键
    /// </summary>
    public const string DepartmentTree = $"{DepartmentPrefix}:tree";

    /// <summary>
    /// 缓存键前缀
    /// </summary>
    private const string Prefix = "rbac";

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
}
