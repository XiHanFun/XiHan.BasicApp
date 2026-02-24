#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IMenuNavigationService
// Guid:a9bcdef1-2345-6789-0abc-def123456789
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/31 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Services.Abstracts;

namespace XiHan.BasicApp.Rbac.DomainServices;

/// <summary>
/// 菜单导航领域服务接口
/// </summary>
public interface IMenuNavigationService : IDomainService
{
    /// <summary>
    /// 构建用户菜单树
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>菜单树（按层级结构组织）</returns>
    Task<List<SysMenu>> BuildUserMenuTreeAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户可见的菜单列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>菜单列表</returns>
    Task<List<SysMenu>> GetVisibleMenusForUserAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查用户是否可以访问指定菜单
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="menuId">菜单ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否可以访问</returns>
    Task<bool> CanAccessMenuAsync(long userId, long menuId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取菜单的所有子菜单（递归）
    /// </summary>
    /// <param name="menuId">菜单ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>子菜单列表</returns>
    Task<List<SysMenu>> GetDescendantMenusAsync(long menuId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取菜单的面包屑路径
    /// </summary>
    /// <param name="menuId">菜单ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>面包屑菜单列表（从根到当前菜单）</returns>
    Task<List<SysMenu>> GetMenuBreadcrumbAsync(long menuId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 验证菜单层级关系是否有效
    /// </summary>
    /// <param name="menuId">菜单ID</param>
    /// <param name="parentId">父菜单ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否有效（防止循环引用）</returns>
    Task<bool> ValidateMenuHierarchyAsync(long menuId, long? parentId, CancellationToken cancellationToken = default);
}
