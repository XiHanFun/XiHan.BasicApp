// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Identity.Domain.Services;

namespace XiHan.BasicApp.Identity.Operations.Profile;

/// <summary>
/// <c>profile.send-verification-code</c> 的请求。
/// </summary>
/// <param name="Channel">验证邮箱还是手机号。</param>
public sealed record SendVerificationCodeRequest(ContactChannel Channel);

/// <summary>
/// 带验证码的联系方式请求（验证当前的、确认换绑）。
/// </summary>
/// <param name="Channel">邮箱还是手机号。</param>
/// <param name="Code">收到的验证码。</param>
public sealed record ContactCodeRequest(ContactChannel Channel, [Required, StringLength(10, MinimumLength = 4)] string Code);

/// <summary>
/// <c>profile.send-change-contact-code</c> 的请求。
/// </summary>
/// <param name="Channel">换邮箱还是手机号。</param>
/// <param name="NewValue">要换成的邮箱或手机号（码发到这里）。</param>
/// <param name="Password">当前密码。</param>
public sealed record SendChangeContactCodeRequest(ContactChannel Channel, [Required, StringLength(200, MinimumLength = 1)] string NewValue, [Required, StringLength(128, MinimumLength = 1)] string Password);

/// <summary>
/// 验证码已发出。
/// </summary>
/// <param name="ExpiresInSeconds">验证码的有效秒数。</param>
public sealed record CodeSentResponse(int ExpiresInSeconds);
