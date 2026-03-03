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
using XiHan.BasicApp.Rbac.Domain.Enums;

namespace XiHan.BasicApp.Rbac.Application.Dtos;

/// <summary>
/// OAuth 应用 DTO
/// </summary>
public class OAuthAppDto : BasicAppDto
{
    public string AppName { get; set; } = string.Empty;

    public string? AppDescription { get; set; }

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public OAuthAppType AppType { get; set; } = OAuthAppType.Web;

    public string GrantTypes { get; set; } = string.Empty;

    public string? RedirectUris { get; set; }

    public string? Scopes { get; set; }

    public int AccessTokenLifetime { get; set; } = 3600;

    public int RefreshTokenLifetime { get; set; } = 2592000;

    public int AuthorizationCodeLifetime { get; set; } = 300;

    public bool SkipConsent { get; set; }

    public YesOrNo Status { get; set; } = YesOrNo.Yes;
}

/// <summary>
/// 创建 OAuth 应用 DTO
/// </summary>
public class OAuthAppCreateDto : BasicAppCDto
{
    [Required(ErrorMessage = "应用名称不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "应用名称长度必须在 1～100 之间")]
    public string AppName { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "应用描述长度不能超过 500")]
    public string? AppDescription { get; set; }

    [Required(ErrorMessage = "客户端ID不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "客户端ID长度必须在 1～100 之间")]
    public string ClientId { get; set; } = string.Empty;

    [Required(ErrorMessage = "客户端密钥不能为空")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "客户端密钥长度必须在 1～200 之间")]
    public string ClientSecret { get; set; } = string.Empty;

    public OAuthAppType AppType { get; set; } = OAuthAppType.Web;

    [Required(ErrorMessage = "授权类型不能为空")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "授权类型长度必须在 1～200 之间")]
    public string GrantTypes { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "重定向地址长度不能超过 1000")]
    public string? RedirectUris { get; set; }

    [StringLength(500, ErrorMessage = "权限范围长度不能超过 500")]
    public string? Scopes { get; set; }

    public int AccessTokenLifetime { get; set; } = 3600;

    public int RefreshTokenLifetime { get; set; } = 2592000;

    public int AuthorizationCodeLifetime { get; set; } = 300;

    public bool SkipConsent { get; set; }

    public long? TenantId { get; set; }

    [StringLength(500, ErrorMessage = "备注长度不能超过 500")]
    public string? Remark { get; set; }
}

/// <summary>
/// 更新 OAuth 应用 DTO
/// </summary>
public class OAuthAppUpdateDto : BasicAppUDto
{
    [Required(ErrorMessage = "应用名称不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "应用名称长度必须在 1～100 之间")]
    public string AppName { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "应用描述长度不能超过 500")]
    public string? AppDescription { get; set; }

    [Required(ErrorMessage = "客户端密钥不能为空")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "客户端密钥长度必须在 1～200 之间")]
    public string ClientSecret { get; set; } = string.Empty;

    public OAuthAppType AppType { get; set; } = OAuthAppType.Web;

    [Required(ErrorMessage = "授权类型不能为空")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "授权类型长度必须在 1～200 之间")]
    public string GrantTypes { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "重定向地址长度不能超过 1000")]
    public string? RedirectUris { get; set; }

    [StringLength(500, ErrorMessage = "权限范围长度不能超过 500")]
    public string? Scopes { get; set; }

    public int AccessTokenLifetime { get; set; } = 3600;

    public int RefreshTokenLifetime { get; set; } = 2592000;

    public int AuthorizationCodeLifetime { get; set; } = 300;

    public bool SkipConsent { get; set; }

    public YesOrNo Status { get; set; } = YesOrNo.Yes;

    [StringLength(500, ErrorMessage = "备注长度不能超过 500")]
    public string? Remark { get; set; }
}
