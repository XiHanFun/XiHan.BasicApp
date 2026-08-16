// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 字段脱敏器测试：各脱敏策略的输出契约与边界行为。
/// </summary>
public sealed class FieldMaskerTests
{
    /// <summary>
    /// 可读且不脱敏时原样返回。
    /// </summary>
    [Fact]
    public void Mask_ReadableWithoutStrategy_ShouldReturnRaw()
    {
        var masked = FieldMasker.Mask("13812345678", isReadable: true, FieldMaskStrategy.None, pattern: null);

        Assert.Equal("13812345678", masked);
    }

    /// <summary>
    /// 不可读但未指定策略时默认完全隐藏。
    /// </summary>
    [Fact]
    public void Mask_UnreadableWithoutStrategy_ShouldHide()
    {
        var masked = FieldMasker.Mask("secret", isReadable: false, FieldMaskStrategy.None, pattern: null);

        Assert.Null(masked);
    }

    /// <summary>
    /// 完全隐藏策略返回 null（从响应中移除字段）。
    /// </summary>
    [Fact]
    public void Mask_HiddenStrategy_ShouldReturnNull()
    {
        var masked = FieldMasker.Mask("secret", isReadable: true, FieldMaskStrategy.Hidden, pattern: null);

        Assert.Null(masked);
    }

    /// <summary>
    /// 空值输入直接返回原空值（null 保持 null）。
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Mask_EmptyRaw_ShouldReturnRaw(string? raw)
    {
        var masked = FieldMasker.Mask(raw, isReadable: true, FieldMaskStrategy.FullMask, pattern: null);

        Assert.Equal(raw, masked);
    }

    /// <summary>
    /// 全部星号策略输出与原文等长的星号串。
    /// </summary>
    [Fact]
    public void Mask_FullMask_ShouldReturnSameLengthStars()
    {
        var masked = FieldMasker.Mask("123456", isReadable: true, FieldMaskStrategy.FullMask, pattern: null);

        Assert.Equal("******", masked);
    }

    /// <summary>
    /// 部分脱敏保留首 N 尾 M（手机号经典形态 138****5678）。
    /// </summary>
    [Fact]
    public void Mask_PartialMask_WithKeepPattern_ShouldKeepEdges()
    {
        var masked = FieldMasker.Mask("13812345678", isReadable: true, FieldMaskStrategy.PartialMask, pattern: "keep:3,4");

        Assert.Equal("138****5678", masked);
    }

    /// <summary>
    /// 部分脱敏未给模式时默认保留后 4 位。
    /// </summary>
    [Fact]
    public void Mask_PartialMask_WithoutPattern_ShouldKeepLastFour()
    {
        var masked = FieldMasker.Mask("13812345678", isReadable: true, FieldMaskStrategy.PartialMask, pattern: null);

        Assert.Equal("*******5678", masked);
    }

    /// <summary>
    /// 保留位数覆盖全长时退化为全星号（避免泄漏任何原文）。
    /// </summary>
    [Fact]
    public void Mask_PartialMask_WhenKeepCoversLength_ShouldReturnAllStars()
    {
        var masked = FieldMasker.Mask("1234567890", isReadable: true, FieldMaskStrategy.PartialMask, pattern: "keep:7,7");

        Assert.Equal("**********", masked);
    }

    /// <summary>
    /// 保留位数为负时应钳制为 0 而非抛异常或负索引。
    /// </summary>
    [Fact]
    public void Mask_PartialMask_WithNegativeKeep_ShouldClampToZero()
    {
        var masked = FieldMasker.Mask("13812345678", isReadable: true, FieldMaskStrategy.PartialMask, pattern: "keep:-1,4");

        Assert.Equal("*******5678", masked);
    }

    /// <summary>
    /// 哈希策略返回 SHA256 前 16 位十六进制小写（可验证的确定值）。
    /// </summary>
    [Fact]
    public void Mask_Hash_ShouldReturnSha256PrefixLowercase()
    {
        // SHA256("abc") = ba7816bf8f01cfea414140de5dae2223...
        var masked = FieldMasker.Mask("abc", isReadable: true, FieldMaskStrategy.Hash, pattern: null);

        Assert.Equal("ba7816bf8f01cfea", masked);
    }

    /// <summary>
    /// 哈希策略对相同输入必须确定性输出。
    /// </summary>
    [Fact]
    public void Mask_Hash_ShouldBeDeterministic()
    {
        var first = FieldMasker.Mask("same-input", isReadable: true, FieldMaskStrategy.Hash, pattern: null);
        var second = FieldMasker.Mask("same-input", isReadable: true, FieldMaskStrategy.Hash, pattern: null);

        Assert.Equal(first, second);
    }

    /// <summary>
    /// 固定替换默认占位符为 [已脱敏]。
    /// </summary>
    [Fact]
    public void Mask_Redact_ShouldUseDefaultPlaceholder()
    {
        var masked = FieldMasker.Mask("secret", isReadable: true, FieldMaskStrategy.Redact, pattern: null);

        Assert.Equal("[已脱敏]", masked);
    }

    /// <summary>
    /// 固定替换支持自定义占位符。
    /// </summary>
    [Fact]
    public void Mask_Redact_WithCustomPattern_ShouldUsePattern()
    {
        var masked = FieldMasker.Mask("secret", isReadable: true, FieldMaskStrategy.Redact, pattern: "***");

        Assert.Equal("***", masked);
    }

    /// <summary>
    /// 自定义策略使用模式作为输出。
    /// </summary>
    [Fact]
    public void Mask_Custom_WithPattern_ShouldUsePattern()
    {
        var masked = FieldMasker.Mask("secret", isReadable: true, FieldMaskStrategy.Custom, pattern: "MASKED");

        Assert.Equal("MASKED", masked);
    }

    /// <summary>
    /// 自定义策略无模式时退化为全星号。
    /// </summary>
    [Fact]
    public void Mask_Custom_WithoutPattern_ShouldFallBackToStars()
    {
        var masked = FieldMasker.Mask("secret", isReadable: true, FieldMaskStrategy.Custom, pattern: null);

        Assert.Equal("******", masked);
    }

    /// <summary>
    /// 可读但配置了脱敏策略时仍应脱敏（可读脱敏值场景，如客服可见 138****5678）。
    /// </summary>
    [Fact]
    public void Mask_ReadableWithMaskStrategy_ShouldStillMask()
    {
        var masked = FieldMasker.Mask("13812345678", isReadable: true, FieldMaskStrategy.PartialMask, pattern: "keep:3,4");

        Assert.Equal("138****5678", masked);
    }

    /// <summary>
    /// 未知策略按原值返回（前向兼容兜底，不抛异常）。
    /// </summary>
    [Fact]
    public void Mask_UnknownStrategy_ShouldReturnRaw()
    {
        var masked = FieldMasker.Mask("value", isReadable: true, (FieldMaskStrategy)123, pattern: null);

        Assert.Equal("value", masked);
    }
}
