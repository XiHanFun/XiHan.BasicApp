// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.RegularExpressions;
using XiHan.BasicApp.Contracts.Security;

namespace XiHan.BasicApp.Contracts.Pages;

/// <summary>
/// 页面登记。
/// </summary>
/// <remarks>
/// 方案 D4：每包一个 <c>&lt;包&gt;Pages</c>，列出页面码、路由、组件路径、图标、排序、所需权限码与子页面。菜单种子从各包登记派生，前端按组件路径精确映射。
/// 组件路径 = 路由路径去前导斜杠 + <c>/index</c>，一律 kebab-case。
/// </remarks>
public sealed partial record PageDescriptor
{
    /// <summary>
    /// 构造页面登记。
    /// </summary>
    /// <exception cref="ArgumentException">页面码不符合 <c>&lt;包&gt;.&lt;页面&gt;</c> 文法、路由不以 <c>/</c> 开头或组件路径不是 kebab-case 时抛出。</exception>
    public PageDescriptor(string code, string route, string titleKey, string? icon = null, int order = 0, string? permission = null, IReadOnlyList<PageDescriptor>? children = null)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(route);
        ArgumentException.ThrowIfNullOrWhiteSpace(titleKey);
        if (!CodePattern().IsMatch(code))
        {
            throw new ArgumentException($"Page code '{code}' must be '<package>.<page>' in lowercase.", nameof(code));
        }

        if (!route.StartsWith('/') || route.Length < 2 || !RoutePattern().IsMatch(route))
        {
            throw new ArgumentException($"Page route '{route}' must start with '/' and be kebab-case segments.", nameof(route));
        }

        if (permission is not null && !PermissionDescriptor.IsValidCode(permission))
        {
            throw new ArgumentException($"Page permission '{permission}' is not '<package>.<resource>.<action>'.", nameof(permission));
        }

        Code = code;
        Route = route;
        TitleKey = titleKey;
        Icon = icon;
        Order = order;
        Permission = permission;
        Children = children ?? [];
    }

    /// <summary>
    /// 页面码，如 <c>identity.users</c>。
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// 路由路径，如 <c>/identity/users</c>。
    /// </summary>
    public string Route { get; }

    /// <summary>
    /// 标题文案键（前端按语言取）。
    /// </summary>
    public string TitleKey { get; }

    /// <summary>
    /// 图标名。
    /// </summary>
    public string? Icon { get; }

    /// <summary>
    /// 同级排序。
    /// </summary>
    public int Order { get; }

    /// <summary>
    /// 进入页面所需的权限码。
    /// </summary>
    /// <remarks>
    /// 为 <see langword="null"/> 时只要求认证。
    /// </remarks>
    public string? Permission { get; }

    /// <summary>
    /// 子页面。
    /// </summary>
    public IReadOnlyList<PageDescriptor> Children { get; }

    /// <summary>
    /// 组件路径：路由去前导斜杠 + <c>/index</c>。
    /// </summary>
    public string Component => Route[1..] + "/index";

    [GeneratedRegex("^[a-z][a-z0-9-]*\\.[a-z][a-z0-9-]*$")]
    private static partial Regex CodePattern();

    [GeneratedRegex("^(/[a-z0-9]+(-[a-z0-9]+)*)+$")]
    private static partial Regex RoutePattern();
}
