#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasMenuSeeder
// Guid:8d9bf0f9-1040-46fe-8bb7-e5c889f11f2b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/03 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Application.Pages;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Infrastructure.Seeders.System;

/// <summary>
/// SaaS 菜单种子数据
/// </summary>
public sealed class SaasMenuSeeder(
    ISqlSugarClientResolver clientResolver,
    ILogger<SaasMenuSeeder> logger,
    IServiceProvider serviceProvider,
    ICurrentTenant currentTenant)
    : DataSeederBase(clientResolver, logger, serviceProvider)
{
    private const string SeededMenuRemark = "系统初始化全局菜单";
    private readonly ICurrentTenant _currentTenant = currentTenant;

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => 25;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[SaaS]系统菜单种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        using var platformScope = _currentTenant.Change(null);
        var client = DbClient;
        var definitions = BuildDefinitions();
        var permissionCodes = definitions
            .Select(definition => definition.PermissionCode)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var permissions = await client.Queryable<SysPermission>()
            .Where(permission =>
                permission.TenantId == 0
                && permissionCodes.Contains(permission.PermissionCode)
                && permission.Status == EnableStatus.Enabled)
            .ToListAsync();
        var permissionMap = permissions
            .GroupBy(permission => permission.PermissionCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.First().BasicId,
                StringComparer.OrdinalIgnoreCase);

        var menuCodes = definitions.Select(definition => definition.MenuCode).ToArray();
        var existingMenus = await client.Queryable<SysMenu>()
            .Where(menu => menu.TenantId == 0 && menuCodes.Contains(menu.MenuCode))
            .ToListAsync();
        var menuMap = existingMenus.ToDictionary(menu => menu.MenuCode, StringComparer.OrdinalIgnoreCase);

        var addCount = 0;
        var updateCount = 0;
        foreach (var definition in definitions)
        {
            if (!TryResolvePermissionId(definition, permissionMap, out var permissionId))
            {
                Logger.LogWarning("菜单 {MenuCode} 依赖权限 {PermissionCode} 不存在，跳过初始化", definition.MenuCode, definition.PermissionCode);
                continue;
            }

            if (!TryResolveParentId(definition, menuMap, out var parentId))
            {
                Logger.LogWarning("菜单 {MenuCode} 依赖父菜单 {ParentCode} 不存在，跳过初始化", definition.MenuCode, definition.ParentCode);
                continue;
            }

            if (menuMap.TryGetValue(definition.MenuCode, out var existing))
            {
                if (ApplyDefinition(existing, definition, parentId, permissionId))
                {
                    _ = await client.Updateable(existing).ExecuteCommandAsync();
                    updateCount++;
                }

                continue;
            }

            var menu = CreateMenu(definition, parentId, permissionId);
            var savedMenu = await client.Insertable(menu).ExecuteReturnEntityAsync();
            menuMap[definition.MenuCode] = savedMenu;
            addCount++;
        }

        if (addCount == 0 && updateCount == 0)
        {
            Logger.LogInformation("SaaS 菜单数据已存在，跳过种子数据");
            return;
        }

        Logger.LogInformation("成功初始化 SaaS 菜单，新增 {AddCount} 个，更新 {UpdateCount} 个", addCount, updateCount);
    }

    private static bool TryResolvePermissionId(
        MenuSeedDefinition definition,
        IReadOnlyDictionary<string, long> permissionMap,
        out long? permissionId)
    {
        permissionId = null;
        if (string.IsNullOrWhiteSpace(definition.PermissionCode))
        {
            return true;
        }

        if (!permissionMap.TryGetValue(definition.PermissionCode, out var resolvedPermissionId))
        {
            return false;
        }

        permissionId = resolvedPermissionId;
        return true;
    }

    private static bool TryResolveParentId(
        MenuSeedDefinition definition,
        IReadOnlyDictionary<string, SysMenu> menuMap,
        out long? parentId)
    {
        parentId = null;
        if (string.IsNullOrWhiteSpace(definition.ParentCode))
        {
            return true;
        }

        if (!menuMap.TryGetValue(definition.ParentCode, out var parent))
        {
            return false;
        }

        parentId = parent.BasicId;
        return true;
    }

    private static SysMenu CreateMenu(MenuSeedDefinition definition, long? parentId, long? permissionId)
    {
        var menu = new SysMenu
        {
            // IsGlobal 为派生属性（= TenantId == 0）：平台菜单仅需置 TenantId = 0
            TenantId = 0
        };
        _ = ApplyDefinition(menu, definition, parentId, permissionId);
        return menu;
    }

    private static bool ApplyDefinition(SysMenu menu, MenuSeedDefinition definition, long? parentId, long? permissionId)
    {
        var changed = false;
        changed |= SetIfChanged(menu.TenantId, 0, value => menu.TenantId = value);
        changed |= SetIfChanged(menu.ParentId, parentId, value => menu.ParentId = value);
        changed |= SetIfChanged(menu.PermissionId, permissionId, value => menu.PermissionId = value);
        changed |= SetIfChanged(menu.MenuName, definition.MenuName, value => menu.MenuName = value);
        changed |= SetIfChanged(menu.MenuCode, definition.MenuCode, value => menu.MenuCode = value);
        changed |= SetIfChanged(menu.MenuType, definition.MenuType, value => menu.MenuType = value);
        changed |= SetIfChanged(menu.Path, definition.Path, value => menu.Path = value);
        changed |= SetIfChanged(menu.Component, definition.Component, value => menu.Component = value);
        changed |= SetIfChanged(menu.RouteName, definition.RouteName, value => menu.RouteName = value);
        changed |= SetIfChanged(menu.Redirect, definition.Redirect, value => menu.Redirect = value);
        changed |= SetIfChanged(menu.Icon, definition.Icon, value => menu.Icon = value);
        changed |= SetIfChanged(menu.Title, definition.Title, value => menu.Title = value);
        changed |= SetIfChanged(menu.IsExternal, false, value => menu.IsExternal = value);
        changed |= SetIfChanged(menu.ExternalUrl, null, value => menu.ExternalUrl = value);
        changed |= SetIfChanged(menu.IsCache, definition.IsCache, value => menu.IsCache = value);
        changed |= SetIfChanged(menu.IsVisible, true, value => menu.IsVisible = value);
        changed |= SetIfChanged(menu.IsAffix, definition.IsAffix, value => menu.IsAffix = value);
        changed |= SetIfChanged(menu.Badge, null, value => menu.Badge = value);
        changed |= SetIfChanged(menu.BadgeType, null, value => menu.BadgeType = value);
        changed |= SetIfChanged(menu.BadgeDot, false, value => menu.BadgeDot = value);
        changed |= SetIfChanged(menu.Metadata, null, value => menu.Metadata = value);
        changed |= SetIfChanged(menu.Status, EnableStatus.Enabled, value => menu.Status = value);
        changed |= SetIfChanged(menu.Sort, definition.Sort, value => menu.Sort = value);
        changed |= SetIfChanged(menu.Remark, SeededMenuRemark, value => menu.Remark = value);
        return changed;
    }

    private static bool SetIfChanged<T>(T current, T next, Action<T> setter)
    {
        if (EqualityComparer<T>.Default.Equals(current, next))
        {
            return false;
        }

        setter(next);
        return true;
    }

    /// <summary>
    /// 从 PageRegistry 映射出菜单种子定义列表（单一事实源）
    /// </summary>
    private static IReadOnlyList<MenuSeedDefinition> BuildDefinitions()
    {
        return PageRegistry.All
            .Select(page => new MenuSeedDefinition(
                page.Code,
                page.Title,
                page.MenuType,
                page.Path,
                page.RouteName,
                page.Component,
                page.ParentCode,
                page.PermissionCode,
                page.Icon,
                page.Title,
                page.Sort,
                page.Redirect,
                page.IsCache,
                page.IsAffix))
            .ToList();
    }

    private sealed record MenuSeedDefinition(
        string MenuCode,
        string MenuName,
        MenuType MenuType,
        string Path,
        string RouteName,
        string? Component,
        string? ParentCode,
        string? PermissionCode,
        string Icon,
        string Title,
        int Sort,
        string? Redirect = null,
        bool IsCache = false,
        bool IsAffix = false);
}
