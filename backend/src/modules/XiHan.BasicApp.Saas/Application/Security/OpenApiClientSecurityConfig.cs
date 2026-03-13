#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OpenApiClientSecurityConfig
// Guid:4d7f2fc2-2719-4f7f-8f52-6f2d6cb5e0ab
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/14 10:05:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Application.Security;

/// <summary>
/// OpenAPI 客户端安全配置
/// </summary>
public sealed class OpenApiClientSecurityConfig
{
    /// <summary>
    /// 是否启用 OpenAPI 安全校验
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 请求签名算法
    /// </summary>
    public string SignatureAlgorithm { get; set; } = "HMACSHA256";

    /// <summary>
    /// 内容签名算法
    /// </summary>
    public string ContentSignatureAlgorithm { get; set; } = "SHA256";

    /// <summary>
    /// 加密算法
    /// </summary>
    public string EncryptionAlgorithm { get; set; } = "AES-CBC";

    /// <summary>
    /// 加密密钥（为空时回退客户端密钥）
    /// </summary>
    public string? EncryptKey { get; set; }

    /// <summary>
    /// RSA 公钥（用于 RSASHA256）
    /// </summary>
    public string? PublicKey { get; set; }

    /// <summary>
    /// SM2 公钥（用于 SM2）
    /// </summary>
    public string? Sm2PublicKey { get; set; }

    /// <summary>
    /// 是否允许响应加密
    /// </summary>
    public bool AllowResponseEncryption { get; set; } = true;

    /// <summary>
    /// IP 白名单，支持逗号/分号/换行分隔
    /// </summary>
    public string? IpWhitelist { get; set; }
}
