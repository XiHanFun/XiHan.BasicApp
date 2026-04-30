#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OAuthTokenApplicationMapper
// Guid:4093b09d-88ff-428c-a739-8ddc8f3880ab
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// OAuth Token 应用层映射器
/// </summary>
public static class OAuthTokenApplicationMapper
{
    /// <summary>
    /// 映射 OAuth Token 列表项
    /// </summary>
    /// <param name="token">OAuth Token 实体</param>
    /// <param name="app">OAuth 应用实体</param>
    /// <param name="session">用户会话实体</param>
    /// <param name="user">用户实体</param>
    /// <param name="now">当前时间</param>
    /// <returns>OAuth Token 列表项 DTO</returns>
    public static OAuthTokenListItemDto ToListItemDto(
        SysOAuthToken token,
        SysOAuthApp? app,
        SysUserSession? session,
        SysUser? user,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(token);

        var userId = token.UserId ?? session?.UserId;
        return new OAuthTokenListItemDto
        {
            BasicId = token.BasicId,
            ClientId = token.ClientId,
            AppName = app?.AppName,
            UserId = userId,
            UserName = user?.UserName,
            RealName = user?.RealName,
            NickName = user?.NickName,
            SessionId = token.SessionId,
            UserSessionId = session?.UserSessionId,
            TokenType = token.TokenType,
            GrantType = token.GrantType,
            Scopes = token.Scopes,
            Status = token.Status,
            AccessTokenExpiresTime = token.AccessTokenExpiresTime,
            IsAccessTokenExpired = token.AccessTokenExpiresTime <= now,
            RefreshTokenExpiresTime = token.RefreshTokenExpiresTime,
            IsRefreshTokenExpired = token.RefreshTokenExpiresTime.HasValue && token.RefreshTokenExpiresTime.Value <= now,
            IsRevoked = token.IsRevoked,
            RevokedTime = token.RevokedTime,
            HasParentToken = token.ParentTokenId.HasValue,
            CreatedTime = token.CreatedTime
        };
    }

    /// <summary>
    /// 映射 OAuth Token 详情
    /// </summary>
    /// <param name="token">OAuth Token 实体</param>
    /// <param name="app">OAuth 应用实体</param>
    /// <param name="session">用户会话实体</param>
    /// <param name="user">用户实体</param>
    /// <param name="now">当前时间</param>
    /// <returns>OAuth Token 详情 DTO</returns>
    public static OAuthTokenDetailDto ToDetailDto(
        SysOAuthToken token,
        SysOAuthApp? app,
        SysUserSession? session,
        SysUser? user,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(token);

        var item = ToListItemDto(token, app, session, user, now);
        return new OAuthTokenDetailDto
        {
            BasicId = item.BasicId,
            ClientId = item.ClientId,
            AppName = item.AppName,
            UserId = item.UserId,
            UserName = item.UserName,
            RealName = item.RealName,
            NickName = item.NickName,
            SessionId = item.SessionId,
            UserSessionId = item.UserSessionId,
            TokenType = item.TokenType,
            GrantType = item.GrantType,
            Scopes = item.Scopes,
            Status = item.Status,
            AccessTokenExpiresTime = item.AccessTokenExpiresTime,
            IsAccessTokenExpired = item.IsAccessTokenExpired,
            RefreshTokenExpiresTime = item.RefreshTokenExpiresTime,
            IsRefreshTokenExpired = item.IsRefreshTokenExpired,
            IsRevoked = item.IsRevoked,
            RevokedTime = item.RevokedTime,
            HasParentToken = item.HasParentToken,
            CreatedTime = item.CreatedTime,
            CreatedId = token.CreatedId,
            CreatedBy = token.CreatedBy
        };
    }
}
