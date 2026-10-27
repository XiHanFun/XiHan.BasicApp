// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Identity.Operations.Auth;

/// <summary>
/// <c>auth.logout</c> 的响应。
/// </summary>
/// <param name="SessionId">被吊销的会话。</param>
/// <param name="RevokedAt">吊销时间（UTC）；会话早已吊销时是原时间。</param>
public sealed record LogoutResponse(Guid SessionId, DateTimeOffset RevokedAt);
