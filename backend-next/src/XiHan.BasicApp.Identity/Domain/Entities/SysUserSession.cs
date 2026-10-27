// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Identity.Domain.Entities;

/// <summary>
/// 登录会话（平台库，表 <c>sys_user_session</c>）：会话 ID 即访问令牌的 <c>sid</c> 声明，刷新令牌不透明、只存散列。吊销即置 <see cref="RevokedAt"/>，由会话门禁在 Authenticate 阶段拒绝。
/// </summary>
public sealed class SysUserSession : AppAuditedEntity
{
    /// <summary>
    /// 用户 ID。
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 本会话的当前租户；平台账号为 <see langword="null"/>。
    /// </summary>
    public string? ActiveTenantId { get; set; }

    /// <summary>
    /// 刷新令牌的 SHA-256 散列（十六进制）。
    /// </summary>
    public string RefreshTokenHash { get; set; } = string.Empty;

    /// <summary>
    /// 签发时间。
    /// </summary>
    public DateTimeOffset IssuedAt { get; set; }

    /// <summary>
    /// 刷新令牌（会话）过期时间；之后必须重新登录。
    /// </summary>
    public DateTimeOffset RefreshExpiresAt { get; set; }

    /// <summary>
    /// 最近一次刷新时间。
    /// </summary>
    public DateTimeOffset? LastSeenAt { get; set; }

    /// <summary>
    /// 吊销时间；为 <see langword="null"/> 即有效。
    /// </summary>
    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>
    /// 吊销原因（<c>logout</c>、<c>password-changed</c>、<c>locked</c>、<c>revoked</c>）。
    /// </summary>
    public string? RevokeReason { get; set; }

    /// <summary>
    /// 客户端信息（User-Agent 等）。
    /// </summary>
    public string? ClientInfo { get; set; }

    /// <summary>
    /// 登录 IP。
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// 当前是否有效：未吊销且未过期。
    /// </summary>
    public bool IsActive(DateTimeOffset now) => RevokedAt is null && RefreshExpiresAt > now;
}
