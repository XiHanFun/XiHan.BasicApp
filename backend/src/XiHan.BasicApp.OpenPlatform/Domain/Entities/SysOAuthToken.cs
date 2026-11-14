// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.OpenPlatform.Domain.Entities;

/// <summary>
/// 发给第三方应用的访问令牌与刷新令牌
/// </summary>
/// <remarks>
/// 访问令牌与刷新令牌都是 256 位不透明随机串，库里只存 SHA-256：每次使用都回库核对，撤销立即生效，库里的行拿去也用不了
/// 只用于身份：只认 userinfo，后台接口不认
/// 同一次授权码兑换出来的令牌属于一个族（<see cref="FamilyId"/> 即授权码行的主键）：刷新令牌轮换时旧的作废、同族发新的一对，作废的刷新令牌或用过的授权码再被使用即整族吊销，重放一次整条链都失效
/// 吊销只写 <see cref="RevokedAt"/>、不删行：留着已吊销的行才认得出重放
/// 每次使用都看应用是否启用、用户是否正常：停用应用或用户即刻生效，不必逐个吊销令牌
/// 随兑换与刷新频繁写入，不进数据变更日志；作业 <c>openplatform.oauth-cleanup</c> 在过期七天后物理删除
/// </remarks>
[SugarTable(TableName = "sys_oauth_token", TableDescription = "发给第三方应用的访问令牌与刷新令牌")]
[ActiveUniqueIndex(nameof(TokenHash), Name = "ux_sys_oauth_token_token_hash")]
[SugarIndex("ix_sys_oauth_token_family_id", nameof(FamilyId), OrderByType.Asc)]
[SugarIndex("ix_sys_oauth_token_user_id_client_id", nameof(UserId), OrderByType.Asc, nameof(ClientId), OrderByType.Asc)]
[SugarIndex("ix_sys_oauth_token_expires_at", nameof(ExpiresAt), OrderByType.Asc)]
[DiffLogIgnored]
public sealed class SysOAuthToken : AppAuditedEntity
{
    /// <summary>
    /// 令牌的 SHA-256（小写十六进制）
    /// </summary>
    [SugarColumn(ColumnName = "token_hash", Length = 64, IsNullable = false, ColumnDescription = "令牌的 SHA-256（小写十六进制）")]
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>
    /// 访问令牌或刷新令牌
    /// </summary>
    [SugarColumn(ColumnName = "kind", IsNullable = false, ColumnDescription = "访问令牌或刷新令牌")]
    public OAuthTokenKind Kind { get; set; }

    /// <summary>
    /// 令牌族（一次授权码兑换出来的全部令牌）
    /// </summary>
    [SugarColumn(ColumnName = "family_id", IsNullable = false, ColumnDescription = "令牌族（一次授权码兑换出来的全部令牌）")]
    public Guid FamilyId { get; set; }

    /// <summary>
    /// ClientId
    /// </summary>
    [SugarColumn(ColumnName = "client_id", Length = 100, IsNullable = false, ColumnDescription = "ClientId")]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// 用户
    /// </summary>
    [SugarColumn(ColumnName = "user_id", IsNullable = false, ColumnDescription = "用户")]
    public Guid UserId { get; set; }

    /// <summary>
    /// 授予的范围（空格分隔）
    /// </summary>
    [SugarColumn(ColumnName = "scopes", Length = 200, IsNullable = false, ColumnDescription = "授予的范围（空格分隔）")]
    public string Scopes { get; set; } = string.Empty;

    /// <summary>
    /// 到期时间
    /// </summary>
    [SugarColumn(ColumnName = "expires_at", IsNullable = false, ColumnDescription = "到期时间")]
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// 撤销时间（轮换、撤销、整族吊销）
    /// </summary>
    [SugarColumn(ColumnName = "revoked_at", IsNullable = true, ColumnDescription = "撤销时间（轮换、撤销、整族吊销）")]
    public DateTimeOffset? RevokedAt { get; set; }
}

/// <summary>
/// 令牌种类
/// </summary>
public enum OAuthTokenKind
{
    /// <summary>
    /// 访问令牌
    /// </summary>
    Access = 0,

    /// <summary>
    /// 刷新令牌
    /// </summary>
    Refresh = 1,
}
