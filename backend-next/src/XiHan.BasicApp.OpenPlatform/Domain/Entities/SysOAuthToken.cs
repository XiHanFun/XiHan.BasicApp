// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.OpenPlatform.Domain.Entities;

/// <summary>
/// 发给第三方应用的令牌（平台库实体）：访问令牌与刷新令牌都是不透明随机串、只存散列，撤销立即生效（4.x 发 JWT，撤销后在过期前仍能用）。
/// 同一次授权码兑换出来的令牌属于一个族（<see cref="FamilyId"/>）：刷新令牌轮换时旧的作废，作废的刷新令牌再被使用即整族吊销（4.x 注释说按族吊销，实际没做）。
/// 只用于身份：只认 userinfo，后台接口不认。
/// </summary>
[DiffLogIgnored]
public sealed class SysOAuthToken : AppAuditedEntity
{
    /// <summary>
    /// 令牌的 SHA-256（小写十六进制）。
    /// </summary>
    [StringLength(64)]
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>
    /// 访问令牌或刷新令牌。
    /// </summary>
    public OAuthTokenKind Kind { get; set; }

    /// <summary>
    /// 令牌族（一次授权码兑换出来的全部令牌）。
    /// </summary>
    public Guid FamilyId { get; set; }

    /// <summary>
    /// ClientId。
    /// </summary>
    [StringLength(100)]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// 用户。
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 授予的范围（空格分隔）。
    /// </summary>
    [StringLength(200)]
    public string Scopes { get; set; } = string.Empty;

    /// <summary>
    /// 到期时间。
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// 撤销时间（轮换、撤销、整族吊销）。
    /// </summary>
    public DateTimeOffset? RevokedAt { get; set; }
}

/// <summary>
/// 令牌种类。
/// </summary>
public enum OAuthTokenKind
{
    /// <summary>
    /// 访问令牌。
    /// </summary>
    Access = 0,

    /// <summary>
    /// 刷新令牌。
    /// </summary>
    Refresh = 1,
}
