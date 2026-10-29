// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Operations.Auth;

namespace XiHan.BasicApp.Access.Domain.Services;

/// <summary>
/// 菜单行 → 树，并按有效权限裁剪。
/// </summary>
/// <remarks>
/// 节点要求的权限不在集合里即连同子树去掉；没有要求权限的节点登录即可见；裁剪后没有子节点的目录不下发。
/// 隐藏的节点（<see cref="SysMenu.IsVisible"/> 为假）照常下发并标 <see cref="MenuNode.Hidden"/>：页面仍可直接访问，导航里不显示。
/// </remarks>
public static class MenuTree
{
    /// <summary>
    /// 从菜单行建树（按 <see cref="SysMenu.Order"/>、<see cref="SysMenu.Code"/> 排序）。
    /// </summary>
    public static IReadOnlyList<MenuNode> Build(IReadOnlyList<SysMenu> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        // 顶层以 Guid.Empty 为键（字典键不能可空）
        var byParent = rows.GroupBy(static r => r.ParentId ?? Guid.Empty).ToDictionary(static g => g.Key, static g => g.OrderBy(static r => r.Order).ThenBy(static r => r.Code, StringComparer.Ordinal).ToList());
        return Children(Guid.Empty);

        List<MenuNode> Children(Guid parent)
            => byParent.TryGetValue(parent, out var items)
                ? items.Select(r => new MenuNode(r.Code, r.Kind, r.Route, r.Component, r.TitleKey, r.Title, r.Icon, r.Order, r.Permission, r.ExternalUrl, !r.IsVisible, r.KeepAlive, r.IsAffix, Children(r.Id))).ToList()
                : [];
    }

    /// <summary>
    /// 按有效权限裁剪。
    /// </summary>
    /// <remarks>
    /// 裁剪后没有子节点的目录一并去掉。
    /// </remarks>
    public static IReadOnlyList<MenuNode> Prune(IReadOnlyList<MenuNode> nodes, IReadOnlySet<string> permissions)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(permissions);
        var result = new List<MenuNode>(nodes.Count);
        foreach (var node in nodes)
        {
            if (node.Permission is not null && !permissions.Contains(node.Permission))
            {
                continue;
            }

            var children = Prune(node.Children, permissions);
            if (node.Kind == MenuKind.Directory && children.Count == 0)
            {
                continue;
            }

            result.Add(node with { Children = children });
        }

        return result;
    }

    /// <summary>
    /// 树里全部页面码（深度优先，只含页面节点）。
    /// </summary>
    public static IReadOnlyList<string> Codes(IReadOnlyList<MenuNode> nodes)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        var codes = new List<string>();
        Walk(nodes);
        return codes;

        void Walk(IReadOnlyList<MenuNode> items)
        {
            foreach (var item in items)
            {
                if (item.Kind == MenuKind.Page)
                {
                    codes.Add(item.Code);
                }

                Walk(item.Children);
            }
        }
    }
}
