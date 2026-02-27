#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IRolePermissionRepository
// Guid:0d86e366-270f-45c8-8658-4a47c127ab4d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:35:06
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Domain.Repositories;

/// <summary>
/// 角色权限关系仓储接口
/// </summary>
public interface IRolePermissionRepository : IRepositoryBase<SysRolePermission, long>
{
    /// <summary>
    /// 获取角色权限关系
    /// </summary>
    Task<IReadOnlyList<SysRolePermission>> GetByRoleIdAsync(long roleId, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除角色权限关系
    /// </summary>
    Task<bool> RemoveByRoleIdAsync(long roleId, long? tenantId = null, CancellationToken cancellationToken = default);
}
