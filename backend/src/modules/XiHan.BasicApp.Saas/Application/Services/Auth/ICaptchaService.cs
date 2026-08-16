// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 登录图形验证码服务：签发 SVG 数字码挑战与一次性校验消费
/// </summary>
public interface ICaptchaService
{
    /// <summary>
    /// 密码登录是否要求图形验证码（配置 Saas:Auth:CaptchaEnabled，默认开启）
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// 签发验证码挑战（验证码以一次性数字码存于分布式缓存，消费即销毁）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>验证码挑战（标识 + SVG 图片 + 有效秒数）</returns>
    Task<CaptchaChallengeDto> GenerateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 校验并消费验证码（无论校验成败，读取后即销毁，同一枚码不可重试）
    /// </summary>
    /// <param name="captchaId">验证码标识</param>
    /// <param name="code">用户提交的验证码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否校验通过</returns>
    Task<bool> TryConsumeAsync(string? captchaId, string? code, CancellationToken cancellationToken = default);
}
