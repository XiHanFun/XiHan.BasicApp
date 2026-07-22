// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// OAuth 应用应用层映射器
/// </summary>
public static class OAuthAppApplicationMapper
{
    /// <summary>
    /// 映射 OAuth 应用创建命令
    /// </summary>
    public static OAuthAppCreateCommand ToCreateCommand(OAuthAppCreateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new OAuthAppCreateCommand(
            input.AppName,
            input.AppDescription,
            input.ClientId,
            input.ClientSecret,
            input.AppType,
            input.GrantTypes,
            input.RedirectUris,
            input.Scopes,
            input.AccessTokenLifetime,
            input.RefreshTokenLifetime,
            input.AuthorizationCodeLifetime,
            input.Logo,
            input.Homepage,
            input.SkipConsent,
            input.Status,
            input.Remark);
    }

    /// <summary>
    /// 映射 OAuth 应用更新命令
    /// </summary>
    public static OAuthAppUpdateCommand ToUpdateCommand(OAuthAppUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new OAuthAppUpdateCommand(
            input.BasicId,
            input.AppName,
            input.AppDescription,
            input.AppType,
            input.GrantTypes,
            input.RedirectUris,
            input.Scopes,
            input.AccessTokenLifetime,
            input.RefreshTokenLifetime,
            input.AuthorizationCodeLifetime,
            input.Logo,
            input.Homepage,
            input.SkipConsent,
            input.Remark);
    }

    /// <summary>
    /// 映射 OAuth 应用状态命令
    /// </summary>
    public static OAuthAppStatusChangeCommand ToStatusCommand(OAuthAppStatusUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new OAuthAppStatusChangeCommand(input.BasicId, input.Status, input.Remark);
    }

    /// <summary>
    /// 映射 OAuth 应用密钥响应
    /// </summary>
    /// <param name="app">OAuth 应用实体</param>
    /// <param name="clientSecret">明文客户端密钥（仅创建/重置时返回一次）</param>
    /// <returns>OAuth 应用密钥 DTO</returns>
    public static OAuthAppSecretDto ToSecretDto(SysOAuthApp app, string clientSecret)
    {
        ArgumentNullException.ThrowIfNull(app);

        return new OAuthAppSecretDto
        {
            BasicId = app.BasicId,
            ClientId = app.ClientId,
            ClientSecret = clientSecret
        };
    }

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
