#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright (c)2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysApiLog.Enum
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// API 签名类型枚举
/// </summary>
public enum SignatureType
{
    /// <summary>
    /// 无签名
    /// </summary>
    [Description("无签名")]
    None = 0,

    /// <summary>
    /// HMAC-SHA256
    /// </summary>
    [Description("HMAC-SHA256")]
    HmacSha256 = 1,

    /// <summary>
    /// HMAC-SHA512
    /// </summary>
    [Description("HMAC-SHA512")]
    HmacSha512 = 2,

    /// <summary>
    /// RSA-SHA256
    /// </summary>
    [Description("RSA-SHA256")]
    RsaSha256 = 3,

    /// <summary>
    /// RSA-SHA512
    /// </summary>
    [Description("RSA-SHA512")]
    RsaSha512 = 4,

    /// <summary>
    /// SM2 国密签名
    /// </summary>
    [Description("SM2 国密签名")]
    Sm2 = 5,

    /// <summary>
    /// SM3 国密摘要
    /// </summary>
    [Description("SM3 国密摘要")]
    Sm3 = 6,

    /// <summary>
    /// Ed25519
    /// </summary>
    [Description("Ed25519")]
    Ed25519 = 7,

    /// <summary>
    /// MD5（不推荐，仅兼容旧系统）
    /// </summary>
    [Description("MD5（不推荐，仅兼容旧系统）")]
    Md5 = 99
}
