#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MenuAppService
// Guid:b1f9600a-eec3-4c39-9bb6-31aed4d4b671
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 06:29:49
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Rbac.Application;
using XiHan.BasicApp.Rbac.Application.Dtos;
using XiHan.BasicApp.Rbac.Application.Queries;
using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Application.ApplicationServices.Implementations;

/// <summary>
/// 菜单应用服务
/// </summary>
public class MenuAppService
    : CrudApplicationServiceBase<SysMenu, MenuDto, long, MenuCreateDto, MenuUpdateDto, BasicAppPRDto>,
        IMenuAppService
{
    private readonly IMenuRepository _menuRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="menuRepository"></param>
    public MenuAppService(IMenuRepository menuRepository)
        : base(menuRepository)
    {
        _menuRepository = menuRepository;
    }

    /// <summary>
    /// 根据角色ID获取菜单
    /// </summary>
    /// <param name="roleId"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<MenuDto>> GetRoleMenusAsync(long roleId, long? tenantId = null)
    {
        var menus = await _menuRepository.GetRoleMenusAsync(roleId, tenantId);
        return menus.Select(static menu => menu.Adapt<MenuDto>()).ToArray();
    }

    /// <summary>
    /// 根据用户ID获取菜单
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<MenuDto>> GetUserMenusAsync(UserMenuQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        var menus = await _menuRepository.GetUserMenusAsync(query.UserId, query.TenantId);
        return menus.Select(static menu => menu.Adapt<MenuDto>()).ToArray();
    }

    /// <summary>
    /// 创建菜单
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public override async Task<MenuDto> CreateAsync(MenuCreateDto input)
    {
        input.ValidateAnnotations();

        var normalizedCode = input.MenuCode.Trim();
        await EnsureMenuCodeUniqueAsync(normalizedCode, null, input.TenantId);

        return await base.CreateAsync(input);
    }

    /// <summary>
    /// 更新菜单
    /// </summary>
    /// <param name="id"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    public override async Task<MenuDto> UpdateAsync(long id, MenuUpdateDto input)
    {
        input.ValidateAnnotations();

        var menu = await _menuRepository.GetByIdAsync(id)
                   ?? throw new KeyNotFoundException($"未找到菜单: {id}");

        var normalizedCode = input.MenuCode.Trim();
        await EnsureMenuCodeUniqueAsync(normalizedCode, id, menu.TenantId);

        await MapDtoToEntityAsync(input, menu);
        var updated = await _menuRepository.UpdateAsync(menu);
        return updated.Adapt<MenuDto>();
    }

    /// <summary>
    /// 映射创建 DTO 到实体
    /// </summary>
    /// <param name="createDto"></param>
    /// <returns></returns>
    protected override Task<SysMenu> MapDtoToEntityAsync(MenuCreateDto createDto)
    {
        var entity = new SysMenu
        {
            TenantId = createDto.TenantId,
            ResourceId = createDto.ResourceId,
            ParentId = createDto.ParentId,
            MenuName = createDto.MenuName.Trim(),
            MenuCode = createDto.MenuCode.Trim(),
            MenuType = createDto.MenuType,
            Path = createDto.Path,
            Component = createDto.Component,
            Icon = createDto.Icon,
            IsVisible = createDto.IsVisible,
            Sort = createDto.Sort,
            Remark = createDto.Remark
        };

        return Task.FromResult(entity);
    }

    /// <summary>
    /// 映射更新 DTO 到实体
    /// </summary>
    /// <param name="updateDto"></param>
    /// <param name="entity"></param>
    protected override Task MapDtoToEntityAsync(MenuUpdateDto updateDto, SysMenu entity)
    {
        entity.ResourceId = updateDto.ResourceId;
        entity.ParentId = updateDto.ParentId;
        entity.MenuName = updateDto.MenuName.Trim();
        entity.MenuCode = updateDto.MenuCode.Trim();
        entity.MenuType = updateDto.MenuType;
        entity.Path = updateDto.Path;
        entity.Component = updateDto.Component;
        entity.Icon = updateDto.Icon;
        entity.IsVisible = updateDto.IsVisible;
        entity.Status = updateDto.Status;
        entity.Sort = updateDto.Sort;
        entity.Remark = updateDto.Remark;
        return Task.CompletedTask;
    }

    /// <summary>
    /// 校验菜单编码唯一性
    /// </summary>
    /// <param name="menuCode"></param>
    /// <param name="excludeMenuId"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private async Task EnsureMenuCodeUniqueAsync(string menuCode, long? excludeMenuId, long? tenantId)
    {
        var existing = await _menuRepository.GetByMenuCodeAsync(menuCode, tenantId);
        if (existing is not null && (!excludeMenuId.HasValue || existing.BasicId != excludeMenuId.Value))
        {
            throw new InvalidOperationException($"菜单编码 '{menuCode}' 已存在");
        }
    }
}
