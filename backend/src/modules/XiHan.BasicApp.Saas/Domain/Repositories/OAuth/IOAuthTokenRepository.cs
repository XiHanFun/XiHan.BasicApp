// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// OAuth Token 仓储接口
/// </summary>
public interface IOAuthTokenRepository : ISaasRepository<SysOAuthToken>
{
    /// <summary>
    /// 根据访问令牌JTI获取
    /// </summary>
    Task<SysOAuthToken?> GetByAccessTokenAsync(string accessTokenJti, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据刷新令牌获取
    /// </summary>
    Task<SysOAuthToken?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据访问令牌JTI跨租户获取（供匿名 /connect/revoke 无租户上下文场景使用）
    /// </summary>
    Task<SysOAuthToken?> GetByAccessTokenIgnoreTenantAsync(string accessTokenJti, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据刷新令牌跨租户获取（RefreshToken 全局唯一；供匿名 /connect/token、/connect/revoke 使用）
    /// </summary>
    Task<SysOAuthToken?> GetByRefreshTokenIgnoreTenantAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// 吊销用户所有令牌
    /// </summary>
    Task<int> RevokeByUserIdAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 跨租户吊销某用户在某客户端下的全部未撤销令牌（刷新令牌重放检测时吊销整个令牌族）
    /// </summary>
    Task<int> RevokeFamilyAsync(long userId, string clientId, DateTimeOffset now, CancellationToken cancellationToken = default);

    /// <summary>
    /// 跨租户吊销指定会话的全部未撤销令牌（会话下线 / 令牌轮换时同步维护令牌台账）
    /// </summary>
    Task<int> RevokeBySessionIdsAsync(IReadOnlyCollection<long> sessionIds, DateTimeOffset now, CancellationToken cancellationToken = default);
}
