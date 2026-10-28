// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Identity.Operations.Auth;

/// <summary>
/// <c>auth.login</c> 的请求：密码登录。租户为空即平台账号；开了图形验证码（<c>auth.login-config</c> 的 <c>captchaEnabled</c>）时须带上 <c>auth.captcha</c> 发的标识与码。
/// </summary>
/// <param name="UserName">登录名。</param>
/// <param name="Password">密码。</param>
/// <param name="Tenant">注册地租户 ID；平台账号不填。</param>
/// <param name="CaptchaId">图形验证码标识。</param>
/// <param name="CaptchaCode">图形验证码。</param>
public sealed record LoginRequest(
    [Required, StringLength(64, MinimumLength = 1)] string UserName,
    [Required, StringLength(128, MinimumLength = 1)] string Password,
    [RegularExpression("^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$")] string? Tenant = null,
    [StringLength(64)] string? CaptchaId = null,
    [StringLength(16)] string? CaptchaCode = null);
