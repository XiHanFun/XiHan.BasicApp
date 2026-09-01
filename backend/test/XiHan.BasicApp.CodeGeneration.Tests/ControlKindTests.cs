// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.CodeGeneration.Domain.Enums;
using XiHan.BasicApp.CodeGeneration.Infrastructure.Generation;

namespace XiHan.BasicApp.CodeGeneration.Tests;

/// <summary>
/// 表单控件判定测试。
/// </summary>
/// <remarks>
/// 「这列该渲什么控件」曾经散在六份模板里各判一次，次序稍有不同就会渲出
/// 「控件是下拉、表单模型是时间戳」这类自相矛盾、编译期还看不出来的代码。
/// 现在收敛到渲染器一处，这里把判定次序与派生出的表单类型一并锁住。
/// </remarks>
public sealed class ControlKindTests
{
    private readonly ScribanTemplateRenderer _renderer = new();

    /// <summary>
    /// 类型与控件配置打架时，能与表单模型自洽的那一方胜出：
    /// 布尔只能是开关、日期只能是日期控件，而数字与文本都能被下拉承载，故下拉排在这两者之前。
    /// </summary>
    /// <param name="csharpType">C# 类型</param>
    /// <param name="tsType">列配置里存的 TS 类型</param>
    /// <param name="htmlType">列配置里存的控件</param>
    /// <param name="dictSelector">字典选择器类型</param>
    /// <param name="expected">期望的控件种类</param>
    [Theory]
    // 常规
    [InlineData("string", "string", HtmlType.Input, null, "text")]
    [InlineData("string", "string", HtmlType.Textarea, null, "textarea")]
    [InlineData("int", "number", HtmlType.InputNumber, null, "number")]
    [InlineData("bool", "boolean", HtmlType.Switch, null, "switch")]
    [InlineData("DateTimeOffset", "string", HtmlType.DatePicker, null, "date")]
    [InlineData("DateTimeOffset", "string", HtmlType.DateTimePicker, null, "datetime")]
    [InlineData("TimeSpan", "string", HtmlType.TimePicker, null, "time")]
    [InlineData("byte[]", "string", HtmlType.FileUpload, null, "binary")]
    [InlineData("string", "string", HtmlType.Select, DictSelectorType.EnumSelector, "select")]
    // 打架：布尔挂下拉/日期控件，一律仍是开关（下拉与日期都绑不上 boolean）
    [InlineData("bool", "boolean", HtmlType.Select, DictSelectorType.ConstSelector, "switch")]
    [InlineData("bool", "boolean", HtmlType.DatePicker, null, "switch")]
    // 打架：日期挂下拉，仍是日期（表单模型是时间戳，下拉绑不上）
    [InlineData("DateTimeOffset", "string", HtmlType.DatePicker, DictSelectorType.ConstSelector, "date")]
    // 打架：数字挂多行文本，仍是数字框（多行文本框绑不上 number）
    [InlineData("decimal", "number", HtmlType.Textarea, null, "number")]
    // 不打架：数字挂下拉是合理配置（下拉的值可以是数字），保留用户选择
    [InlineData("int", "number", HtmlType.Select, DictSelectorType.ConstSelector, "select")]
    // 二进制优先于一切：byte[] 渲成别的都没有意义
    [InlineData("byte[]", "string", HtmlType.Textarea, DictSelectorType.ConstSelector, "binary")]
    public async Task ControlKind_ShouldResolveByTypeFirst(
        string csharpType,
        string tsType,
        HtmlType htmlType,
        DictSelectorType? dictSelector,
        string expected)
    {
        var column = CodeGenerationTestHelper.CreateColumn("col", csharpType, tsType, htmlType: htmlType);
        column.DictSelectorType = dictSelector;
        var context = CodeGenerationTestHelper.CreateContext(columns: [column]);

        var result = await _renderer.RenderAsync("{{ for col in Columns }}{{ col.ControlKind }}{{ end }}", context);

        Assert.Equal(expected, result, StringComparer.Ordinal);
    }

    /// <summary>
    /// 表单模型类型随控件走：开关恒 boolean、纯日期按时间戳承载，其余同归一化后的 TS 类型。
    /// </summary>
    /// <param name="csharpType">C# 类型</param>
    /// <param name="tsType">列配置里存的 TS 类型</param>
    /// <param name="htmlType">列配置里存的控件</param>
    /// <param name="expected">期望的表单字段类型</param>
    [Theory]
    [InlineData("bool", "boolean", HtmlType.Switch, "boolean")]
    [InlineData("DateTimeOffset", "string", HtmlType.DatePicker, "number")]
    [InlineData("DateTimeOffset", "string", HtmlType.DateTimePicker, "string")]
    [InlineData("int", "number", HtmlType.InputNumber, "number")]
    [InlineData("string", "string", HtmlType.Input, "string")]
    public async Task FormTsType_ShouldFollowControlKind(string csharpType, string tsType, HtmlType htmlType, string expected)
    {
        var column = CodeGenerationTestHelper.CreateColumn("col", csharpType, tsType, htmlType: htmlType);
        var context = CodeGenerationTestHelper.CreateContext(columns: [column]);

        var result = await _renderer.RenderAsync("{{ for col in Columns }}{{ col.FormTsType }}{{ end }}", context);

        Assert.Equal(expected, result, StringComparer.Ordinal);
    }

    /// <summary>
    /// long 列在报文里是字符串（全局 LongJsonConverter），TsType 一律归一化。
    /// </summary>
    /// <remarks>
    /// 存量表配置里可能还存着 <c>ts_type='number'</c>——那是本次改动之前导入的。
    /// 归一化放在渲染期，产物的正确性就不依赖存量数据是否被升级脚本刷过。
    /// </remarks>
    /// <param name="csharpType">C# 类型</param>
    /// <param name="storedTsType">列配置里存的 TS 类型</param>
    [Theory]
    [InlineData("long", "number")]
    [InlineData("long?", "number")]
    [InlineData("long", "string")]
    public async Task TsType_ShouldNormalizeLongToString(string csharpType, string storedTsType)
    {
        var column = CodeGenerationTestHelper.CreateColumn("col", csharpType, storedTsType, htmlType: HtmlType.InputNumber);
        var context = CodeGenerationTestHelper.CreateContext(columns: [column]);

        var result = await _renderer.RenderAsync("{{ for col in Columns }}{{ col.TsType }}|{{ col.IsLongColumn }}|{{ col.ControlKind }}{{ end }}", context);

        Assert.Equal("string|true|text", result, StringComparer.Ordinal);
    }

    /// <summary>
    /// 非 long 的数字列不受归一化影响。
    /// </summary>
    [Fact]
    public async Task TsType_ShouldLeaveOtherNumbersAlone()
    {
        var column = CodeGenerationTestHelper.CreateColumn("col", "int", "number", htmlType: HtmlType.InputNumber);
        var context = CodeGenerationTestHelper.CreateContext(columns: [column]);

        var result = await _renderer.RenderAsync("{{ for col in Columns }}{{ col.TsType }}|{{ col.IsLongColumn }}{{ end }}", context);

        Assert.Equal("number|false", result, StringComparer.Ordinal);
    }
}
