#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OAuthAppDto
// Guid:e9a33c79-783d-4e5e-97c5-0d783a4f6de4
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 12:31:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// OAuth 应用 DTO
/// </summary>
public class OAuthAppDto : BasicAppDto
{
    /// <summary>
    /// 应用名称
    /// </summary>
    public string AppName { get; set; } = string.Empty;

    /// <summary>
    /// 应用描述
    /// </summary>
    public string? AppDescription { get; set; }

    /// <summary>
    /// 客户端 ID
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// 客户端密钥
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// 应用类型
    /// </summary>
    public OAuthAppType AppType { get; set; } = OAuthAppType.Web;

    /// <summary>
    /// 授权类型
    /// </summary>
    public string GrantTypes { get; set; } = string.Empty;

    /// <summary>
    /// 重定向地址
    /// </summary>
    public string? RedirectUris { get; set; }

    /// <summary>
    /// 权限范围
    /// </summary>
    public string? Scopes { get; set; }

    /// <summary>
    /// 访问令牌有效期（秒）
    /// </summary>
    public int AccessTokenLifetime { get; set; } = 3600;

    /// <summary>
    /// 刷新令牌有效期（秒）
    /// </summary>
    public int RefreshTokenLifetime { get; set; } = 2592000;

    /// <summary>
    /// 授权码有效期（秒）
    /// </summary>
    public int AuthorizationCodeLifetime { get; set; } = 300;

    /// <summary>
    /// 是否跳过用户同意
    /// </summary>
    public bool SkipConsent { get; set; }

    /// <summary>
    /// 租户标识
    /// </summary>
    public long? TenantId { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; } = YesOrNo.Yes;
}

