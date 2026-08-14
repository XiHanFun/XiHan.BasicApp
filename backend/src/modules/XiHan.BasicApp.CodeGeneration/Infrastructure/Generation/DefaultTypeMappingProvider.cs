// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
    /// <summary>
    /// 映射列类型
    /// </summary>
    /// <param name="databaseType">数据库类型（决定方言）</param>
    /// <param name="dbColumnType">数据库列类型（如 varchar、bigint、datetime）</param>
    /// <param name="isNullable">是否可空（影响 C# 可空标注）</param>
    /// <returns>映射结果</returns>
    public ColumnTypeMapping Map(DatabaseType databaseType, string? dbColumnType, bool isNullable)
    {
        var normalized = Normalize(dbColumnType);

        return normalized switch
        {
            "bigint" or "long" or "int8" or "bigserial" or "serial8" => ValueType("long", "number", HtmlType.InputNumber, QueryType.Equal, isNullable),
            "int" or "integer" or "int4" or "mediumint" or "serial" or "serial4" => ValueType("int", "number", HtmlType.InputNumber, QueryType.Equal, isNullable),
            "smallint" or "tinyint" or "int2" or "smallserial" or "serial2" => ValueType("int", "number", HtmlType.InputNumber, QueryType.Equal, isNullable),
            "bit" or "bool" or "boolean" => ValueType("bool", "boolean", HtmlType.Switch, QueryType.Equal, isNullable),
            "decimal" or "numeric" or "money" or "smallmoney" => ValueType("decimal", "number", HtmlType.InputNumber, QueryType.Between, isNullable),
            "float" or "double" or "real" or "float4" or "float8" or "double precision" => ValueType("double", "number", HtmlType.InputNumber, QueryType.Between, isNullable),
            // PostgreSQL 报出的时间类型是 timestamptz / timestamp without time zone，
            // 漏掉这些名字会落到末尾的字符串兜底分支，时间列被推断成 string + 文本框 + 模糊查询。
            "datetime" or "datetime2" or "smalldatetime" or "timestamp" or "timestamptz"
                or "timestamp with time zone" or "timestamp without time zone" or "datetimeoffset"
                => ValueType("DateTimeOffset", "string", HtmlType.DateTimePicker, QueryType.Between, isNullable),
            "date" => ValueType("DateTimeOffset", "string", HtmlType.DatePicker, QueryType.Between, isNullable),
            "time" or "timetz" or "time with time zone" or "time without time zone"
                => ValueType("TimeSpan", "string", HtmlType.TimePicker, QueryType.Equal, isNullable),
            "uniqueidentifier" or "uuid" or "guid" => ValueType("Guid", "string", HtmlType.Input, QueryType.Equal, isNullable),
            "text" or "longtext" or "mediumtext" or "ntext" or "clob" => RefType("string", "string", HtmlType.Textarea, QueryType.Like),
            "varbinary" or "binary" or "blob" or "image" or "bytea" => RefType("byte[]", "string", HtmlType.FileUpload, QueryType.Equal),
            "json" or "jsonb" => RefType("string", "string", HtmlType.Textarea, QueryType.Like),
            // varchar/nvarchar/char/bpchar/string 及未知类型默认按字符串处理
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
