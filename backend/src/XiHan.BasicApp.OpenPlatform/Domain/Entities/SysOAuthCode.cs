// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.OpenPlatform.Domain.Entities;

/// <summary>
/// OAuth 授权码：只存散列，用一次即作废
/// </summary>
/// <remarks>
/// 平台库实体（放租户库的话，库隔离的租户在匿名的令牌端点上查不到）
/// 作废经条件更新抢占，抢输的按重放处理
/// PKCE 的 challenge 按 RFC 7636 最长 128 字符
/// </remarks>
[SugarTable(TableName = "sys_oauth_code", TableDescription = "OAuth 授权码：只存散列，用一次即作废")]
[ActiveUniqueIndex(nameof(CodeHash), Name = "ux_sys_oauth_code_code_hash")]
[SugarIndex("ix_sys_oauth_code_expires_at", nameof(ExpiresAt), OrderByType.Asc)]
[DiffLogIgnored]
public sealed class SysOAuthCode : AppAuditedEntity
{
    /// <summary>
    /// 授权码的 SHA-256（小写十六进制）
    /// </summary>
    [SugarColumn(ColumnName = "code_hash", Length = 64, IsNullable = false, ColumnDescription = "授权码的 SHA-256（小写十六进制）")]
    public string CodeHash { get; set; } = string.Empty;

    /// <summary>
    /// ClientId
    /// </summary>
    [SugarColumn(ColumnName = "client_id", Length = 100, IsNullable = false, ColumnDescription = "ClientId")]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// 授权的用户
    /// </summary>
    [SugarColumn(ColumnName = "user_id", IsNullable = false, ColumnDescription = "授权的用户")]
    public Guid UserId { get; set; }

    /// <summary>
    /// 授权时的回调地址（换令牌时须逐字一致）
    /// </summary>
    [SugarColumn(ColumnName = "redirect_uri", Length = 500, IsNullable = false, ColumnDescription = "授权时的回调地址（换令牌时须逐字一致）")]
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>
    /// 授予的范围（空格分隔）
    /// </summary>
    [SugarColumn(ColumnName = "scopes", Length = 200, IsNullable = false, ColumnDescription = "授予的范围（空格分隔）")]
    public string Scopes { get; set; } = string.Empty;

    /// <summary>
    /// PKCE challenge
    /// </summary>
    [SugarColumn(ColumnName = "code_challenge", Length = 128, IsNullable = true, ColumnDescription = "PKCE challenge")]
    public string? CodeChallenge { get; set; }

    /// <summary>
    /// PKCE 方法（<c>S256</c> / <c>plain</c>）
    /// </summary>
    [SugarColumn(ColumnName = "code_challenge_method", Length = 10, IsNullable = true, ColumnDescription = "PKCE 方法（S256 / plain）")]
    public string? CodeChallengeMethod { get; set; }

    /// <summary>
    /// OIDC nonce（原样写进 id_token）
    /// </summary>
    [SugarColumn(ColumnName = "nonce", Length = 200, IsNullable = true, ColumnDescription = "OIDC nonce（原样写进 id_token）")]
    public string? Nonce { get; set; }

    /// <summary>
    /// 到期时间
    /// </summary>
    [SugarColumn(ColumnName = "expires_at", IsNullable = false, ColumnDescription = "到期时间")]
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// 是否已用过
    /// </summary>
    [SugarColumn(ColumnName = "is_consumed", IsNullable = false, ColumnDescription = "是否已用过")]
    public bool IsConsumed { get; set; }
}
