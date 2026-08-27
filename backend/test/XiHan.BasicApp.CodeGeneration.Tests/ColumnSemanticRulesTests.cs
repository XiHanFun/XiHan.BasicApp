// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.CodeGeneration.Domain.Enums;
using XiHan.BasicApp.CodeGeneration.Infrastructure.Inference;

namespace XiHan.BasicApp.CodeGeneration.Tests;

/// <summary>
/// 列名语义规则测试。
/// </summary>
/// <remarks>
/// 规则表顺序敏感、首个命中即止，且关键字是"子串包含"而非词边界匹配。
/// 这两点决定了 <c>icon_url</c> 命中图片上传而不是被后面的规则接管，
/// 也决定了 <c>profile_name</c> 会因为含 <c>file</c> 而被判成文件上传——
/// 后者是当前实现的真实行为，写成回归用例是为了防止后续改成词边界匹配却无人察觉。
/// </remarks>
public sealed class ColumnSemanticRulesTests
{
    /// <summary>
    /// 第一组关键字（图片语义）命中图片上传控件。
    /// </summary>
    /// <param name="columnName">列名</param>
    [Theory]
    [InlineData("icon")]
    [InlineData("img")]
    [InlineData("image")]
    [InlineData("avatar")]
    [InlineData("logo")]
    [InlineData("photo")]
    [InlineData("cover")]
    [InlineData("banner")]
    [InlineData("ProfileIcon")]
    [InlineData("PROFILE_ICON")]
    [InlineData("head_img_url")]
    public void Infer_ImageKeywordsShouldReturnImageUpload(string columnName)
    {
        Assert.Equal(HtmlType.ImageUpload, ColumnSemanticRules.Infer(columnName, null));
    }

    /// <summary>
    /// 第二组关键字（pic/picture）同样命中图片上传。
    /// </summary>
    /// <param name="columnName">列名</param>
    [Theory]
    [InlineData("pic")]
    [InlineData("picture")]
    [InlineData("MainPicUrl")]
    public void Infer_PictureKeywordsShouldReturnImageUpload(string columnName)
    {
        Assert.Equal(HtmlType.ImageUpload, ColumnSemanticRules.Infer(columnName, null));
    }

    /// <summary>
    /// 第三组关键字（附件语义）命中文件上传控件。
    /// </summary>
    /// <param name="columnName">列名</param>
    [Theory]
    [InlineData("file")]
    [InlineData("attachment")]
    [InlineData("annex")]
    [InlineData("FileName")]
    [InlineData("ATTACHMENT_PATH")]
    public void Infer_FileKeywordsShouldReturnFileUpload(string columnName)
    {
        Assert.Equal(HtmlType.FileUpload, ColumnSemanticRules.Infer(columnName, null));
    }

    /// <summary>
    /// 第四组关键字（颜色语义）保持普通文本框（当前未接入取色控件）。
    /// </summary>
    /// <param name="columnName">列名</param>
    [Theory]
    [InlineData("color")]
    [InlineData("colour")]
    [InlineData("ThemeColor")]
    public void Infer_ColorKeywordsShouldReturnInput(string columnName)
    {
        Assert.Equal(HtmlType.Input, ColumnSemanticRules.Infer(columnName, null));
    }

    /// <summary>
    /// 第五组关键字（长文本语义）命中文本域。
    /// </summary>
    /// <param name="columnName">列名</param>
    [Theory]
    [InlineData("content")]
    [InlineData("description")]
    [InlineData("remark")]
    [InlineData("detail")]
    [InlineData("intro")]
    [InlineData("summary")]
    [InlineData("note")]
    [InlineData("ProductDescription")]
    [InlineData("REMARK")]
    public void Infer_LongTextKeywordsShouldReturnTextarea(string columnName)
    {
        Assert.Equal(HtmlType.Textarea, ColumnSemanticRules.Infer(columnName, null));
    }

