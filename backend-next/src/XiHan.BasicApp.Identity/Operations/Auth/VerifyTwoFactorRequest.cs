// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Identity.Domain.Entities;

namespace XiHan.BasicApp.Identity.Operations.Auth;

/// <summary>
/// <c>auth.verify-two-factor</c> 的请求：凭 <c>auth.login</c> 发的票据提交第二步验证码。
/// </summary>
/// <param name="Ticket">两步验证票据。</param>
/// <param name="Method">验证方式。</param>
/// <param name="Code">验证码。</param>
public sealed record VerifyTwoFactorRequest(
    [Required, StringLength(128, MinimumLength = 1)] string Ticket,
    TwoFactorMethod Method,
    [Required, StringLength(16, MinimumLength = 1)] string Code);
