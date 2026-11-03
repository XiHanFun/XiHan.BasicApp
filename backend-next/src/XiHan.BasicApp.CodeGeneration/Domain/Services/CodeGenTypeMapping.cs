// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.Framework.Persistence.Abstractions.Schema;

namespace XiHan.BasicApp.CodeGeneration.Domain.Services;

/// <summary>
/// 库里的列类型推出的生成类型。
/// </summary>
/// <param name="CSharpType">C# 类型（可空时带 <c>?</c>）。</param>
/// <param name="TsType">TypeScript 类型。</param>
/// <param name="HtmlType">缺省的表单控件。</param>
/// <param name="QueryType">缺省的查询方式。</param>
public sealed record CodeGenColumnType(string CSharpType, string TsType, CodeGenHtmlType HtmlType, CodeGenQueryType QueryType);

/// <summary>
/// 列类型到 C# 与 TypeScript 类型的映射。
/// </summary>
/// <remarks>
/// 按各库的数据库类型名映射（框架读库结构的口径，修订（八十三））：Oracle 的 <c>NUMBER</c> 按精度与小数位分整数与定点数，
/// 有小数位是 <c>decimal</c>，否则精度不超过 9 是 <c>int</c>、不超过 18 是 <c>long</c>，没写精度或更大是 <c>decimal</c>；Oracle 的 <c>DATE</c> 带时分秒，按日期时间；
/// SQL Server 的 <c>timestamp</c> 是行版本，按二进制。
/// 其余照 4.x 的映射，不认得的类型按字符串；只有 <c>long</c> 不同：6.0 的报文里 long 是数字（4.x 由全局转换器写成字符串），TypeScript 是 number、控件是数字框。
/// </remarks>
public static class CodeGenTypeMapping
{
    private static readonly HashSet<string> _booleans = new(StringComparer.Ordinal) { "bit", "bool", "boolean" };
    private static readonly HashSet<string> _integers = new(StringComparer.Ordinal) { "int", "integer", "int4", "mediumint", "serial", "serial4", "smallint", "tinyint", "int2", "smallserial", "serial2" };
    private static readonly HashSet<string> _longs = new(StringComparer.Ordinal) { "bigint", "long", "int8", "bigserial", "serial8" };
    private static readonly HashSet<string> _decimals = new(StringComparer.Ordinal) { "decimal", "dec", "numeric", "number", "money", "smallmoney" };
    private static readonly HashSet<string> _doubles = new(StringComparer.Ordinal) { "float", "double", "real", "float4", "float8", "double precision", "binary_float", "binary_double" };
    private static readonly HashSet<string> _dateTimes = new(StringComparer.Ordinal) { "datetime", "datetime2", "smalldatetime", "timestamp", "timestamptz", "timestamp with time zone", "timestamp without time zone", "timestamp with local time zone", "datetimeoffset" };
    private static readonly HashSet<string> _times = new(StringComparer.Ordinal) { "time", "timetz", "time with time zone", "time without time zone" };
    private static readonly HashSet<string> _guids = new(StringComparer.Ordinal) { "uniqueidentifier", "uuid", "guid" };
    private static readonly HashSet<string> _texts = new(StringComparer.Ordinal) { "text", "tinytext", "mediumtext", "longtext", "ntext", "clob", "nclob", "long" };
    private static readonly HashSet<string> _binaries = new(StringComparer.Ordinal) { "binary", "varbinary", "blob", "tinyblob", "mediumblob", "longblob", "image", "bytea", "raw", "long raw" };
    private static readonly HashSet<string> _jsons = new(StringComparer.Ordinal) { "json", "jsonb" };

