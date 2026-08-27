// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Exporting;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// DTO 行投影器测试。
/// </summary>
/// <remarks>
/// 投影器是导出链路上唯一把「后端对象」翻译成「表格字符串」的地方，且全是无副作用的分支判断。
/// 它有两条容易踩的暗线：
/// <list type="bullet">
/// <item>枚举渲染成**名称**而非数值——列快照的 valueMap 是按名称建键的，改成数值会导致 label 全部命中不了；</item>
/// <item>数值/日期一律走 <see cref="CultureInfo.InvariantCulture"/>——否则同一份代码在不同区域设置的机器上导出结果不同。</item>
/// </list>
/// 这里对 FormatValue 的每一条分支与 ValueMap 的命中/未命中都给出用例。
/// </remarks>
public sealed class SaasAppExportRowProjectorTests
{
    /// <summary>
    /// 供投影的示例 DTO。
    /// </summary>
    private sealed class SampleDto
    {
        /// <summary>字符串字段。</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>可空字符串字段。</summary>
        public string? Nickname { get; set; }

        /// <summary>布尔字段。</summary>
        public bool IsEnabled { get; set; }

        /// <summary>整型字段。</summary>
        public int Age { get; set; }

        /// <summary>长整型字段。</summary>
        public long BasicId { get; set; }

        /// <summary>小数字段。</summary>
        public decimal Amount { get; set; }

        /// <summary>双精度字段。</summary>
        public double Ratio { get; set; }

        /// <summary>枚举字段。</summary>
        public SampleStatus Status { get; set; }

        /// <summary>可空枚举字段。</summary>
        public SampleStatus? OptionalStatus { get; set; }

        /// <summary>日期时间字段。</summary>
        public DateTime CreatedTime { get; set; }

        /// <summary>带偏移的日期时间字段。</summary>
        public DateTimeOffset UpdatedTime { get; set; }

        /// <summary>可空日期字段。</summary>
        public DateTimeOffset? DeletedTime { get; set; }

        /// <summary>标识字段。</summary>
        public Guid TraceId { get; set; }

        /// <summary>集合字段。</summary>
        public List<int> RoleIds { get; set; } = [];

        /// <summary>字符串集合字段。</summary>
        public string[] Tags { get; set; } = [];

        /// <summary>复杂对象字段。</summary>
        public SampleNested? Nested { get; set; }
    }

    /// <summary>
    /// 嵌套对象。
    /// </summary>
    private sealed class SampleNested
    {
        /// <summary>嵌套值。</summary>
        public int Inner { get; set; }
    }

    /// <summary>
    /// 示例枚举。
    /// </summary>
    private enum SampleStatus
    {
        /// <summary>草稿。</summary>
        Draft = 0,

        /// <summary>已发布。</summary>
        Published = 7
    }

    /// <summary>
    /// 投影结果的元素个数与顺序必须与列定义一一对应。
    /// </summary>
    [Fact]
    public void Project_ShouldFollowColumnOrderAndCount()
    {
        var dto = new SampleDto { Name = "张三", Age = 18 };

        var row = DtoRowProjector.Project(dto, [Column("age"), Column("name")]);

        Assert.Equal(2, row.Count);
        Assert.Equal("18", row[0], StringComparer.Ordinal);
        Assert.Equal("张三", row[1], StringComparer.Ordinal);
    }

    /// <summary>
    /// 列定义为空时得到空行，而不是抛异常。
    /// </summary>
    [Fact]
    public void Project_WithoutColumns_ShouldReturnEmptyRow()
    {
        Assert.Empty(DtoRowProjector.Project(new SampleDto(), []));
    }

