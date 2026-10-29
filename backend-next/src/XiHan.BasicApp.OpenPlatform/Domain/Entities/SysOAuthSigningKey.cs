// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.OpenPlatform.Domain.Entities;

/// <summary>
/// id_token 的签名密钥（平台库实体）：RSA 2048，私钥经 <c>ISecretProtector</c> 加密存库，多个实例共用（4.x 在本机生成 PEM 文件，多实例各签各的）。第一次用到时生成。
/// </summary>
[DiffLogIgnored]
public sealed class SysOAuthSigningKey : AppAuditedEntity
{
    /// <summary>
    /// 密钥 ID（JWKS 的 <c>kid</c>）。
    /// </summary>
    [StringLength(64)]
    public string KeyId { get; set; } = string.Empty;

    /// <summary>
    /// 私钥（PKCS#8 的 Base64，密文）。
    /// </summary>
    [LongText]
    public string PrivateKey { get; set; } = string.Empty;

    /// <summary>
    /// 是否在用（签名取最早的在用密钥，JWKS 公布全部在用的）。
    /// </summary>
    public bool IsActive { get; set; } = true;
}
