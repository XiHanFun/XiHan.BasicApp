#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OAuthTokenListItemDto
// Guid:a99f23b6-0937-45a6-86bf-d183d5df1b83
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// OAuth Token 列表项 DTO
/// </summary>
public class OAuthTokenListItemDto : BasicAppDto
{
    /// <summary>
    /// 客户端 ID
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// 应用名称
    /// </summary>
    public string? AppName { get; set; }

    /// <summary>
    /// 用户主键
    /// </summary>
    public long? UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// 真实姓名
    /// </summary>
    public string? RealName { get; set; }

    /// <summary>
    /// 昵称
    /// </summary>
    public string? NickName { get; set; }

    /// <summary>
    /// 会话主键
    /// </summary>
    public long? SessionId { get; set; }

    /// <summary>
    /// 会话业务标识
    /// </summary>
    public string? UserSessionId { get; set; }

    /// <summary>
    /// 令牌类型
    /// </summary>
    public string TokenType { get; set; } = string.Empty;

    /// <summary>
    /// 授权类型
    /// </summary>
    public GrantType GrantType { get; set; }

    /// <summary>
    /// 权限范围
    /// </summary>
    public string? Scopes { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public EnableStatus Status { get; set; }

    /// <summary>
    /// 访问令牌过期时间
    /// </summary>
    public DateTimeOffset AccessTokenExpiresTime { get; set; }

    /// <summary>
    /// 访问令牌是否已过期
    /// </summary>
    public bool IsAccessTokenExpired { get; set; }

    /// <summary>
    /// 刷新令牌过期时间
    /// </summary>
    public DateTimeOffset? RefreshTokenExpiresTime { get; set; }

    /// <summary>
    /// 刷新令牌是否已过期
    /// </summary>
    public bool IsRefreshTokenExpired { get; set; }

    /// <summary>
    /// 是否已撤销
    /// </summary>
    public bool IsRevoked { get; set; }

    /// <summary>
    /// 撤销时间
    /// </summary>
    public DateTimeOffset? RevokedTime { get; set; }

    /// <summary>
    /// 是否存在父 Token
    /// </summary>
    public bool HasParentToken { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }
}
