// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text;
using Microsoft.Extensions.Configuration;
using Moq;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.Framework.Authentication.OneTimeCode;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 登录图形验证码服务测试：签发契约、一次性消费、配置开关与 SVG 渲染确定性。
/// </summary>
public sealed class CaptchaServiceTests
{
    /// <summary>
    /// 签发应以 auth:captcha 用途产生 4 位数字码，并返回标识与 SVG Data URL。
    /// </summary>
    [Fact]
    public async Task Generate_ShouldIssueFourDigitChallenge()
    {
        var otc = new Mock<IOneTimeCodeService>();
        otc
            .Setup(service => service.IssueAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<OneTimeCodeOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OneTimeCodeIssueResult("4821", 300));
        var service = new CaptchaService(otc.Object, CreateConfiguration(enabled: null));

        var challenge = await service.GenerateAsync();

        Assert.Equal(32, challenge.CaptchaId.Length);
        Assert.StartsWith("data:image/svg+xml;base64,", challenge.Image, StringComparison.Ordinal);
        Assert.Equal(300, challenge.ExpiresInSeconds);
        otc.Verify(
            s => s.IssueAsync(
                "auth:captcha",
                challenge.CaptchaId,
                null,
                It.Is<OneTimeCodeOptions>(options => options.CodeLength == 4 && options.ExpiresInSeconds == 300),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 签发的 SVG 应包含验证码的全部数字字符与 SVG 结构。
    /// </summary>
    [Fact]
    public async Task Generate_ShouldRenderCodeIntoImage()
    {
        var otc = new Mock<IOneTimeCodeService>();
        otc
            .Setup(service => service.IssueAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<OneTimeCodeOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OneTimeCodeIssueResult("4821", 300));
        var service = new CaptchaService(otc.Object, CreateConfiguration(enabled: null));

        var challenge = await service.GenerateAsync();
        var svg = DecodeSvg(challenge.Image);

        Assert.StartsWith("<svg", svg, StringComparison.Ordinal);
        Assert.EndsWith("</svg>", svg, StringComparison.Ordinal);
        Assert.Contains(">4<", svg, StringComparison.Ordinal);
        Assert.Contains(">8<", svg, StringComparison.Ordinal);
        Assert.Contains(">2<", svg, StringComparison.Ordinal);
        Assert.Contains(">1<", svg, StringComparison.Ordinal);
    }

    /// <summary>
    /// 空白标识或空白验证码直接拒绝，不触碰一次性码服务。
    /// </summary>
    [Theory]
    [InlineData(null, null)]
    [InlineData("", "1234")]
    [InlineData("captcha-id", "")]
    [InlineData("   ", "1234")]
    public async Task TryConsume_WithBlankInput_ShouldReturnFalseWithoutServiceCall(string? captchaId, string? code)
    {
        var otc = new Mock<IOneTimeCodeService>();
        var service = new CaptchaService(otc.Object, CreateConfiguration(enabled: null));

        var succeeded = await service.TryConsumeAsync(captchaId, code);

        Assert.False(succeeded);
        otc.Verify(
            s => s.TryConsumeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 消费结果委托框架一次性码服务（通过/失败原样透传）。
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TryConsume_ShouldDelegateToOneTimeCodeService(bool serviceResult)
    {
        var otc = new Mock<IOneTimeCodeService>();
        otc
            .Setup(service => service.TryConsumeAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OneTimeCodeConsumeResult(serviceResult, null));
        var service = new CaptchaService(otc.Object, CreateConfiguration(enabled: null));

        var succeeded = await service.TryConsumeAsync("captcha-id", "4821");

        Assert.Equal(serviceResult, succeeded);
        otc.Verify(
            s => s.TryConsumeAsync("auth:captcha", "captcha-id", "4821", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 取消令牌立即抛出。
    /// </summary>
    [Fact]
    public async Task Generate_Cancelled_ShouldThrow()
    {
        var otc = new Mock<IOneTimeCodeService>();
        var service = new CaptchaService(otc.Object, CreateConfiguration(enabled: null));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GenerateAsync(cts.Token));
    }

    /// <summary>
    /// 未配置开关时默认开启（安全默认）。
    /// </summary>
    [Fact]
    public void IsEnabled_ShouldDefaultToTrueWhenConfigMissing()
    {
        var service = new CaptchaService(new Mock<IOneTimeCodeService>().Object, CreateConfiguration(enabled: null));

        Assert.True(service.IsEnabled);
    }

    /// <summary>
    /// 显式配置关闭时关闭。
    /// </summary>
    [Fact]
    public void IsEnabled_ShouldRespectConfiguredValue()
    {
        var disabled = new CaptchaService(new Mock<IOneTimeCodeService>().Object, CreateConfiguration(enabled: false));
        var enabled = new CaptchaService(new Mock<IOneTimeCodeService>().Object, CreateConfiguration(enabled: true));

        Assert.False(disabled.IsEnabled);
        Assert.True(enabled.IsEnabled);
    }

    /// <summary>
    /// SVG 渲染确定性：同一码输出一致，不同码输出不同。
    /// </summary>
    [Fact]
    public void Render_ShouldBeDeterministic()
    {
        var first = CaptchaSvgRenderer.Render("4821");
        var second = CaptchaSvgRenderer.Render("4821");
        var other = CaptchaSvgRenderer.Render("7395");

        Assert.Equal(first, second);
        Assert.NotEqual(first, other);
    }

    /// <summary>
    /// 空白码拒绝渲染。
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Render_WithBlankCode_ShouldThrow(string? code)
    {
        Assert.ThrowsAny<ArgumentException>(() => CaptchaSvgRenderer.Render(code!));
    }

    /// <summary>
    /// 构建内存配置（enabled 为 null 表示不配置该键）。
    /// </summary>
    private static IConfiguration CreateConfiguration(bool? enabled)
    {
        var values = new Dictionary<string, string?>();
        if (enabled.HasValue)
        {
            values["Saas:Auth:CaptchaEnabled"] = enabled.Value.ToString();
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    /// <summary>
    /// 从 Data URL 解出 SVG 文本。
    /// </summary>
    private static string DecodeSvg(string image)
    {
        var base64 = image["data:image/svg+xml;base64,".Length..];
        return Encoding.UTF8.GetString(Convert.FromBase64String(base64));
    }
}
