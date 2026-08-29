// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Caching.Distributed;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Caching.Distributed.Abstracts;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 菜单路由查询服务实现
/// </summary>
public sealed class MenuRouteQueryService
    : IMenuRouteQueryService
{
    private readonly IMenuRepository _menuRepository;

    private readonly IPermissionRepository _permissionRepository;

    private readonly IDistributedCache<SaasMenuRoutesCacheItem, string> _menuRoutesCache;

    /// <summary>
    /// 构造函数
    /// </summary>
    public MenuRouteQueryService(
        IMenuRepository menuRepository,
        IPermissionRepository permissionRepository,
        IDistributedCache<SaasMenuRoutesCacheItem, string> menuRoutesCache)
    {
        _menuRepository = menuRepository;
        _permissionRepository = permissionRepository;
        _menuRoutesCache = menuRoutesCache;
    }

    /// <summary>
    /// 按授权快照获取菜单路由
    /// </summary>
    public async Task<List<MenuRouteDto>> GetRoutesAsync(
        AuthorizationSnapshot snapshot,
        IReadOnlySet<string>? deniedPermissionCodes = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        cancellationToken.ThrowIfCancellationRequested();

        var hasAllPermissions = snapshot.Permissions.Contains("*", StringComparer.OrdinalIgnoreCase);
        // 分布式缓存：按权限集缓存菜单路由（同权限集的用户共享）。失效由写路径直接触发——
        // 菜单增删改时 MenuAppService、权限定义启停删时 PermissionAppService 调 InvalidateNavigationAsync
        //（considerUow，事务提交后失效）；用户授权变更则因权限集实时变化使缓存键自动改变。
        // 禁用清单必须进缓存键：模仿态与常态的权限主键集是同一份（清单过滤的是权限码），
        // 不并进去两者就共用同一条缓存，模仿态直接吃到常态那份完整菜单
        var cacheKey = SaasCacheKeys.MenuRoutes(snapshot.PermissionIds, hasAllPermissions, deniedPermissionCodes);
        var item = await _menuRoutesCache.GetOrAddAsync(
            cacheKey,
            async () => new SaasMenuRoutesCacheItem
            {
                Routes = await BuildRoutesAsync(snapshot, hasAllPermissions, deniedPermissionCodes, cancellationToken),
                CachedAt = DateTimeOffset.UtcNow
            },
            CreateCacheOptions,
            hideErrors: true,
            token: cancellationToken);

        return item is null
            ? await BuildRoutesAsync(snapshot, hasAllPermissions, deniedPermissionCodes, cancellationToken)
            : item.Routes;
    }

    /// <summary>
    /// 按授权快照获取当前用户可用的按钮码
    /// </summary>
    /// <remarks>
    /// 不缓存：本方法只在登录与权限刷新时被调用，两次仓储查询的代价低于维护一份随权限集变化的缓存。
    /// 按钮行缺 PermissionId 视为未授权（fail-closed）——按钮登记必带权限码，缺失说明权限没播种到位。
    /// </remarks>
    /// <param name="snapshot">授权快照</param>
    /// <param name="deniedPermissionCodes">额外禁用的权限码（如模仿态禁用清单）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>可用按钮码</returns>
    public async Task<List<string>> GetGrantedButtonCodesAsync(
        AuthorizationSnapshot snapshot,
        IReadOnlySet<string>? deniedPermissionCodes = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        cancellationToken.ThrowIfCancellationRequested();

        var buttons = await _menuRepository.GetListAsync(
            menu => menu.Status == EnableStatus.Enabled && menu.MenuType == MenuType.Button,
            cancellationToken);
        if (buttons.Count == 0)
        {
            return [];
        }

        var permissions = await _permissionRepository.GetListAsync(
            permission => permission.Status == EnableStatus.Enabled,
            cancellationToken);
        var permissionCodeMap = permissions.ToDictionary(
            permission => permission.BasicId,
            permission => permission.PermissionCode);

        var hasAllPermissions = snapshot.Permissions.Contains("*", StringComparer.OrdinalIgnoreCase);

        return
        [
            .. buttons
                .Where(menu => menu.IsVisible)
                .Where(menu => IsButtonGranted(menu, snapshot, hasAllPermissions, permissionCodeMap, deniedPermissionCodes))
                .Select(menu => menu.MenuCode)
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(code => code, StringComparer.Ordinal)
        ];
    }

    /// <summary>
    /// 判定单个菜单是否可见
    /// </summary>
    /// <remarks>
    /// 与按钮同口径：禁用清单先于通配权限判断，否则被模仿者持有通配权限时禁用项照旧显示。
    /// 不关联权限的菜单（目录、公共页）对所有人可见。
    /// </remarks>
    private static bool IsMenuGranted(
        SysMenu menu,
        AuthorizationSnapshot snapshot,
        bool hasAllPermissions,
        IReadOnlyDictionary<long, string> permissionCodeMap,
        IReadOnlySet<string>? deniedPermissionCodes)
    {
        if (!menu.PermissionId.HasValue)
        {
            return true;
        }

        if (deniedPermissionCodes is { Count: > 0 }
            && permissionCodeMap.TryGetValue(menu.PermissionId.Value, out var permissionCode)
            && deniedPermissionCodes.Contains(permissionCode))
        {
            return false;
        }

        return hasAllPermissions || snapshot.PermissionIds.Contains(menu.PermissionId.Value);
    }

    /// <summary>
    /// 判定单个按钮是否可用
    /// </summary>
    private static bool IsButtonGranted(
        SysMenu menu,
        AuthorizationSnapshot snapshot,
        bool hasAllPermissions,
        IReadOnlyDictionary<long, string> permissionCodeMap,
        IReadOnlySet<string>? deniedPermissionCodes)
    {
        if (!menu.PermissionId.HasValue)
        {
            return false;
        }

        if (deniedPermissionCodes is { Count: > 0 }
            && permissionCodeMap.TryGetValue(menu.PermissionId.Value, out var permissionCode)
            && deniedPermissionCodes.Contains(permissionCode))
        {
            return false;
        }

        return hasAllPermissions || snapshot.PermissionIds.Contains(menu.PermissionId.Value);
    }

    private static DistributedCacheEntryOptions CreateCacheOptions()
    {
        return new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
        };
    }

    /// <summary>
    /// 递归剪掉无可见子菜单的目录（自底向上：先剪子目录，父目录随之可能变空再剪）
    /// </summary>
    private static List<MenuRouteDto> PruneEmptyDirectories(List<MenuRouteDto> nodes, HashSet<string> directoryIds)
    {
        var result = new List<MenuRouteDto>();
        foreach (var node in nodes)
        {
            if (node.Children is { Count: > 0 })
            {
                node.Children = PruneEmptyDirectories(node.Children, directoryIds);
            }

            var isDirectory = node.BasicId != null && directoryIds.Contains(node.BasicId);
            var hasChildren = node.Children is { Count: > 0 };
            // 目录且剪枝后无子节点 → 丢弃；叶子菜单（含外链）一律保留
            if (isDirectory && !hasChildren)
            {
                continue;
            }

            result.Add(node);
        }

        return result;
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
                // 国际化优先：有 I18nKey 时下发键（前端 te(key) 命中则翻译，否则回退原文），无键回退 Title/MenuName
                Title = !string.IsNullOrWhiteSpace(menu.I18nKey)
                    ? menu.I18nKey.Trim()
                    : string.IsNullOrWhiteSpace(menu.Title) ? menu.MenuName : menu.Title.Trim(),
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

    /// <summary>
    /// 构建菜单路由树（缓存未命中时执行，含空目录剪枝）。
    /// </summary>
    private async Task<List<MenuRouteDto>> BuildRoutesAsync(
        AuthorizationSnapshot snapshot,
        bool hasAllPermissions,
        IReadOnlySet<string>? deniedPermissionCodes,
        CancellationToken cancellationToken)
    {
        var allPermissions = await _permissionRepository.GetListAsync(permission => permission.Status == EnableStatus.Enabled, cancellationToken);
        var permissionCodeMap = allPermissions.ToDictionary(permission => permission.BasicId, permission => permission.PermissionCode);
        var menus = await _menuRepository.GetListAsync(
            menu => menu.Status == EnableStatus.Enabled && menu.MenuType != MenuType.Button,
            cancellationToken);
        var visibleMenus = menus
            .Where(menu => menu.IsVisible)
            .Where(menu => IsMenuGranted(menu, snapshot, hasAllPermissions, permissionCodeMap, deniedPermissionCodes))
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

        // 剪掉无可见子菜单的目录：目录本身不关联权限（对所有人可见），但若其下无任何已授权的子菜单，
        // 显示一个点不进去的空目录没有意义，应隐藏（叶子菜单已按权限过滤，目录可见性派生自子菜单）。
        var directoryIds = visibleMenus
            .Where(menu => menu.MenuType == MenuType.Directory)
            .Select(menu => menu.BasicId.ToString())
            .ToHashSet();
        roots = PruneEmptyDirectories(roots, directoryIds);

        return roots.Count == 0 ? [BuildFallbackDashboardRoute()] : roots;
    }
}
