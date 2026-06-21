#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DefaultTypeMappingProvider
// Guid:c0de9e00-0305-4a00-9000-000000000305
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/20 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.CodeGeneration.Domain.Enums;
using XiHan.BasicApp.CodeGeneration.Domain.Generation;

namespace XiHan.BasicApp.CodeGeneration.Infrastructure.Generation;

/// <summary>
/// 默认类型映射提供器：内置常见 DB 类型 → C#/TS + 默认表单/查询语义的映射表
/// </summary>
/// <remarks>
/// TODO(S2)：可演进为数据驱动（映射规则落配置表/字典），以支持自定义类型与方言差异。
/// 当前对 MySQL/SqlServer/PostgreSQL 的常见类型做通用归一化处理。
/// </remarks>
public sealed class DefaultTypeMappingProvider : ITypeMappingProvider
{
    /// <inheritdoc />
    public ColumnTypeMapping Map(DatabaseType databaseType, string? dbColumnType, bool isNullable)
    {
        var normalized = Normalize(dbColumnType);

        return normalized switch
        {
            "bigint" or "long" or "int8" => ValueType("long", "number", HtmlType.InputNumber, QueryType.Equal, isNullable),
            "int" or "integer" or "int4" or "mediumint" => ValueType("int", "number", HtmlType.InputNumber, QueryType.Equal, isNullable),
            "smallint" or "tinyint" or "int2" => ValueType("int", "number", HtmlType.InputNumber, QueryType.Equal, isNullable),
            "bit" or "bool" or "boolean" => ValueType("bool", "boolean", HtmlType.Switch, QueryType.Equal, isNullable),
            "decimal" or "numeric" or "money" or "smallmoney" => ValueType("decimal", "number", HtmlType.InputNumber, QueryType.Between, isNullable),
            "float" or "double" or "real" => ValueType("double", "number", HtmlType.InputNumber, QueryType.Between, isNullable),
            "datetime" or "datetime2" or "timestamp" or "datetimeoffset" => ValueType("DateTimeOffset", "string", HtmlType.DateTimePicker, QueryType.Between, isNullable),
            "date" => ValueType("DateTimeOffset", "string", HtmlType.DatePicker, QueryType.Between, isNullable),
            "time" => ValueType("TimeSpan", "string", HtmlType.TimePicker, QueryType.Equal, isNullable),
            "uniqueidentifier" or "uuid" or "guid" => ValueType("Guid", "string", HtmlType.Input, QueryType.Equal, isNullable),
            "text" or "longtext" or "mediumtext" or "ntext" or "clob" => RefType("string", "string", HtmlType.Textarea, QueryType.Like),
            "varbinary" or "binary" or "blob" or "image" or "bytea" => RefType("byte[]", "string", HtmlType.FileUpload, QueryType.Equal),
            "json" or "jsonb" => RefType("string", "string", HtmlType.Textarea, QueryType.Like),
            // varchar/nvarchar/char/string 及未知类型默认按字符串处理
            _ => RefType("string", "string", HtmlType.Input, QueryType.Like)
        };
    }

    /// <summary>值类型映射（可空时附加 ? 标注）</summary>
    private static ColumnTypeMapping ValueType(string csharp, string ts, HtmlType html, QueryType query, bool isNullable)
        => new(isNullable ? csharp + "?" : csharp, ts, html, query);

    /// <summary>引用类型映射（不附加可空标注，由项目可空上下文处理）</summary>
    private static ColumnTypeMapping RefType(string csharp, string ts, HtmlType html, QueryType query)
        => new(csharp, ts, html, query);

    private static string Normalize(string? dbColumnType)
    {
        if (string.IsNullOrWhiteSpace(dbColumnType))
        {
            return string.Empty;
        }

        var value = dbColumnType.Trim().ToLowerInvariant();

        // 去除长度/精度括号，如 varchar(200) → varchar、decimal(18,2) → decimal
        var parenIndex = value.IndexOf('(');
        if (parenIndex > 0)
        {
            value = value[..parenIndex];
        }

        // 去除无符号/数组等后缀修饰
        return value.Replace(" unsigned", string.Empty).Replace("[]", string.Empty).Trim();
    }
}
