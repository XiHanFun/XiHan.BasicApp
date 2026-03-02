#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IRoleRelationRepository
// Guid:9f8adc89-95ab-4984-be38-f20f3fcf15ce
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/03 14:26:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Domain.Entities;

namespace XiHan.BasicApp.Rbac.Domain.Repositories;

/// <summary>
/// 角色关系仓储接口（角色-权限/菜单）
/// </summary>
public interface IRoleRelationRepository
{
    /// <summary>
    /// 获取角色权限关系
    /// </summary>
    Task<IReadOnlyList<SysRolePermission>> GetRolePermissionsAsync(long roleId, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取角色菜单关系
    /// </summary>
    Task<IReadOnlyList<SysRoleMenu>> GetRoleMenusAsync(long roleId, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 替换角色权限关系
    /// </summary>
    Task ReplaceRolePermissionsAsync(long roleId, IReadOnlyCollection<long> permissionIds, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 替换角色菜单关系
    /// </summary>
    Task ReplaceRoleMenusAsync(long roleId, IReadOnlyCollection<long> menuIds, long? tenantId = null, CancellationToken cancellationToken = default);
}
