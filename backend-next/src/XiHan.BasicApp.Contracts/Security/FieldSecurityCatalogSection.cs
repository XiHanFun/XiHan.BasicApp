// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;

namespace XiHan.BasicApp.Contracts.Security;

/// <summary>
/// 字段安全里一个可配置的字段。
/// </summary>
/// <remarks>
/// 名字与接口里的字段名（筛选、排序、响应的 JSON 字段）一致。
/// </remarks>
/// <param name="Name">字段名（camelCase，与响应字段同名）。</param>
/// <param name="TitleKey">显示名的界面文案键。</param>
/// <param name="IsText">是否文本；非文本只能明文只读或隐藏。</param>
/// <param name="IsNullable">响应里是否可空；不可空的字段不能隐藏（隐藏即返回空值，会破坏契约）。</param>
public sealed record FieldSecurityFieldDescriptor(string Name, string TitleKey, bool IsText, bool IsNullable);

/// <summary>
/// 字段安全里一个可配置的实体。
/// </summary>
/// <remarks>
/// 由所属包登记它在响应里真正给出的字段，并在投影、查询与写入处自己套用规则。
/// </remarks>
/// <param name="Code">实体码（<c>&lt;包&gt;.&lt;资源&gt;</c>，与权限码的前两段同词，如 <c>identity.users</c>）。</param>
/// <param name="TitleKey">显示名的界面文案键。</param>
/// <param name="Fields">可配置的字段。</param>
public sealed record FieldSecurityEntityDescriptor(string Code, string TitleKey, IReadOnlyList<FieldSecurityFieldDescriptor> Fields)
{
    /// <summary>
    /// 按名字找字段（区分大小写）。
    /// </summary>
    public FieldSecurityFieldDescriptor? FindField(string name) => Fields.FirstOrDefault(f => string.Equals(f.Name, name, StringComparison.Ordinal));
}

/// <summary>
/// 一个能力包登记的字段安全实体（包名 + 它的实体）。
/// </summary>
/// <remarks>
/// 各包在 <c>Configure</c> 里 <see cref="FieldSecurityCatalogServiceCollectionExtensions.AddFieldSecurityCatalog"/>，Access 汇总成字段安全的实体目录。
/// </remarks>
/// <param name="Package">包名（实体码的首段，小写）。</param>
/// <param name="Entities">该包的实体。</param>
public sealed record FieldSecurityCatalogSection(string Package, IReadOnlyList<FieldSecurityEntityDescriptor> Entities);

/// <summary>
/// 字段安全实体的登记。
/// </summary>
public static class FieldSecurityCatalogServiceCollectionExtensions
{
    /// <summary>
    /// 登记一个包的字段安全实体。
    /// </summary>
    /// <remarks>
    /// 实体码须以包名开头，同一实体里字段名不重复。
    /// </remarks>
    public static IServiceCollection AddFieldSecurityCatalog(this IServiceCollection services, string package, IReadOnlyList<FieldSecurityEntityDescriptor> entities)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(package);
        ArgumentNullException.ThrowIfNull(entities);
        foreach (var entity in entities)
        {
            if (!entity.Code.StartsWith(package + ".", StringComparison.Ordinal))
            {
                throw new ArgumentException($"Field security entity '{entity.Code}' does not belong to package '{package}'.", nameof(entities));
            }

            if (entity.Fields.GroupBy(static f => f.Name, StringComparer.Ordinal).FirstOrDefault(static g => g.Count() > 1) is { } duplicate)
            {
                throw new ArgumentException($"Field '{duplicate.Key}' is registered twice on '{entity.Code}'.", nameof(entities));
            }
        }

        services.AddSingleton(new FieldSecurityCatalogSection(package, entities));
        return services;
    }
}
