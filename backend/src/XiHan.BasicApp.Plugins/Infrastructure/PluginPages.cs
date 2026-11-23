// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using XiHan.BasicApp.Contracts.Pages;

namespace XiHan.BasicApp.Plugins.Infrastructure;

/// <summary>
/// 已安装插件的页面（单例）
/// </summary>
/// <remarks>
/// 对齐服务每次读库后换上新的一份：启用与否都在，菜单按它对齐，卸载之后才不再列出；页面挂在「插件应用」目录（<see cref="MenuDirectories.PluginApps"/>）
/// 插件没在本节点加载时它的页面不下发（<see cref="IsHidden"/>），菜单节点与运营的展示调整留着
/// </remarks>
/// <param name="catalog">已加载插件的运行期贡献</param>
public sealed class PluginPages(PluginRuntimeCatalog catalog) : IRuntimePageSource
{
    private IReadOnlyList<PageCatalogSection> _installed = [];
    private string _signature = string.Empty;

    /// <inheritdoc />
    public IReadOnlyList<PageCatalogSection> Sections => Volatile.Read(ref _installed);

    /// <inheritdoc />
    public bool IsHidden(string package)
    {
        ArgumentNullException.ThrowIfNull(package);
        return Sections.Any(s => string.Equals(s.Package, package, StringComparison.Ordinal))
            && !catalog.Loaded.Any(p => string.Equals(p.Name, package, StringComparison.Ordinal));
    }

    /// <summary>
    /// 换上新的一份
    /// </summary>
    /// <param name="installed">已安装插件的页面片段</param>
    /// <returns>与原来的是否不同</returns>
    public bool Replace(IReadOnlyList<PageCatalogSection> installed)
    {
        ArgumentNullException.ThrowIfNull(installed);
        var signature = SignatureOf(installed);
        if (string.Equals(signature, _signature, StringComparison.Ordinal))
        {
            return false;
        }

        Volatile.Write(ref _installed, installed);
        _signature = signature;
        return true;
    }

    /// <summary>
    /// 旁挂清单里的页面
    /// </summary>
    /// <param name="descriptorJson">旁挂清单原文</param>
    public static IReadOnlyList<PageDescriptor> PagesOf(string descriptorJson)
    {
        var descriptor = JsonSerializer.Deserialize(descriptorJson, PluginsJsonContext.Default.PluginDescriptor)
            ?? throw new InvalidOperationException("The plugin descriptor is empty.");
        return [.. descriptor.Pages.Select(static p => new PageDescriptor(p.Code, p.Route, p.TitleKey, p.Icon, p.Order, p.Permission, directory: MenuDirectories.PluginApps))];
    }

    /// <summary>
    /// 比较用的指纹：菜单对齐关心的字段
    /// </summary>
    private static string SignatureOf(IReadOnlyList<PageCatalogSection> sections)
        => string.Join('\n', sections.SelectMany(static s => s.Pages.Select(p => $"{s.Package}|{p.Code}|{p.Route}|{p.TitleKey}|{p.Icon}|{p.Order}|{p.Permission}")));
}
