#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MenuRouteQueryService
// Guid:7b261e02-9e29-4404-bf6c-f08a66c6c4f0
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 菜单路由查询服务实现
/// </summary>
public sealed class MenuRouteQueryService
    : IMenuRouteQueryService
{
    private readonly IMenuRepository _menuRepository;

    private readonly IPermissionRepository _permissionRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public MenuRouteQueryService(
        IMenuRepository menuRepository,
        IPermissionRepository permissionRepository)
    {
        _menuRepository = menuRepository;
        _permissionRepository = permissionRepository;
    }

    /// <inheritdoc />
    public async Task<List<MenuRouteDto>> GetRoutesAsync(AuthorizationSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        cancellationToken.ThrowIfCancellationRequested();

        var hasAllPermissions = snapshot.Permissions.Contains("*", StringComparer.OrdinalIgnoreCase);
        var allPermissions = await _permissionRepository.GetListAsync(permission => permission.Status == EnableStatus.Enabled, cancellationToken);
        var permissionCodeMap = allPermissions.ToDictionary(permission => permission.BasicId, permission => permission.PermissionCode);
        var menus = await _menuRepository.GetListAsync(
            menu => menu.Status == EnableStatus.Enabled && menu.MenuType != MenuType.Button,
            cancellationToken);
        var visibleMenus = menus
            .Where(menu => menu.IsVisible)
            .Where(menu => !menu.PermissionId.HasValue || hasAllPermissions || snapshot.PermissionIds.Contains(menu.PermissionId.Value))
            .OrderBy(menu => menu.Sort)
            .ThenBy(menu => menu.BasicId)
            .ToList();

        if (visibleMenus.Count == 0)
        {
            return [BuildFallbackDashboardRoute()];
        }

        var routeMap = visibleMenus.ToDictionary(menu => menu.BasicId, menu => ToMenuRoute(menu, permissionCodeMap));
        var roots = new List<MenuRouteDto>();
        foreach (var menu in visibleMenus)
        {
            var route = routeMap[menu.BasicId];
            if (menu.ParentId.HasValue && routeMap.TryGetValue(menu.ParentId.Value, out var parent))
            {
                parent.Children ??= [];
                parent.Children.Add(route);
            }
            else
            {
                roots.Add(route);
            }
        }

        return roots.Count == 0 ? [BuildFallbackDashboardRoute()] : roots;
    }

    private static MenuRouteDto ToMenuRoute(SysMenu menu, IReadOnlyDictionary<long, string> permissionCodeMap)
    {
        var permissionCodes = menu.PermissionId.HasValue && permissionCodeMap.TryGetValue(menu.PermissionId.Value, out var code)
            ? new List<string> { code }
            : null;

        return new MenuRouteDto
        {
            BasicId = menu.BasicId.ToString(),
            Path = string.IsNullOrWhiteSpace(menu.Path) ? $"/{menu.MenuCode}" : menu.Path.Trim(),
            Name = string.IsNullOrWhiteSpace(menu.RouteName) ? ToRouteName(menu.MenuCode) : menu.RouteName.Trim(),
            Component = menu.IsExternal ? null : NormalizeComponent(menu.Component),
            Redirect = NormalizeNullable(menu.Redirect, 200),
            Meta = new MenuMetaDto
            {
                Title = string.IsNullOrWhiteSpace(menu.Title) ? menu.MenuName : menu.Title.Trim(),
                Icon = NormalizeNullable(menu.Icon, 100),
                Hidden = !menu.IsVisible,
                KeepAlive = menu.IsCache,
                AffixTab = menu.IsAffix,
                Permissions = permissionCodes,
                Order = menu.Sort,
                Badge = NormalizeNullable(menu.Badge, 50),
                BadgeType = NormalizeNullable(menu.BadgeType, 20),
                Dot = menu.BadgeDot,
                Link = menu.IsExternal ? NormalizeNullable(menu.ExternalUrl, 500) : null
            }
        };
    }

    private static MenuRouteDto BuildFallbackDashboardRoute()
    {
        return new MenuRouteDto
        {
            Path = "/workbench/dashboard",
            Name = "Dashboard",
            Component = "workbench/dashboard/index",
            Meta = new MenuMetaDto
            {
                Title = "menu.dashboard",
                Icon = "lucide:layout-dashboard",
                AffixTab = true,
                Order = 1
            }
        };
    }

    private static string? NormalizeComponent(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim().Replace('\\', '/').TrimStart('/').Replace(".vue", string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private static string ToRouteName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "DynamicRoute";
        }

        var chars = value
            .Split([':', '-', '_', '.', '/', '\\'], StringSplitOptions.RemoveEmptyEntries)
            .Select(ToPascalSegment);
        var routeName = string.Concat(chars);
        return string.IsNullOrWhiteSpace(routeName) ? "DynamicRoute" : routeName;
    }

    private static string ToPascalSegment(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        return trimmed.Length == 1
            ? trimmed.ToUpperInvariant()
            : char.ToUpperInvariant(trimmed[0]) + trimmed[1..];
    }

    private static string? NormalizeNullable(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length > maxLength ? normalized[..maxLength] : normalized;
    }
}
