#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IOAuthTokenRepository
// Guid:a3b4c5d6-e7f8-4a5b-9c0d-2e3f4a5b6c7d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Abstracts;

/// <summary>
/// OAuth令牌仓储接口
/// </summary>
public interface IOAuthTokenRepository : IRepositoryBase<SysOAuthToken, long>
{
    /// <summary>
    /// 根据访问令牌查询
    /// </summary>
    /// <param name="accessToken">访问令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>令牌实体</returns>
    Task<SysOAuthToken?> GetByAccessTokenAsync(string accessToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据刷新令牌查询
    /// </summary>
    /// <param name="refreshToken">刷新令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>令牌实体</returns>
    Task<SysOAuthToken?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// 验证访问令牌
    /// </summary>
    /// <param name="accessToken">访问令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>令牌实体（如果有效）</returns>
    Task<SysOAuthToken?> ValidateAccessTokenAsync(string accessToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// 刷新令牌
    /// </summary>
    /// <param name="refreshToken">刷新令牌</param>
    /// <param name="newAccessToken">新访问令牌</param>
    /// <param name="newRefreshToken">新刷新令牌</param>
    /// <param name="newAccessTokenExpiresAt">新访问令牌过期时间</param>
    /// <param name="newRefreshTokenExpiresAt">新刷新令牌过期时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新后的令牌实体</returns>
    Task<SysOAuthToken?> RefreshTokenAsync(
        string refreshToken,
        string newAccessToken,
        string newRefreshToken,
        DateTimeOffset newAccessTokenExpiresAt,
        DateTimeOffset newRefreshTokenExpiresAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤销令牌
    /// </summary>
    /// <param name="token">令牌（访问令牌或刷新令牌）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> RevokeTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// 清理过期令牌
    /// </summary>
    /// <param name="currentTime">当前时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>清理数量</returns>
    Task<int> CleanExpiredTokensAsync(DateTimeOffset currentTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户的所有令牌
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>令牌列表</returns>
    Task<List<SysOAuthToken>> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤销用户的所有令牌
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>撤销数量</returns>
    Task<int> RevokeUserTokensAsync(long userId, CancellationToken cancellationToken = default);
}
