// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Pages;

namespace XiHan.BasicApp.Access;

/// <summary>
/// 授权包的页面登记：汇总进菜单种子与 <c>auth.permissions</c> 的菜单树。
/// </summary>
public static class AccessPages
{
    /// <summary>
    /// 角色页。
    /// </summary>
    public static PageDescriptor Roles { get; } = new("access.roles", "/access/roles", "access.roles.title", "lucide:shield", 200, AccessPermissions.RolesRead);

    /// <summary>
    /// 全部顶层页面。
    /// </summary>
    public static IReadOnlyList<PageDescriptor> All { get; } = [Roles];
}