    /// <summary>
    /// 回归锚点：null 入参必须抛 <see cref="ArgumentNullException"/>，而不是 NullReferenceException。
    /// </summary>
    /// <remarks>
    /// 修复前方法体第一句就是 <c>dto.GetType()</c>，传 null 直接以 NullReferenceException 崩在投影器内部，
    /// 调用栈指向"反射取类型"，看不出是调用方给了空对象。同层的导出写入器与全部映射器都做了空守卫，
    /// 这里是最后一个缺口。
    /// </remarks>
    [Fact]
    public void Project_NullArguments_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => DtoRowProjector.Project(null!, [Column("name")]));
        Assert.Throws<ArgumentNullException>(() => DtoRowProjector.Project(new SampleDto(), null!));
    }

    /// <summary>
    /// 字段键大小写不敏感：前端 camelCase 的键要能命中后端 PascalCase 属性。
    /// </summary>
    /// <param name="key">列定义中的字段键。</param>
    [Theory]
    [InlineData("Name")]
    [InlineData("name")]
    [InlineData("NAME")]
    [InlineData("nAmE")]
    public void Project_FieldKey_ShouldBeCaseInsensitive(string key)
    {
        var row = DtoRowProjector.Project(new SampleDto { Name = "张三" }, [Column(key)]);

        Assert.Equal("张三", row[0], StringComparer.Ordinal);
    }

    /// <summary>
    /// 字段键在 DTO 上找不到对应属性时输出空串（多余的列不该把整次导出打断）。
    /// </summary>
    [Fact]
    public void Project_UnknownFieldKey_ShouldRenderEmpty()
    {
        var row = DtoRowProjector.Project(new SampleDto(), [Column("thisFieldDoesNotExist")]);

        Assert.Equal(string.Empty, row[0], StringComparer.Ordinal);
    }

    /// <summary>
    /// null 值渲染为空串。
    /// </summary>
    [Fact]
    public void Project_NullValue_ShouldRenderEmpty()
    {
        var row = DtoRowProjector.Project(
            new SampleDto { Nickname = null, OptionalStatus = null, DeletedTime = null, Nested = null },
            [Column("nickname"), Column("optionalStatus"), Column("deletedTime"), Column("nested")]);

        Assert.All(row, cell => Assert.Equal(string.Empty, cell, StringComparer.Ordinal));
    }

    /// <summary>
    /// 布尔渲染为小写 true/false（前端再按 ValueMap 映射成 是/否）。
    /// </summary>
    /// <param name="value">布尔值。</param>
    /// <param name="expected">期望文本。</param>
    [Theory]
    [InlineData(true, "true")]
    [InlineData(false, "false")]
    public void Project_Boolean_ShouldRenderLowerCaseLiteral(bool value, string expected)
    {
        var row = DtoRowProjector.Project(new SampleDto { IsEnabled = value }, [Column("isEnabled")]);

        Assert.Equal(expected, row[0], StringComparer.Ordinal);
    }

    /// <summary>
    /// 枚举渲染为**名称**，而不是底层数值——列快照 valueMap 是按名称建键的。
    /// </summary>
    [Fact]
    public void Project_Enum_ShouldRenderNameNotNumericValue()
    {
        var row = DtoRowProjector.Project(
            new SampleDto { Status = SampleStatus.Published, OptionalStatus = SampleStatus.Draft },
            [Column("status"), Column("optionalStatus")]);

        Assert.Equal("Published", row[0], StringComparer.Ordinal);
        Assert.Equal("Draft", row[1], StringComparer.Ordinal);
    }

    /// <summary>
    /// 未定义的枚举数值退化为数字文本（保持 Enum.ToString 语义，不抛异常）。
    /// </summary>
    [Fact]
    public void Project_UndefinedEnumValue_ShouldRenderNumericText()
    {
        var row = DtoRowProjector.Project(new SampleDto { Status = (SampleStatus)99 }, [Column("status")]);

        Assert.Equal("99", row[0], StringComparer.Ordinal);
    }

    /// <summary>
    /// 日期时间统一 yyyy-MM-dd HH:mm:ss，且与运行机器的区域设置无关。
    /// </summary>
    [Fact]
    public void Project_DateTime_ShouldUseInvariantFixedFormat()
    {
        var dto = new SampleDto
        {
            CreatedTime = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc),
            UpdatedTime = new DateTimeOffset(2026, 12, 31, 23, 59, 58, TimeSpan.FromHours(8))
        };

        var row = DtoRowProjector.Project(dto, [Column("createdTime"), Column("updatedTime")]);

        Assert.Equal("2026-01-02 03:04:05", row[0], StringComparer.Ordinal);
        Assert.Equal("2026-12-31 23:59:58", row[1], StringComparer.Ordinal);
    }

    /// <summary>
    /// 带偏移的时间按其自身偏移下的挂钟时间渲染，不会被换算到本机时区。
    /// </summary>
    [Fact]
    public void Project_DateTimeOffset_ShouldNotBeConvertedToLocalTime()
    {
        var sameInstantDifferentOffsets = new[]
        {
            new DateTimeOffset(2026, 5, 1, 12, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 5, 1, 20, 0, 0, TimeSpan.FromHours(8))
        };

        var rendered = sameInstantDifferentOffsets
            .Select(time => DtoRowProjector.Project(new SampleDto { UpdatedTime = time }, [Column("updatedTime")])[0])
            .ToList();

        Assert.Equal("2026-05-01 12:00:00", rendered[0], StringComparer.Ordinal);
        Assert.Equal("2026-05-01 20:00:00", rendered[1], StringComparer.Ordinal);
    }

    /// <summary>
    /// 数值一律走不变文化，小数点恒为 <c>.</c>，不受机器区域设置影响。
    /// </summary>
    [Fact]
    public void Project_Numbers_ShouldUseInvariantCulture()
    {
        var dto = new SampleDto
        {
            Age = -7,
            BasicId = 9_007_199_254_740_993L,
            Amount = 1234.5m,
            Ratio = 0.5d
        };

        var row = DtoRowProjector.Project(dto, [Column("age"), Column("basicId"), Column("amount"), Column("ratio")]);

        Assert.Equal("-7", row[0], StringComparer.Ordinal);
        Assert.Equal("9007199254740993", row[1], StringComparer.Ordinal);
        Assert.Equal("1234.5", row[2], StringComparer.Ordinal);
        Assert.Equal("0.5", row[3], StringComparer.Ordinal);
    }

    /// <summary>
    /// 极值不会被截断或科学计数化。
    /// </summary>
    [Fact]
    public void Project_ExtremeIntegers_ShouldRenderFullPrecision()
    {
        var row = DtoRowProjector.Project(
            new SampleDto { Age = int.MinValue, BasicId = long.MaxValue },
            [Column("age"), Column("basicId")]);

        Assert.Equal(int.MinValue.ToString(CultureInfo.InvariantCulture), row[0], StringComparer.Ordinal);
        Assert.Equal(long.MaxValue.ToString(CultureInfo.InvariantCulture), row[1], StringComparer.Ordinal);
    }

    /// <summary>
    /// Guid 渲染为默认短横线格式。
    /// </summary>
    [Fact]
    public void Project_Guid_ShouldRenderDashedForm()
    {
        var id = Guid.NewGuid();

        var row = DtoRowProjector.Project(new SampleDto { TraceId = id }, [Column("traceId")]);

        Assert.Equal(id.ToString(), row[0], StringComparer.Ordinal);
    }

    /// <summary>
    /// 集合字段以逗号拼接元素（写出器会再按各自规则整体转义）。
    /// </summary>
    [Fact]
    public void Project_Collection_ShouldJoinWithComma()
    {
        var row = DtoRowProjector.Project(
            new SampleDto { RoleIds = [1, 2, 3], Tags = ["a", "b"] },
            [Column("roleIds"), Column("tags")]);

        Assert.Equal("1,2,3", row[0], StringComparer.Ordinal);
        Assert.Equal("a,b", row[1], StringComparer.Ordinal);
    }

    /// <summary>
    /// 空集合渲染为空串。
    /// </summary>
    [Fact]
    public void Project_EmptyCollection_ShouldRenderEmpty()
    {
        var row = DtoRowProjector.Project(new SampleDto { RoleIds = [] }, [Column("roleIds")]);

        Assert.Equal(string.Empty, row[0], StringComparer.Ordinal);
    }

    /// <summary>
    /// 字符串字段先于集合分支命中，不会被逐字符拆成 "a,b,c"。
    /// </summary>
    [Fact]
    public void Project_String_ShouldNotBeTreatedAsCharCollection()
    {
        var row = DtoRowProjector.Project(new SampleDto { Name = "abc" }, [Column("name")]);

        Assert.Equal("abc", row[0], StringComparer.Ordinal);
    }

    /// <summary>
    /// 未被前面分支覆盖的复杂对象序列化为 JSON。
    /// </summary>
    [Fact]
    public void Project_ComplexObject_ShouldFallBackToJson()
    {
        var row = DtoRowProjector.Project(
            new SampleDto { Nested = new SampleNested { Inner = 5 } },
            [Column("nested")]);

        Assert.Equal("{\"Inner\":5}", row[0], StringComparer.Ordinal);
    }

    /// <summary>
    /// ValueMap 命中时输出 label，未命中时保留原始渲染文本。
    /// </summary>
    [Fact]
    public void Project_ValueMap_ShouldReplaceOnlyOnHit()
    {
        var mapped = new ExportColumnDto
        {
            Key = "status",
            Title = "状态",
            ValueMap = new Dictionary<string, string> { ["Published"] = "已发布" }
        };

        var hit = DtoRowProjector.Project(new SampleDto { Status = SampleStatus.Published }, [mapped]);
        var miss = DtoRowProjector.Project(new SampleDto { Status = SampleStatus.Draft }, [mapped]);

        Assert.Equal("已发布", hit[0], StringComparer.Ordinal);
        Assert.Equal("Draft", miss[0], StringComparer.Ordinal);
    }

    /// <summary>
    /// ValueMap 的键必须与渲染后的文本对齐：布尔列要用 true/false 建键才能命中。
    /// </summary>
    [Fact]
    public void Project_ValueMap_ShouldKeyOnRenderedText()
    {
        var column = new ExportColumnDto
        {
            Key = "isEnabled",
            Title = "启用",
            ValueMap = new Dictionary<string, string> { ["true"] = "是", ["false"] = "否" }
        };

        Assert.Equal("是", DtoRowProjector.Project(new SampleDto { IsEnabled = true }, [column])[0], StringComparer.Ordinal);
        Assert.Equal("否", DtoRowProjector.Project(new SampleDto { IsEnabled = false }, [column])[0], StringComparer.Ordinal);
    }

    /// <summary>
    /// 空 ValueMap 等同于未配置，原样输出。
    /// </summary>
    [Fact]
    public void Project_EmptyValueMap_ShouldBeIgnored()
    {
        var column = new ExportColumnDto
        {
            Key = "status",
            Title = "状态",
            ValueMap = []
        };

        Assert.Equal("Draft", DtoRowProjector.Project(new SampleDto(), [column])[0], StringComparer.Ordinal);
    }

    /// <summary>
    /// 值为 null 时直接短路成空串，ValueMap 不参与（不会出现 "" → label 的意外映射）。
    /// </summary>
    [Fact]
    public void Project_NullValue_ShouldBypassValueMap()
    {
        var column = new ExportColumnDto
        {
            Key = "nickname",
            Title = "昵称",
            ValueMap = new Dictionary<string, string> { [string.Empty] = "未填写" }
        };

        Assert.Equal(string.Empty, DtoRowProjector.Project(new SampleDto { Nickname = null }, [column])[0], StringComparer.Ordinal);
    }

    /// <summary>
    /// 属性反射结果按 (类型, 字段键) 缓存，重复投影同一 DTO 类型必须保持结果一致。
    /// </summary>
    [Fact]
    public void Project_RepeatedCalls_ShouldStayConsistentAcrossCachedLookups()
    {
        var columns = new[] { Column("Name"), Column("name") };

        var first = DtoRowProjector.Project(new SampleDto { Name = "甲" }, columns);
        var second = DtoRowProjector.Project(new SampleDto { Name = "乙" }, columns);

        Assert.Equal(["甲", "甲"], first);
        Assert.Equal(["乙", "乙"], second);
    }

    /// <summary>
    /// 构造一个仅带字段键的列定义。
    /// </summary>
    /// <param name="key">字段键。</param>
    /// <returns>列定义。</returns>
    private static ExportColumnDto Column(string key)
    {
        return new ExportColumnDto { Key = key, Title = key };
    }
}
