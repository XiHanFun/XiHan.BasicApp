#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OAuthAppApplicationMapper
// Guid:fb3086b5-2f8f-44a0-941a-0a9d28307dfb
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// OAuth 应用应用层映射器
/// </summary>
public static class OAuthAppApplicationMapper
{
    /// <summary>
    /// 映射 OAuth 应用列表项
    /// </summary>
    /// <param name="app">OAuth 应用实体</param>
    /// <returns>OAuth 应用列表项 DTO</returns>
    public static OAuthAppListItemDto ToListItemDto(SysOAuthApp app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return new OAuthAppListItemDto
        {
            BasicId = app.BasicId,
            AppName = app.AppName,
            AppDescription = app.AppDescription,
            ClientId = app.ClientId,
            AppType = app.AppType,
            GrantTypes = app.GrantTypes,
            Scopes = app.Scopes,
            AccessTokenLifetime = app.AccessTokenLifetime,
            RefreshTokenLifetime = app.RefreshTokenLifetime,
            AuthorizationCodeLifetime = app.AuthorizationCodeLifetime,
            SkipConsent = app.SkipConsent,
            Status = app.Status,
            CreatedTime = app.CreatedTime,
            ModifiedTime = app.ModifiedTime
        };
    }

    /// <summary>
    /// 映射 OAuth 应用详情
    /// </summary>
    /// <param name="app">OAuth 应用实体</param>
    /// <returns>OAuth 应用详情 DTO</returns>
    public static OAuthAppDetailDto ToDetailDto(SysOAuthApp app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var item = ToListItemDto(app);
        return new OAuthAppDetailDto
        {
            BasicId = item.BasicId,
            AppName = item.AppName,
            AppDescription = item.AppDescription,
            ClientId = item.ClientId,
            AppType = item.AppType,
            GrantTypes = item.GrantTypes,
            RedirectUris = app.RedirectUris,
            Scopes = item.Scopes,
            AccessTokenLifetime = item.AccessTokenLifetime,
            RefreshTokenLifetime = item.RefreshTokenLifetime,
            AuthorizationCodeLifetime = item.AuthorizationCodeLifetime,
            Logo = app.Logo,
            Homepage = app.Homepage,
            SkipConsent = item.SkipConsent,
            Status = item.Status,
            Remark = app.Remark,
            CreatedTime = item.CreatedTime,
            CreatedId = app.CreatedId,
            CreatedBy = app.CreatedBy,
            ModifiedTime = item.ModifiedTime,
            ModifiedId = app.ModifiedId,
            ModifiedBy = app.ModifiedBy
        };
    }
}
