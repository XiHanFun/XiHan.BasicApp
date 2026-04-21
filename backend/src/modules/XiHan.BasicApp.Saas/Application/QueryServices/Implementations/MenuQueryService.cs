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

    /// <summary>
    /// 构造函数
    /// </summary>
    public MenuQueryService(IMenuRepository menuRepository)
    {
        _menuRepository = menuRepository;
    }

    /// <inheritdoc />
    [Cacheable(Key = "menu:id:{id}", ExpireSeconds = 300)]
    public async Task<MenuDto?> GetByIdAsync(long id)
    {
        var entity = await _menuRepository.GetByIdAsync(id);
        return entity?.Adapt<MenuDto>();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MenuDto>> GetListAsync()
    {
        var menus = await _menuRepository.GetAllAsync();
        return BuildMenuTree(menus);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MenuDto>> GetRoleMenusAsync(long roleId, long? tenantId = null)
    {
        var menus = await _menuRepository.GetRoleMenusAsync(roleId, tenantId);
        return menus.Select(static menu => menu.Adapt<MenuDto>()!).ToArray();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MenuDto>> GetUserMenusAsync(long userId, long? tenantId = null)
    {
        var menus = await _menuRepository.GetUserMenusAsync(userId, tenantId);
        return menus.Select(static menu => menu.Adapt<MenuDto>()!).ToArray();
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
}
