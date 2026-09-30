// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using Microsoft.Extensions.Options;
using SqlSugar;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 字段安全实体目录：由各模块登记的实体类型反射出表说明与列说明，首次使用时构建一次
/// </summary>
public sealed class FieldSecurityEntityCatalog : IFieldSecurityEntityCatalog
{
    /// <summary>
    /// 框架维护、不属于业务数据的列：租户、并发版本与软删除标记，配规则没有意义
    /// </summary>
    private static readonly HashSet<string> FrameworkColumns = new(StringComparer.Ordinal)
    {
        "TenantId", "RowVersion", "IsDeleted", "DeletedTime", "DeletedId", "DeletedBy",
    };

    private readonly Lazy<Catalog> _catalog;

    /// <summary>
    /// 构造函数
    /// </summary>
    public FieldSecurityEntityCatalog(IOptions<FieldSecurityEntityOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _catalog = new Lazy<Catalog>(() => Build(options.Value.Entities));
    }

    /// <inheritdoc />
    public IReadOnlyList<FieldSecurityEntityDescriptor> GetEntities() => _catalog.Value.Ordered;

    /// <inheritdoc />
    public FieldSecurityEntityDescriptor? Find(string entityName)
    {
        return string.IsNullOrWhiteSpace(entityName)
            ? null
            : _catalog.Value.ByName.GetValueOrDefault(entityName);
    }

    /// <inheritdoc />
    public string GetEntityName(Type entityType)
    {
        ArgumentNullException.ThrowIfNull(entityType);
        return _catalog.Value.ByType.TryGetValue(entityType, out var descriptor)
            ? descriptor.EntityName
            : throw new InvalidOperationException($"实体 {entityType.FullName} 未登记字段安全，请在所属模块的服务注册里登记。");
    }

    /// <summary>
    /// 反射构建目录
    /// </summary>
    private static FieldSecurityEntityDescriptor Describe(string entityName, Type entityType)
    {
        var table = entityType.GetCustomAttribute<SugarTable>(inherit: true);
        var fields = entityType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(IsConfigurableField)
            .Select(property => new FieldSecurityFieldDescriptor(
                property.Name,
                ColumnOf(property)?.ColumnDescription is { Length: > 0 } description ? description : property.Name,
                property.PropertyType == typeof(string)))
            .ToArray();

        return new FieldSecurityEntityDescriptor(
            entityName,
            table?.TableDescription is { Length: > 0 } tableDescription ? tableDescription : entityName,
            fields);
    }

    private static Catalog Build(IReadOnlyDictionary<string, Type> entities)
    {
        var descriptors = entities
            .Select(entry => (Type: entry.Value, Descriptor: Describe(entry.Key, entry.Value)))
            .ToArray();

        return new Catalog(
            [.. descriptors.Select(item => item.Descriptor).OrderBy(descriptor => descriptor.DisplayName, StringComparer.Ordinal)],
            descriptors.ToDictionary(item => item.Descriptor.EntityName, item => item.Descriptor, StringComparer.Ordinal),
            descriptors.ToDictionary(item => item.Type, item => item.Descriptor));
    }

    /// <summary>
    /// 可配置字段：落库的业务列（非主键、非框架列、非导航），且是标量类型
    /// </summary>
    private static bool IsConfigurableField(PropertyInfo property)
    {
        if (!property.CanRead || property.GetIndexParameters().Length > 0 || FrameworkColumns.Contains(property.Name))
        {
            return false;
        }

        if (property.GetCustomAttribute<Navigate>(inherit: true) is not null)
        {
            return false;
        }

        var column = ColumnOf(property);
        if (column is { IsIgnore: true } or { IsPrimaryKey: true })
        {
            return false;
        }

        var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        return type.IsPrimitive
            || type.IsEnum
            || type == typeof(string)
            || type == typeof(decimal)
            || type == typeof(DateTime)
            || type == typeof(DateTimeOffset)
            || type == typeof(DateOnly)
            || type == typeof(TimeOnly)
            || type == typeof(TimeSpan)
            || type == typeof(Guid);
    }

    private static SugarColumn? ColumnOf(PropertyInfo property) =>
        (SugarColumn?)Attribute.GetCustomAttribute(property, typeof(SugarColumn), inherit: true);

    private sealed record Catalog(
        IReadOnlyList<FieldSecurityEntityDescriptor> Ordered,
        IReadOnlyDictionary<string, FieldSecurityEntityDescriptor> ByName,
        IReadOnlyDictionary<Type, FieldSecurityEntityDescriptor> ByType);
}
