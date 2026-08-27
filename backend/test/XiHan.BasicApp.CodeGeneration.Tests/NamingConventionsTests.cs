// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using System.Text.RegularExpressions;
using XiHan.BasicApp.CodeGeneration.Domain.Generation;

namespace XiHan.BasicApp.CodeGeneration.Tests;

/// <summary>
/// 命名转换约定测试。
/// </summary>
/// <remarks>
/// 锁定生成产物的命名口径：后端属性名、前端文件名/路由路径、i18n 键段三者由同一组函数派生，
/// 任一分支漂移都会让前后端产物对不上（属性名与 camelCase JSON 不匹配、Vue 文件落点与手写页不一致、
/// 文案键塌缩）。i18n 键段还承担 fail-closed 职责：前端门禁的孤儿扫描正则只认 <c>[a-z]\w*</c>，
/// 非法键段会静默漏检并在运行期渲染裸键。
/// </remarks>
public sealed class NamingConventionsTests
{
    /// <summary>
    /// i18n 键段的合法形态（与前端门禁扫描正则同口径）。
    /// </summary>
    private static readonly Regex I18nSegmentShape = new("^[a-z][a-z0-9_]*$", RegexOptions.None, TimeSpan.FromSeconds(1));

    /// <summary>
    /// camelCase 转换只把首字母小写、其余字符原样保留，
    /// 保证生成的 TS 字段名与后端 camelCase JSON 序列化结果逐字一致。
    /// </summary>
    /// <param name="input">原始标识符</param>
    /// <param name="expected">期望的 camelCase 结果</param>
    [Theory]
    [InlineData("ProductName", "productName")]
    [InlineData("A", "a")]
    [InlineData("ABC", "aBC")]
    [InlineData("productName", "productName")]
    [InlineData("_Value", "_Value")]
    [InlineData("1Value", "1Value")]
    [InlineData("", "")]
    public void Camelize_ShouldOnlyLowerFirstCharacter(string input, string expected)
    {
        Assert.Equal(expected, NamingConventions.Camelize(input), StringComparer.Ordinal);
    }

    /// <summary>
    /// 空串按原样返回（不是抛异常也不是转成 null），调用方无需做空值分支。
    /// </summary>
    [Fact]
    public void Camelize_EmptyInputShouldReturnSameInstance()
    {
        var empty = string.Empty;

        Assert.Same(empty, NamingConventions.Camelize(empty));
    }

    /// <summary>
    /// kebab-case 必须同时处理连续大写缩写边界与"小写/数字接大写"边界，并把下划线折成连字符后整体小写；
    /// 该结果直接作为生成的 Vue 文件名与路由路径，漂移会让产物落点与手写页不一致。
    /// </summary>
    /// <param name="input">原始标识符</param>
    /// <param name="expected">期望的 kebab-case 结果</param>
    [Theory]
    [InlineData("SysProduct", "sys-product")]
    [InlineData("HTTPServer", "http-server")]
    [InlineData("sysUser2Name", "sys-user2-name")]
    [InlineData("sys_user", "sys-user")]
    [InlineData("Sys_UserName", "sys-user-name")]
    [InlineData("Product", "product")]
    [InlineData("product", "product")]
    [InlineData("ABC", "abc")]
    [InlineData("A", "a")]
    [InlineData("XMLHttpRequest", "xml-http-request")]
    [InlineData("Order2Item", "order2-item")]
    [InlineData("", "")]
    public void Kebabize_ShouldSplitOnAcronymAndWordBoundaries(string input, string expected)
    {
        Assert.Equal(expected, NamingConventions.Kebabize(input), StringComparer.Ordinal);
    }

    /// <summary>
    /// 中文与非 ASCII 字符不参与边界切分，只被整体小写化，
    /// 保证外部库的中文列名不会被切成碎片（其合规化交由 <c>I18nSegment</c> 兜底）。
    /// </summary>
    [Fact]
    public void Kebabize_NonAsciiShouldPassThrough()
    {
        Assert.Equal("产品名称", NamingConventions.Kebabize("产品名称"), StringComparer.Ordinal);
    }

