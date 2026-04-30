#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OAuthAppListItemDto
// Guid:1d9a8a8e-9e20-4ebf-ab61-c8be8ccb62e2
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
/// OAuth 应用列表项 DTO
/// </summary>
public class OAuthAppListItemDto : BasicAppDto
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
    /// 应用类型
    /// </summary>
    public OAuthAppType AppType { get; set; }

    /// <summary>
    /// 支持的授权类型
    /// </summary>
    public string GrantTypes { get; set; } = string.Empty;

    /// <summary>
    /// 权限范围
    /// </summary>
    public string? Scopes { get; set; }

    /// <summary>
    /// 访问令牌有效期（秒）
    /// </summary>
    public int AccessTokenLifetime { get; set; }

    /// <summary>
    /// 刷新令牌有效期（秒）
    /// </summary>
    public int RefreshTokenLifetime { get; set; }

    /// <summary>
    /// 授权码有效期（秒）
    /// </summary>
    public int AuthorizationCodeLifetime { get; set; }

    /// <summary>
    /// 是否跳过授权确认
    /// </summary>
    public bool SkipConsent { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public EnableStatus Status { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }

    /// <summary>
    /// 修改时间
    /// </summary>
    public DateTimeOffset? ModifiedTime { get; set; }
}
