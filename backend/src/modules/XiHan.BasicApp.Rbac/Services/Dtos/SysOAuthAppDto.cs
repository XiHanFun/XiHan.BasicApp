#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysOAuthAppDto
// Guid:d6e7f8a9-b0c1-2345-6789-012d34567890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/8 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Dtos.Base;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Services.Dtos;

/// <summary>
/// 系统 OAuth 应用创建 DTO
/// </summary>
public class SysOAuthAppCreateDto : RbacCreationDtoBase
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
    /// 支持的授权类型
    /// </summary>
    public string GrantTypes { get; set; } = string.Empty;

    /// <summary>
    /// 重定向URI
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
/// 系统 OAuth 应用更新 DTO
/// </summary>
public class SysOAuthAppUpdateDto : RbacUpdateDtoBase
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
    /// 客户端密钥
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// 应用类型
    /// </summary>
    public OAuthAppType AppType { get; set; } = OAuthAppType.Web;

    /// <summary>
    /// 支持的授权类型
    /// </summary>
    public string GrantTypes { get; set; } = string.Empty;

    /// <summary>
    /// 重定向URI
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
/// 系统 OAuth 应用查询 DTO
/// </summary>
public class SysOAuthAppGetDto : RbacFullAuditedDtoBase
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
    /// 应用类型
    /// </summary>
    public OAuthAppType AppType { get; set; } = OAuthAppType.Web;

    /// <summary>
    /// 支持的授权类型
    /// </summary>
    public string GrantTypes { get; set; } = string.Empty;

    /// <summary>
    /// 重定向URI
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
