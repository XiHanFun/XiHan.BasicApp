#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysMenuRepository
// Guid:d4e5f6a7-b8c9-0123-4567-890123def012
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/30 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Abstracts;

/// <summary>
/// 菜单仓储接口
/// </summary>
public interface ISysMenuRepository : IAggregateRootRepository<SysMenu, long>
{
    /// <summary>
    /// 获取用户菜单树
    /// </summary>
    Task<List<SysMenu>> GetUserMenuTreeAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取角色菜单
    /// </summary>
    Task<List<SysMenu>> GetByRoleIdAsync(long roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取子菜单
    /// </summary>
    Task<List<SysMenu>> GetChildrenAsync(long parentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取所有启用菜单树
    /// </summary>
    Task<List<SysMenu>> GetActiveMenuTreeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量获取菜单
    /// </summary>
    Task<List<SysMenu>> GetByIdsAsync(IEnumerable<long> menuIds, CancellationToken cancellationToken = default);
}
