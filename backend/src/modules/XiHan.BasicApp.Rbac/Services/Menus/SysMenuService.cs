#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysMenuService
// Guid:6c2b3c4d-5e6f-7890-abcd-ef12345678bb
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/10/31 7:05:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Constants;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Extensions;
using XiHan.BasicApp.Rbac.Managers;
using XiHan.BasicApp.Rbac.Repositories.Menus;
using XiHan.BasicApp.Rbac.Services.Menus.Dtos;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Services.Menus;

/// <summary>
/// 系统菜单服务实现
/// </summary>
public class SysMenuService : CrudApplicationServiceBase<SysMenu, MenuDto, XiHanBasicAppIdType, CreateMenuDto, UpdateMenuDto>, ISysMenuService
{
    private readonly ISysMenuRepository _menuRepository;
    private readonly MenuManager _menuManager;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysMenuService(
        ISysMenuRepository menuRepository,
        MenuManager menuManager) : base(menuRepository)
    {
        _menuRepository = menuRepository;
        _menuManager = menuManager;
    }

    #region 重写基类方法

    /// <summary>
    /// 创建菜单
    /// </summary>
    public override async Task<MenuDto> CreateAsync(CreateMenuDto input)
    {
        // 验证菜单编码唯一性
        if (!await _menuManager.IsMenuCodeUniqueAsync(input.MenuCode))
        {
            throw new InvalidOperationException(ErrorMessageConstants.MenuCodeExists);
        }

        var menu = new SysMenu
        {
            ParentId = input.ParentId,
            MenuName = input.MenuName,
            MenuCode = input.MenuCode,
            MenuType = input.MenuType,
            Path = input.Path,
            Component = input.Component,
            Icon = input.Icon,
            Permission = input.Permission,
            IsExternal = input.IsExternal,
            IsCache = input.IsCache,
            IsVisible = input.IsVisible,
            Sort = input.Sort,
            Remark = input.Remark
        };

        await _menuRepository.AddAsync(menu);

        return menu.Adapt<MenuDto>();
    }

    /// <summary>
    /// 更新菜单
    /// </summary>
    public override async Task<MenuDto> UpdateAsync(XiHanBasicAppIdType id, UpdateMenuDto input)
    {
        var menu = await _menuRepository.GetByIdAsync(id) ??
            throw new InvalidOperationException(ErrorMessageConstants.MenuNotFound);

        // 更新菜单信息
        if (input.ParentId.HasValue)
        {
            menu.ParentId = input.ParentId;
        }

        if (input.MenuName != null)
        {
            menu.MenuName = input.MenuName;
        }

        if (input.MenuType.HasValue)
        {
            menu.MenuType = input.MenuType.Value;
        }

        if (input.Path != null)
        {
            menu.Path = input.Path;
        }

        if (input.Component != null)
        {
            menu.Component = input.Component;
        }

        if (input.Icon != null)
        {
            menu.Icon = input.Icon;
        }

        if (input.Permission != null)
        {
            menu.Permission = input.Permission;
        }

        if (input.IsExternal.HasValue)
        {
            menu.IsExternal = input.IsExternal.Value;
        }

        if (input.IsCache.HasValue)
        {
            menu.IsCache = input.IsCache.Value;
        }

        if (input.IsVisible.HasValue)
        {
            menu.IsVisible = input.IsVisible.Value;
        }

        if (input.Status.HasValue)
        {
            menu.Status = input.Status.Value;
        }

        if (input.Sort.HasValue)
        {
            menu.Sort = input.Sort.Value;
        }

        if (input.Remark != null)
        {
            menu.Remark = input.Remark;
        }

        await _menuRepository.UpdateAsync(menu);

        return menu.Adapt<MenuDto>();
    }

    /// <summary>
    /// 删除菜单
    /// </summary>
    public override async Task<bool> DeleteAsync(XiHanBasicAppIdType id)
    {
        var menu = await _menuRepository.GetByIdAsync(id) ??
            throw new InvalidOperationException(ErrorMessageConstants.MenuNotFound);

        // 检查是否可以删除
        if (!await _menuManager.CanDeleteAsync(id))
        {
            throw new InvalidOperationException(ErrorMessageConstants.MenuHasChildren);
        }

        return await _menuRepository.DeleteAsync(menu);
    }

    #endregion 重写基类方法

    #region 业务特定方法

    /// <summary>
    /// 根据菜单编码获取菜单
    /// </summary>
    public async Task<MenuDto?> GetByMenuCodeAsync(string menuCode)
    {
        var menu = await _menuRepository.GetByMenuCodeAsync(menuCode);
        return menu.Adapt<MenuDto>();
    }

    /// <summary>
    /// 获取菜单树
    /// </summary>
    public async Task<List<MenuTreeDto>> GetTreeAsync()
    {
        var allMenus = await _menuRepository.GetListAsync();
        var menuDtos = allMenus.Adapt<List<MenuDto>>();

        // TODO 优化BuildTree方法，直接生成MenuTreeDto列表，避免重复转换
        //return menuDtos.BuildTree();

        throw new NotImplementedException();
    }

    /// <summary>
    /// 根据父级ID获取子菜单
    /// </summary>
    public async Task<List<MenuDto>> GetChildrenAsync(XiHanBasicAppIdType parentId)
    {
        var children = await _menuRepository.GetChildrenAsync(parentId);
        return children.Adapt<List<MenuDto>>();
    }

    /// <summary>
    /// 获取角色的菜单列表
    /// </summary>
    public async Task<List<MenuDto>> GetByRoleIdAsync(XiHanBasicAppIdType roleId)
    {
        var menus = await _menuRepository.GetByRoleIdAsync(roleId);
        return menus.Adapt<List<MenuDto>>();
    }

    /// <summary>
    /// 获取用户的菜单树
    /// </summary>
    public async Task<List<MenuTreeDto>> GetUserMenuTreeAsync(XiHanBasicAppIdType userId)
    {
        var menus = await _menuRepository.GetByUserIdAsync(userId);
        var menuDtos = menus.Adapt<List<MenuDto>>();

        // TODO 优化BuildTree方法，直接生成MenuTreeDto列表，避免重复转换
        //return menuDtos.BuildTree();
        throw new NotImplementedException();
    }

    #endregion 业务特定方法
}
