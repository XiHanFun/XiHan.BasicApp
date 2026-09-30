// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 文本字段脱敏测试：每种读取方式的输出与边界。
/// </summary>
public sealed class FieldMaskerTests
{
    /// <summary>
    /// 明文原样返回。
    /// </summary>
    [Fact]
    public void Mask_None_ShouldReturnRaw()
    {
        Assert.Equal("13812345678", FieldMasker.Mask("13812345678", Rule(FieldMaskStrategy.None)));
    }

    /// <summary>
    /// 隐藏一律返回空，空串也不例外。
    /// </summary>
    [Theory]
    [InlineData("13812345678")]
    [InlineData("")]
    public void Mask_Hidden_ShouldReturnNull(string raw)
    {
        Assert.Null(FieldMasker.Mask(raw, Rule(FieldMaskStrategy.Hidden)));
    }

    /// <summary>
    /// 空值没什么可打码的，原样返回（隐藏除外）。
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Mask_EmptyRaw_ShouldReturnRaw(string? raw)
    {
        Assert.Equal(raw, FieldMasker.Mask(raw, Rule(FieldMaskStrategy.FullMask)));
    }

    /// <summary>
    /// 全部星号保留长度。
    /// </summary>
    [Fact]
    public void Mask_FullMask_ShouldReturnSameLengthStars()
    {
        Assert.Equal("******", FieldMasker.Mask("secret", Rule(FieldMaskStrategy.FullMask)));
    }

    /// <summary>
    /// 部分脱敏保留前几位、后几位。
    /// </summary>
    [Fact]
    public void Mask_PartialMask_ShouldKeepHeadAndTail()
    {
        Assert.Equal("138****5678", FieldMasker.Mask("13812345678", Rule(FieldMaskStrategy.PartialMask, keepHead: 3, keepTail: 4)));
    }

    /// <summary>
    /// 只保留一端也可以。
    /// </summary>
    [Fact]
    public void Mask_PartialMask_TailOnly_ShouldKeepTail()
    {
        Assert.Equal("*******5678", FieldMasker.Mask("13812345678", Rule(FieldMaskStrategy.PartialMask, keepHead: 0, keepTail: 4)));
    }

    /// <summary>
    /// 保留位数不小于原长时整体打码，不能原样漏出。
    /// </summary>
    [Fact]
    public void Mask_PartialMask_WhenKeepCoversLength_ShouldReturnAllStars()
    {
        Assert.Equal("*****", FieldMasker.Mask("abcde", Rule(FieldMaskStrategy.PartialMask, keepHead: 3, keepTail: 2)));
    }

    /// <summary>
    /// 哈希为 SHA-256 前 16 位小写十六进制，同一原值结果相同。
    /// </summary>
    [Fact]
    public void Mask_Hash_ShouldBeDeterministicLowercasePrefix()
    {
        var first = FieldMasker.Mask("alice@example.com", Rule(FieldMaskStrategy.Hash));
        var second = FieldMasker.Mask("alice@example.com", Rule(FieldMaskStrategy.Hash));

        Assert.NotNull(first);
        Assert.Equal(16, first.Length);
        Assert.Matches("^[0-9a-f]{16}$", first);
        Assert.Equal(first, second);
        Assert.NotEqual(first, FieldMasker.Mask("bob@example.com", Rule(FieldMaskStrategy.Hash)));
    }

    /// <summary>
    /// 固定文本一律显示规则里的文字。
    /// </summary>
    [Fact]
    public void Mask_Redact_ShouldReturnReplacement()
    {
        Assert.Equal("[保密]", FieldMasker.Mask("13812345678", Rule(FieldMaskStrategy.Redact, replacement: "[保密]")));
    }

    /// <summary>
    /// 固定文本规则缺文字是坏数据，报错而不是悄悄放出原值。
    /// </summary>
    [Fact]
    public void Mask_Redact_WithoutReplacement_ShouldThrow()
    {
        _ = Assert.Throws<InvalidOperationException>(() => FieldMasker.Mask("13812345678", Rule(FieldMaskStrategy.Redact)));
    }

    /// <summary>
    /// 未定义的读取方式报错。
    /// </summary>
    [Fact]
    public void Mask_UnknownStrategy_ShouldThrow()
    {
        _ = Assert.Throws<InvalidOperationException>(() => FieldMasker.Mask("value", Rule((FieldMaskStrategy)99)));
    }

    private static EffectiveFieldRule Rule(FieldMaskStrategy strategy, int? keepHead = null, int? keepTail = null, string? replacement = null)
    {
        return new EffectiveFieldRule
        {
            FieldName = "Phone",
            MaskStrategy = strategy,
            MaskKeepHead = keepHead,
            MaskKeepTail = keepTail,
            MaskReplacement = replacement
        };
    }
}
