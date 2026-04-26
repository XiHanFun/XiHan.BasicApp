#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SignatureType
// Guid:b2c3d4e5-6789-01ab-cdef-234567890abc
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/08 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Domain.Entities.Enums;

/// <summary>
/// API 签名类型枚举
/// </summary>
public enum SignatureType
{
    /// <summary>
    /// 无签名
    /// </summary>
    None = 0,

    /// <summary>
    /// HMAC-SHA256
    /// </summary>
    HmacSha256 = 1,

    /// <summary>
    /// HMAC-SHA512
    /// </summary>
    HmacSha512 = 2,

    /// <summary>
    /// RSA-SHA256
    /// </summary>
    RsaSha256 = 3,

    /// <summary>
    /// RSA-SHA512
    /// </summary>
    RsaSha512 = 4,

    /// <summary>
    /// SM2 国密签名
    /// </summary>
    Sm2 = 5,

    /// <summary>
    /// SM3 国密摘要
    /// </summary>
    Sm3 = 6,

    /// <summary>
    /// Ed25519
    /// </summary>
    Ed25519 = 7,

    /// <summary>
    /// MD5（不推荐，仅兼容旧系统）
    /// </summary>
    Md5 = 99
}
