#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysOAuthTokenService
// Guid:k1l2m3n4-o5p6-7890-abcd-ef1234567890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 19:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Repositories.OAuthTokens;
using XiHan.BasicApp.Rbac.Services.OAuthTokens.Dtos;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Services.OAuthTokens;

/// <summary>
/// 系统OAuth令牌服务实现
/// </summary>
public class SysOAuthTokenService : CrudApplicationServiceBase<SysOAuthToken, OAuthTokenDto, long, CreateOAuthTokenDto, UpdateOAuthTokenDto>, ISysOAuthTokenService
{
    private readonly ISysOAuthTokenRepository _oauthTokenRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysOAuthTokenService(ISysOAuthTokenRepository oauthTokenRepository) : base(oauthTokenRepository)
    {
        _oauthTokenRepository = oauthTokenRepository;
    }

    #region 业务特定方法

    /// <summary>
    /// 根据访问令牌获取
    /// </summary>
    public async Task<OAuthTokenDto?> GetByAccessTokenAsync(string accessToken)
    {
        var token = await _oauthTokenRepository.GetByAccessTokenAsync(accessToken);
        return token?.Adapt<OAuthTokenDto>();
    }

    /// <summary>
    /// 根据刷新令牌获取
    /// </summary>
    public async Task<OAuthTokenDto?> GetByRefreshTokenAsync(string refreshToken)
    {
        var token = await _oauthTokenRepository.GetByRefreshTokenAsync(refreshToken);
        return token?.Adapt<OAuthTokenDto>();
    }

    /// <summary>
    /// 根据客户端ID和用户ID获取令牌列表
    /// </summary>
    public async Task<List<OAuthTokenDto>> GetByClientAndUserAsync(string clientId, long userId)
    {
        var tokens = await _oauthTokenRepository.GetByClientAndUserAsync(clientId, userId);
        return tokens.Adapt<List<OAuthTokenDto>>();
    }

    /// <summary>
    /// 删除过期的令牌
    /// </summary>
    public async Task<int> DeleteExpiredTokensAsync()
    {
        return await _oauthTokenRepository.DeleteExpiredTokensAsync();
    }

    #endregion 业务特定方法
}
