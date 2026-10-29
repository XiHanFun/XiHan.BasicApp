// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;

namespace XiHan.BasicApp.Contracts.Pages;

/// <summary>
/// 一个能力包登记的页面片段（包名 + 它的顶层 <see cref="PageDescriptor"/>）。
/// </summary>
/// <remarks>
/// 各包在 <c>Configure</c> 里 <see cref="PageCatalogServiceCollectionExtensions.AddPageCatalog"/>，Access 据此生成菜单种子。
/// </remarks>
/// <param name="Package">包名（页面码的首段，小写）。</param>
/// <param name="Pages">该包的顶层页面（子页面在 <see cref="PageDescriptor.Children"/>）。</param>
public sealed record PageCatalogSection(string Package, IReadOnlyList<PageDescriptor> Pages);

/// <summary>
/// 页面片段的登记。
/// </summary>
public static class PageCatalogServiceCollectionExtensions
{
    /// <summary>
    /// 登记一个包的页面。
    /// </summary>
    /// <remarks>
    /// 页面码的首段必须是包名。
    /// </remarks>
    public static IServiceCollection AddPageCatalog(this IServiceCollection services, string package, IReadOnlyList<PageDescriptor> pages)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(package);
        ArgumentNullException.ThrowIfNull(pages);
        foreach (var page in pages)
        {
            if (!page.Code.StartsWith(package + ".", StringComparison.Ordinal))
            {
                throw new ArgumentException($"Page '{page.Code}' does not belong to package '{package}'.", nameof(pages));
            }
        }

        services.AddSingleton(new PageCatalogSection(package, pages));
        return services;
    }
}
