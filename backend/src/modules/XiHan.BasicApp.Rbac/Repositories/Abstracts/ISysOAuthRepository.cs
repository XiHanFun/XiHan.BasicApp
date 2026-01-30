#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysOAuthRepository
// Guid:e7f8a9b0-c1d2-3456-7890-123456e12345
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/30 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Abstracts;

/// <summary>
/// OAuth仓储接口
/// </summary>
/// <remarks>
/// 聚合范围：SysOAuthApp + SysOAuthCode + SysOAuthToken
/// </remarks>
public interface ISysOAuthRepository : IAggregateRootRepository<SysOAuthApp, long>
{
    // ========== OAuth应用 ==========

    /// <summary>
    /// 根据ClientId获取应用
    /// </summary>
    Task<SysOAuthApp?> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 验证ClientSecret
    /// </summary>
    Task<bool> ValidateClientSecretAsync(string clientId, string clientSecret, CancellationToken cancellationToken = default);

    // ========== OAuth授权码 ==========

    /// <summary>
    /// 添加授权码
    /// </summary>
    Task<SysOAuthCode> AddOAuthCodeAsync(SysOAuthCode code, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据授权码获取
    /// </summary>
    Task<SysOAuthCode?> GetOAuthCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除授权码
    /// </summary>
    Task DeleteOAuthCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// 清理过期授权码
    /// </summary>
    Task CleanExpiredCodesAsync(DateTime beforeDate, CancellationToken cancellationToken = default);

    // ========== OAuth令牌 ==========

    /// <summary>
    /// 添加令牌
    /// </summary>
    Task<SysOAuthToken> AddOAuthTokenAsync(SysOAuthToken token, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据AccessToken获取令牌
    /// </summary>
    Task<SysOAuthToken?> GetByAccessTokenAsync(string accessToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据RefreshToken获取令牌
    /// </summary>
    Task<SysOAuthToken?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新令牌
    /// </summary>
    Task UpdateOAuthTokenAsync(SysOAuthToken token, CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤销令牌
    /// </summary>
    Task RevokeTokenAsync(string accessToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// 清理过期令牌
    /// </summary>
    Task CleanExpiredTokensAsync(DateTime beforeDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户的所有令牌
    /// </summary>
    Task<List<SysOAuthToken>> GetUserTokensAsync(long userId, CancellationToken cancellationToken = default);
}
