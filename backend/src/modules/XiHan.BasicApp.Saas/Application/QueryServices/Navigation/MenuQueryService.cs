// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Extensions;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Domain.Shared.Paging.Dtos;
using XiHan.Framework.Domain.Shared.Paging.Enums;
using XiHan.Framework.Domain.Shared.Paging.Models;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 菜单查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "菜单")]
public sealed class MenuQueryService
    : SaasApplicationService, IMenuQueryService
{
    /// <summary>
    /// 菜单仓储
    /// </summary>
    private readonly IMenuRepository _menuRepository;

    /// <summary>
    /// 权限仓储
    /// </summary>
    private readonly IPermissionRepository _permissionRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public MenuQueryService(
        IMenuRepository menuRepository,
        IPermissionRepository permissionRepository)
    {
        _menuRepository = menuRepository;
        _permissionRepository = permissionRepository;
    }

    /// <summary>
    /// 获取菜单分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>菜单分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.Menu.Read)]
    [HttpPost]
    public async Task<PageResultDtoBase<MenuListItemDto>> GetMenuPageAsync(MenuPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildMenuPageRequest(input);
        var menus = await _menuRepository.GetPagedAsync(request, cancellationToken);
        if (menus.Items.Count == 0)
        {
            return new PageResultDtoBase<MenuListItemDto>([], menus.Page)
            {
                ExtendDatas = menus.ExtendDatas
            };
        }

        var permissionMap = await BuildPermissionMapAsync(menus.Items.Select(menu => menu.PermissionId), cancellationToken);
        var items = menus.Items
            .Select(menu => MenuApplicationMapper.ToListItemDto(menu, TryGetMapValue(permissionMap, menu.PermissionId)))
            .ToList();

        return new PageResultDtoBase<MenuListItemDto>(items, menus.Page)
        {
            ExtendDatas = menus.ExtendDatas
        };
    }

    /// <summary>
    /// 获取菜单列表（不分页，返回全部匹配项）
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>菜单列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.Menu.Read)]
    public async Task<IReadOnlyList<MenuListItemDto>> GetMenuListAsync(MenuListQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildMenuListRequest(input);
        var menus = await _menuRepository.GetListAsync(request, cancellationToken);
        if (menus.Count == 0)
        {
            return [];
        }

        var permissionMap = await BuildPermissionMapAsync(menus.Select(menu => menu.PermissionId), cancellationToken);
        return [.. menus.Select(menu => MenuApplicationMapper.ToListItemDto(menu, TryGetMapValue(permissionMap, menu.PermissionId)))];
    }

    /// <summary>
    /// 获取菜单详情
    /// </summary>
    /// <param name="id">菜单主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>菜单详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.Menu.Read)]
    public async Task<MenuDetailDto?> GetMenuDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "菜单主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var menu = await _menuRepository.GetByIdAsync(id, cancellationToken);
        if (menu is null)
        {
            return null;
        }

        var permission = menu.PermissionId.HasValue
            ? await _permissionRepository.GetByIdAsync(menu.PermissionId.Value, cancellationToken)
            : null;

        return MenuApplicationMapper.ToDetailDto(menu, permission);
    }

    /// <summary>
    /// 获取菜单树
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>菜单树</returns>
    [PermissionAuthorize(SaasPermissionCodes.Menu.Read)]
    public async Task<IReadOnlyList<MenuTreeNodeDto>> GetMenuTreeAsync(MenuTreeQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildMenuTreeRequest(input);
        var menus = await _menuRepository.GetPagedAsync(request, cancellationToken);
        var menuItems = input.IncludeButtons
            ? menus.Items
            : [.. menus.Items.Where(menu => menu.MenuType != MenuType.Button)];
        if (menuItems.Count == 0)
        {
            return [];
        }

        var permissionMap = await BuildPermissionMapAsync(menuItems.Select(menu => menu.PermissionId), cancellationToken);
        var nodes = menuItems
            .Select(menu => MenuApplicationMapper.ToTreeNodeDto(menu, TryGetMapValue(permissionMap, menu.PermissionId)))
            .ToList();

        return BuildTree(nodes);
    }

    /// <summary>
    /// 构建菜单分页请求
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>菜单分页请求</returns>
    private static BasicAppPRDto BuildMenuPageRequest(MenuPageQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Page = input.Page,
            Conditions = new QueryConditions()
        };

        ApplyCommonMenuFilters(
            request,
            input.Keyword,
            input.ParentId,
            input.PermissionId,
            input.MenuType,
            input.IsExternal,
            input.IsVisible,
            input.IsGlobal,
            input.Status);
        ApplyMenuSorts(request);
        return request;
    }

    /// <summary>
    /// 构建菜单列表请求（不分页）
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>菜单列表请求</returns>
    private static BasicAppPRDto BuildMenuListRequest(MenuListQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Conditions = new QueryConditions()
        };

        ApplyCommonMenuFilters(
            request,
            input.Keyword,
            input.ParentId,
            input.PermissionId,
            input.MenuType,
            input.IsExternal,
            input.IsVisible,
            input.IsGlobal,
            input.Status);
        ApplyMenuSorts(request);
        return request;
    }

    /// <summary>
    /// 构建菜单树请求
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>菜单树请求</returns>
    private static BasicAppPRDto BuildMenuTreeRequest(MenuTreeQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Conditions = new QueryConditions()
        };

        request.Page.PageSize = Math.Clamp(input.Limit, 1, 5000);
        ApplyCommonMenuFilters(
            request,
            input.Keyword,
            parentId: null,
            permissionId: null,
            menuType: null,
            isExternal: null,
            isVisible: input.OnlyVisible ? true : null,
            isGlobal: null,
            status: input.OnlyEnabled ? EnableStatus.Enabled : null);
        ApplyMenuSorts(request);
        return request;
    }

    /// <summary>
    /// 应用菜单通用筛选条件
    /// </summary>
    private static void ApplyCommonMenuFilters(
        BasicAppPRDto request,
        string? keyword,
        long? parentId,
        long? permissionId,
        MenuType? menuType,
        bool? isExternal,
        bool? isVisible,
        bool? isGlobal,
        EnableStatus? status)
    {
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            request.Conditions.SetKeyword<SysMenu>(
                keyword.Trim(),
                menu => menu.MenuCode,
                menu => menu.MenuName,
                menu => menu.Path,
                menu => menu.Component,
                menu => menu.RouteName,
                menu => menu.Title,
                menu => menu.Remark);
        }

        if (parentId.HasValue && parentId.Value > 0)
        {
            request.Conditions.AddFilter((SysMenu menu) => menu.ParentId, parentId.Value);
        }

        if (permissionId.HasValue && permissionId.Value > 0)
        {
            request.Conditions.AddFilter((SysMenu menu) => menu.PermissionId, permissionId.Value);
        }

        if (menuType.HasValue)
        {
            request.Conditions.AddFilter((SysMenu menu) => menu.MenuType, menuType.Value);
        }

        if (isExternal.HasValue)
        {
            request.Conditions.AddFilter((SysMenu menu) => menu.IsExternal, isExternal.Value);
        }

        if (isVisible.HasValue)
        {
            request.Conditions.AddFilter((SysMenu menu) => menu.IsVisible, isVisible.Value);
        }

        if (isGlobal.HasValue)
        {
            // IsGlobal 为派生属性（TenantId == 0），不落库，故按租户列过滤：全局=租户0，非全局=非租户0
            request.Conditions.AddFilter(
                (SysMenu menu) => menu.TenantId,
                0L,
                isGlobal.Value ? QueryOperator.Equal : QueryOperator.NotEqual);
        }

        if (status.HasValue)
        {
            request.Conditions.AddFilter((SysMenu menu) => menu.Status, status.Value);
        }
    }

    /// <summary>
    /// 应用菜单排序
    /// </summary>
    private static void ApplyMenuSorts(BasicAppPRDto request)
    {
        request.Conditions.AddSort((SysMenu menu) => menu.ParentId, SortDirection.Ascending, 0);
        request.Conditions.AddSort((SysMenu menu) => menu.Sort, SortDirection.Ascending, 1);
        request.Conditions.AddSort((SysMenu menu) => menu.MenuCode, SortDirection.Ascending, 2);
    }

    /// <summary>
    /// 构建菜单树
    /// </summary>
    private static IReadOnlyList<MenuTreeNodeDto> BuildTree(IReadOnlyList<MenuTreeNodeDto> nodes)
    {
        var nodeMap = nodes.ToDictionary(node => node.BasicId);
        var roots = new List<MenuTreeNodeDto>();

        foreach (var node in nodes.OrderBy(node => node.Sort).ThenBy(node => node.MenuCode, StringComparer.Ordinal))
        {
            if (node.ParentId.HasValue
                && node.ParentId.Value != node.BasicId
                && nodeMap.TryGetValue(node.ParentId.Value, out var parent))
            {
                parent.Children.Add(node);
                continue;
            }

            roots.Add(node);
        }

        return roots;
    }

    /// <summary>
    /// 从可空主键映射中读取实体
    /// </summary>
    private static TValue? TryGetMapValue<TValue>(IReadOnlyDictionary<long, TValue> map, long? id)
        where TValue : class
    {
        return id.HasValue && map.TryGetValue(id.Value, out var value) ? value : null;
    }

    /// <summary>
    /// 构建权限定义映射
    /// </summary>
    private async Task<IReadOnlyDictionary<long, SysPermission>> BuildPermissionMapAsync(IEnumerable<long?> permissionIds, CancellationToken cancellationToken)
    {
        var ids = permissionIds
            .Where(permissionId => permissionId.HasValue && permissionId.Value > 0)
            .Select(permissionId => permissionId!.Value)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<long, SysPermission>();
        }

        var permissions = await _permissionRepository.GetByIdsAsync(ids, cancellationToken);
        return permissions.ToDictionary(permission => permission.BasicId);
    }
}
