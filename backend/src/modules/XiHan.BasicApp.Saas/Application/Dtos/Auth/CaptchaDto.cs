// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 登录图形验证码挑战 DTO
/// </summary>
public sealed class CaptchaChallengeDto
{
    /// <summary>
    /// 验证码标识（登录时随验证码一并提交，用于定位待消费的一次性验证码）
    /// </summary>
    public string CaptchaId { get; set; } = string.Empty;

    /// <summary>
    /// 验证码图片（SVG Data URL，前端直接作为 img src 展示）
    /// </summary>
    public string Image { get; set; } = string.Empty;

    /// <summary>
    /// 有效秒数
    /// </summary>
    public int ExpiresInSeconds { get; set; }
}