    /// <summary>
    /// PascalCase 转换按下划线/空格/连字符分段并丢弃空段，
    /// 保证外部库表名（sys_user、__a__b 这类脏名字）都能推断出合法类名。
    /// </summary>
    /// <param name="input">原始文本</param>
    /// <param name="expected">期望的 PascalCase 结果</param>
    [Theory]
    [InlineData("sys_user", "SysUser")]
    [InlineData("__a__b", "AB")]
    [InlineData("sys user name", "SysUserName")]
    [InlineData("sys-user", "SysUser")]
    [InlineData("a", "A")]
    [InlineData("Product", "Product")]
    [InlineData("productName", "ProductName")]
    [InlineData("_", "")]
    [InlineData("2fa_code", "2faCode")]
    public void Pascalize_ShouldUpperEachSegmentAndDropEmptySegments(string input, string expected)
    {
        Assert.Equal(expected, NamingConventions.Pascalize(input), StringComparer.Ordinal);
    }

    /// <summary>
    /// 空白输入返回空串而非原值：外部库表名只有空白时不得推断出以空白开头的类名。
    /// </summary>
    /// <param name="input">空白输入</param>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Pascalize_BlankInputShouldReturnEmpty(string input)
    {
        Assert.Equal(string.Empty, NamingConventions.Pascalize(input), StringComparer.Ordinal);
    }

    /// <summary>
    /// snake_case 等价于 kebab-case 后把连字符换成下划线，
    /// 它是 i18n 键段与文案键的基准，两者口径必须一致。
    /// </summary>
    /// <param name="input">原始标识符</param>
    /// <param name="expected">期望的 snake_case 结果</param>
    [Theory]
    [InlineData("SysProduct", "sys_product")]
    [InlineData("HTTPServer", "http_server")]
    [InlineData("sysUser2Name", "sys_user2_name")]
    [InlineData("sys_user", "sys_user")]
    [InlineData("", "")]
    public void Snakeize_ShouldEqualKebabizeWithUnderscore(string input, string expected)
    {
        Assert.Equal(expected, NamingConventions.Snakeize(input), StringComparer.Ordinal);
        Assert.Equal(
            NamingConventions.Kebabize(input).Replace('-', '_'),
            NamingConventions.Snakeize(input),
            StringComparer.Ordinal);
    }

    /// <summary>
    /// 常规英文标识符的 i18n 键段就是其 snake_case 形态，不额外加前缀。
    /// </summary>
    /// <param name="input">原始文本</param>
    /// <param name="expected">期望的键段</param>
    [Theory]
    [InlineData("SysProduct", "sys_product")]
    [InlineData("ProductName", "product_name")]
    [InlineData("product", "product")]
    [InlineData("sys_user", "sys_user")]
    [InlineData("HTTPServer", "http_server")]
    public void I18nSegment_AsciiIdentifierShouldEqualSnakeCase(string input, string expected)
    {
        Assert.Equal(expected, NamingConventions.I18nSegment(input), StringComparer.Ordinal);
    }

    /// <summary>
    /// 首字符不是小写字母时必须补 <c>k_</c> 前缀，否则前端门禁正则匹配不到整条 t() 调用。
    /// </summary>
    /// <param name="input">首字符非小写字母的输入</param>
    /// <param name="expected">期望的键段</param>
    [Theory]
    [InlineData("1abc", "k_1abc")]
    [InlineData("2FA", "k_2_fa")]
    [InlineData("__9__", "k_9")]
    public void I18nSegment_LeadingNonLetterShouldGetKPrefix(string input, string expected)
    {
        Assert.Equal(expected, NamingConventions.I18nSegment(input), StringComparer.Ordinal);
    }

    /// <summary>
    /// 纯中文/纯符号/空输入没有任何可用 ASCII 字符时退到确定性哈希，
    /// 产出 <c>k_</c> + MD5 前 8 位小写十六进制，保证任何输入都能得到合规键段。
    /// </summary>
    /// <param name="input">无可用 ASCII 字符的输入</param>
    [Theory]
    [InlineData("产品名称")]
    [InlineData("！！！")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("😀")]
    public void I18nSegment_NonAsciiShouldFallBackToDeterministicHash(string? input)
    {
        var segment = NamingConventions.I18nSegment(input);

        Assert.Matches(I18nSegmentShape, segment);
        Assert.StartsWith("k_", segment, StringComparison.Ordinal);
        Assert.Equal(10, segment.Length);
        Assert.All(segment[2..], ch => Assert.True(ch is >= '0' and <= '9' or >= 'a' and <= 'f'));
    }

    /// <summary>
    /// null 与空串走同一条哈希分支，键段稳定不抛异常。
    /// </summary>
    [Fact]
    public void I18nSegment_NullShouldBehaveAsEmpty()
    {
        Assert.Equal(NamingConventions.I18nSegment(string.Empty), NamingConventions.I18nSegment(null), StringComparer.Ordinal);
    }

