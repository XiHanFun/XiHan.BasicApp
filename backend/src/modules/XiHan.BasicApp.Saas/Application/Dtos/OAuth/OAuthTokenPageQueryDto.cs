// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// OAuth Token 分页查询 DTO
/// </summary>
public sealed class OAuthTokenPageQueryDto : BasicAppPRDto
{
    /// <summary>
    /// 客户端 ID
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// 用户主键
    /// </summary>
    public long? UserId { get; set; }

    /// <summary>
    /// 会话主键
    /// </summary>
    public long? SessionId { get; set; }

    /// <summary>
    /// 授权类型
    /// </summary>
    public GrantType? GrantType { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public EnableStatus? Status { get; set; }

    /// <summary>
    /// 是否已撤销
    /// </summary>
    public bool? IsRevoked { get; set; }

    /// <summary>
    /// 访问令牌是否已过期
    /// </summary>
    public bool? IsAccessTokenExpired { get; set; }

    /// <summary>
    /// 刷新令牌是否已过期
    /// </summary>
    public bool? IsRefreshTokenExpired { get; set; }

    /// <summary>
    /// 访问令牌过期开始时间
    /// </summary>
    public DateTimeOffset? AccessTokenExpirationTimeStart { get; set; }

    /// <summary>
    /// 访问令牌过期结束时间
    /// </summary>
    public DateTimeOffset? AccessTokenExpirationTimeEnd { get; set; }

    /// <summary>
    /// 刷新令牌过期开始时间
    /// </summary>
    public DateTimeOffset? RefreshTokenExpirationTimeStart { get; set; }

    /// <summary>
    /// 刷新令牌过期结束时间
    /// </summary>
    public DateTimeOffset? RefreshTokenExpirationTimeEnd { get; set; }
}
