#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysRoleRelationRepository
// Guid:b8c9d0e1-f2a3-4567-8901-234567b23456
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/30 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;

namespace XiHan.BasicApp.Rbac.Repositories.Abstracts;

/// <summary>
/// 角色关系映射仓储接口
/// </summary>
/// <remarks>
/// 覆盖实体：SysRoleMenu + SysRolePermission
/// </remarks>
public interface ISysRoleRelationRepository
{
    // ========== 角色菜单关系 ==========

    /// <summary>
    /// 批量添加角色菜单
    /// </summary>
    Task AddRoleMenusAsync(IEnumerable<SysRoleMenu> roleMenus, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除角色菜单
    /// </summary>
    Task DeleteRoleMenusAsync(long roleId, IEnumerable<long> menuIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除角色所有菜单
    /// </summary>
    Task DeleteRoleAllMenusAsync(long roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取角色菜单列表
    /// </summary>
    Task<List<SysRoleMenu>> GetRoleMenusAsync(long roleId, CancellationToken cancellationToken = default);

    // ========== 角色权限关系 ==========

    /// <summary>
    /// 批量添加角色权限
    /// </summary>
    Task AddRolePermissionsAsync(IEnumerable<SysRolePermission> rolePermissions, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除角色权限
    /// </summary>
    Task DeleteRolePermissionsAsync(long roleId, IEnumerable<long> permissionIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除角色所有权限
    /// </summary>
    Task DeleteRoleAllPermissionsAsync(long roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取角色权限列表
    /// </summary>
    Task<List<SysRolePermission>> GetRolePermissionsAsync(long roleId, CancellationToken cancellationToken = default);
}
