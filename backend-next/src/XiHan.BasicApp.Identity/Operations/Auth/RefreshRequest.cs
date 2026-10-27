// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Identity.Operations.Auth;

/// <summary>
/// <c>auth.refresh</c> 的请求：用刷新令牌换新的访问令牌，刷新令牌随之轮换。
/// </summary>
/// <param name="RefreshToken">上次登录或刷新得到的刷新令牌。</param>
public sealed record RefreshRequest([Required, StringLength(128, MinimumLength = 16)] string RefreshToken);
