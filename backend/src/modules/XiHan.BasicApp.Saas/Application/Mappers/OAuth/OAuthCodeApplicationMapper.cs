#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OAuthCodeApplicationMapper
// Guid:454ddf8f-1d4c-4cf3-8135-f8bbccb397e1
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
            HasPkce = !string.IsNullOrWhiteSpace(code.CodeChallenge),
            CodeChallengeMethod = code.CodeChallengeMethod,
            ExpiresTime = code.ExpiresTime,
            IsExpired = code.ExpiresTime <= now,
            IsUsed = code.IsUsed,
            UsedAt = code.UsedAt,
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
            HasPkce = item.HasPkce,
            CodeChallengeMethod = item.CodeChallengeMethod,
            ExpiresTime = item.ExpiresTime,
            IsExpired = item.IsExpired,
            IsUsed = item.IsUsed,
            UsedAt = item.UsedAt,
            CreatedTime = item.CreatedTime,
            CreatedId = code.CreatedId,
            CreatedBy = code.CreatedBy
        };
    }
}
