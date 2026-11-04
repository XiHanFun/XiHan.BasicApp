// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;

namespace XiHan.BasicApp.Access;

/// <summary>
/// 内置的目录与外链节点
/// </summary>
/// <param name="Code">节点编码（<c>custom.&lt;名&gt;</c>）</param>
/// <param name="Kind">种类：目录或外链</param>
/// <param name="TitleKey">标题文案键（运营没改标题时按语言取）</param>
/// <param name="Icon">图标</param>
/// <param name="Order">排序号</param>
/// <param name="ParentCode">所属目录的节点编码，顶层为空</param>
/// <param name="ExternalUrl">外链地址，目录为空</param>
public sealed record BuiltInMenu(string Code, MenuKind Kind, string TitleKey, string Icon, int Order, string? ParentCode = null, string? ExternalUrl = null);

/// <summary>
/// 内置的运营节点：帮助中心目录与三个外链
/// </summary>
/// <remarks>
/// 种类与运营建的一样（可改、可停用、可删），种子 <c>access.menus</c> 只首写：写过一次之后不再碰，删掉的也不补
/// </remarks>
public static class BuiltInMenus
{
    /// <summary>
    /// 帮助中心目录的节点编码
    /// </summary>
    public const string AboutDirectoryCode = "custom.about";

    /// <summary>
    /// 全部内置节点（目录排在它的外链之前）
    /// </summary>
    public static IReadOnlyList<BuiltInMenu> All { get; } =
    [
        new(AboutDirectoryCode, MenuKind.Directory, "menu.about", "lucide:info", 1100),
        new("custom.about-docs", MenuKind.Link, "menu.about_docs", "lucide:book-open-text", 1120, AboutDirectoryCode, "https://docs.xihanfun.com"),
        new("custom.about-github", MenuKind.Link, "menu.about_github", "lucide:github", 1130, AboutDirectoryCode, "https://github.com/XiHanFun/XiHan.BasicApp"),
        new("custom.about-gitee", MenuKind.Link, "menu.about_gitee", "lucide:git-branch", 1140, AboutDirectoryCode, "https://gitee.com/XiHanFun/XiHan.BasicApp"),
    ];
}
