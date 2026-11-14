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
/// 几个实例同时生成也不冲突：签名取最早的在用密钥，JWKS 公布全部在用的，多出来的那把只被公布、不会用来签
/// 生成在独立事务里做，令牌端点兑换授权码前在调用的工作单元之外先备好：同一个库上嵌套写事务会互相等锁
/// 实例把解密后的密钥放内存、十分钟重读一次；目前只在生成时写，没有轮换与停用的入口
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
    /// 是否在用（签名取最早的在用密钥，JWKS 公布全部在用的）
    /// </summary>
    [SugarColumn(ColumnName = "is_active", IsNullable = false, ColumnDescription = "是否在用（签名取最早的在用密钥，JWKS 公布全部在用的）")]
    public bool IsActive { get; set; } = true;
}