    /// <summary>
    /// 同输入必须确定性（无随机、无时间参与），否则重复生成会产出不同键、前端文案对不上。
    /// </summary>
    /// <param name="input">任意输入</param>
    [Theory]
    [InlineData("产品名称")]
    [InlineData("SysProduct")]
    [InlineData("")]
    [InlineData("混合Mixed名称")]
    public void I18nSegment_ShouldBeDeterministicForSameInput(string input)
    {
        Assert.Equal(NamingConventions.I18nSegment(input), NamingConventions.I18nSegment(input), StringComparer.Ordinal);
    }

    /// <summary>
    /// 不同输入必须产出互异键段，避免两张中文名表的文案键塌缩到一起。
    /// </summary>
    [Fact]
    public void I18nSegment_DifferentInputsShouldProduceDifferentSegments()
    {
        string[] inputs = ["产品", "名称", "订单", "客户", ""];

        var segments = inputs.Select(NamingConventions.I18nSegment).ToArray();

        Assert.Equal(inputs.Length, segments.Distinct(StringComparer.Ordinal).Count());
    }

    /// <summary>
    /// 任意输入（含混合中英文、尾随符号、超长文本）产出的键段都必须满足门禁正则，
    /// 这是本函数 fail-closed 的核心承诺。
    /// </summary>
    /// <param name="input">任意输入</param>
    [Theory]
    [InlineData("SysProduct")]
    [InlineData("产品名称")]
    [InlineData("Mixed混合Name")]
    [InlineData("a-b-c")]
    [InlineData("__leading__")]
    [InlineData("trailing__")]
    [InlineData("2FA")]
    [InlineData("!!!")]
    [InlineData("")]
    public void I18nSegment_ShouldAlwaysMatchGateRegex(string input)
    {
        Assert.Matches(I18nSegmentShape, NamingConventions.I18nSegment(input));
    }

    /// <summary>
    /// 超长输入不截断也不抛异常，仍然产出合规键段。
    /// </summary>
    [Fact]
    public void I18nSegment_VeryLongInputShouldStillBeValid()
    {
        var input = string.Concat(Enumerable.Repeat("SegmentPart", 200));

        var segment = NamingConventions.I18nSegment(input);

        Assert.Matches(I18nSegmentShape, segment);
    }

    /// <summary>
    /// 标识符转英文标签是 en-US 侧文案的唯一素材，必须按词边界拆开并逐词首字母大写。
    /// </summary>
    /// <param name="input">原始标识符</param>
    /// <param name="expected">期望的英文标签</param>
    [Theory]
    [InlineData("ProductName", "Product Name")]
    [InlineData("sys_user", "Sys User")]
    [InlineData("HTTPServer", "Http Server")]
    [InlineData("Product", "Product")]
    [InlineData("sysUser2Name", "Sys User2 Name")]
    public void HumanizeIdentifier_ShouldProduceSpaceSeparatedLabel(string input, string expected)
    {
        Assert.Equal(expected, NamingConventions.HumanizeIdentifier(input), StringComparer.Ordinal);
    }

    /// <summary>
    /// 空输入返回空串而非 null，避免产物里出现 <c>label: null</c>。
    /// </summary>
    /// <param name="input">空输入</param>
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void HumanizeIdentifier_EmptyInputShouldReturnEmpty(string? input)
    {
        Assert.Equal(string.Empty, NamingConventions.HumanizeIdentifier(input), StringComparer.Ordinal);
    }

    /// <summary>
    /// 所有大小写转换必须用不变区域，土耳其语环境下 I 不得被折成点上无点的 ı，
    /// 否则同一份表结构在不同服务器上会生成出不同的文件名与文案键。
    /// </summary>
    [Fact]
    public void AllConversions_ShouldBeCultureInvariant()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");

            Assert.Equal("id", NamingConventions.Camelize("Id"), StringComparer.Ordinal);
            Assert.Equal("item-id", NamingConventions.Kebabize("ItemId"), StringComparer.Ordinal);
            Assert.Equal("item_id", NamingConventions.Snakeize("ItemId"), StringComparer.Ordinal);
            Assert.Equal("ItemId", NamingConventions.Pascalize("item_id"), StringComparer.Ordinal);
            Assert.Equal("item_id", NamingConventions.I18nSegment("ItemId"), StringComparer.Ordinal);
            Assert.Equal("Item Id", NamingConventions.HumanizeIdentifier("ItemId"), StringComparer.Ordinal);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
