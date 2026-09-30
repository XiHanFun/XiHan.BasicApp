// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 可配置字段安全的实体目录：配置页从这里选实体、选字段，写入与生效都以它为准
/// </summary>
public interface IFieldSecurityEntityCatalog
{
    /// <summary>
    /// 全部已登记实体，按显示名排序
    /// </summary>
    IReadOnlyList<FieldSecurityEntityDescriptor> GetEntities();

    /// <summary>
    /// 按实体名查找；未登记返回空
    /// </summary>
    /// <param name="entityName">实体名（类名，区分大小写）</param>
    FieldSecurityEntityDescriptor? Find(string entityName);

    /// <summary>
    /// 取实体类型对应的实体名
    /// </summary>
    /// <param name="entityType">实体类型</param>
    /// <exception cref="InvalidOperationException">实体未登记：查询服务按它打码却没登记，配置页里配不到，属于接线遗漏。</exception>
    string GetEntityName(Type entityType);
}

/// <summary>
/// 可配置字段安全的实体
/// </summary>
/// <param name="EntityName">实体名（类名）</param>
/// <param name="DisplayName">显示名（取表说明）</param>
/// <param name="Fields">可配置字段</param>
public sealed record FieldSecurityEntityDescriptor(
    string EntityName,
    string DisplayName,
    IReadOnlyList<FieldSecurityFieldDescriptor> Fields)
{
    /// <summary>
    /// 按字段名查找（区分大小写，与打码时按属性名匹配一致）
    /// </summary>
    public FieldSecurityFieldDescriptor? FindField(string fieldName) =>
        Fields.FirstOrDefault(field => string.Equals(field.FieldName, fieldName, StringComparison.Ordinal));
}

/// <summary>
/// 可配置字段安全的字段
/// </summary>
/// <param name="FieldName">字段名（实体属性名）</param>
/// <param name="DisplayName">显示名（取列说明）</param>
/// <param name="IsText">是否文本字段：脱敏只对文本有效，非文本字段只能明文只读或隐藏</param>
public sealed record FieldSecurityFieldDescriptor(string FieldName, string DisplayName, bool IsText);
