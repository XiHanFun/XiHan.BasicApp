#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IPermissionRepository
// Guid:3c11bea0-70da-448d-a724-3b5ac05022ea
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:34:12
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 权限仓储接口
/// </summary>
public interface IPermissionRepository : IAuditedRepository<SysPermission, long>
{
    /// <summary>
    /// 根据权限编码获取权限
    /// </summary>
    Task<SysPermission?> GetByPermissionCodeAsync(string permissionCode, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户可用权限
    /// </summary>
    Task<IReadOnlyList<SysPermission>> GetUserPermissionsAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取角色权限
    /// </summary>
    Task<IReadOnlyList<SysPermission>> GetRolePermissionsAsync(long roleId, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取多个角色的权限ID集合（纯数据查询，不含业务逻辑）
    /// </summary>
    Task<IReadOnlyList<long>> GetRolePermissionIdsAsync(IReadOnlyList<long> roleIds, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户直接授权记录（纯数据查询，含 Grant/Deny 标记）
    /// </summary>
    Task<IReadOnlyList<SysUserPermission>> GetUserDirectPermissionGrantsAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default);
}
