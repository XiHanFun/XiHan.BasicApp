// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Access.Domain.Entities;

namespace XiHan.BasicApp.Access.Operations.Menus;

/// <summary>
/// <c>menus.list</c> 的请求：没有输入，整棵树一次返回（含隐藏节点）。
/// </summary>
public sealed record ListMenusRequest;

/// <summary>
/// <c>menus.get</c> 的请求：标识由路由值 <c>{id}</c> 绑定。
/// </summary>
/// <param name="Id">菜单主键。</param>
public sealed record GetMenuRequest(Guid Id);

/// <summary>
/// <c>menus.create</c> 的请求：新建运营节点（目录或外链；页面节点由页面登记生成，不能新建）。
/// </summary>
/// <param name="Kind">种类：<c>Directory</c> 或 <c>Link</c>。</param>
/// <param name="Code">名称：小写字母开头，只含小写字母、数字与 <c>-</c>，2 到 32 个字符；存为 <c>custom.&lt;名&gt;</c>，创建后不可改。</param>
/// <param name="Title">标题原文：1 到 64 个字符。</param>
/// <param name="Icon">图标（如 <c>lucide:folder</c>），可空。</param>
/// <param name="Order">排序号。</param>
/// <param name="ParentId">上级：目录只能在顶层；外链可在顶层或某个目录下。</param>
/// <param name="ExternalUrl">外链地址（http / https 绝对地址）；外链必填，目录不填。</param>
/// <param name="Permission">所需权限码，可空；须在权限目录里。</param>
/// <param name="IsVisible">是否在导航里显示。</param>
public sealed record CreateMenuRequest(
    MenuKind Kind,
    [Required, StringLength(32, MinimumLength = 2), RegularExpression("^[a-z][a-z0-9-]{1,31}$")] string Code,
    [Required, StringLength(64, MinimumLength = 1)] string Title,
    [StringLength(64)] string? Icon = null,
    [Range(0, 100_000)] int Order = 0,
    Guid? ParentId = null,
    [StringLength(1024)] string? ExternalUrl = null,
    [StringLength(128)] string? Permission = null,
    bool IsVisible = true);

/// <summary>
/// <c>menus.update</c> 的请求：整体替换节点的可改字段。标识由路由值 <c>{id}</c> 绑定。
/// 页面节点只改展示（标题覆盖、图标、排序、顶层页面的所属目录、可见、缓存、固定标签），<paramref name="ExternalUrl"/> 与 <paramref name="Permission"/> 须为空（来自页面登记）；改过即标为已定制。
/// </summary>
/// <param name="Id">菜单主键。</param>
/// <param name="Title">标题：运营节点必填；页面节点为空即用登记的文案。</param>
/// <param name="Icon">图标，可空。</param>
/// <param name="Order">排序号。</param>
/// <param name="ParentId">上级：规则同新建；子页面只能留在登记的父页面下。</param>
/// <param name="IsVisible">是否在导航里显示（隐藏的页面仍可直接访问）。</param>
/// <param name="KeepAlive">页面标签是否参与缓存。</param>
/// <param name="IsAffix">是否固定在标签栏。</param>
/// <param name="ExternalUrl">外链地址：外链必填，其余为空。</param>
/// <param name="Permission">所需权限码：运营节点可选，页面节点为空。</param>
public sealed record UpdateMenuRequest(
    Guid Id,
    [StringLength(64)] string? Title = null,
    [StringLength(64)] string? Icon = null,
    [Range(0, 100_000)] int Order = 0,
    Guid? ParentId = null,
    bool IsVisible = true,
    bool KeepAlive = false,
    bool IsAffix = false,
    [StringLength(1024)] string? ExternalUrl = null,
    [StringLength(128)] string? Permission = null);

/// <summary>
/// <c>menus.reset</c> 的请求：标识由路由值 <c>{id}</c> 绑定。
/// </summary>
/// <param name="Id">菜单主键（页面节点）。</param>
public sealed record ResetMenuRequest(Guid Id);

/// <summary>
/// <c>menus.delete</c> 的请求：集合入参。
/// </summary>
/// <param name="Ids">菜单主键，1 到 100 个。</param>
public sealed record MenuIdsRequest([Required, MinLength(1), MaxLength(100)] IReadOnlyList<Guid> Ids);
