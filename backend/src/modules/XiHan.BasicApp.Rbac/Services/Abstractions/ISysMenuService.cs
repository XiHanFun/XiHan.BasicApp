#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysMenuService
// Guid:1b2b3c4d-5e6f-7890-abcd-ef12345678a6
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/10/31 5:20:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Dtos.Menus;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Services.Abstractions;

/// <summary>
/// 菜单服务接口
/// </summary>
public interface ISysMenuService : ICrudApplicationService<MenuDto, XiHanBasicAppIdType, CreateMenuDto, UpdateMenuDto>
{
    /// <summary>
    /// 根据菜单编码获取菜单
    /// </summary>
    /// <param name="menuCode">菜单编码</param>
    /// <returns></returns>
    Task<MenuDto?> GetByMenuCodeAsync(string menuCode);

    /// <summary>
    /// 获取菜单树
    /// </summary>
    /// <returns></returns>
    Task<List<MenuTreeDto>> GetTreeAsync();

    /// <summary>
    /// 根据父级ID获取子菜单
    /// </summary>
    /// <param name="parentId">父级菜单ID</param>
    /// <returns></returns>
    Task<List<MenuDto>> GetChildrenAsync(XiHanBasicAppIdType parentId);

    /// <summary>
    /// 获取角色的菜单列表
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <returns></returns>
    Task<List<MenuDto>> GetByRoleIdAsync(XiHanBasicAppIdType roleId);

    /// <summary>
    /// 获取用户的菜单树
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    Task<List<MenuTreeDto>> GetUserMenuTreeAsync(XiHanBasicAppIdType userId);
}
