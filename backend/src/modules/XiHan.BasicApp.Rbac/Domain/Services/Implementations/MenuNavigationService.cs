#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MenuNavigationService
// Guid:a9bcdef1-2345-6789-0abc-def123456789
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/31 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Services;

namespace XiHan.BasicApp.Rbac.Domain.Services.Implementations;

/// <summary>
/// 菜单导航领域服务实现
/// </summary>
public class MenuNavigationService : DomainService, IMenuNavigationService
{
    private readonly ISysMenuRepository _menuRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public MenuNavigationService(ISysMenuRepository menuRepository)
    {
        _menuRepository = menuRepository;
    }

    /// <summary>
    /// 构建用户菜单树
    /// </summary>
    public async Task<List<SysMenu>> BuildUserMenuTreeAsync(long userId, CancellationToken cancellationToken = default)
    {
        var allMenus = await _menuRepository.GetMenuTreeByUserIdAsync(userId, cancellationToken);
        return BuildMenuTree(allMenus, null);
    }

    /// <summary>
    /// 获取用户可见的菜单列表
    /// </summary>
    public async Task<List<SysMenu>> GetVisibleMenusForUserAsync(long userId, CancellationToken cancellationToken = default)
    {
        return await _menuRepository.GetMenuTreeByUserIdAsync(userId, cancellationToken);
    }

    /// <summary>
    /// 检查用户是否可以访问指定菜单
    /// </summary>
    public async Task<bool> CanAccessMenuAsync(long userId, long menuId, CancellationToken cancellationToken = default)
    {
        var userMenus = await _menuRepository.GetMenuTreeByUserIdAsync(userId, cancellationToken);
        return userMenus.Any(m => m.BasicId == menuId);
    }

    /// <summary>
    /// 获取菜单的所有子菜单（递归）
    /// </summary>
    public async Task<List<SysMenu>> GetDescendantMenusAsync(long menuId, CancellationToken cancellationToken = default)
    {
        var result = new List<SysMenu>();
        await CollectDescendantMenusAsync(menuId, result, cancellationToken);
        return result;
    }

    /// <summary>
    /// 获取菜单的面包屑路径
    /// </summary>
    public async Task<List<SysMenu>> GetMenuBreadcrumbAsync(long menuId, CancellationToken cancellationToken = default)
    {
        var result = new List<SysMenu>();
        var currentMenu = await _menuRepository.GetByIdAsync(menuId, cancellationToken);

        while (currentMenu != null)
        {
            result.Insert(0, currentMenu);

            if (!currentMenu.ParentId.HasValue)
            {
                break;
            }

            currentMenu = await _menuRepository.GetByIdAsync(currentMenu.ParentId.Value, cancellationToken);
        }

        return result;
    }

    /// <summary>
    /// 验证菜单层级关系是否有效
    /// </summary>
    public async Task<bool> ValidateMenuHierarchyAsync(long menuId, long? parentId, CancellationToken cancellationToken = default)
    {
        if (!parentId.HasValue)
        {
            return true;
        }

        // 不能将菜单设置为自己的父菜单
        if (menuId == parentId.Value)
        {
            return false;
        }

        // 检查是否会形成循环引用
        var ancestors = await GetAncestorMenuIdsAsync(parentId.Value, cancellationToken);
        if (ancestors.Contains(menuId))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 构建菜单树
    /// </summary>
    private List<SysMenu> BuildMenuTree(List<SysMenu> allMenus, long? parentId)
    {
        return allMenus
            .Where(m => m.ParentId == parentId)
            .OrderBy(m => m.Sort)
            .ToList();
    }

    /// <summary>
    /// 递归收集所有子菜单
    /// </summary>
    private async Task CollectDescendantMenusAsync(long menuId, List<SysMenu> result, CancellationToken cancellationToken)
    {
        var children = await _menuRepository.GetChildrenMenusAsync(menuId, cancellationToken);

        foreach (var child in children)
        {
            result.Add(child);
            await CollectDescendantMenusAsync(child.BasicId, result, cancellationToken);
        }
    }

    /// <summary>
    /// 获取菜单的所有祖先菜单ID
    /// </summary>
    private async Task<HashSet<long>> GetAncestorMenuIdsAsync(long menuId, CancellationToken cancellationToken)
    {
        var ancestors = new HashSet<long>();
        var currentId = menuId;

        while (currentId > 0)
        {
            var menu = await _menuRepository.GetByIdAsync(currentId, cancellationToken);
            if (menu == null || !menu.ParentId.HasValue)
            {
                break;
            }

            ancestors.Add(menu.ParentId.Value);
            currentId = menu.ParentId.Value;
        }

        return ancestors;
    }
}
