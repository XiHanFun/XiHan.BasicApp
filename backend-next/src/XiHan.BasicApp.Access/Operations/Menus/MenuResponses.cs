// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;

namespace XiHan.BasicApp.Access.Operations.Menus;

/// <summary>
/// 菜单节点的管理视图（<c>menus.*</c>）。
/// </summary>
/// <remarks>
/// 含隐藏节点与定制标记；<c>menus.list</c> 里带子节点，其余操作的 <see cref="Children"/> 为空。
/// </remarks>
/// <param name="Id">主键。</param>
/// <param name="Code">编码（页面码或 <c>custom.&lt;名&gt;</c>）。</param>
/// <param name="Kind">种类。</param>
/// <param name="Package">所属包；运营节点是 <c>custom</c>。</param>
/// <param name="ParentId">上级。</param>
/// <param name="Route">路由路径；目录与外链为空。</param>
/// <param name="Component">组件路径；目录与外链为空。</param>
/// <param name="TitleKey">标题文案键；运营节点为空。</param>
/// <param name="Title">标题原文（运营节点）或覆盖（页面节点）。</param>
/// <param name="Icon">图标。</param>
/// <param name="Order">排序号。</param>
/// <param name="Permission">所需权限码。</param>
/// <param name="ExternalUrl">外链地址。</param>
/// <param name="IsVisible">是否在导航里显示。</param>
/// <param name="KeepAlive">页面标签是否参与缓存。</param>
/// <param name="IsAffix">是否固定在标签栏。</param>
/// <param name="IsCustomized">页面节点的展示是否被改过（可恢复默认）。</param>
/// <param name="Children">子节点。</param>
public sealed record MenuResponse(
    Guid Id,
    string Code,
    MenuKind Kind,
    string Package,
    Guid? ParentId,
    string Route,
    string Component,
    string TitleKey,
    string? Title,
    string? Icon,
    int Order,
    string? Permission,
    string? ExternalUrl,
    bool IsVisible,
    bool KeepAlive,
    bool IsAffix,
    bool IsCustomized,
    IReadOnlyList<MenuResponse> Children)
{
    /// <summary>
    /// 由实体投影。
    /// </summary>
    public static MenuResponse From(SysMenu row, IReadOnlyList<MenuResponse>? children = null)
    {
        ArgumentNullException.ThrowIfNull(row);
        return new MenuResponse(row.Id, row.Code, row.Kind, row.Package, row.ParentId, row.Route, row.Component, row.TitleKey, row.Title, row.Icon, row.Order, row.Permission, row.ExternalUrl, row.IsVisible, row.KeepAlive, row.IsAffix, row.IsCustomized, children ?? []);
    }
}

/// <summary>
/// <c>menus.list</c> 的响应：整棵树（按排序号、编码排列）。
/// </summary>
/// <param name="Items">顶层节点。</param>
public sealed record MenusResponse(IReadOnlyList<MenuResponse> Items)
{
    /// <summary>
    /// 由全部菜单行建树。
    /// </summary>
    public static MenusResponse Tree(IReadOnlyList<SysMenu> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var byParent = rows.GroupBy(static r => r.ParentId ?? Guid.Empty).ToDictionary(static g => g.Key, static g => g.OrderBy(static r => r.Order).ThenBy(static r => r.Code, StringComparer.Ordinal).ToList());
        return new MenusResponse(Children(Guid.Empty));

        List<MenuResponse> Children(Guid parent)
            => byParent.TryGetValue(parent, out var items) ? items.Select(r => MenuResponse.From(r, Children(r.Id))).ToList() : [];
    }
}
