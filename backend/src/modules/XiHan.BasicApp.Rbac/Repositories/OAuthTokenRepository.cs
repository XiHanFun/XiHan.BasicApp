#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OAuthTokenRepository
// Guid:a3b4c5d6-e7f8-4a5b-9c0d-2e3f4a5b6c7d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/8 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Repositories.Abstracts;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;

namespace XiHan.BasicApp.Rbac.Repositories;

/// <summary>
/// OAuth令牌仓储实现
/// </summary>
public class OAuthTokenRepository : SqlSugarRepositoryBase<SysOAuthToken, long>, IOAuthTokenRepository
{
    private readonly ISqlSugarClient _dbClient;

    /// <summary>
    /// 构造函数
    /// </summary>
    public OAuthTokenRepository(ISqlSugarDbContext dbContext)
        : base(dbContext)
    {
        _dbClient = dbContext.GetClient();
    }

    /// <summary>
    /// 根据访问令牌查询
    /// </summary>
    public async Task<SysOAuthToken?> GetByAccessTokenAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysOAuthToken>()
            .FirstAsync(t => t.AccessToken == accessToken, cancellationToken);
    }

    /// <summary>
    /// 根据刷新令牌查询
    /// </summary>
    public async Task<SysOAuthToken?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysOAuthToken>()
            .FirstAsync(t => t.RefreshToken == refreshToken, cancellationToken);
    }

    /// <summary>
    /// 验证访问令牌
    /// </summary>
    public async Task<SysOAuthToken?> ValidateAccessTokenAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var currentTime = DateTimeOffset.UtcNow;
        return await _dbClient.Queryable<SysOAuthToken>()
            .FirstAsync(t => t.AccessToken == accessToken
                && !t.IsRevoked
                && t.AccessTokenExpiresAt > currentTime, cancellationToken);
    }

    /// <summary>
    /// 刷新令牌
    /// </summary>
    public async Task<SysOAuthToken?> RefreshTokenAsync(
        string refreshToken,
        string newAccessToken,
        string newRefreshToken,
        DateTimeOffset newAccessTokenExpiresAt,
        DateTimeOffset newRefreshTokenExpiresAt,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var currentTime = DateTimeOffset.UtcNow;
        var token = await _dbClient.Queryable<SysOAuthToken>()
            .FirstAsync(t => t.RefreshToken == refreshToken
                && !t.IsRevoked
                && t.RefreshTokenExpiresAt > currentTime, cancellationToken);

        if (token != null)
        {
            await _dbClient.Updateable<SysOAuthToken>()
                .SetColumns(t => new SysOAuthToken
                {
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken,
                    AccessTokenExpiresAt = newAccessTokenExpiresAt,
                    RefreshTokenExpiresAt = newRefreshTokenExpiresAt
                })
                .Where(t => t.BasicId == token.BasicId)
                .ExecuteCommandAsync(cancellationToken);

            token.AccessToken = newAccessToken;
            token.RefreshToken = newRefreshToken;
            token.AccessTokenExpiresAt = newAccessTokenExpiresAt;
            token.RefreshTokenExpiresAt = newRefreshTokenExpiresAt;
        }

        return token;
    }

    /// <summary>
    /// 撤销令牌
    /// </summary>
    public async Task<bool> RevokeTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var affectedRows = await _dbClient.Updateable<SysOAuthToken>()
            .SetColumns(t => new SysOAuthToken { IsRevoked = true })
            .Where(t => t.AccessToken == token || t.RefreshToken == token)
            .ExecuteCommandAsync(cancellationToken);

        return affectedRows > 0;
    }

    /// <summary>
    /// 清理过期令牌
    /// </summary>
    public async Task<int> CleanExpiredTokensAsync(DateTimeOffset currentTime, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Deleteable<SysOAuthToken>()
            .Where(t => t.RefreshTokenExpiresAt < currentTime)
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 获取用户的所有令牌
    /// </summary>
    public async Task<List<SysOAuthToken>> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysOAuthToken>()
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.CreatedTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 撤销用户的所有令牌
    /// </summary>
    public async Task<int> RevokeUserTokensAsync(long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Updateable<SysOAuthToken>()
            .SetColumns(t => new SysOAuthToken { IsRevoked = true })
            .Where(t => t.UserId == userId)
            .ExecuteCommandAsync(cancellationToken);
    }
}
