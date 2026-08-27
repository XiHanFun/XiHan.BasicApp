// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.CodeGeneration.Infrastructure.Generation;

namespace XiHan.BasicApp.CodeGeneration.Tests;

/// <summary>
/// 模板文本转义器测试。
/// </summary>
/// <remarks>
/// 表注释、列注释取自数据库元数据或用户输入，是本模块唯一的注入面：
/// 一个未转义的引号会把产物打成语法错误，一个未转义的 <c>--&gt;</c> 会提前关闭 HTML 注释。
/// 这里穷举各宿主上下文的保留字符，并锁定"反斜杠先于引号""&amp; 先于尖括号"这类顺序约束。
/// </remarks>
public sealed class TemplateTextEscaperTests
{
    /// <summary>
    /// C# 字符串字面量必须转义反斜杠、双引号与三类空白控制符，
    /// 且反斜杠先于引号处理，不能把自己产出的转义序列再转一次。
    /// </summary>
    /// <param name="input">原始文本</param>
    /// <param name="expected">期望的转义结果</param>
    [Theory]
    [InlineData("普通注释", "普通注释")]
    [InlineData("含\"引号\"", "含\\\"引号\\\"")]
    [InlineData("含\\反斜杠", "含\\\\反斜杠")]
    [InlineData("\\\"", "\\\\\\\"")]
    [InlineData("第一行\r\n第二行", "第一行\\r\\n第二行")]
    [InlineData("含\t制表", "含\\t制表")]
    [InlineData("a\\nb", "a\\\\nb")]
    public void CSharpString_ShouldEscapeAllFiveCharacters(string input, string expected)
    {
        Assert.Equal(expected, TemplateTextEscaper.CSharpString(input), StringComparer.Ordinal);
    }

    /// <summary>
    /// C# 字面量是唯一保留换行（转成 \r\n 转义序列）的分支，不做换行折叠，
    /// 否则多行注释会在产物里丢掉换行语义。
    /// </summary>
    [Fact]
    public void CSharpString_ShouldNotCollapseNewLinesLikeOtherEscapers()
    {
        const string Input = "第一行\n第二行";

        Assert.Equal("第一行\\n第二行", TemplateTextEscaper.CSharpString(Input), StringComparer.Ordinal);
        Assert.Equal("第一行 第二行", TemplateTextEscaper.XmlDoc(Input), StringComparer.Ordinal);
    }

    /// <summary>
    /// XML 文档注释必须转义三类实体字符且 &amp; 先行，避免把 &amp;lt; 二次转义成 &amp;amp;lt;。
    /// </summary>
    /// <param name="input">原始文本</param>
    /// <param name="expected">期望的转义结果</param>
    [Theory]
    [InlineData("a & b", "a &amp; b")]
    [InlineData("<tag>", "&lt;tag&gt;")]
    [InlineData("a<b>c&d", "a&lt;b&gt;c&amp;d")]
    [InlineData("&amp;", "&amp;amp;")]
    [InlineData("List<int>", "List&lt;int&gt;")]
    public void XmlDoc_ShouldEscapeEntitiesWithAmpersandFirst(string input, string expected)
    {
        Assert.Equal(expected, TemplateTextEscaper.XmlDoc(input), StringComparer.Ordinal);
    }

    /// <summary>
    /// XML 文档注释把换行折成空格并去首尾空白，否则第二行会丢失 /// 前缀而破坏文档结构。
    /// </summary>
    /// <param name="input">含换行的文本</param>
    /// <param name="expected">期望的单行结果</param>
    [Theory]
    [InlineData("  第一行\r\n第二行  ", "第一行 第二行")]
    [InlineData("第一行\r第二行", "第一行 第二行")]
    [InlineData("第一行\n第二行", "第一行 第二行")]
    [InlineData("\n\n", "")]
    public void XmlDoc_ShouldCollapseNewLinesAndTrim(string input, string expected)
    {
        Assert.Equal(expected, TemplateTextEscaper.XmlDoc(input), StringComparer.Ordinal);
    }

    /// <summary>
    /// TS 单引号字面量转义反斜杠与单引号，并折行；双引号不需要转义（宿主是单引号）。
    /// </summary>
    /// <param name="input">原始文本</param>
    /// <param name="expected">期望的转义结果</param>
    [Theory]
    [InlineData("普通", "普通")]
    [InlineData("it's", "it\\'s")]
    [InlineData("a\\b", "a\\\\b")]
    [InlineData("\\'", "\\\\\\'")]
    [InlineData("含\"双引号\"", "含\"双引号\"")]
    [InlineData("第一行\r\n第二行", "第一行 第二行")]
    public void TsString_ShouldEscapeBackslashAndSingleQuote(string input, string expected)
    {
        Assert.Equal(expected, TemplateTextEscaper.TsString(input), StringComparer.Ordinal);
    }

