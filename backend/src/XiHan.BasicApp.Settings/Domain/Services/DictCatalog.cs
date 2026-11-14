// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Frozen;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Settings;
using XiHan.BasicApp.Settings.Domain.Entities;

namespace XiHan.BasicApp.Settings.Domain.Services;

/// <summary>
/// 各包登记的内建字典
/// </summary>
/// <remarks>
/// 单例。种子据此写平台的内建字典；删字典项时据此认出登记的项：它们随代码走，删了下次播种又会建回来，所以不让删
/// 两个片段登记了同一个编码即启动失败
/// </remarks>
public sealed class DictCatalog
{
    private readonly FrozenDictionary<string, DictDescriptor> _dicts;

    /// <summary>
    /// 汇总各包的片段
    /// </summary>
    /// <param name="sections">各包登记的内建字典</param>
    public DictCatalog(IEnumerable<DictCatalogSection> sections)
    {
        ArgumentNullException.ThrowIfNull(sections);
        _dicts = sections.SelectMany(static s => s.Dicts).ToFrozenDictionary(static d => d.Code, StringComparer.Ordinal);
    }

    /// <summary>
    /// 全部内建字典
    /// </summary>
    public IEnumerable<DictDescriptor> All => _dicts.Values;

    /// <summary>
    /// 这一项是不是内建字典里登记的
    /// </summary>
    /// <param name="dictCode">所在字典的编码</param>
    /// <param name="item">字典项</param>
    /// <returns>平台的、登记过的项为真</returns>
    public bool IsBuiltInItem(string dictCode, SysDictItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return string.Equals(item.OwnerTenantId, TenantIds.Platform, StringComparison.Ordinal)
            && _dicts.TryGetValue(dictCode, out var dict)
            && dict.Items.Any(i => string.Equals(i.Code, item.Code, StringComparison.Ordinal));
    }
}
