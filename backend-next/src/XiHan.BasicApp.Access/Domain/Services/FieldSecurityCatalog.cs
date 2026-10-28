// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Security;

namespace XiHan.BasicApp.Access.Domain.Services;

/// <summary>
/// 字段安全的实体目录（单例）：汇总各包登记的 <see cref="FieldSecurityCatalogSection"/>，按实体码排序；两个包登记同一实体码即装配失败。
/// </summary>
public sealed class FieldSecurityCatalog
{
    private readonly Dictionary<string, FieldSecurityEntityDescriptor> _byCode;

    /// <summary>
    /// 由各包的登记构造。
    /// </summary>
    public FieldSecurityCatalog(IEnumerable<FieldSecurityCatalogSection> sections)
    {
        ArgumentNullException.ThrowIfNull(sections);
        _byCode = new Dictionary<string, FieldSecurityEntityDescriptor>(StringComparer.Ordinal);
        foreach (var entity in sections.SelectMany(static s => s.Entities))
        {
            if (!_byCode.TryAdd(entity.Code, entity))
            {
                throw new InvalidOperationException($"Field security entity '{entity.Code}' is registered twice.");
            }
        }

        Entities = _byCode.Values.OrderBy(static e => e.Code, StringComparer.Ordinal).ToList();
    }

    /// <summary>
    /// 全部实体（按实体码排序）。
    /// </summary>
    public IReadOnlyList<FieldSecurityEntityDescriptor> Entities { get; }

    /// <summary>
    /// 按实体码找实体；没登记即为空。
    /// </summary>
    public FieldSecurityEntityDescriptor? Find(string code) => _byCode.GetValueOrDefault(code);
}
