#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysMenuRepository
// Guid:ba2b3c4d-5e6f-7890-abcd-ef12345678a0
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/10/31 4:50:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Menus;

/// <summary>
/// 系统菜单仓储接口
/// </summary>
public interface ISysMenuRepository : IRepositoryBase<SysMenu, long>
{
    /// <summary>
    /// 根据菜单编码获取菜单
    /// </summary>
    /// <param name="menuCode">菜单编码</param>
    /// <returns></returns>
    Task<SysMenu?> GetByMenuCodeAsync(string menuCode);

    /// <summary>
    /// 检查菜单编码是否存在
    /// </summary>
    /// <param name="menuCode">菜单编码</param>
    /// <param name="excludeId">排除的菜单ID</param>
    /// <returns></returns>
    Task<bool> ExistsByMenuCodeAsync(string menuCode, long? excludeId = null);

    /// <summary>
    /// 获取所有根菜单
    /// </summary>
    /// <returns></returns>
    Task<List<SysMenu>> GetRootMenusAsync();

    /// <summary>
    /// 根据父级ID获取子菜单
    /// </summary>
    /// <param name="parentId">父级菜单ID</param>
    /// <returns></returns>
    Task<List<SysMenu>> GetChildrenAsync(long parentId);

    /// <summary>
    /// 根据角色ID获取菜单列表
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <returns></returns>
    Task<List<SysMenu>> GetByRoleIdAsync(long roleId);

    /// <summary>
    /// 根据用户ID获取菜单列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    Task<List<SysMenu>> GetByUserIdAsync(long userId);
}
