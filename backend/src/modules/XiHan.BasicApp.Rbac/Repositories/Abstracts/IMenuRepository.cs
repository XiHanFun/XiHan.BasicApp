#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IMenuRepository
// Guid:d4e5f6a7-b8c9-4d5e-0f1a-3b4c5d6e7f8a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Abstracts;

/// <summary>
/// 菜单仓储接口
/// </summary>
public interface IMenuRepository : IAggregateRootRepository<SysMenu, long>
{
    /// <summary>
    /// 根据菜单编码查询菜单
    /// </summary>
    /// <param name="menuCode">菜单编码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>菜单实体</returns>
    Task<SysMenu?> GetByMenuCodeAsync(string menuCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查菜单编码是否存在
    /// </summary>
    /// <param name="menuCode">菜单编码</param>
    /// <param name="excludeMenuId">排除的菜单ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否存在</returns>
    Task<bool> ExistsByMenuCodeAsync(string menuCode, long? excludeMenuId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户的所有菜单
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>菜单列表</returns>
    Task<List<SysMenu>> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取角色的所有菜单
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>菜单列表</returns>
    Task<List<SysMenu>> GetByRoleIdAsync(long roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取父菜单的所有子菜单
    /// </summary>
    /// <param name="parentId">父菜单ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>子菜单列表</returns>
    Task<List<SysMenu>> GetByParentIdAsync(long? parentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取所有根菜单
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>根菜单列表</returns>
    Task<List<SysMenu>> GetRootMenusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取菜单树（包含所有子菜单）
    /// </summary>
    /// <param name="parentId">父菜单ID（null表示从根节点开始）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>菜单树</returns>
    Task<List<SysMenu>> GetMenuTreeAsync(long? parentId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查是否有子菜单
    /// </summary>
    /// <param name="menuId">菜单ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否有子菜单</returns>
    Task<bool> HasChildrenAsync(long menuId, CancellationToken cancellationToken = default);
}
