// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Contracts.Pages;

/// <summary>
/// 已登记的全部页面（单例）
/// </summary>
/// <remarks>
/// 各包启动期登记的 <see cref="PageCatalogSection"/> 与运行期来源（<see cref="IRuntimePageSource"/>，已安装的插件）当前的片段合在一起，每次取都是来源此刻的样子
/// 菜单的页面节点按它对齐（Access 的菜单种子与运行期对齐），下发菜单时跳过 <see cref="IsHidden"/> 的包
/// </remarks>
/// <param name="sections">各包启动期登记的页面片段</param>
/// <param name="sources">运行期来源</param>
public sealed class RegisteredPages(IEnumerable<PageCatalogSection> sections, IEnumerable<IRuntimePageSource> sources)
{
    private readonly PageCatalogSection[] _builtIn = [.. sections];
    private readonly IRuntimePageSource[] _sources = [.. sources];

    /// <summary>
    /// 当前的全部页面片段
    /// </summary>
    public IReadOnlyList<PageCatalogSection> Sections => [.. _builtIn, .. _sources.SelectMany(static s => s.Sections)];

    /// <summary>
    /// 这个包的页面当前是否不下发
    /// </summary>
    /// <param name="package">包名（页面码的首段）</param>
    public bool IsHidden(string package)
    {
        ArgumentNullException.ThrowIfNull(package);
        return _sources.Any(s => s.IsHidden(package));
    }
}
