// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.OpenPlatform.Domain.Entities;

/// <summary>
/// OAuth 授权码（平台库实体；4.x 写进租户库，库隔离的租户在匿名的令牌端点上查不到）：只存散列，用一次即作废（条件更新抢占，抢输的按重放处理）。
/// PKCE 的 challenge 按 RFC 7636 最长 128 字符（4.x 截到 100，plain 的长 verifier 必然校验失败）。
/// </summary>
[DiffLogIgnored]
public sealed class SysOAuthCode : AppAuditedEntity
{
    /// <summary>
    /// 授权码的 SHA-256（小写十六进制）。
    /// </summary>
    [StringLength(64)]
    public string CodeHash { get; set; } = string.Empty;

    /// <summary>
    /// ClientId。
    /// </summary>
    [StringLength(100)]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// 授权的用户。
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 授权时的回调地址（换令牌时须逐字一致）。
    /// </summary>
    [StringLength(500)]
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>
    /// 授予的范围（空格分隔）。
    /// </summary>
    [StringLength(200)]
    public string Scopes { get; set; } = string.Empty;

    /// <summary>
    /// PKCE challenge。
    /// </summary>
    [StringLength(128)]
    public string? CodeChallenge { get; set; }

    /// <summary>
    /// PKCE 方法（<c>S256</c> / <c>plain</c>）。
    /// </summary>
    [StringLength(10)]
    public string? CodeChallengeMethod { get; set; }

    /// <summary>
    /// OIDC nonce（原样写进 id_token）。
    /// </summary>
    [StringLength(200)]
    public string? Nonce { get; set; }

    /// <summary>
    /// 到期时间。
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// 是否已用过。
    /// </summary>
    public bool IsConsumed { get; set; }
}
