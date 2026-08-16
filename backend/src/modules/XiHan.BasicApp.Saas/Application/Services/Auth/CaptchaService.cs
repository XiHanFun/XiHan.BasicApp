// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text;
using Microsoft.Extensions.Configuration;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Authentication.OneTimeCode;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 登录图形验证码服务实现
/// </summary>
/// <remarks>
/// 复用框架 <see cref="IOneTimeCodeService"/>（加密安全随机数、分布式缓存后端、消费即销毁、恒定时间比较）：
/// 验证码明文只存在于签发瞬间，由本服务绘制进 SVG 后不再保留；Redis 侧仅存哈希形态的待消费状态。
/// </remarks>
public sealed class CaptchaService : ICaptchaService
{
    private const string Purpose = "auth:captcha";

    private const int CodeLength = 4;

    private const int ExpiresInSeconds = 300;

    private const string CaptchaEnabledConfigKey = "Saas:Auth:CaptchaEnabled";

    private readonly IOneTimeCodeService _oneTimeCodeService;

    private readonly IConfiguration _configuration;

    /// <summary>
    /// 构造函数
    /// </summary>
    public CaptchaService(IOneTimeCodeService oneTimeCodeService, IConfiguration configuration)
    {
        _oneTimeCodeService = oneTimeCodeService;
        _configuration = configuration;
    }

    /// <summary>
    /// 密码登录是否要求图形验证码（默认开启）
    /// </summary>
    public bool IsEnabled => _configuration.GetValue(CaptchaEnabledConfigKey, true);

    /// <summary>
    /// 签发验证码挑战
    /// </summary>
    public async Task<CaptchaChallengeDto> GenerateAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var captchaId = Guid.NewGuid().ToString("N");
        var result = await _oneTimeCodeService.IssueAsync(
            Purpose,
            captchaId,
            payload: null,
            new OneTimeCodeOptions { CodeLength = CodeLength, ExpiresInSeconds = ExpiresInSeconds },
            cancellationToken);

        var svg = CaptchaSvgRenderer.Render(result.Code);
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(svg));
        return new CaptchaChallengeDto
        {
            CaptchaId = captchaId,
            Image = $"data:image/svg+xml;base64,{base64}",
            ExpiresInSeconds = result.ExpiresInSeconds
        };
    }

    /// <summary>
    /// 校验并消费验证码（读取后即销毁，失败不可重试同一枚码）
    /// </summary>
    public async Task<bool> TryConsumeAsync(string? captchaId, string? code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(captchaId) || string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var result = await _oneTimeCodeService.TryConsumeAsync(Purpose, captchaId, code, cancellationToken);
        return result.Succeeded;
    }
}
