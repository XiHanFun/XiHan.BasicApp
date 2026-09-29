// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Application.Pages;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Seeders;

/// <summary>
/// 菜单种子定义（页面/按钮登记项 → 菜单行的中间形态）
/// </summary>
public sealed record MenuSeedDefinition(
    string MenuCode,
    string MenuName,
    string? I18nKey,
    MenuType MenuType,
    string? Path,
    string? RouteName,
    string? Component,
    string? ParentCode,
    string? PermissionCode,
    string? Icon,
    string Title,
    int Sort,
    string? Redirect = null,
    bool IsCache = false,
    bool IsAffix = false,
    bool IsExternal = false,
    string? ExternalUrl = null);

/// <summary>
/// 页面登记表驱动的菜单种子基类
/// </summary>
/// <remarks>
/// 各模块登记自己的 <see cref="PageDescriptor"/>/<see cref="ButtonDescriptor"/>，由本类统一按 MenuCode 落库（平台菜单，TenantId = 0）：
/// 已有的菜单对齐结构（父级、绑定的权限、名称、路由、组件、图标等），缺的按声明插入，不删除任何行；
/// 排序、启停、显隐、徽标归运营（菜单管理页就是调这些的），只在插入时写。
/// 菜单绑定的权限与父菜单必须已经存在（权限目录与父模块的菜单先于本种子执行），缺了直接报错，不能跳过留下残缺的菜单树。
/// </remarks>
public abstract class PageRegistryMenuSeederBase : PlatformDataSeederBase
{
    /// <summary>
    /// 种子菜单备注（标识该行由种子维护）
    /// </summary>
    protected const string SeededMenuRemark = "系统初始化全局菜单";

    /// <summary>
    /// 构造函数
    /// </summary>
    protected PageRegistryMenuSeederBase(
        ISqlSugarClientResolver clientResolver,
        ILogger logger,
        IServiceProvider serviceProvider)
        : base(clientResolver, logger, serviceProvider)
    {
    }

    /// <summary>
    /// 本模块登记的页面（父目录须排在子项之前，按顺序解析 ParentId）
    /// </summary>
    protected abstract IReadOnlyList<PageDescriptor> Pages { get; }

    /// <summary>
    /// 本模块登记的页面内按钮
    /// </summary>
    protected virtual IReadOnlyList<ButtonDescriptor> Buttons => [];

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        var client = DbClient;
        var definitions = BuildDefinitions();
        var permissionCodes = definitions
            .Select(definition => definition.PermissionCode)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var permissions = await client.Queryable<SysPermission>()
            .Where(permission => permission.TenantId == 0 && permissionCodes.Contains(permission.PermissionCode))
            .ToListAsync();
        var permissionMap = permissions
            .GroupBy(permission => permission.PermissionCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.First().BasicId,
                StringComparer.OrdinalIgnoreCase);

        // 父菜单可能由别的模块登记（如开发工具目录），按 MenuCode 连同父码一起查回来
        var menuCodes = definitions
            .Select(definition => definition.MenuCode)
            .Concat(definitions.Select(definition => definition.ParentCode).Where(code => !string.IsNullOrWhiteSpace(code))!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
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
                throw new InvalidOperationException($"{Name}：菜单 {definition.MenuCode} 绑定的权限 {definition.PermissionCode} 不存在，权限目录须先于菜单执行。");
            }

            if (!TryResolveParentId(definition, menuMap, out var parentId))
            {
                throw new InvalidOperationException($"{Name}：菜单 {definition.MenuCode} 的父菜单 {definition.ParentCode} 不存在，父菜单须先于子菜单登记。");
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

        Logger.LogInformation("{Seeder}：新增 {AddCount} 个、对齐 {UpdateCount} 个", Name, addCount, updateCount);
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
            TenantId = 0,
            IsVisible = true,
            BadgeDot = false,
            Status = EnableStatus.Enabled,
            Sort = definition.Sort
        };
        _ = ApplyDefinition(menu, definition, parentId, permissionId);
        return menu;
    }

    private static bool ApplyDefinition(SysMenu menu, MenuSeedDefinition definition, long? parentId, long? permissionId)
    {
        var changed = false;
        changed |= SeedValues.SetIfChanged(menu.TenantId, 0, value => menu.TenantId = value);
        changed |= SeedValues.SetIfChanged(menu.ParentId, parentId, value => menu.ParentId = value);
        changed |= SeedValues.SetIfChanged(menu.PermissionId, permissionId, value => menu.PermissionId = value);
        changed |= SeedValues.SetIfChanged(menu.MenuName, definition.MenuName, value => menu.MenuName = value);
        changed |= SeedValues.SetIfChanged(menu.MenuCode, definition.MenuCode, value => menu.MenuCode = value);
        changed |= SeedValues.SetIfChanged(menu.MenuType, definition.MenuType, value => menu.MenuType = value);
        changed |= SeedValues.SetIfChanged(menu.Path, definition.Path, value => menu.Path = value);
        changed |= SeedValues.SetIfChanged(menu.Component, definition.Component, value => menu.Component = value);
        changed |= SeedValues.SetIfChanged(menu.RouteName, definition.RouteName, value => menu.RouteName = value);
        changed |= SeedValues.SetIfChanged(menu.Redirect, definition.Redirect, value => menu.Redirect = value);
        changed |= SeedValues.SetIfChanged(menu.Icon, definition.Icon, value => menu.Icon = value);
        changed |= SeedValues.SetIfChanged(menu.Title, definition.Title, value => menu.Title = value);
        changed |= SeedValues.SetIfChanged(menu.I18nKey, definition.I18nKey, value => menu.I18nKey = value);
        changed |= SeedValues.SetIfChanged(menu.IsExternal, definition.IsExternal, value => menu.IsExternal = value);
        changed |= SeedValues.SetIfChanged(menu.ExternalUrl, definition.ExternalUrl, value => menu.ExternalUrl = value);
        changed |= SeedValues.SetIfChanged(menu.IsCache, definition.IsCache, value => menu.IsCache = value);
        changed |= SeedValues.SetIfChanged(menu.IsAffix, definition.IsAffix, value => menu.IsAffix = value);
        changed |= SeedValues.SetIfChanged(menu.Remark, SeededMenuRemark, value => menu.Remark = value);
        return changed;
    }

    /// <summary>
    /// 从页面登记表映射出菜单种子定义列表
    /// </summary>
    /// <remarks>页面（目录/菜单）排在前，按钮排在后：按钮的父页面必先于其入库。</remarks>
    private IReadOnlyList<MenuSeedDefinition> BuildDefinitions()
    {
        var pages = Pages
            .Select(page => new MenuSeedDefinition(
                page.Code,
                page.Title,
                page.I18nKey,
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
                page.IsAffix,
                page.IsExternal,
                page.ExternalUrl));

        var buttons = Buttons
            .Select(button => new MenuSeedDefinition(
                button.Code,
                button.Title,
                I18nKey: null,
                MenuType.Button,
                Path: null,
                RouteName: null,
                Component: null,
                button.ParentCode,
                button.PermissionCode,
                Icon: null,
                button.Title,
                button.Sort));

        return [.. pages, .. buttons];
    }
}
