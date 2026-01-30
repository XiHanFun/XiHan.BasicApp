#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysRoleRepository
// Guid:b2c3d4e5-f6a7-8901-2345-678901bcdef0
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/30 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Abstracts;

/// <summary>
/// 角色聚合仓储接口
/// </summary>
/// <remarks>
/// 聚合范围：SysRole + SysRoleHierarchy + SysRoleDataScope
/// </remarks>
public interface ISysRoleRepository : IAggregateRootRepository<SysRole, long>
{
    /// <summary>
    /// 根据角色编码获取角色
    /// </summary>
    Task<SysRole?> GetByRoleCodeAsync(string roleCode, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取角色及层级关系
    /// </summary>
    Task<SysRole?> GetWithHierarchyAsync(long roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取角色及数据权限范围
    /// </summary>
    Task<SysRole?> GetWithDataScopeAsync(long roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户的所有角色
    /// </summary>
    Task<List<SysRole>> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取子角色列表
    /// </summary>
    Task<List<SysRole>> GetChildRolesAsync(long parentRoleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量获取角色
    /// </summary>
    Task<List<SysRole>> GetByIdsAsync(IEnumerable<long> roleIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查角色编码是否存在
    /// </summary>
    Task<bool> ExistsByRoleCodeAsync(string roleCode, long? excludeRoleId = null, long? tenantId = null, CancellationToken cancellationToken = default);
}
