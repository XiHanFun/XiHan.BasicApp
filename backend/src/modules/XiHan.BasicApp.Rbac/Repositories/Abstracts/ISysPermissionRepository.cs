#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysPermissionRepository
// Guid:c3d4e5f6-a7b8-9012-3456-789012cdef01
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/30 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Abstracts;

/// <summary>
/// 权限聚合仓储接口
/// </summary>
/// <remarks>
/// 聚合范围：SysPermission + SysResource + SysOperation
/// </remarks>
public interface ISysPermissionRepository : IAggregateRootRepository<SysPermission, long>
{
    /// <summary>
    /// 根据权限编码获取权限
    /// </summary>
    Task<SysPermission?> GetByPermissionCodeAsync(string permissionCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取角色的所有权限
    /// </summary>
    Task<List<SysPermission>> GetByRoleIdAsync(long roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户的所有权限（通过角色）
    /// </summary>
    Task<List<SysPermission>> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取资源的所有权限
    /// </summary>
    Task<List<SysPermission>> GetByResourceIdAsync(long resourceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量获取权限
    /// </summary>
    Task<List<SysPermission>> GetByIdsAsync(IEnumerable<long> permissionIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查权限编码是否存在
    /// </summary>
    Task<bool> ExistsByPermissionCodeAsync(string permissionCode, long? excludePermissionId = null, CancellationToken cancellationToken = default);
}
