// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// OAuth 令牌仓储实现
/// </summary>
public sealed class OAuthTokenRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysOAuthToken>(clientResolver), IOAuthTokenRepository
{
    /// <summary>
    /// 根据访问令牌JTI获取
    /// </summary>
    public async Task<SysOAuthToken?> GetByAccessTokenAsync(string accessTokenJti, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessTokenJti);
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(t => t.AccessTokenJti == accessTokenJti)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 根据刷新令牌获取
    /// </summary>
    public async Task<SysOAuthToken?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(t => t.RefreshToken == refreshToken)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 根据访问令牌JTI跨租户获取（供匿名 /connect/revoke 无租户上下文场景使用）
    /// </summary>
    public async Task<SysOAuthToken?> GetByAccessTokenIgnoreTenantAsync(string accessTokenJti, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessTokenJti);
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateNoTenantQueryable()
            .Where(t => t.AccessTokenJti == accessTokenJti)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 根据刷新令牌跨租户获取（RefreshToken 全局唯一；供匿名 /connect/token、/connect/revoke 使用）
    /// </summary>
    public async Task<SysOAuthToken?> GetByRefreshTokenIgnoreTenantAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateNoTenantQueryable()
            .Where(t => t.RefreshToken == refreshToken)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 吊销用户所有令牌
    /// </summary>
    public async Task<int> RevokeByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await DbClient.Updateable<SysOAuthToken>()
            .SetColumns(t => t.IsRevoked == true)
            .Where(t => t.UserId == userId && !t.IsRevoked)
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 跨租户吊销某用户在某客户端下的全部未撤销令牌（刷新令牌重放检测时吊销整个令牌族）
    /// </summary>
    public async Task<int> RevokeFamilyAsync(long userId, string clientId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        cancellationToken.ThrowIfCancellationRequested();

        // 条件更新不受租户查询过滤影响；按 用户 × 客户端 吊销全部未撤销令牌（重放检测时吊销整个令牌族）
        return await DbClient.Updateable<SysOAuthToken>()
            .SetColumns(t => new SysOAuthToken { IsRevoked = true, RevokedTime = now })
            .Where(t => t.UserId == userId && t.ClientId == clientId && !t.IsRevoked)
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 跨租户吊销指定会话的全部未撤销令牌（会话下线 / 令牌轮换时同步维护令牌台账）
    /// </summary>
    public async Task<int> RevokeBySessionIdsAsync(IReadOnlyCollection<long> sessionIds, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sessionIds);
        cancellationToken.ThrowIfCancellationRequested();

        if (sessionIds.Count == 0)
        {
            return 0;
        }

        // 条件更新不受租户查询过滤影响；令牌行带发起登录时租户戳，会话跨租户下线时须一并吊销
        var ids = sessionIds.ToList();
        return await DbClient.Updateable<SysOAuthToken>()
            .SetColumns(t => new SysOAuthToken { IsRevoked = true, RevokedTime = now })
            .Where(t => t.SessionId != null && ids.Contains(t.SessionId.Value) && !t.IsRevoked)
            .ExecuteCommandAsync(cancellationToken);
    }
}
