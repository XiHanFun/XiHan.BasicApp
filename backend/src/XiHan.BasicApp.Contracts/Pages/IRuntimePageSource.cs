// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Contracts.Pages;

/// <summary>
/// 运行期增减的页面登记
/// </summary>
/// <remarks>
/// 启动期之外贡献页面的一方（插件模块）实现它，<see cref="RegisteredPages"/> 把它与各包启动期登记的片段合在一起
/// 片段是已安装的全部页面（启用与否都在）：菜单按它对齐，卸载之后不再列出，页面节点才删；当前不该下发的包由 <see cref="IsHidden"/> 回答
/// </remarks>
public interface IRuntimePageSource
{
    /// <summary>
    /// 已安装的页面片段
    /// </summary>
    IReadOnlyList<PageCatalogSection> Sections { get; }

    /// <summary>
    /// 这个包的页面当前是否不下发
    /// </summary>
    /// <remarks>
    /// 只对自己给出的包回答；别的包一律为否
    /// </remarks>
    /// <param name="package">包名（页面码的首段）</param>
    bool IsHidden(string package);
}