    /// <summary>
    /// HTML 属性值转义 &amp;、双引号与尖括号且 &amp; 先行。
    /// </summary>
    /// <param name="input">原始文本</param>
    /// <param name="expected">期望的转义结果</param>
    [Theory]
    [InlineData("普通", "普通")]
    [InlineData("a\"b", "a&quot;b")]
    [InlineData("a&b", "a&amp;b")]
    [InlineData("<i>", "&lt;i&gt;")]
    [InlineData("&\"<>", "&amp;&quot;&lt;&gt;")]
    [InlineData("&quot;", "&amp;quot;")]
    [InlineData("第一行\n第二行", "第一行 第二行")]
    public void HtmlAttribute_ShouldEscapeQuotesAndAnglesWithAmpersandFirst(string input, string expected)
    {
        Assert.Equal(expected, TemplateTextEscaper.HtmlAttribute(input), StringComparer.Ordinal);
    }

    /// <summary>
    /// vue-i18n 消息把四个保留字符各自折成字面量插值，反斜杠与单引号按 TS 字面量转义。
    /// </summary>
    /// <param name="input">原始文本</param>
    /// <param name="expected">期望的转义结果</param>
    [Theory]
    [InlineData("普通文案", "普通文案")]
    [InlineData("@", "{\\'@\\'}")]
    [InlineData("|", "{\\'|\\'}")]
    [InlineData("{", "{\\'{\\'}")]
    [InlineData("}", "{\\'}\\'}")]
    [InlineData("it's", "it\\'s")]
    [InlineData("a\\b", "a\\\\b")]
    public void I18nMessage_ShouldEscapeEachReservedCharacter(string input, string expected)
    {
        Assert.Equal(expected, TemplateTextEscaper.I18nMessage(input), StringComparer.Ordinal);
    }

    /// <summary>
    /// 单趟逐字符处理的核心证明：连续保留字符不会让先产出的花括号被后续规则再转义一次。
    /// 顺序替换实现（先换 @ 再换 {）会在这里出现双重转义而变红。
    /// </summary>
    [Fact]
    public void I18nMessage_ContiguousReservedCharsShouldBeEscapedExactlyOnce()
    {
        Assert.Equal(
            "a{\\'@\\'}b{\\'|\\'}c{\\'{\\'}d{\\'}\\'}e",
            TemplateTextEscaper.I18nMessage("a@b|c{d}e"),
            StringComparer.Ordinal);

        // 产出中出现的花括号数量 = 每个保留字符各贡献一对，未被二次转义
        var result = TemplateTextEscaper.I18nMessage("@@");
        Assert.Equal("{\\'@\\'}{\\'@\\'}", result, StringComparer.Ordinal);
    }

    /// <summary>
    /// i18n 消息同样折行（消息必须是单行 TS 字面量）。
    /// </summary>
    [Fact]
    public void I18nMessage_ShouldCollapseNewLines()
    {
        Assert.Equal("第一行 第二行", TemplateTextEscaper.I18nMessage(" 第一行\r\n第二行 "), StringComparer.Ordinal);
    }

    /// <summary>
    /// 块注释必须拆开 <c>*/</c>，避免注释提前结束把后续代码露出来。
    /// </summary>
    /// <param name="input">原始文本</param>
    /// <param name="expected">期望的转义结果</param>
    [Theory]
    [InlineData("普通", "普通")]
    [InlineData("a*/b", "a* /b")]
    [InlineData("*/*/", "* /* /")]
    [InlineData("/*", "/*")]
    [InlineData("第一行\n*/", "第一行 * /")]
    public void BlockComment_ShouldSplitTerminator(string input, string expected)
    {
        Assert.Equal(expected, TemplateTextEscaper.BlockComment(input), StringComparer.Ordinal);
    }

    /// <summary>
    /// HTML 注释必须同时拆开 <c>--&gt;</c> 与 <c>--!&gt;</c>，
    /// 且 <c>--!&gt;</c> 规则先行，不被 <c>--&gt;</c> 规则截胡（两者在浏览器里都会关闭注释）。
    /// </summary>
    /// <param name="input">原始文本</param>
    /// <param name="expected">期望的转义结果</param>
    [Theory]
    [InlineData("普通", "普通")]
    [InlineData("a-->b", "a- ->b")]
    [InlineData("a--!>b", "a- -!>b")]
    [InlineData("--!>-->", "- -!>- ->")]
    [InlineData("--", "--")]
    public void HtmlComment_ShouldSplitBothTerminatorsWithBangFirst(string input, string expected)
    {
        Assert.Equal(expected, TemplateTextEscaper.HtmlComment(input), StringComparer.Ordinal);
    }

    /// <summary>
    /// 除 JS 字面量外的所有转义方法对 null / 空串返回空串，不抛异常；
    /// 列注释为空是常态，抛异常会让整批生成失败。
    /// </summary>
    /// <param name="input">空值或空串</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void AllEscapers_NullOrEmptyShouldReturnEmpty(string? input)
    {
        Assert.Equal(string.Empty, TemplateTextEscaper.CSharpString(input), StringComparer.Ordinal);
        Assert.Equal(string.Empty, TemplateTextEscaper.XmlDoc(input), StringComparer.Ordinal);
        Assert.Equal(string.Empty, TemplateTextEscaper.TsString(input), StringComparer.Ordinal);
        Assert.Equal(string.Empty, TemplateTextEscaper.HtmlAttribute(input), StringComparer.Ordinal);
        Assert.Equal(string.Empty, TemplateTextEscaper.I18nMessage(input), StringComparer.Ordinal);
        Assert.Equal(string.Empty, TemplateTextEscaper.BlockComment(input), StringComparer.Ordinal);
        Assert.Equal(string.Empty, TemplateTextEscaper.HtmlComment(input), StringComparer.Ordinal);
    }