    /// <summary>
    /// 优先级冲突用例：源码注释显式点名的 <c>icon_url</c> 必须命中图片而不是被后面的规则接管；
    /// 同时含高低两组关键字时一律取靠前的那组。
    /// </summary>
    /// <param name="columnName">同时含多组关键字的列名</param>
    /// <param name="expected">期望命中的控件（靠前规则）</param>
    [Theory]
    [InlineData("icon_url", HtmlType.ImageUpload)]
    [InlineData("icon_file", HtmlType.ImageUpload)]
    [InlineData("image_attachment", HtmlType.ImageUpload)]
    [InlineData("icon_color", HtmlType.ImageUpload)]
    [InlineData("logo_description", HtmlType.ImageUpload)]
    [InlineData("picture_note", HtmlType.ImageUpload)]
    [InlineData("file_remark", HtmlType.FileUpload)]
    [InlineData("file_color", HtmlType.FileUpload)]
    [InlineData("color_detail", HtmlType.Input)]
    public void Infer_FirstMatchingRuleShouldWin(string columnName, HtmlType expected)
    {
        Assert.Equal(expected, ColumnSemanticRules.Infer(columnName, null));
    }

    /// <summary>
    /// 关键字是子串包含匹配，<c>profile</c> 中含 <c>file</c> 会被判成文件上传。
    /// 这是当前实现的真实行为，若将来改成词边界匹配，必须显式修改本用例而不是让它悄悄漂移。
    /// </summary>
    /// <param name="columnName">看起来不该命中却因子串而命中的列名</param>
    /// <param name="expected">当前实现的真实结果</param>
    [Theory]
    [InlineData("profile_name", HtmlType.FileUpload)]
    [InlineData("UserProfile", HtmlType.FileUpload)]
    [InlineData("discover_flag", HtmlType.ImageUpload)]
    [InlineData("recover_time", HtmlType.ImageUpload)]
    public void Infer_SubstringMatchingIsTheCurrentContract(string columnName, HtmlType expected)
    {
        Assert.Equal(expected, ColumnSemanticRules.Infer(columnName, null));
    }

    /// <summary>
    /// 未命中关键字时按长度兜底：严格大于 500 才转文本域，边界 500 必须返回 null。
    /// </summary>
    /// <param name="length">列长度</param>
    /// <param name="expected">期望结果（null 表示沿用类型映射默认）</param>
    [Theory]
    [InlineData(null, null)]
    [InlineData(1, null)]
    [InlineData(499, null)]
    [InlineData(500, null)]
    [InlineData(501, HtmlType.Textarea)]
    [InlineData(4000, HtmlType.Textarea)]
    public void Infer_LengthFallbackShouldUseStrictGreaterThan500(int? length, HtmlType? expected)
    {
        Assert.Equal(expected, ColumnSemanticRules.Infer("PlainBusinessValue", length));
    }

    /// <summary>
    /// 关键字命中优先于长度兜底：命中颜色规则的超长列仍是文本框而不是文本域。
    /// </summary>
    [Fact]
    public void Infer_KeywordShouldTakePrecedenceOverLengthFallback()
    {
        Assert.Equal(HtmlType.Input, ColumnSemanticRules.Infer("ThemeColor", 4000));
    }

    /// <summary>
    /// 未命中任何关键字且长度不超阈值时返回 null，让类型映射的默认控件继续生效。
    /// </summary>
    /// <param name="columnName">无语义列名</param>
    [Theory]
    [InlineData("Sort")]
    [InlineData("Status")]
    [InlineData("ProductName")]
    [InlineData("Amount")]
    public void Infer_NoKeywordShouldReturnNull(string columnName)
    {
        Assert.Null(ColumnSemanticRules.Infer(columnName, 200));
    }

    /// <summary>
    /// 列名为空或纯空白直接返回 null 且不抛异常（外部库可能给出空列名）。
    /// </summary>
    /// <param name="columnName">空白列名</param>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Infer_BlankColumnNameShouldReturnNull(string columnName)
    {
        Assert.Null(ColumnSemanticRules.Infer(columnName, 9999));
    }

    /// <summary>
    /// null 列名同样安全降级为 null，不抛异常。
    /// </summary>
    [Fact]
    public void Infer_NullColumnNameShouldReturnNull()
    {
        Assert.Null(ColumnSemanticRules.Infer(null!, 9999));
    }
}
