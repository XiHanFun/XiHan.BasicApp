#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IMenuRepository
// Guid:38105457-c072-44b0-b428-0c4584b9a599
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:34:30
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Domain.Repositories;

/// <summary>
/// 菜单聚合仓储接口
/// </summary>
public interface IMenuRepository : IAggregateRootRepository<SysMenu, long>
{
    /// <summary>
    /// 根据菜单编码获取菜单
    /// </summary>
    Task<SysMenu?> GetByMenuCodeAsync(string menuCode, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取角色菜单
    /// </summary>
    Task<IReadOnlyList<SysMenu>> GetRoleMenusAsync(long roleId, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户菜单
    /// </summary>
    Task<IReadOnlyList<SysMenu>> GetUserMenusAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default);
}
