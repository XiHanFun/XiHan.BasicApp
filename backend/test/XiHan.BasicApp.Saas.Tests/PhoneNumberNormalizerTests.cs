// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.DomainServices;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 手机号码正规化单元测试。
/// </summary>
/// <remarks>
/// 号码是登录身份标识：存进库的写法与登录时提交的写法必须一致，否则用户永远登录不上，
/// 而且不会有任何报错。这里锁定「任何入口最终都得到同一个 E.164 字符串」。
/// </remarks>
public sealed class PhoneNumberNormalizerTests
{
    private readonly PhoneNumberNormalizer _normalizer = new();

    [Theory]
    [InlineData("+886912345678", null, "+886912345678")]
    [InlineData("0912345678", "TW", "+886912345678")]
    [InlineData("886912345678", "TW", "+886912345678")]
    [InlineData("09 1234-5678", "TW", "+886912345678")]
    [InlineData("13800138000", "CN", "+8613800138000")]
    [InlineData("+49 151 12345678", null, "+4915112345678")]
    public void NormalizeOrThrow_ShouldProduceE164(string raw, string? region, string expected)
    {
        Assert.Equal(expected, _normalizer.NormalizeOrThrow(raw, region));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizeOrThrow_ShouldReturnNullForBlank(string? raw)
    {
        Assert.Null(_normalizer.NormalizeOrThrow(raw, "TW"));
    }

    [Theory]
    // 号码在该国家不成立：台湾手机是 09 开头十位
    [InlineData("0912345", "TW")]
    [InlineData("12345678901234567890", "TW")]
    [InlineData("abcdefg", "TW")]
    // 没有国码也没有默认地区，无法判断归属
    [InlineData("0912345678", null)]
    public void NormalizeOrThrow_ShouldThrowForInvalid(string raw, string? region)
    {
        var exception = Assert.Throws<InvalidOperationException>(() => _normalizer.NormalizeOrThrow(raw, region));
        Assert.Equal("手机号码格式无效。", exception.Message);
    }

    [Fact]
    public void TryNormalize_ShouldReportFailureWithoutThrowing()
    {
        Assert.False(_normalizer.TryNormalize("0912345", "TW", out var normalized));
        Assert.Equal(string.Empty, normalized);

        Assert.True(_normalizer.TryNormalize("0912345678", "TW", out var ok));
        Assert.Equal("+886912345678", ok);
    }
}
