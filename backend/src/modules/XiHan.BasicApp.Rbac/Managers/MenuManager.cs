#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MenuManager
// Guid:ab2b3c4d-5e6f-7890-abcd-ef12345678af
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/10/31 6:05:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Repositories.Menus;
using XiHan.Framework.Domain.Services;

namespace XiHan.BasicApp.Rbac.Managers;

/// <summary>
/// 系统菜单领域管理器
/// </summary>
public class MenuManager : DomainService
{
    private readonly ISysMenuRepository _menuRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="menuRepository">菜单仓储</param>
    public MenuManager(ISysMenuRepository menuRepository)
    {
        _menuRepository = menuRepository;
    }

    /// <summary>
    /// 验证菜单编码是否唯一
    /// </summary>
    /// <param name="menuCode">菜单编码</param>
    /// <param name="excludeId">排除的菜单ID</param>
    /// <returns></returns>
    public async Task<bool> IsMenuCodeUniqueAsync(string menuCode, long? excludeId = null)
    {
        return !await _menuRepository.ExistsByMenuCodeAsync(menuCode, excludeId);
    }

    /// <summary>
    /// 检查菜单是否可以删除
    /// </summary>
    /// <param name="menuId">菜单ID</param>
    /// <returns></returns>
    public async Task<bool> CanDeleteAsync(long menuId)
    {
        var children = await _menuRepository.GetChildrenAsync(menuId);
        return children.Count == 0;
    }
}
