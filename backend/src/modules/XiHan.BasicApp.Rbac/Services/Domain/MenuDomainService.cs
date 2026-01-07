#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MenuDomainService
// Guid:e5f6a7b8-c9d0-4e5f-1a2b-4c5d6e7f8a9b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Repositories.Abstracts;
using XiHan.Framework.Domain.Services;

namespace XiHan.BasicApp.Rbac.Services.Domain;

/// <summary>
/// 菜单领域服务
/// 处理菜单树构建、菜单权限等业务逻辑
/// </summary>
public class MenuDomainService : DomainService
{
    private readonly IMenuRepository _menuRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public MenuDomainService(IMenuRepository menuRepository)
    {
        _menuRepository = menuRepository;
    }

    /// <summary>
    /// 构建菜单树（递归）
    /// </summary>
    /// <param name="parentId">父菜单ID（null为根节点）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>菜单树</returns>
    public async Task<List<SysMenu>> BuildMenuTreeAsync(long? parentId = null, CancellationToken cancellationToken = default)
    {
        LogDomainOperation(nameof(BuildMenuTreeAsync), new { parentId });

        var menus = await _menuRepository.GetByParentIdAsync(parentId, cancellationToken);

        // 递归构建子菜单（注意：实际实现中可能需要优化递归深度）
        foreach (var menu in menus)
        {
            // 子菜单可以通过导航属性或递归加载
            // 这里只是示例，实际实现可能在 Partial 类中处理
        }

        Logger.LogInformation("构建菜单树，父节点: {ParentId}, 菜单数: {MenuCount}", parentId, menus.Count);
        return menus;
    }

    /// <summary>
    /// 检查菜单编码唯一性
    /// </summary>
    /// <param name="menuCode">菜单编码</param>
    /// <param name="excludeMenuId">排除的菜单ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否唯一</returns>
    public async Task<bool> IsMenuCodeUniqueAsync(string menuCode, long? excludeMenuId = null, CancellationToken cancellationToken = default)
    {
        var exists = await _menuRepository.ExistsByMenuCodeAsync(menuCode, excludeMenuId, cancellationToken);
        return !exists;
    }

    /// <summary>
    /// 验证菜单删除前置条件
    /// </summary>
    /// <param name="menuId">菜单ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否可以删除</returns>
    public async Task<bool> CanDeleteMenuAsync(long menuId, CancellationToken cancellationToken = default)
    {
        LogDomainOperation(nameof(CanDeleteMenuAsync), new { menuId });

        // 检查是否有子菜单
        var hasChildren = await _menuRepository.HasChildrenAsync(menuId, cancellationToken);
        if (hasChildren)
        {
            throw new InvalidOperationException("菜单有子菜单，无法删除");
        }

        Logger.LogInformation("菜单 {MenuId} 可以删除", menuId);
        return true;
    }

    /// <summary>
    /// 获取菜单路径（从根到当前节点）
    /// </summary>
    /// <param name="menuId">菜单ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>菜单路径列表（从根到叶子）</returns>
    public async Task<List<SysMenu>> GetMenuPathAsync(long menuId, CancellationToken cancellationToken = default)
    {
        LogDomainOperation(nameof(GetMenuPathAsync), new { menuId });

        var path = new List<SysMenu>();
        var currentMenu = await _menuRepository.GetByIdAsync(menuId, cancellationToken);

        while (currentMenu != null)
        {
            path.Insert(0, currentMenu); // 插入到列表开头

            if (currentMenu.ParentId.HasValue)
            {
                currentMenu = await _menuRepository.GetByIdAsync(currentMenu.ParentId.Value, cancellationToken);
            }
            else
            {
                break;
            }
        }

        Logger.LogInformation("菜单 {MenuId} 路径长度: {PathLength}", menuId, path.Count);
        return path;
    }
}
