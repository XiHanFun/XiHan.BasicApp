#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysMenuRepository
// Guid:b8c9d0e1-f2a3-4567-89ab-cdef12345678
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/31 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Domain.Repositories;

/// <summary>
/// 系统菜单仓储接口
/// </summary>
public interface ISysMenuRepository : IAggregateRootRepository<SysMenu, long>
{
    /// <summary>
    /// 根据菜单编码获取菜单
    /// </summary>
    /// <param name="menuCode">菜单编码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>菜单实体</returns>
    Task<SysMenu?> GetByMenuCodeAsync(string menuCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查菜单编码是否存在
    /// </summary>
    /// <param name="menuCode">菜单编码</param>
    /// <param name="excludeMenuId">排除的菜单ID（用于更新时检查）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否存在</returns>
    Task<bool> IsMenuCodeExistsAsync(string menuCode, long? excludeMenuId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户的菜单树
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>菜单列表</returns>
    Task<List<SysMenu>> GetMenuTreeByUserIdAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取角色的菜单列表
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>菜单列表</returns>
    Task<List<SysMenu>> GetMenusByRoleIdAsync(long roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取父级菜单下的子菜单列表
    /// </summary>
    /// <param name="parentId">父级菜单ID（null 表示获取顶级菜单）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>菜单列表</returns>
    Task<List<SysMenu>> GetChildrenMenusAsync(long? parentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取租户下的菜单树
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>菜单列表</returns>
    Task<List<SysMenu>> GetMenuTreeByTenantAsync(long tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存菜单
    /// </summary>
    /// <param name="menu">菜单实体</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>保存的菜单实体</returns>
    Task<SysMenu> SaveAsync(SysMenu menu, CancellationToken cancellationToken = default);

    /// <summary>
    /// 启用菜单
    /// </summary>
    /// <param name="menuId">菜单ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task EnableMenuAsync(long menuId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 禁用菜单
    /// </summary>
    /// <param name="menuId">菜单ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DisableMenuAsync(long menuId, CancellationToken cancellationToken = default);
}
