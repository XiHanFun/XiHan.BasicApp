#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OAuthAppDto
// Guid:c1d2e3f4-g5h6-7890-abcd-ef1234567890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 18:20:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Dtos.Base;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Services.OAuthApps.Dtos;

/// <summary>
/// OAuth应用 DTO
/// </summary>
public class OAuthAppDto : RbacFullAuditedDtoBase
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
    /// 客户端ID
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
    /// 支持的授权类型（多个用逗号分隔）
    /// </summary>
    public string GrantTypes { get; set; } = string.Empty;

    /// <summary>
    /// 重定向URI（多个用逗号分隔）
    /// </summary>
    public string? RedirectUris { get; set; }

    /// <summary>
    /// 权限范围（多个用逗号分隔）
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
    /// 应用Logo
    /// </summary>
    public string? Logo { get; set; }

    /// <summary>
    /// 应用主页
    /// </summary>
    public string? Homepage { get; set; }

    /// <summary>
    /// 是否跳过授权
    /// </summary>
    public bool SkipConsent { get; set; } = false;

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 创建OAuth应用 DTO
/// </summary>
public class CreateOAuthAppDto : RbacCreationDtoBase
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
    /// 客户端ID
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
    /// 支持的授权类型（多个用逗号分隔）
    /// </summary>
    public string GrantTypes { get; set; } = string.Empty;

    /// <summary>
    /// 重定向URI（多个用逗号分隔）
    /// </summary>
    public string? RedirectUris { get; set; }

    /// <summary>
    /// 权限范围（多个用逗号分隔）
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
    /// 应用Logo
    /// </summary>
    public string? Logo { get; set; }

    /// <summary>
    /// 应用主页
    /// </summary>
    public string? Homepage { get; set; }

    /// <summary>
    /// 是否跳过授权
    /// </summary>
    public bool SkipConsent { get; set; } = false;

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 更新OAuth应用 DTO
/// </summary>
public class UpdateOAuthAppDto : RbacUpdateDtoBase
{
    /// <summary>
    /// 应用名称
    /// </summary>
    public string? AppName { get; set; }

    /// <summary>
    /// 应用描述
    /// </summary>
    public string? AppDescription { get; set; }

    /// <summary>
    /// 客户端密钥
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// 应用类型
    /// </summary>
    public OAuthAppType? AppType { get; set; }

    /// <summary>
    /// 支持的授权类型（多个用逗号分隔）
    /// </summary>
    public string? GrantTypes { get; set; }

    /// <summary>
    /// 重定向URI（多个用逗号分隔）
    /// </summary>
    public string? RedirectUris { get; set; }

    /// <summary>
    /// 权限范围（多个用逗号分隔）
    /// </summary>
    public string? Scopes { get; set; }

    /// <summary>
    /// 访问令牌有效期（秒）
    /// </summary>
    public int? AccessTokenLifetime { get; set; }

    /// <summary>
    /// 刷新令牌有效期（秒）
    /// </summary>
    public int? RefreshTokenLifetime { get; set; }

    /// <summary>
    /// 授权码有效期（秒）
    /// </summary>
    public int? AuthorizationCodeLifetime { get; set; }

    /// <summary>
    /// 应用Logo
    /// </summary>
    public string? Logo { get; set; }

    /// <summary>
    /// 应用主页
    /// </summary>
    public string? Homepage { get; set; }

    /// <summary>
    /// 是否跳过授权
    /// </summary>
    public bool? SkipConsent { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo? Status { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
