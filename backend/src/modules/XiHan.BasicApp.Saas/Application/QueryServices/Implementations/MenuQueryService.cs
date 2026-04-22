#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MenuQueryService
// Guid:4c5d6e7f-8091-0123-cdef-012345678902
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.InternalServices;
using XiHan.BasicApp.Saas.Constants.Caching;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Caching.Attributes;
using XiHan.Framework.Core.DependencyInjection.ServiceLifetimes;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 菜单查询服务
/// </summary>
public class MenuQueryService : IMenuQueryService, ITransientDependency
{
    private readonly IMenuRepository _menuRepository;
    private readonly ITenantAccessContextService _tenantAccessContextService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public MenuQueryService(IMenuRepository menuRepository, ITenantAccessContextService tenantAccessContextService)
    {
        _menuRepository = menuRepository;
        _tenantAccessContextService = tenantAccessContextService;
    }

    /// <inheritdoc />
    [Cacheable(Key = QueryCacheKeys.MenuById, ExpireSeconds = 300)]
    public async Task<MenuDto?> GetByIdAsync(long id)
    {
        var entity = await _menuRepository.GetByIdAsync(id);
        return entity?.Adapt<MenuDto>();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MenuDto>> GetListAsync()
    {
        var tenantId = await ResolveTenantIdAsync(null);
        var menus = await _menuRepository.GetAllAsync();
        menus = menus
            .Where(menu => tenantId.HasValue ? menu.TenantId == tenantId.Value || menu.TenantId == 0 : menu.TenantId == 0)
            .Where(menu => menu.Status == Domain.Enums.YesOrNo.Yes)
            .ToArray();
        return BuildMenuTree(menus);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MenuDto>> GetRoleMenusAsync(long roleId, long? tenantId = null)
    {
        var resolvedTenantId = await ResolveTenantIdAsync(tenantId);
        var menus = await _menuRepository.GetRoleMenusAsync(roleId, resolvedTenantId);
        return BuildMenuTree(menus);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MenuDto>> GetUserMenusAsync(long userId, long? tenantId = null)
    {
        var resolvedTenantId = await ResolveTenantIdAsync(tenantId);
        var menus = await _menuRepository.GetUserMenusAsync(userId, resolvedTenantId);
        return BuildMenuTree(menus);
    }

    private static IReadOnlyList<MenuDto> BuildMenuTree(IReadOnlyList<XiHan.BasicApp.Saas.Domain.Entities.SysMenu> menus)
    {
        var allDtos = menus
            .OrderBy(static m => m.Sort)
            .ThenBy(static m => m.BasicId)
            .Select(static m =>
            {
                var dto = m.Adapt<MenuDto>()!;
                dto.Children ??= [];
                return dto;
            })
            .ToList();

        var map = allDtos.ToDictionary(static d => d.BasicId);
        var roots = new List<MenuDto>();

        foreach (var dto in allDtos)
        {
            if (dto.ParentId.HasValue && map.TryGetValue(dto.ParentId.Value, out var parent))
            {
                parent.Children.Add(dto);
            }
            else
            {
                roots.Add(dto);
            }
        }

        return roots;
    }

    private async Task<long?> ResolveTenantIdAsync(long? tenantId)
    {
        if (tenantId.HasValue && tenantId.Value > 0)
        {
            return tenantId;
        }

        var currentContext = await _tenantAccessContextService.GetCurrentContextAsync();
        return currentContext?.CurrentTenantId;
    }
}
