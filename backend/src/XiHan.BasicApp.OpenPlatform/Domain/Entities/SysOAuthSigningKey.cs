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
/// 平台库实体。私钥经 <c>ISecretProtector</c> 加密存库，多个实例共用。第一次用到时生成
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
