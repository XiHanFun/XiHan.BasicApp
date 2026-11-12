// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;

namespace XiHan.BasicApp.Contracts.Settings;

/// <summary>
/// 一个能力包登记的内建参数片段（包名 + 它的 <see cref="ConfigDescriptor"/>）
/// </summary>
/// <remarks>
/// 各包在 <c>Configure</c> 里 <see cref="ConfigCatalogServiceCollectionExtensions.AddConfigCatalog"/>，Settings 据此写参数种子
/// </remarks>
/// <param name="Package">包名（参数键的首段，小写）</param>
/// <param name="Configs">该包的内建参数</param>
public sealed record ConfigCatalogSection(string Package, IReadOnlyList<ConfigDescriptor> Configs);

/// <summary>
/// 内建参数片段的登记
/// </summary>
public static class ConfigCatalogServiceCollectionExtensions
{
    /// <summary>
    /// 登记一个包的内建参数
    /// </summary>
    /// <remarks>
    /// 参数键的首段必须是包名，键在片段内不重复
    /// </remarks>
    public static IServiceCollection AddConfigCatalog(this IServiceCollection services, string package, IReadOnlyList<ConfigDescriptor> configs)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(package);
        ArgumentNullException.ThrowIfNull(configs);
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var config in configs)
        {
            if (!config.Key.StartsWith(package + ".", StringComparison.Ordinal))
            {
                throw new ArgumentException($"Config '{config.Key}' does not belong to package '{package}'.", nameof(configs));
            }

            if (!keys.Add(config.Key))
            {
                throw new ArgumentException($"Config '{config.Key}' is registered twice.", nameof(configs));
            }
        }

        services.AddSingleton(new ConfigCatalogSection(package, configs));
        return services;
    }
}
