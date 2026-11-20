// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.OpenPlatform.Domain.Entities;

/// <summary>
/// id_token 的签名密钥（RSA 2048）
/// </summary>
/// <remarks>
/// id_token 的 RS256 签名密钥落库而不放配置：多个实例要用同一把签名、JWKS 公布同一组公钥；第一次用到时自动生成，部署不用预先备密钥
/// 私钥经 <c>ISecretProtector</c> 加密存库，库里只有密文；解不开的跳过
/// 签名取已到签名时间、签名时间最晚的在用密钥（同一时刻的取最早建的），JWKS 公布全部在用的；几个实例同时生成也只用同一把，多出来的只被公布
/// 轮换：新密钥先公布、过一段时间才接替签名（等各实例与依赖方都拿到它的公钥），旧密钥留着公布、验它签过的 id_token，用不着了再停用；正在签名的不能停用
/// 生成在独立事务里做，令牌端点兑换授权码前在调用的工作单元之外先备好：同一个库上嵌套写事务会互相等锁
/// </remarks>
[SugarTable(TableName = "sys_oauth_signing_key", TableDescription = "id_token 的签名密钥（RSA 2048）")]
[DiffLogIgnored]
public sealed class SysOAuthSigningKey : AppAuditedEntity
{
    /// <summary>
    /// 密钥 ID（JWKS 的 <c>kid</c>）
    /// </summary>
    [SugarColumn(ColumnName = "key_id", Length = 64, IsNullable = false, ColumnDescription = "密钥 ID（JWKS 的 kid）")]
    public string KeyId { get; set; } = string.Empty;

    /// <summary>
    /// 私钥（PKCS#8 的 Base64，密文）
    /// </summary>
    [SugarColumn(ColumnName = "private_key", IsNullable = false, ColumnDescription = "私钥（PKCS#8 的 Base64，密文）")]
    [LongText]
    public string PrivateKey { get; set; } = string.Empty;

    /// <summary>
    /// 是否在用（JWKS 公布全部在用的，停用的不再公布、不再签名）
    /// </summary>
    [SugarColumn(ColumnName = "is_active", IsNullable = false, ColumnDescription = "是否在用（JWKS 公布全部在用的，停用的不再公布、不再签名）")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 开始签名的时间
    /// </summary>
    /// <remarks>
    /// 空即一建好就签；轮换出来的定在公布之后，等各实例与依赖方的缓存拿到它的公钥
    /// </remarks>
    [SugarColumn(ColumnName = "signs_from", IsNullable = true, ColumnDescription = "开始签名的时间（空即一建好就签）")]
    public DateTimeOffset? SignsFrom { get; set; }

    /// <summary>
    /// 停用时间
    /// </summary>
    [SugarColumn(ColumnName = "retired_at", IsNullable = true, ColumnDescription = "停用时间")]
    public DateTimeOffset? RetiredAt { get; set; }
}
