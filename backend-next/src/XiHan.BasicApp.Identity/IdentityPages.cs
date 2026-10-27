// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Pages;

namespace XiHan.BasicApp.Identity;

/// <summary>
/// 身份包的页面登记（方案 D4）：菜单种子与前端路由从这里派生；组件路径 = 路由去前导斜杠 + <c>/index</c>。
/// </summary>
public static class IdentityPages
{
    /// <summary>
    /// 用户管理页。
    /// </summary>
    public static PageDescriptor Users { get; } = new("identity.users", "/identity/users", "identity.users.title", "lucide:users", 100, IdentityPermissions.UsersRead);

    /// <summary>
    /// 本包全部页面。
    /// </summary>
    public static IReadOnlyList<PageDescriptor> All { get; } = [Users];
}
