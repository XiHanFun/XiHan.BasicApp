// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.CodeGeneration.Infrastructure.Generation;

namespace XiHan.BasicApp.CodeGeneration.Tests;

/// <summary>
/// 常量选择器候选项转义测试。
/// </summary>
/// <remarks>
/// 候选项是列配置界面上的自由文本，填错的形式远多于填对的。这里锁定「不合规就在生成期抛」的口径：
/// 放行一个脏值，用户拿到的就是一份编译不过的前端代码，而不是一条能照着改的错误。
/// </remarks>
public sealed class SelectOptionsEscaperTests
{
    /// <summary>
    /// 合规候选项按 JS 对象字面量输出，键不加引号、值用单引号。
    /// </summary>
    [Fact]
    public void SelectOptions_ValidShouldRenderJsLiteral()
    {
        var result = TemplateTextEscaper.SelectOptions("""[{"label":"日用","value":"daily"}]""", "分类", "string");

        Assert.Equal("[{ label: '日用', value: 'daily' }]", result, StringComparer.Ordinal);
    }

    /// <summary>
    /// 数字值与列的 number 类型对得上时放行；额外的 disabled 字段不影响判定。
    /// </summary>
    [Fact]
    public void SelectOptions_NumericValueShouldPassForNumberColumn()
    {
        var result = TemplateTextEscaper.SelectOptions("""[{"label":"低","value":0,"disabled":true}]""", "等级", "number");

        Assert.Equal("[{ label: '低', value: 0, disabled: true }]", result, StringComparer.Ordinal);
    }

    /// <summary>
    /// 各类不合规填法都要在生成期抛出，且错误信息里带列身份，用户能定位到是哪一列。
    /// </summary>
    /// <param name="json">候选项原文</param>
    /// <param name="tsType">列的 TS 类型</param>
    [Theory]
    // 压根不是 JSON
    [InlineData("日用,生鲜", "string")]
    // 是 JSON 但不是数组
    [InlineData("""{"daily":"日用"}""", "string")]
    // 数组元素不是对象
    [InlineData("""["是","否"]""", "string")]
    // 缺 value
    [InlineData("""[{"label":"甲"}]""", "string")]
    // 缺 label
    [InlineData("""[{"value":"a"}]""", "string")]
    // value 类型不被下拉接受
    [InlineData("""[{"label":"是","value":true}]""", "string")]
    // 同名键：JSON 合法，写成 JS 对象字面量是 TS1117
    [InlineData("""[{"label":"日用","value":"daily","value":"fresh"}]""", "string")]
    // 值类型与列类型对不上：回抛进表单字段后提交必被后端拒
    [InlineData("""[{"label":"低","value":0}]""", "string")]
    [InlineData("""[{"label":"低","value":"0"}]""", "number")]
    // 空数组：产出的是一个选不出东西的下拉
    [InlineData("[]", "string")]
    public void SelectOptions_InvalidShouldThrowWithColumnIdentity(string json, string tsType)
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => TemplateTextEscaper.SelectOptions(json, "分类", tsType));

        Assert.Contains("分类", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 没填候选项同样是错，不能回退成空数组——那会渲出一个必填却无选项的下拉，表单从此提交不了。
    /// </summary>
    /// <param name="json">候选项原文</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SelectOptions_EmptyShouldThrowInsteadOfFallingBack(string? json)
    {
        Assert.Throws<InvalidOperationException>(() => TemplateTextEscaper.SelectOptions(json, "分类", "string"));
    }

    /// <summary>
    /// 列没有注释时（MySQL/PG 建表默认就没有 COMMENT），错误信息仍要能读，不能只剩一个空的「列「」」。
    /// </summary>
    [Fact]
    public void SelectOptions_MissingColumnLabelShouldStillReadable()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => TemplateTextEscaper.SelectOptions("不是 JSON", null, "string"));

        Assert.Contains("无注释", exception.Message, StringComparison.Ordinal);
    }
}