    /// <summary>
    /// JS 字面量是唯一的例外：空输入返回空数组字面量，保证产物里是可用的 <c>[]</c> 而不是语法空洞。
    /// </summary>
    /// <param name="input">空值或空白</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void JsLiteral_BlankShouldReturnEmptyArrayLiteral(string? input)
    {
        Assert.Equal("[]", TemplateTextEscaper.JsLiteral(input), StringComparer.Ordinal);
    }

    /// <summary>
    /// JSON 重排为 JS 字面量：字符串用单引号且内容走 TS 转义、合法标识符键去引号、
    /// 对象写成 <c>{ k: v }</c> 带空格、数组元素以 <c>", "</c> 分隔、数字/布尔/null 原样。
    /// </summary>
    /// <param name="json">JSON 文本</param>
    /// <param name="expected">期望的 JS 字面量</param>
    [Theory]
    [InlineData("[]", "[]")]
    [InlineData("{}", "{  }")]
    [InlineData("[1,2,3]", "[1, 2, 3]")]
    [InlineData("[true,false,null]", "[true, false, null]")]
    [InlineData("[\"a\",\"b\"]", "['a', 'b']")]
    [InlineData("{\"label\":\"甲\",\"value\":1}", "{ label: '甲', value: 1 }")]
    [InlineData("{\"_x\":1,\"$y\":2}", "{ _x: 1, $y: 2 }")]
    [InlineData("{\"a-b\":1}", "{ 'a-b': 1 }")]
    [InlineData("{\"2x\":1}", "{ '2x': 1 }")]
    [InlineData("{\"a b\":1}", "{ 'a b': 1 }")]
    [InlineData("[{\"label\":\"启用\",\"value\":1},{\"label\":\"停用\",\"value\":0}]", "[{ label: '启用', value: 1 }, { label: '停用', value: 0 }]")]
    [InlineData("{\"a\":{\"b\":[1,\"c\"]}}", "{ a: { b: [1, 'c'] } }")]
    [InlineData("1.5", "1.5")]
    [InlineData("null", "null")]
    public void JsLiteral_ShouldRewriteJsonAsJsObjectLiteral(string json, string expected)
    {
        Assert.Equal(expected, TemplateTextEscaper.JsLiteral(json), StringComparer.Ordinal);
    }

    /// <summary>
    /// 字符串值内部的单引号与反斜杠必须按 TS 字面量转义，否则常量项会打断产物里的字符串。
    /// </summary>
    [Fact]
    public void JsLiteral_StringValueShouldGoThroughTsEscaping()
    {
        Assert.Equal("['it\\'s']", TemplateTextEscaper.JsLiteral("[\"it's\"]"), StringComparer.Ordinal);
        Assert.Equal("['a\\\\b']", TemplateTextEscaper.JsLiteral("[\"a\\\\b\"]"), StringComparer.Ordinal);
    }

    /// <summary>
    /// 非法 JSON 原样返回——这是当前实现的兜底口径，必须被锁定，
    /// 避免误以为该方法对任意脏字符串都做了转义（见返回结果中的 sourceBugs 记录）。
    /// </summary>
    /// <param name="json">非法 JSON 文本</param>
    [Theory]
    [InlineData("not-json")]
    [InlineData("{bad")]
    [InlineData("[1,")]
    [InlineData("'; alert(1); //")]
    public void JsLiteral_InvalidJsonShouldReturnInputVerbatim(string json)
    {
        Assert.Equal(json, TemplateTextEscaper.JsLiteral(json), StringComparer.Ordinal);
    }

    /// <summary>
    /// JS 标识符判据：首字符必须是字母 / 下划线 / 美元符号，其余为字母数字 / 下划线 / 美元符号；
    /// 不满足的键必须补引号，否则产物里会出现 <c>{ 2x: 1 }</c> 这种语法错误。
    /// </summary>
    /// <param name="key">对象键名</param>
    /// <param name="shouldBeQuoted">是否应当补引号</param>
    [Theory]
    [InlineData("abc", false)]
    [InlineData("_abc", false)]
    [InlineData("$abc", false)]
    [InlineData("a1", false)]
    [InlineData("中文键", false)]
    [InlineData("1a", true)]
    [InlineData("a-b", true)]
    [InlineData("a b", true)]
    [InlineData("a.b", true)]
    [InlineData("", true)]
    public void JsLiteral_ObjectKeyQuotingShouldFollowIdentifierRule(string key, bool shouldBeQuoted)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, int> { [key] = 1 });

        var literal = TemplateTextEscaper.JsLiteral(json);

        Assert.Equal(shouldBeQuoted, literal.Contains($"'{key}':", StringComparison.Ordinal));
    }
}
