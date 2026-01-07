#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IPermissionRepository
// Guid:c3d4e5f6-a7b8-4c5d-9e0f-2a3b4c5d6e7f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Abstracts;

/// <summary>
/// 权限仓储接口
/// </summary>
public interface IPermissionRepository : IAggregateRootRepository<SysPermission, long>
{
    /// <summary>
    /// 根据权限编码查询权限
    /// </summary>
    /// <param name="permissionCode">权限编码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限实体</returns>
    Task<SysPermission?> GetByPermissionCodeAsync(string permissionCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查权限编码是否存在
    /// </summary>
    /// <param name="permissionCode">权限编码</param>
    /// <param name="excludePermissionId">排除的权限ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否存在</returns>
    Task<bool> ExistsByPermissionCodeAsync(string permissionCode, long? excludePermissionId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户的所有权限
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限列表</returns>
    Task<List<SysPermission>> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取角色的所有权限
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限列表</returns>
    Task<List<SysPermission>> GetByRoleIdAsync(long roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取资源的所有权限
    /// </summary>
    /// <param name="resourceId">资源ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限列表</returns>
    Task<List<SysPermission>> GetByResourceIdAsync(long resourceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量获取权限
    /// </summary>
    /// <param name="permissionIds">权限ID列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限列表</returns>
    Task<List<SysPermission>> GetByIdsAsync(List<long> permissionIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据权限编码批量获取权限
    /// </summary>
    /// <param name="permissionCodes">权限编码列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限列表</returns>
    Task<List<SysPermission>> GetByCodesAsync(List<string> permissionCodes, CancellationToken cancellationToken = default);
}
