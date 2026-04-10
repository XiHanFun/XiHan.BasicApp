#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AuthMenuBuilder
// Guid:066c0665-d467-413d-b2f5-477b54ed02d1
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Helpers;

/// <summary>
/// 认证菜单路由构建器
/// </summary>
internal static class AuthMenuBuilder
{
    /// <summary>
    /// 将菜单实体列表构建为前端路由树
    /// </summary>
    public static List<AuthMenuRouteDto> BuildMenuRoutes(
        IReadOnlyList<SysMenu> menus,
        IReadOnlyDictionary<long, IReadOnlyCollection<string>> resourcePermissionMap)
    {
        var routeMenus = menus
            .Where(static menu => menu.MenuType != MenuType.Button)
            .OrderBy(static menu => menu.Sort)
            .ThenBy(static menu => menu.BasicId)
            .ToList();
        var menuMap = routeMenus.ToDictionary(static menu => menu.BasicId, menu => MapMenuRoute(menu, resourcePermissionMap));
        var rootMenus = new List<AuthMenuRouteDto>();

        foreach (var menu in routeMenus)
        {
            var route = menuMap[menu.BasicId];
            if (menu.ParentId.HasValue && menuMap.TryGetValue(menu.ParentId.Value, out var parent))
            {
                parent.Children.Add(route);
            }
            else
            {
                rootMenus.Add(route);
            }
        }

        SortMenuTree(rootMenus);
        return rootMenus;
    }

    private static AuthMenuRouteDto MapMenuRoute(SysMenu menu, IReadOnlyDictionary<long, IReadOnlyCollection<string>> resourcePermissionMap)
    {
        var permissionCodes = menu.ResourceId.HasValue && resourcePermissionMap.TryGetValue(menu.ResourceId.Value, out var permissions)
            ? permissions
            : [];
        var permissionCode = ResolvePrimaryPermission(permissionCodes);
        return new AuthMenuRouteDto
        {
            Name = !string.IsNullOrWhiteSpace(menu.RouteName)
                ? menu.RouteName
                : !string.IsNullOrWhiteSpace(menu.MenuCode)
                    ? menu.MenuCode
                    : $"menu_{menu.BasicId}",
            Path = !string.IsNullOrWhiteSpace(menu.Path) ? menu.Path : BuildFallbackMenuPath(menu),
            Component = menu.Component,
            Redirect = menu.Redirect,
            Permission = permissionCode,
            Meta = new AuthMenuMetaDto
            {
                Title = !string.IsNullOrWhiteSpace(menu.Title) ? menu.Title : menu.MenuName,
                Icon = menu.Icon,
                Hidden = !menu.IsVisible,
                KeepAlive = menu.IsCache,
                AffixTab = menu.IsAffix,
                Permissions = [.. permissionCodes],
                Order = menu.Sort,
                Link = menu.IsExternal ? menu.ExternalUrl : null,
                Badge = menu.Badge,
                BadgeType = menu.BadgeType,
                Dot = menu.BadgeDot
            }
        };
    }

    private static string? ResolvePrimaryPermission(IReadOnlyCollection<string> permissionCodes)
    {
        if (permissionCodes.Count == 0)
        {
            return null;
        }

        var candidates = permissionCodes
            .Where(static permissionCode => !string.IsNullOrWhiteSpace(permissionCode))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (candidates.Length == 0)
        {
            return null;
        }

        var preferred = candidates.FirstOrDefault(static permissionCode =>
            permissionCode.EndsWith(":read", StringComparison.OrdinalIgnoreCase)
            || permissionCode.EndsWith(":view", StringComparison.OrdinalIgnoreCase)
            || permissionCode.EndsWith(":list", StringComparison.OrdinalIgnoreCase)
            || permissionCode.EndsWith(":query", StringComparison.OrdinalIgnoreCase));
        return preferred ?? candidates[0];
    }

    private static void SortMenuTree(List<AuthMenuRouteDto> menus)
    {
        menus.Sort((left, right) => left.Meta.Order.CompareTo(right.Meta.Order));
        foreach (var menu in menus.Where(static menu => menu.Children.Count > 0))
        {
            SortMenuTree(menu.Children);
        }
    }

    private static string BuildFallbackMenuPath(SysMenu menu)
    {
        var seed = string.IsNullOrWhiteSpace(menu.MenuCode) ? $"menu-{menu.BasicId}" : menu.MenuCode;
        var normalized = seed
            .Replace("_", "-", StringComparison.Ordinal)
            .Replace(":", "-", StringComparison.Ordinal)
            .ToLowerInvariant();
        return menu.ParentId.HasValue ? normalized : $"/{normalized}";
    }
}
