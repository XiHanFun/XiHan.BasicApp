#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MenuApplicationService
// Guid:42e26db6-4851-4768-8dc3-b59ded912063
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/31 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Repositories;
using XiHan.BasicApp.Rbac.Services.Application.Menus.Dtos;
using XiHan.BasicApp.Rbac.Services.Domain;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Rbac.Services.Application.Menus;

/// <summary>
/// 菜单应用服务
/// </summary>
public class MenuApplicationService : CrudApplicationServiceBase<SysMenu, SysMenuDto, long, CreateSysMenuDto, UpdateSysMenuDto, SysMenuPageRequestDto>
{
    private readonly ISysMenuRepository _menuRepository;
    private readonly IMenuNavigationService _menuNavigationService;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    /// <summary>
    /// 构造函数
    /// </summary>
    public MenuApplicationService(
        ISysMenuRepository menuRepository,
        IMenuNavigationService menuNavigationService,
        IUnitOfWorkManager unitOfWorkManager)
        : base(menuRepository)
    {
        _menuRepository = menuRepository;
        _menuNavigationService = menuNavigationService;
        _unitOfWorkManager = unitOfWorkManager;
    }

    /// <summary>
    /// 创建菜单
    /// </summary>
    public override async Task<SysMenuDto> CreateAsync(CreateSysMenuDto input)
    {
        //using var uow = _unitOfWorkManager.Begin();

        // 检查菜单编码是否已存在
        var exists = await _menuRepository.IsMenuCodeExistsAsync(input.MenuCode);
        if (exists)
        {
            throw new InvalidOperationException($"菜单编码 {input.MenuCode} 已存在");
        }

        // 验证父菜单
        if (input.ParentId.HasValue)
        {
            var parent = await _menuRepository.GetByIdAsync(input.ParentId.Value);
            if (parent == null)
            {
                throw new InvalidOperationException($"父菜单 {input.ParentId.Value} 不存在");
            }
        }

        // 创建菜单实体
        var menu = input.Adapt<SysMenu>();
        menu.Status = YesOrNo.Yes;
        menu.CreatedTime = DateTimeOffset.Now;

        // 保存菜单
        menu = await _menuRepository.SaveAsync(menu);

        //await uow.CompleteAsync();

        return menu.Adapt<SysMenuDto>();
    }

    /// <summary>
    /// 更新菜单
    /// </summary>
    public override async Task<SysMenuDto> UpdateAsync(long id, UpdateSysMenuDto input)
    {
        //using var uow = _unitOfWorkManager.Begin();

        var menu = await _menuRepository.GetByIdAsync(id);
        if (menu == null)
        {
            throw new KeyNotFoundException($"菜单 {id} 不存在");
        }

        // 验证层级关系
        if (input.ParentId.HasValue)
        {
            var isValid = await _menuNavigationService.ValidateMenuHierarchyAsync(id, input.ParentId);
            if (!isValid)
            {
                throw new InvalidOperationException("不能将菜单设置为自己或其子菜单的父菜单");
            }
        }

        // 映射更新数据
        input.Adapt(menu);
        menu.ModifiedTime = DateTimeOffset.Now;

        // 保存菜单
        menu = await _menuRepository.SaveAsync(menu);

        //await uow.CompleteAsync();

        return menu.Adapt<SysMenuDto>();
    }

    /// <summary>
    /// 获取用户的菜单树
    /// </summary>
    public async Task<List<SysMenuDto>> GetUserMenuTreeAsync(long userId)
    {
        var menus = await _menuNavigationService.BuildUserMenuTreeAsync(userId);
        return BuildMenuTreeDto(menus, null);
    }

    /// <summary>
    /// 获取租户下的菜单树
    /// </summary>
    public async Task<List<SysMenuDto>> GetTenantMenuTreeAsync(long tenantId)
    {
        var menus = await _menuRepository.GetMenuTreeByTenantAsync(tenantId);
        return BuildMenuTreeDto(menus, null);
    }

    /// <summary>
    /// 获取菜单的面包屑路径
    /// </summary>
    public async Task<List<SysMenuDto>> GetMenuBreadcrumbAsync(long menuId)
    {
        var menus = await _menuNavigationService.GetMenuBreadcrumbAsync(menuId);
        return menus.Adapt<List<SysMenuDto>>();
    }

    /// <summary>
    /// 启用菜单
    /// </summary>
    public async Task<bool> EnableAsync(long menuId)
    {
        //using var uow = _unitOfWorkManager.Begin();
        await _menuRepository.EnableMenuAsync(menuId);
        //await uow.CompleteAsync();
        return true;
    }

    /// <summary>
    /// 禁用菜单
    /// </summary>
    public async Task<bool> DisableAsync(long menuId)
    {
        //using var uow = _unitOfWorkManager.Begin();
        await _menuRepository.DisableMenuAsync(menuId);
        //await uow.CompleteAsync();
        return true;
    }

    /// <summary>
    /// 构建菜单树DTO
    /// </summary>
    private List<SysMenuDto> BuildMenuTreeDto(List<SysMenu> allMenus, long? parentId)
    {
        var result = new List<SysMenuDto>();

        var children = allMenus.Where(m => m.ParentId == parentId).OrderBy(m => m.Sort).ToList();

        foreach (var menu in children)
        {
            var dto = menu.Adapt<SysMenuDto>();
            dto.Children = BuildMenuTreeDto(allMenus, menu.BasicId);
            result.Add(dto);
        }

        return result;
    }
}
