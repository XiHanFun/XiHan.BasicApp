#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysRolePermissionRepository
// Guid:56a743aa-7614-46d0-8bac-7c51dc8dea53
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/11 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Domain.Repositories;

/// <summary>
/// 系统角色权限仓储接口
/// </summary>
public interface ISysRolePermissionRepository : IReadOnlyRepositoryBase<SysRolePermission, long>
{
    /// <summary>
    /// 根据角色ID获取权限列表
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色权限列表</returns>
    Task<List<SysRolePermission>> GetByRoleIdAsync(long roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据权限ID获取角色权限列表
    /// </summary>
    /// <param name="permissionId">权限ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色权限列表</returns>
    Task<List<SysRolePermission>> GetByPermissionIdAsync(long permissionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除角色的所有权限
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteByRoleIdAsync(long roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存角色权限
    /// </summary>
    /// <param name="rolePermission">角色权限实体</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>保存的角色权限实体</returns>
    Task<SysRolePermission> SaveAsync(SysRolePermission rolePermission, CancellationToken cancellationToken = default);
}
