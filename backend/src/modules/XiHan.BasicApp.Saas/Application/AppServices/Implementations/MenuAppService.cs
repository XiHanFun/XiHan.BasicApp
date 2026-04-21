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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.InternalServices;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Application.UseCases.Queries;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Core.Exceptions;

namespace XiHan.BasicApp.Saas.Application.AppServices.Implementations;

/// <summary>
/// 菜单应用服务
/// </summary>
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统Saas服务")]
[Authorize]
[PermissionAuthorize("menu:read")]
public class MenuAppService
    : CrudApplicationServiceBase<SysMenu, MenuDto, long, MenuCreateDto, MenuUpdateDto, BasicAppPRDto>,
        IMenuAppService
{
    private readonly IMenuRepository _menuRepository;
    private readonly IMenuQueryService _queryService;
    private readonly IMenuDomainService _domainService;
    private readonly IRbacChangeNotifier _rbacChangeNotifier;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="menuRepository"></param>
    /// <param name="queryService"></param>
    /// <param name="domainService"></param>
    /// <param name="rbacChangeNotifier"></param>
    public MenuAppService(
        IMenuRepository menuRepository,
        IMenuQueryService queryService,
        IMenuDomainService domainService,
        IRbacChangeNotifier rbacChangeNotifier)
        : base(menuRepository)
    {
        _menuRepository = menuRepository;
        _queryService = queryService;
        _domainService = domainService;
        _rbacChangeNotifier = rbacChangeNotifier;
    }

    /// <summary>
    /// 根据ID获取菜单
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [PermissionAuthorize("menu:read")]
    public override async Task<MenuDto?> GetByIdAsync(long id)
    {
        return await _queryService.GetByIdAsync(id);
    }

    /// <summary>
    /// 获取所有菜单树
    /// </summary>
    [HttpGet]
    [PermissionAuthorize("menu:read")]
    public async Task<IReadOnlyList<MenuDto>> GetListAsync()
    {
        return await _queryService.GetListAsync();
    }

    /// <summary>
    /// 根据角色ID获取菜单
    /// </summary>
    /// <param name="roleId"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    [PermissionAuthorize("menu:read")]
    public async Task<IReadOnlyList<MenuDto>> GetRoleMenusAsync(long roleId, long? tenantId = null)
    {
        if (roleId <= 0)
        {
            throw new ArgumentException("角色 ID 无效", nameof(roleId));
        }

        return await _queryService.GetRoleMenusAsync(roleId, tenantId);
    }

    /// <summary>
    /// 根据用户ID获取菜单
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    [PermissionAuthorize("menu:read")]
    public async Task<IReadOnlyList<MenuDto>> GetUserMenusAsync(UserMenuQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.UserId <= 0)
        {
            throw new ArgumentException("用户 ID 无效", nameof(query.UserId));
        }

        return await _queryService.GetUserMenusAsync(query.UserId, query.TenantId);
    }

    /// <summary>
    /// 创建菜单
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [PermissionAuthorize("menu:create")]
    public override async Task<MenuDto> CreateAsync(MenuCreateDto input)
    {
        input.ValidateAnnotations();

        var normalizedCode = input.MenuCode.Trim();
        await EnsureMenuCodeUniqueAsync(normalizedCode, null, input.TenantId);
        await EnsureValidParentAsync(input.ParentId, null, input.TenantId);

        var entity = await MapDtoToEntityAsync(input);
        var created = await _domainService.CreateAsync(entity);
        await _rbacChangeNotifier.NotifyAsync(created.TenantId, AuthorizationChangeType.Permission);
        return created.Adapt<MenuDto>()!;
    }

    /// <summary>
    /// 更新菜单
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [PermissionAuthorize("menu:update")]
    public override async Task<MenuDto> UpdateAsync(MenuUpdateDto input)
    {
        input.ValidateAnnotations();

        var menu = await _menuRepository.GetByIdAsync(input.BasicId)
                   ?? throw new KeyNotFoundException($"未找到菜单: {input.BasicId}");

        var normalizedCode = input.MenuCode.Trim();
        await EnsureMenuCodeUniqueAsync(normalizedCode, input.BasicId, menu.TenantId);
        await EnsureValidParentAsync(input.ParentId, input.BasicId, menu.TenantId);

        await MapDtoToEntityAsync(input, menu);
        var updated = await _domainService.UpdateAsync(menu);
        await _rbacChangeNotifier.NotifyAsync(updated.TenantId, AuthorizationChangeType.Permission);
        return updated.Adapt<MenuDto>()!;
    }

    /// <summary>
    /// 删除菜单
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [PermissionAuthorize("menu:delete")]
    public override async Task<bool> DeleteAsync(long id)
    {
        var menu = await _menuRepository.GetByIdAsync(id);
        if (menu is null)
        {
            return false;
        }

        var deleted = await _domainService.DeleteAsync(id);
        if (deleted)
        {
            await _rbacChangeNotifier.NotifyAsync(menu.TenantId, AuthorizationChangeType.Permission);
        }

        return deleted;
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
            TenantId = createDto.TenantId ?? 0,
            ParentId = createDto.ParentId,
            MenuName = createDto.MenuName.Trim(),
            MenuCode = createDto.MenuCode.Trim(),
            MenuType = createDto.MenuType,
            Path = createDto.Path,
            Component = createDto.Component,
            RouteName = createDto.RouteName,
            Redirect = createDto.Redirect,
            Icon = createDto.Icon,
            Title = createDto.Title,
            IsExternal = createDto.IsExternal,
            ExternalUrl = createDto.ExternalUrl,
            IsCache = createDto.IsCache,
            IsVisible = createDto.IsVisible,
            IsAffix = createDto.IsAffix,
            Badge = createDto.Badge,
            BadgeType = createDto.BadgeType,
            BadgeDot = createDto.BadgeDot,
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
    /// <returns></returns>
    protected override Task MapDtoToEntityAsync(MenuUpdateDto updateDto, SysMenu entity)
    {
        entity.ParentId = updateDto.ParentId;
        entity.MenuName = updateDto.MenuName.Trim();
        entity.MenuCode = updateDto.MenuCode.Trim();
        entity.MenuType = updateDto.MenuType;
        entity.Path = updateDto.Path;
        entity.Component = updateDto.Component;
        entity.RouteName = updateDto.RouteName;
        entity.Redirect = updateDto.Redirect;
        entity.Icon = updateDto.Icon;
        entity.Title = updateDto.Title;
        entity.IsExternal = updateDto.IsExternal;
        entity.ExternalUrl = updateDto.ExternalUrl;
        entity.IsCache = updateDto.IsCache;
        entity.IsVisible = updateDto.IsVisible;
        entity.IsAffix = updateDto.IsAffix;
        entity.Badge = updateDto.Badge;
        entity.BadgeType = updateDto.BadgeType;
        entity.BadgeDot = updateDto.BadgeDot;
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
            throw new BusinessException(message: $"菜单编码 '{menuCode}' 已存在");
        }
    }

    /// <summary>
    /// 校验菜单父级合法性
    /// </summary>
    /// <param name="parentId"></param>
    /// <param name="currentMenuId"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="KeyNotFoundException"></exception>
    private async Task EnsureValidParentAsync(long? parentId, long? currentMenuId, long? tenantId)
    {
        if (!parentId.HasValue)
        {
            return;
        }

        if (currentMenuId.HasValue && parentId.Value == currentMenuId.Value)
        {
            throw new BusinessException(message: "上级菜单不能是当前菜单本身");
        }

        var parent = await _menuRepository.GetByIdAsync(parentId.Value)
                     ?? throw new KeyNotFoundException($"未找到上级菜单: {parentId.Value}");

        if (parent.Status != YesOrNo.Yes)
        {
            throw new BusinessException(message: "上级菜单已禁用，无法关联");
        }

        if (parent.TenantId != tenantId)
        {
            throw new BusinessException(message: "上级菜单与当前菜单租户不一致");
        }

        if (!currentMenuId.HasValue)
        {
            return;
        }

        var visited = new HashSet<long> { currentMenuId.Value };
        var currentParent = parent;
        while (true)
        {
            if (!visited.Add(currentParent.BasicId))
            {
                throw new BusinessException(message: "菜单层级存在循环引用");
            }

            if (!currentParent.ParentId.HasValue)
            {
                break;
            }

            var nextParent = await _menuRepository.GetByIdAsync(currentParent.ParentId.Value);
            if (nextParent is null || nextParent.Status != YesOrNo.Yes)
            {
                break;
            }

            currentParent = nextParent;
        }
    }

}
