// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.CodeGeneration.Domain.Enums;
using XiHan.BasicApp.CodeGeneration.Infrastructure.Generation;

namespace XiHan.BasicApp.CodeGeneration.Tests;

/// <summary>
/// 默认类型映射提供器测试。
/// </summary>
/// <remarks>
/// 映射表是"DB 列类型 → C# 类型 / TS 类型 / 表单控件 / 查询方式"的四元组事实源，
/// 任一条漏掉都会落到末尾的字符串兜底分支：时间列变成文本框 + 模糊查询、金额列失去区间查询。
/// 源码注释显式记载过 PostgreSQL 的 timestamptz 曾漏掉，这里为整张表逐条加回归锚点。
/// </remarks>
public sealed class DefaultTypeMappingProviderTests
{
    private readonly DefaultTypeMappingProvider _provider = new();

    /// <summary>
    /// 整张映射表逐条锁定：每个已收录的 DB 类型名必须映射到确定的四元组。
    /// </summary>
    /// <param name="dbType">数据库列类型名</param>
    /// <param name="csharpType">期望的 C# 类型</param>
    /// <param name="tsType">期望的 TS 类型</param>
    /// <param name="htmlType">期望的表单控件</param>
    /// <param name="queryType">期望的查询方式</param>
    [Theory]
    // 长整型族
    [InlineData("bigint", "long", "number", HtmlType.InputNumber, QueryType.Equal)]
    [InlineData("long", "long", "number", HtmlType.InputNumber, QueryType.Equal)]
    [InlineData("int8", "long", "number", HtmlType.InputNumber, QueryType.Equal)]
    [InlineData("bigserial", "long", "number", HtmlType.InputNumber, QueryType.Equal)]
    [InlineData("serial8", "long", "number", HtmlType.InputNumber, QueryType.Equal)]
    // 整型族
    [InlineData("int", "int", "number", HtmlType.InputNumber, QueryType.Equal)]
    [InlineData("integer", "int", "number", HtmlType.InputNumber, QueryType.Equal)]
    [InlineData("int4", "int", "number", HtmlType.InputNumber, QueryType.Equal)]
    [InlineData("mediumint", "int", "number", HtmlType.InputNumber, QueryType.Equal)]
    [InlineData("serial", "int", "number", HtmlType.InputNumber, QueryType.Equal)]
    [InlineData("serial4", "int", "number", HtmlType.InputNumber, QueryType.Equal)]
    // 小整型族同样落到 int（不是 short，避免产物里出现 short 与前端 number 的歧义）
    [InlineData("smallint", "int", "number", HtmlType.InputNumber, QueryType.Equal)]
    [InlineData("tinyint", "int", "number", HtmlType.InputNumber, QueryType.Equal)]
    [InlineData("int2", "int", "number", HtmlType.InputNumber, QueryType.Equal)]
    [InlineData("smallserial", "int", "number", HtmlType.InputNumber, QueryType.Equal)]
    [InlineData("serial2", "int", "number", HtmlType.InputNumber, QueryType.Equal)]
    // 布尔族
    [InlineData("bit", "bool", "boolean", HtmlType.Switch, QueryType.Equal)]
    [InlineData("bool", "bool", "boolean", HtmlType.Switch, QueryType.Equal)]
    [InlineData("boolean", "bool", "boolean", HtmlType.Switch, QueryType.Equal)]
    // 定点数族：默认区间查询（金额/数量按范围筛）
    [InlineData("decimal", "decimal", "number", HtmlType.InputNumber, QueryType.Between)]
    [InlineData("numeric", "decimal", "number", HtmlType.InputNumber, QueryType.Between)]
    [InlineData("money", "decimal", "number", HtmlType.InputNumber, QueryType.Between)]
    [InlineData("smallmoney", "decimal", "number", HtmlType.InputNumber, QueryType.Between)]
    // 浮点族
    [InlineData("float", "double", "number", HtmlType.InputNumber, QueryType.Between)]
    [InlineData("double", "double", "number", HtmlType.InputNumber, QueryType.Between)]
    [InlineData("real", "double", "number", HtmlType.InputNumber, QueryType.Between)]
    [InlineData("float4", "double", "number", HtmlType.InputNumber, QueryType.Between)]
    [InlineData("float8", "double", "number", HtmlType.InputNumber, QueryType.Between)]
    [InlineData("double precision", "double", "number", HtmlType.InputNumber, QueryType.Between)]
    // 日期时间族：TS 侧是 string（ISO 文本），控件是日期时间选择器
    [InlineData("datetime", "DateTimeOffset", "string", HtmlType.DateTimePicker, QueryType.Between)]
    [InlineData("datetime2", "DateTimeOffset", "string", HtmlType.DateTimePicker, QueryType.Between)]
    [InlineData("smalldatetime", "DateTimeOffset", "string", HtmlType.DateTimePicker, QueryType.Between)]
    [InlineData("timestamp", "DateTimeOffset", "string", HtmlType.DateTimePicker, QueryType.Between)]
    [InlineData("timestamptz", "DateTimeOffset", "string", HtmlType.DateTimePicker, QueryType.Between)]
    [InlineData("timestamp with time zone", "DateTimeOffset", "string", HtmlType.DateTimePicker, QueryType.Between)]
    [InlineData("timestamp without time zone", "DateTimeOffset", "string", HtmlType.DateTimePicker, QueryType.Between)]
    [InlineData("datetimeoffset", "DateTimeOffset", "string", HtmlType.DateTimePicker, QueryType.Between)]
    // 纯日期用日期控件（不带时分秒）
    [InlineData("date", "DateTimeOffset", "string", HtmlType.DatePicker, QueryType.Between)]
    // 纯时间族
    [InlineData("time", "TimeSpan", "string", HtmlType.TimePicker, QueryType.Equal)]
    [InlineData("timetz", "TimeSpan", "string", HtmlType.TimePicker, QueryType.Equal)]
    [InlineData("time with time zone", "TimeSpan", "string", HtmlType.TimePicker, QueryType.Equal)]
    [InlineData("time without time zone", "TimeSpan", "string", HtmlType.TimePicker, QueryType.Equal)]
    // Guid 族
    [InlineData("uniqueidentifier", "Guid", "string", HtmlType.Input, QueryType.Equal)]
    [InlineData("uuid", "Guid", "string", HtmlType.Input, QueryType.Equal)]
    [InlineData("guid", "Guid", "string", HtmlType.Input, QueryType.Equal)]
    // 大文本族：文本域 + 模糊查询
    [InlineData("text", "string", "string", HtmlType.Textarea, QueryType.Like)]
    [InlineData("longtext", "string", "string", HtmlType.Textarea, QueryType.Like)]
    [InlineData("mediumtext", "string", "string", HtmlType.Textarea, QueryType.Like)]
    [InlineData("ntext", "string", "string", HtmlType.Textarea, QueryType.Like)]
    [InlineData("clob", "string", "string", HtmlType.Textarea, QueryType.Like)]
    // 二进制族：byte[] + 文件上传 + 等值（不得模糊查询）
    [InlineData("varbinary", "byte[]", "string", HtmlType.FileUpload, QueryType.Equal)]
    [InlineData("binary", "byte[]", "string", HtmlType.FileUpload, QueryType.Equal)]
    [InlineData("blob", "byte[]", "string", HtmlType.FileUpload, QueryType.Equal)]
    [InlineData("image", "byte[]", "string", HtmlType.FileUpload, QueryType.Equal)]
    [InlineData("bytea", "byte[]", "string", HtmlType.FileUpload, QueryType.Equal)]
    // JSON 族按大文本处理
    [InlineData("json", "string", "string", HtmlType.Textarea, QueryType.Like)]
    [InlineData("jsonb", "string", "string", HtmlType.Textarea, QueryType.Like)]
    public void Map_ShouldLockWholeMappingTable(string dbType, string csharpType, string tsType, HtmlType htmlType, QueryType queryType)
    {
        var mapping = _provider.Map(DatabaseType.PostgreSql, dbType, false);

        Assert.Equal(csharpType, mapping.CSharpType, StringComparer.Ordinal);
        Assert.Equal(tsType, mapping.TsType, StringComparer.Ordinal);
        Assert.Equal(htmlType, mapping.DefaultHtmlType);
        Assert.Equal(queryType, mapping.DefaultQueryType);
    }

