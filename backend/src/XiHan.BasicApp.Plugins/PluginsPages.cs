// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Pages;

namespace XiHan.BasicApp.Plugins;

/// <summary>
/// 插件模块登记的页面
/// </summary>
/// <remarks>
/// 管理页在 <c>/plugins/installed</c>；插件自己的页面在 <c>/plugins/&lt;短名&gt;/&lt;页&gt;</c>，运行期随加载进「插件应用」目录（<see cref="Infrastructure.PluginPages"/>），<c>installed</c> 因此是保留的短名
/// </remarks>
public static class PluginsPages
{
    /// <summary>
    /// 插件管理
    /// </summary>
    public static PageDescriptor Installed { get; } = new("plugins.installed", "/plugins/installed", "plugins.installed.title", "lucide:puzzle", 690, PluginsPermissions.PluginsRead, directory: MenuDirectories.SystemServices);

    /// <summary>
    /// 本模块全部页面
    /// </summary>
    public static IReadOnlyList<PageDescriptor> All { get; } = [Installed];
}
