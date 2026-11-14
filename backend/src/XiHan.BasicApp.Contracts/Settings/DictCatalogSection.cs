// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;

namespace XiHan.BasicApp.Contracts.Settings;

/// <summary>
/// 一个能力包登记的内建字典片段（包名 + 它的 <see cref="DictDescriptor"/>）
/// </summary>
/// <remarks>
/// 各包在 <c>Configure</c> 里 <see cref="DictCatalogServiceCollectionExtensions.AddDictCatalog"/>，Settings 据此写字典种子
/// </remarks>
/// <param name="Package">包名（字典编码的首段，小写）</param>
/// <param name="Dicts">该包的内建字典</param>
public sealed record DictCatalogSection(string Package, IReadOnlyList<DictDescriptor> Dicts);

/// <summary>
/// 内建字典片段的登记
/// </summary>
public static partial class DictCatalogServiceCollectionExtensions
{
    /// <summary>
    /// 登记一个包的内建字典
    /// </summary>
    /// <remarks>
    /// 字典编码的首段必须是包名、在片段内不重复；项编码在字典内不重复，上级要排在下级前面，缺省项至多一个；编码都要符合 <see cref="DictDescriptor.CodePattern"/>
    /// </remarks>
    public static IServiceCollection AddDictCatalog(this IServiceCollection services, string package, IReadOnlyList<DictDescriptor> dicts)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(package);
        ArgumentNullException.ThrowIfNull(dicts);
        var codes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var dict in dicts)
        {
            if (!dict.Code.StartsWith(package + ".", StringComparison.Ordinal) || !CodeRegex().IsMatch(dict.Code))
            {
                throw new ArgumentException($"Dict '{dict.Code}' does not belong to package '{package}' or is not a valid dict code.", nameof(dicts));
            }

            if (!codes.Add(dict.Code))
            {
                throw new ArgumentException($"Dict '{dict.Code}' is registered twice.", nameof(dicts));
            }

            var items = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in dict.Items)
            {
                if (!CodeRegex().IsMatch(item.Code) || !items.Add(item.Code))
                {
                    throw new ArgumentException($"Item '{item.Code}' of dict '{dict.Code}' is not a valid code or is registered twice.", nameof(dicts));
                }

                if (item.Parent is { } parent && (parent == item.Code || !items.Contains(parent)))
                {
                    throw new ArgumentException($"Item '{item.Code}' of dict '{dict.Code}' must come after its parent '{parent}'.", nameof(dicts));
                }
            }

            if (dict.Items.Count(static i => i.IsDefault) > 1)
            {
                throw new ArgumentException($"Dict '{dict.Code}' has more than one default item.", nameof(dicts));
            }
        }

        services.AddSingleton(new DictCatalogSection(package, dicts));
        return services;
    }

    [GeneratedRegex(DictDescriptor.CodePattern)]
    private static partial Regex CodeRegex();
}