/// <summary>
/// 创建 OAuth 应用 DTO
/// </summary>
public class OAuthAppCreateDto : BasicAppCDto
{
    /// <summary>
    /// 应用名称
    /// </summary>
    [Required(ErrorMessage = "应用名称不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "应用名称长度必须在 1～100 之间")]
    public string AppName { get; set; } = string.Empty;

    /// <summary>
    /// 应用描述
    /// </summary>
    [StringLength(500, ErrorMessage = "应用描述长度不能超过 500")]
    public string? AppDescription { get; set; }

    /// <summary>
    /// 客户端 ID
    /// </summary>
    [Required(ErrorMessage = "客户端ID不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "客户端ID长度必须在 1～100 之间")]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// 客户端密钥
    /// </summary>
    [Required(ErrorMessage = "客户端密钥不能为空")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "客户端密钥长度必须在 1～200 之间")]
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// 应用类型
    /// </summary>
    public OAuthAppType AppType { get; set; } = OAuthAppType.Web;

    /// <summary>
    /// 授权类型
    /// </summary>
    [Required(ErrorMessage = "授权类型不能为空")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "授权类型长度必须在 1～200 之间")]
    public string GrantTypes { get; set; } = string.Empty;

    /// <summary>
    /// 重定向地址
    /// </summary>
    [StringLength(1000, ErrorMessage = "重定向地址长度不能超过 1000")]
    public string? RedirectUris { get; set; }

    /// <summary>
    /// 权限范围
    /// </summary>
    [StringLength(500, ErrorMessage = "权限范围长度不能超过 500")]
    public string? Scopes { get; set; }

    /// <summary>
    /// 访问令牌有效期（秒）
    /// </summary>
    public int AccessTokenLifetime { get; set; } = 3600;

    /// <summary>
    /// 刷新令牌有效期（秒）
    /// </summary>
    public int RefreshTokenLifetime { get; set; } = 2592000;

    /// <summary>
    /// 授权码有效期（秒）
    /// </summary>
    public int AuthorizationCodeLifetime { get; set; } = 300;

    /// <summary>
    /// 是否跳过用户同意
    /// </summary>
    public bool SkipConsent { get; set; }

    /// <summary>
    /// 租户标识
    /// </summary>
    public long? TenantId { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [StringLength(500, ErrorMessage = "备注长度不能超过 500")]
    public string? Remark { get; set; }
}

/// <summary>
/// 更新 OAuth 应用 DTO
/// </summary>
public class OAuthAppUpdateDto : BasicAppUDto
{
    /// <summary>
    /// 应用名称
    /// </summary>
    [Required(ErrorMessage = "应用名称不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "应用名称长度必须在 1～100 之间")]
    public string AppName { get; set; } = string.Empty;

    /// <summary>
    /// 应用描述
    /// </summary>
    [StringLength(500, ErrorMessage = "应用描述长度不能超过 500")]
    public string? AppDescription { get; set; }

    /// <summary>
    /// 客户端密钥
    /// </summary>
    [Required(ErrorMessage = "客户端密钥不能为空")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "客户端密钥长度必须在 1～200 之间")]
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// 应用类型
    /// </summary>
    public OAuthAppType AppType { get; set; } = OAuthAppType.Web;

    /// <summary>
    /// 授权类型
    /// </summary>
    [Required(ErrorMessage = "授权类型不能为空")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "授权类型长度必须在 1～200 之间")]
    public string GrantTypes { get; set; } = string.Empty;

    /// <summary>
    /// 重定向地址
    /// </summary>
    [StringLength(1000, ErrorMessage = "重定向地址长度不能超过 1000")]
    public string? RedirectUris { get; set; }

    /// <summary>
    /// 权限范围
    /// </summary>
    [StringLength(500, ErrorMessage = "权限范围长度不能超过 500")]
    public string? Scopes { get; set; }

    /// <summary>
    /// 访问令牌有效期（秒）
    /// </summary>
    public int AccessTokenLifetime { get; set; } = 3600;

    /// <summary>
    /// 刷新令牌有效期（秒）
    /// </summary>
    public int RefreshTokenLifetime { get; set; } = 2592000;

    /// <summary>
    /// 授权码有效期（秒）
    /// </summary>
    public int AuthorizationCodeLifetime { get; set; } = 300;

    /// <summary>
    /// 是否跳过用户同意
    /// </summary>
    public bool SkipConsent { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 备注
    /// </summary>
    [StringLength(500, ErrorMessage = "备注长度不能超过 500")]
    public string? Remark { get; set; }
}

/// <summary>
/// OAuth 应用 OpenAPI 安全配置 DTO
/// </summary>
public class OAuthAppOpenApiSecurityDto : BasicAppDto
{
    /// <summary>
    /// 是否启用 OpenAPI 安全策略
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

/// <summary>
/// 更新 OAuth 应用 OpenAPI 安全配置 DTO
/// </summary>
public class OAuthAppOpenApiSecurityUpdateDto : BasicAppUDto
{
    /// <summary>
    /// 是否启用 OpenAPI 安全策略
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 请求签名算法
    /// </summary>
    [Required(ErrorMessage = "请求签名算法不能为空")]
    [StringLength(50, ErrorMessage = "请求签名算法长度不能超过 50")]
    public string SignatureAlgorithm { get; set; } = "HMACSHA256";

    /// <summary>
    /// 内容签名算法
    /// </summary>
    [Required(ErrorMessage = "内容签名算法不能为空")]
    [StringLength(50, ErrorMessage = "内容签名算法长度不能超过 50")]
    public string ContentSignatureAlgorithm { get; set; } = "SHA256";

    /// <summary>
    /// 加密算法
    /// </summary>
    [Required(ErrorMessage = "加密算法不能为空")]
    [StringLength(50, ErrorMessage = "加密算法长度不能超过 50")]
    public string EncryptionAlgorithm { get; set; } = "AES-CBC";

    /// <summary>
    /// 加密密钥（为空时回退客户端密钥）
    /// </summary>
    [StringLength(1000, ErrorMessage = "加密密钥长度不能超过 1000")]
    public string? EncryptKey { get; set; }

    /// <summary>
    /// RSA 公钥（用于 RSASHA256）
    /// </summary>
    [StringLength(10000, ErrorMessage = "RSA 公钥长度不能超过 10000")]
    public string? PublicKey { get; set; }

    /// <summary>
    /// SM2 公钥（用于 SM2）
    /// </summary>
    [StringLength(10000, ErrorMessage = "SM2 公钥长度不能超过 10000")]
    public string? Sm2PublicKey { get; set; }

    /// <summary>
    /// 是否允许响应加密
    /// </summary>
    public bool AllowResponseEncryption { get; set; } = true;

    /// <summary>
    /// IP 白名单，支持逗号/分号/换行分隔
    /// </summary>
    [StringLength(4000, ErrorMessage = "IP 白名单长度不能超过 4000")]
    public string? IpWhitelist { get; set; }
}
