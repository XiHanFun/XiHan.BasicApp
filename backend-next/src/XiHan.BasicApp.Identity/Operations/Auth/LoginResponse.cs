// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Identity.Domain.Entities;

namespace XiHan.BasicApp.Identity.Operations.Auth;

/// <summary>
/// <c>auth.login</c> 的响应。
/// </summary>
/// <remarks>
/// 要么直接拿到令牌，要么账号开了双因素、拿到第二步的挑战（二者恰有一个）。
/// </remarks>
/// <param name="Token">令牌对与会话；需要第二步时为 <see langword="null"/>。</param>
/// <param name="TwoFactor">第二步的挑战；不需要时为 <see langword="null"/>。</param>
public sealed record LoginResponse(TokenResponse? Token, TwoFactorChallengeResponse? TwoFactor);

/// <summary>
/// 两步验证的挑战。
/// </summary>
/// <remarks>
/// 凭票据调 <c>auth.verify-two-factor</c>，不必再输密码；票据过期或输错次数用完须重新登录。
/// </remarks>
/// <param name="Ticket">两步验证票据（只出现这一次）。</param>
/// <param name="Methods">可用的方式。</param>
/// <param name="Method">只有一种方式时就是它；有多种时为 <see langword="null"/>，由用户选。</param>
/// <param name="CodeSent">是否已经发出验证码（邮箱、短信方式；认证器为否）。</param>
/// <param name="ExpiresAt">票据过期时间（UTC）。</param>
public sealed record TwoFactorChallengeResponse(string Ticket, IReadOnlyList<TwoFactorMethod> Methods, TwoFactorMethod? Method, bool CodeSent, DateTimeOffset ExpiresAt);