    /// <summary>
    /// 未收录类型、常见变长字符串类型与空值一律回退到 string + 文本框 + 模糊查询，
    /// 兜底分支必须存在且不抛异常（外部库的方言类型名千奇百怪）。
    /// </summary>
    /// <param name="dbType">未收录或字符串类型名</param>
    [Theory]
    [InlineData("varchar")]
    [InlineData("nvarchar")]
    [InlineData("char")]
    [InlineData("bpchar")]
    [InlineData("nchar")]
    [InlineData("string")]
    [InlineData("enum")]
    [InlineData("set")]
    [InlineData("hstore")]
    [InlineData("SOMETHING_UNKNOWN")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Map_UnknownTypeShouldFallBackToStringInputLike(string? dbType)
    {
        var mapping = _provider.Map(DatabaseType.MySql, dbType, false);

        Assert.Equal("string", mapping.CSharpType, StringComparer.Ordinal);
        Assert.Equal("string", mapping.TsType, StringComparer.Ordinal);
        Assert.Equal(HtmlType.Input, mapping.DefaultHtmlType);
        Assert.Equal(QueryType.Like, mapping.DefaultQueryType);
    }

    /// <summary>
    /// 可空标注只加在 C# 类型尾部；TS 类型永远不带可空标注（前端由 DTO 的可选属性表达）。
    /// </summary>
    /// <param name="dbType">数据库列类型名</param>
    /// <param name="expectedNullableCSharp">期望的可空 C# 类型</param>
    /// <param name="expectedTs">期望的 TS 类型</param>
    [Theory]
    [InlineData("bigint", "long?", "number")]
    [InlineData("bit", "bool?", "boolean")]
    [InlineData("datetime", "DateTimeOffset?", "string")]
    [InlineData("varchar", "string?", "string")]
    [InlineData("blob", "byte[]?", "string")]
    [InlineData("uuid", "Guid?", "string")]
    public void Map_NullableShouldOnlyAnnotateCSharpType(string dbType, string expectedNullableCSharp, string expectedTs)
    {
        var mapping = _provider.Map(DatabaseType.SqlServer, dbType, true);

        Assert.Equal(expectedNullableCSharp, mapping.CSharpType, StringComparer.Ordinal);
        Assert.Equal(expectedTs, mapping.TsType, StringComparer.Ordinal);
    }

    /// <summary>
    /// 归一化必须去掉长度/精度括号，否则 varchar(200) 这类真实元数据会全部落到兜底分支之外的错误分支。
    /// </summary>
    /// <param name="dbType">带括号的类型名</param>
    /// <param name="expectedCSharp">期望的 C# 类型</param>
    [Theory]
    [InlineData("varchar(200)", "string")]
    [InlineData("decimal(18,2)", "decimal")]
    [InlineData("numeric(10, 4)", "decimal")]
    [InlineData("bigint(20)", "long")]
    [InlineData("tinyint(1)", "int")]
    [InlineData("datetime(6)", "DateTimeOffset")]
    [InlineData("char(1)", "string")]
    public void Map_ShouldStripLengthAndPrecisionParentheses(string dbType, string expectedCSharp)
    {
        Assert.Equal(expectedCSharp, _provider.Map(DatabaseType.MySql, dbType, false).CSharpType, StringComparer.Ordinal);
    }

    /// <summary>
    /// 括号出现在首位时不截断（parenIndex 必须严格大于 0），
    /// 否则会把整个类型名清空、让归一化结果变成空串。
    /// </summary>
    [Fact]
    public void Map_LeadingParenthesisShouldNotTruncate()
    {
        var mapping = _provider.Map(DatabaseType.MySql, "(bigint)", false);

        Assert.Equal("string", mapping.CSharpType, StringComparer.Ordinal);
        Assert.Equal(QueryType.Like, mapping.DefaultQueryType);
    }

    /// <summary>
    /// 归一化去掉 unsigned 与数组后缀，并对大小写与首尾空白不敏感。
    /// </summary>
    /// <param name="dbType">带修饰或大小写不一致的类型名</param>
    /// <param name="expectedCSharp">期望的 C# 类型</param>
    [Theory]
    [InlineData("int unsigned", "int")]
    [InlineData("bigint unsigned", "long")]
    [InlineData("BIGINT", "long")]
    [InlineData("  BigInt  ", "long")]
    [InlineData("VARCHAR(50)", "string")]
    [InlineData("integer[]", "int")]
    [InlineData("text[]", "string")]
    [InlineData("bigint(20) unsigned", "long")]
    [InlineData("TIMESTAMPTZ", "DateTimeOffset")]
    public void Map_ShouldNormalizeSuffixesAndCase(string dbType, string expectedCSharp)
    {
        Assert.Equal(expectedCSharp, _provider.Map(DatabaseType.PostgreSql, dbType, false).CSharpType, StringComparer.Ordinal);
    }

    /// <summary>
    /// 映射结果与数据库方言无关：同一个类型名在五种数据库下映射完全一致，
    /// 保证同一张外部表换数据源导入时不会产出不同的配置。
    /// </summary>
    /// <param name="dbType">数据库列类型名</param>
    [Theory]
    [InlineData("bigint")]
    [InlineData("varchar(50)")]
    [InlineData("timestamptz")]
    [InlineData("bytea")]
    public void Map_ShouldBeIndependentOfDatabaseDialect(string dbType)
    {
        var baseline = _provider.Map(DatabaseType.MySql, dbType, false);

        foreach (var database in Enum.GetValues<DatabaseType>())
        {
            var mapping = _provider.Map(database, dbType, false);
            Assert.Equal(baseline, mapping);
        }
    }

    /// <summary>
    /// 二进制列必须与 <c>CSharpTypeFacts.IsBinary</c> 判据咬合：
    /// 映射产出 byte[] 而查询区据此跳过该列，两处口径不一致会让 blob 列进搜索条件。
    /// </summary>
    /// <param name="dbType">二进制类型名</param>
    [Theory]
    [InlineData("varbinary(max)")]
    [InlineData("blob")]
    [InlineData("bytea")]
    public void Map_BinaryColumnShouldBeRecognizedByTypeFacts(string dbType)
    {
        var mapping = _provider.Map(DatabaseType.SqlServer, dbType, true);

        Assert.True(XiHan.BasicApp.CodeGeneration.Domain.Generation.CSharpTypeFacts.IsBinary(mapping.CSharpType));
    }
}
