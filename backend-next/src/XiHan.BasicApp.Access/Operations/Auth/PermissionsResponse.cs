// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Access.Operations.Auth;

/// <summary>
/// <c>auth.permissions</c> 的响应：当前主体的有效权限码、角色、按权限裁剪过的菜单树与页面码；前端据此门控按钮、装载动态路由、渲染导航。
/// </summary>
/// <param name="Permissions">有效权限码（<c>*</c> 已展开为本侧目录的全部码），按码排序。</param>
/// <param name="Roles">令牌里的角色（本侧生效的）。</param>
/// <param name="Menus">菜单树。</param>
/// <param name="Pages">可访问的页面码。</param>
/// <param name="IsPlatform">是否平台侧。</param>
public sealed record PermissionsResponse(
    IReadOnlyList<string> Permissions,
    IReadOnlyList<RoleSummary> Roles,
    IReadOnlyList<MenuNode> Menus,
    IReadOnlyList<string> Pages,
    bool IsPlatform);

/// <summary>
/// 角色摘要。
/// </summary>
/// <param name="Id">角色 ID。</param>
/// <param name="Code">角色码。</param>
/// <param name="Name">显示名。</param>
public sealed record RoleSummary(Guid Id, string Code, string Name);

/// <summary>
/// 菜单节点。
/// </summary>
/// <param name="Code">页面码。</param>
/// <param name="Route">路由路径。</param>
/// <param name="Component">组件路径（<c>&lt;包&gt;/&lt;路径&gt;/index</c>）。</param>
/// <param name="TitleKey">标题文案键。</param>
/// <param name="Icon">图标。</param>
/// <param name="Order">排序号。</param>
/// <param name="Permission">所需权限码。</param>
/// <param name="Children">子节点。</param>
public sealed record MenuNode(string Code, string Route, string Component, string TitleKey, string? Icon, int Order, string? Permission, IReadOnlyList<MenuNode> Children);
