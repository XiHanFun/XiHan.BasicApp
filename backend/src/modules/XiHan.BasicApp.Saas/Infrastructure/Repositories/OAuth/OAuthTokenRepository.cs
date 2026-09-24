// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Domain.Repositories;

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
    /// 跨租户吊销某用户在某客户端下的全部未撤销令牌（刷新令牌重放检测时吊销整个令牌族）
    /// </summary>
    public async Task<int> RevokeFamilyAsync(long userId, string clientId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        cancellationToken.ThrowIfCancellationRequested();

        // 令牌族可能跨租户（令牌行带签发时的租户戳）：显式跨租户取出后按主键吊销
        var tokens = await CreateNoTenantQueryable()
            .Where(t => t.UserId == userId && t.ClientId == clientId && !t.IsRevoked)
            .ToListAsync(cancellationToken);
        return await RevokeAllAsync(tokens, now, cancellationToken);
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

        // 令牌行带发起登录时的租户戳，会话跨租户下线时须一并吊销：显式跨租户取出后按主键吊销
        var ids = sessionIds.ToList();
        var tokens = await CreateNoTenantQueryable()
            .Where(t => t.SessionId != null && ids.Contains(t.SessionId.Value) && !t.IsRevoked)
            .ToListAsync(cancellationToken);
        return await RevokeAllAsync(tokens, now, cancellationToken);
    }

    /// <summary>
    /// 跨租户判断客户端是否签发过令牌
    /// </summary>
    public async Task<bool> AnyByClientIdIgnoreTenantAsync(string clientId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateNoTenantQueryable()
            .Where(token => token.ClientId == clientId)
            .AnyAsync(cancellationToken);
    }

    /// <summary>
    /// 批量置为已吊销
    /// </summary>
    /// <remarks>
    /// 走对象式 Updateable：表达式式工厂会自动挂上全局租户过滤，把 UPDATE 收窄到当前作用域，
    /// 别的租户戳的令牌就改不动了。令牌是用户自有行，按主键写并显式声明写边界豁免。
    /// </remarks>
    private async Task<int> RevokeAllAsync(List<SysOAuthToken> tokens, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (tokens.Count == 0)
        {
            return 0;
        }

        foreach (var token in tokens)
        {
            token.IsRevoked = true;
            token.RevokedTime = now;
        }

        using (TenantWriteGuard.Suppress())
        {
            return await DbClient.Updateable(tokens)
                .UpdateColumns(token => new { token.IsRevoked, token.RevokedTime })
                .ExecuteCommandAsync(cancellationToken);
        }
    }
}
