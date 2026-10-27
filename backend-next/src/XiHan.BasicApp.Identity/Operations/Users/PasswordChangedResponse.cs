// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Identity.Operations.Users;

/// <summary>
/// 改密 / 重置密码的响应。
/// </summary>
/// <param name="UserId">用户 ID。</param>
/// <param name="ChangedAt">生效时间（UTC）。</param>
/// <param name="SessionsRevoked">是否吊销了既有会话。</param>
public sealed record PasswordChangedResponse(Guid UserId, DateTimeOffset ChangedAt, bool SessionsRevoked);
