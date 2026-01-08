#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysOAuthTokenDto
// Guid:e7f8a9b0-c1d2-3456-7890-123e45678901
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/8 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Dtos.Base;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Services.Dtos;

/// <summary>
/// 系统 OAuth 令牌创建 DTO
/// </summary>
public class SysOAuthTokenCreateDto : RbacCreationDtoBase
{
    /// <summary>
    /// 访问令牌
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// 刷新令牌
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// 令牌类型
    /// </summary>
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// 客户端ID
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// 用户ID
    /// </summary>
    public long? UserId { get; set; }

    /// <summary>
    /// 授权类型
    /// </summary>
    public GrantType GrantType { get; set; } = GrantType.AuthorizationCode;

    /// <summary>
    /// 权限范围
    /// </summary>
    public string? Scopes { get; set; }

    /// <summary>
    /// 访问令牌过期时间
    /// </summary>
    public DateTimeOffset AccessTokenExpiresAt { get; set; }

    /// <summary>
    /// 刷新令牌过期时间
    /// </summary>
    public DateTimeOffset? RefreshTokenExpiresAt { get; set; }
}

/// <summary>
/// 系统 OAuth 令牌更新 DTO
/// </summary>
public class SysOAuthTokenUpdateDto : RbacUpdateDtoBase
{
    /// <summary>
    /// 是否已撤销
    /// </summary>
    public bool IsRevoked { get; set; }

    /// <summary>
    /// 撤销时间
    /// </summary>
    public DateTimeOffset? RevokedAt { get; set; }
}

/// <summary>
/// 系统 OAuth 令牌查询 DTO
/// </summary>
public class SysOAuthTokenGetDto : RbacFullAuditedDtoBase
{
    /// <summary>
    /// 访问令牌
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// 刷新令牌
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// 令牌类型
    /// </summary>
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// 客户端ID
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// 用户ID
    /// </summary>
    public long? UserId { get; set; }

    /// <summary>
    /// 授权类型
    /// </summary>
    public GrantType GrantType { get; set; } = GrantType.AuthorizationCode;

    /// <summary>
    /// 权限范围
    /// </summary>
    public string? Scopes { get; set; }

    /// <summary>
    /// 访问令牌过期时间
    /// </summary>
    public DateTimeOffset AccessTokenExpiresAt { get; set; }

    /// <summary>
    /// 刷新令牌过期时间
    /// </summary>
    public DateTimeOffset? RefreshTokenExpiresAt { get; set; }

    /// <summary>
    /// 是否已撤销
    /// </summary>
    public bool IsRevoked { get; set; } = false;

    /// <summary>
    /// 撤销时间
    /// </summary>
    public DateTimeOffset? RevokedAt { get; set; }
}
