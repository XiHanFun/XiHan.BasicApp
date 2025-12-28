#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysOAuthTokenService
// Guid:j1k2l3m4-n5o6-7890-abcd-ef1234567890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 18:55:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Services.OAuthTokens.Dtos;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Services.OAuthTokens;

/// <summary>
/// 系统OAuth令牌服务接口
/// </summary>
public interface ISysOAuthTokenService : ICrudApplicationService<OAuthTokenDto, XiHanBasicAppIdType, CreateOAuthTokenDto, UpdateOAuthTokenDto>
{
    /// <summary>
    /// 根据访问令牌获取
    /// </summary>
    /// <param name="accessToken">访问令牌</param>
    /// <returns></returns>
    Task<OAuthTokenDto?> GetByAccessTokenAsync(string accessToken);

    /// <summary>
    /// 根据刷新令牌获取
    /// </summary>
    /// <param name="refreshToken">刷新令牌</param>
    /// <returns></returns>
    Task<OAuthTokenDto?> GetByRefreshTokenAsync(string refreshToken);

    /// <summary>
    /// 根据客户端ID和用户ID获取令牌列表
    /// </summary>
    /// <param name="clientId">客户端ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    Task<List<OAuthTokenDto>> GetByClientAndUserAsync(string clientId, XiHanBasicAppIdType userId);

    /// <summary>
    /// 删除过期的令牌
    /// </summary>
    /// <returns></returns>
    Task<int> DeleteExpiredTokensAsync();
}

