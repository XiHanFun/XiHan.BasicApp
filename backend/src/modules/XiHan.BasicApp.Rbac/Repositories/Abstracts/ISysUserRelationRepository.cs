#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysUserRelationRepository
// Guid:a7b8c9d0-e1f2-3456-7890-123456a12345
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/30 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Abstracts;

/// <summary>
/// 用户关系映射仓储接口
/// </summary>
/// <remarks>
/// 覆盖实体：SysUserRole + SysUserPermission + SysUserDepartment + SysSessionRole
/// </remarks>
public interface ISysUserRelationRepository
{
    // ========== 用户角色关系 ==========

    /// <summary>
    /// 批量添加用户角色
    /// </summary>
    Task AddUserRolesAsync(IEnumerable<SysUserRole> userRoles, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除用户角色
    /// </summary>
    Task DeleteUserRolesAsync(long userId, IEnumerable<long> roleIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除用户所有角色
    /// </summary>
    Task DeleteUserAllRolesAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户角色列表
    /// </summary>
    Task<List<SysUserRole>> GetUserRolesAsync(long userId, CancellationToken cancellationToken = default);

    // ========== 用户权限关系 ==========

    /// <summary>
    /// 批量添加用户权限
    /// </summary>
    Task AddUserPermissionsAsync(IEnumerable<SysUserPermission> userPermissions, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除用户权限
    /// </summary>
    Task DeleteUserPermissionsAsync(long userId, IEnumerable<long> permissionIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除用户所有权限
    /// </summary>
    Task DeleteUserAllPermissionsAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户权限列表
    /// </summary>
    Task<List<SysUserPermission>> GetUserPermissionsAsync(long userId, CancellationToken cancellationToken = default);

    // ========== 用户部门关系 ==========

    /// <summary>
    /// 批量添加用户部门
    /// </summary>
    Task AddUserDepartmentsAsync(IEnumerable<SysUserDepartment> userDepartments, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除用户部门
    /// </summary>
    Task DeleteUserDepartmentsAsync(long userId, IEnumerable<long> departmentIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除用户所有部门
    /// </summary>
    Task DeleteUserAllDepartmentsAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户部门列表
    /// </summary>
    Task<List<SysUserDepartment>> GetUserDepartmentsAsync(long userId, CancellationToken cancellationToken = default);

    // ========== 会话角色关系 ==========

    /// <summary>
    /// 添加会话角色
    /// </summary>
    Task<SysSessionRole> AddSessionRoleAsync(SysSessionRole sessionRole, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除会话角色
    /// </summary>
    Task DeleteSessionRolesAsync(long sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取会话角色列表
    /// </summary>
    Task<List<SysSessionRole>> GetSessionRolesAsync(long sessionId, CancellationToken cancellationToken = default);
}