    /// <summary>
    /// 推出一列的生成类型。
    /// </summary>
    /// <param name="database">数据库类型。</param>
    /// <param name="column">读库结构得到的列。</param>
    public static CodeGenColumnType Map(CodeGenDatabaseType database, ColumnInfo column)
    {
        ArgumentNullException.ThrowIfNull(column);
        var type = Normalize(column.DataType);
        var nullable = column.IsNullable;
        if (database == CodeGenDatabaseType.Oracle && type == "number")
        {
            return OracleNumber(column.Precision, column.Scale, nullable);
        }

        if (database == CodeGenDatabaseType.Oracle && type == "date")
        {
            return Of("DateTimeOffset", "string", CodeGenHtmlType.DateTimePicker, CodeGenQueryType.Between, nullable);
        }

        if (database == CodeGenDatabaseType.SqlServer && type is "timestamp" or "rowversion")
        {
            // SQL Server 的 timestamp 是行版本（8 字节二进制），不是时间
            return Of("byte[]", "string", CodeGenHtmlType.FileUpload, CodeGenQueryType.Equal, nullable);
        }

        if (_longs.Contains(type))
        {
            return Of("long", "number", CodeGenHtmlType.InputNumber, CodeGenQueryType.Equal, nullable);
        }

        if (_integers.Contains(type))
        {
            return Of("int", "number", CodeGenHtmlType.InputNumber, CodeGenQueryType.Equal, nullable);
        }

        if (_booleans.Contains(type))
        {
            return Of("bool", "boolean", CodeGenHtmlType.Switch, CodeGenQueryType.Equal, nullable);
        }

        if (_decimals.Contains(type))
        {
            return Of("decimal", "number", CodeGenHtmlType.InputNumber, CodeGenQueryType.Between, nullable);
        }

        if (_doubles.Contains(type))
        {
            return Of("double", "number", CodeGenHtmlType.InputNumber, CodeGenQueryType.Between, nullable);
        }

        if (_dateTimes.Contains(type))
        {
            return Of("DateTimeOffset", "string", CodeGenHtmlType.DateTimePicker, CodeGenQueryType.Between, nullable);
        }

        if (type == "date")
        {
            return Of("DateTimeOffset", "string", CodeGenHtmlType.DatePicker, CodeGenQueryType.Between, nullable);
        }

        if (_times.Contains(type))
        {
            return Of("TimeSpan", "string", CodeGenHtmlType.TimePicker, CodeGenQueryType.Equal, nullable);
        }

        if (_guids.Contains(type))
        {
            return Of("Guid", "string", CodeGenHtmlType.Input, CodeGenQueryType.Equal, nullable);
        }

        if (_texts.Contains(type) || _jsons.Contains(type))
        {
            return Of("string", "string", CodeGenHtmlType.Textarea, CodeGenQueryType.Like, nullable);
        }

        return _binaries.Contains(type)
            ? Of("byte[]", "string", CodeGenHtmlType.FileUpload, CodeGenQueryType.Equal, nullable)
            : Of("string", "string", CodeGenHtmlType.Input, CodeGenQueryType.Like, nullable);
    }

    /// <summary>
    /// 去掉长度与精度括号、无符号与数组修饰，转小写。
    /// </summary>
    /// <param name="dataType">库里的类型名。</param>
    public static string Normalize(string? dataType)
    {
        if (string.IsNullOrWhiteSpace(dataType))
        {
            return string.Empty;
        }

        var value = dataType.Trim().ToLowerInvariant();
        var paren = value.IndexOf('(', StringComparison.Ordinal);
        if (paren > 0)
        {
            var close = value.IndexOf(')', paren);
            // timestamp(6) with time zone 这类括号在中间的，去掉括号后接上后半段
            value = close > paren ? (value[..paren] + value[(close + 1)..]).Trim() : value[..paren];
        }

        return value.Replace(" unsigned", string.Empty, StringComparison.Ordinal).Replace("[]", string.Empty, StringComparison.Ordinal).Trim();
    }

    private static CodeGenColumnType OracleNumber(int? precision, int? scale, bool nullable)
    {
        if (scale is > 0 || precision is null or > 18)
        {
            return Of("decimal", "number", CodeGenHtmlType.InputNumber, CodeGenQueryType.Between, nullable);
        }

        return precision <= 9
            ? Of("int", "number", CodeGenHtmlType.InputNumber, CodeGenQueryType.Equal, nullable)
            : Of("long", "number", CodeGenHtmlType.InputNumber, CodeGenQueryType.Equal, nullable);
    }

    private static CodeGenColumnType Of(string csharp, string ts, CodeGenHtmlType html, CodeGenQueryType query, bool nullable)
        => new(nullable ? csharp + "?" : csharp, ts, html, query);
}
