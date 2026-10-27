// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Identity.Infrastructure;

/// <summary>
/// 一枚已签发的访问令牌及其过期时间。
/// </summary>
/// <param name="Token">紧凑 JWT。</param>
/// <param name="ExpiresAt">过期时间（UTC）。</param>
public sealed record IssuedToken(string Token, DateTimeOffset ExpiresAt);
