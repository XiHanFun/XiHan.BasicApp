// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Identity.Operations.Users;

namespace XiHan.BasicApp.Identity.Operations.Auth;

/// <summary>
/// 登录与刷新的响应：访问令牌（HS256 JWT）、不透明的刷新令牌与会话信息。
/// </summary>
/// <param name="AccessToken">访问令牌，放 <c>Authorization: Bearer</c>。</param>
/// <param name="AccessExpiresAt">访问令牌过期时间（UTC）。</param>
/// <param name="RefreshToken">刷新令牌；每次刷新轮换，旧的立即失效。</param>
/// <param name="RefreshExpiresAt">刷新令牌（会话）过期时间（UTC）。</param>
/// <param name="SessionId">会话 ID（令牌的 <c>sid</c>）。</param>
/// <param name="TenantId">当前租户；平台账号为 <see langword="null"/>。</param>
/// <param name="MustChangePassword">是否必须先改密。</param>
/// <param name="User">用户资料。</param>
public sealed record TokenResponse(
    string AccessToken,
    DateTimeOffset AccessExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshExpiresAt,
    Guid SessionId,
    string? TenantId,
    bool MustChangePassword,
    UserResponse User);
