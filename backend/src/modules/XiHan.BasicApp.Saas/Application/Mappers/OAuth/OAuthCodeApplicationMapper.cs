// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// OAuth 授权码应用层映射器
/// </summary>
public static class OAuthCodeApplicationMapper
{
    /// <summary>
    /// 映射 OAuth 授权码列表项
    /// </summary>
    /// <param name="code">OAuth 授权码实体</param>
    /// <param name="app">OAuth 应用实体</param>
    /// <param name="user">用户实体</param>
    /// <param name="now">当前时间</param>
    /// <returns>OAuth 授权码列表项 DTO</returns>
    public static OAuthCodeListItemDto ToListItemDto(SysOAuthCode code, SysOAuthApp? app, SysUser? user, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(code);

        return new OAuthCodeListItemDto
        {
            BasicId = code.BasicId,
            ClientId = code.ClientId,
            AppName = app?.AppName,
            UserId = code.UserId,
            UserName = user?.UserName,
            RealName = user?.RealName,
            NickName = user?.NickName,
            RedirectUri = code.RedirectUri,
            Scopes = code.Scopes,
            CodeChallengeMethod = code.CodeChallengeMethod,
            ExpirationTime = code.ExpirationTime,
            IsExpired = code.ExpirationTime <= now,
            IsUsed = code.IsUsed,
            UsedTime = code.UsedTime,
            CreatedTime = code.CreatedTime
        };
    }

    /// <summary>
    /// 映射 OAuth 授权码详情
    /// </summary>
    /// <param name="code">OAuth 授权码实体</param>
    /// <param name="app">OAuth 应用实体</param>
    /// <param name="user">用户实体</param>
    /// <param name="now">当前时间</param>
    /// <returns>OAuth 授权码详情 DTO</returns>
    public static OAuthCodeDetailDto ToDetailDto(SysOAuthCode code, SysOAuthApp? app, SysUser? user, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(code);

        var item = ToListItemDto(code, app, user, now);
        return new OAuthCodeDetailDto
        {
            BasicId = item.BasicId,
            ClientId = item.ClientId,
            AppName = item.AppName,
            UserId = item.UserId,
            UserName = item.UserName,
            RealName = item.RealName,
            NickName = item.NickName,
            RedirectUri = item.RedirectUri,
            Scopes = item.Scopes,
            CodeChallengeMethod = item.CodeChallengeMethod,
            ExpirationTime = item.ExpirationTime,
            IsExpired = item.IsExpired,
            IsUsed = item.IsUsed,
            UsedTime = item.UsedTime,
            CreatedTime = item.CreatedTime,
            CreatedId = code.CreatedId,
            CreatedBy = code.CreatedBy
        };
    }
}
