#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysPermissionRepository
// Guid:c3d4e5f6-a7b8-9012-3456-7890abcdef12
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/31 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Domain.Repositories;

/// <summary>
/// 系统权限仓储接口
/// </summary>
public interface ISysPermissionRepository : IAggregateRootRepository<SysPermission, long>
{
    /// <summary>
    /// 根据权限编码获取权限
    /// </summary>
    /// <param name="permissionCode">权限编码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限实体</returns>
    Task<SysPermission?> GetByPermissionCodeAsync(string permissionCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据资源ID和操作ID获取权限
    /// </summary>
    /// <param name="resourceId">资源ID</param>
    /// <param name="operationId">操作ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限实体</returns>
    Task<SysPermission?> GetByResourceAndOperationAsync(long resourceId, long operationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查权限编码是否存在
    /// </summary>
    /// <param name="permissionCode">权限编码</param>
    /// <param name="excludePermissionId">排除的权限ID（用于更新时检查）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否存在</returns>
    Task<bool> IsPermissionCodeExistsAsync(string permissionCode, long? excludePermissionId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取角色的权限列表
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限列表</returns>
    Task<List<SysPermission>> GetPermissionsByRoleIdAsync(long roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户的权限列表（包含继承自角色的权限）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限列表</returns>
    Task<List<SysPermission>> GetPermissionsByUserIdAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取资源下的所有权限
    /// </summary>
    /// <param name="resourceId">资源ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限列表</returns>
    Task<List<SysPermission>> GetPermissionsByResourceIdAsync(long resourceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存权限
    /// </summary>
    /// <param name="permission">权限实体</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>保存的权限实体</returns>
    Task<SysPermission> SaveAsync(SysPermission permission, CancellationToken cancellationToken = default);

    /// <summary>
    /// 启用权限
    /// </summary>
    /// <param name="permissionId">权限ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task EnablePermissionAsync(long permissionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 禁用权限
    /// </summary>
    /// <param name="permissionId">权限ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DisablePermissionAsync(long permissionId, CancellationToken cancellationToken = default);
}
