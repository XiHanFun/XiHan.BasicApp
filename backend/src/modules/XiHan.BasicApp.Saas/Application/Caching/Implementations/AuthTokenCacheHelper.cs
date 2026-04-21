#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AuthTokenCacheHelper
// Guid:d2e3f4a5-6b7c-8d9e-0f1a-2b3c4d5e6f7a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/04 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using XiHan.BasicApp.Saas.Constants.Caching;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Authentication.Jwt;
using XiHan.Framework.Caching.Distributed.Abstracts;

namespace XiHan.BasicApp.Saas.Application.Caching.Implementations;

/// <summary>
/// 认证令牌缓存辅助服务实现
/// </summary>
public class AuthTokenCacheHelper : IAuthTokenCacheHelper
{
    private readonly IDistributedCache<AuthRefreshTokenCacheItem> _refreshTokenCache;
    private readonly IDistributedCache<AuthSessionTokenMapCacheItem> _sessionTokenMapCache;
    private readonly JwtOptions _jwtOptions;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AuthTokenCacheHelper(
        IDistributedCache<AuthRefreshTokenCacheItem> refreshTokenCache,
        IDistributedCache<AuthSessionTokenMapCacheItem> sessionTokenMapCache,
        IOptions<JwtOptions> jwtOptions)
    {
        _refreshTokenCache = refreshTokenCache;
        _sessionTokenMapCache = sessionTokenMapCache;
        _jwtOptions = jwtOptions.Value;
    }

    /// <summary>
    /// 保存刷新令牌到缓存
    /// </summary>
    public async Task SaveRefreshTokenAsync(string refreshToken, SysUser user, string sessionId)
    {
        var refreshTokenCacheKey = BuildRefreshTokenCacheKey(refreshToken);
        var expireAt = DateTimeOffset.UtcNow.AddDays(Math.Max(1, _jwtOptions.RefreshTokenExpirationDays));
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = expireAt
        };

        await _refreshTokenCache.SetAsync(
            refreshTokenCacheKey,
            new AuthRefreshTokenCacheItem
            {
                UserId = user.BasicId,
                UserName = user.UserName,
                TenantId = user.TenantId,
                SessionId = sessionId,
                ExpireAt = expireAt
            },
            options: cacheOptions,
            hideErrors: true);

        await _sessionTokenMapCache.SetAsync(
            BuildSessionTokenMapCacheKey(sessionId),
            new AuthSessionTokenMapCacheItem
            {
                RefreshTokenCacheKey = refreshTokenCacheKey
            },
            options: cacheOptions,
            hideErrors: true);
    }

    /// <summary>
    /// 移除会话关联的刷新令牌缓存
    /// </summary>
    public async Task RemoveSessionTokenAsync(string sessionId)
    {
        var sessionTokenMapCacheKey = BuildSessionTokenMapCacheKey(sessionId);
        var tokenMap = await _sessionTokenMapCache.GetAsync(sessionTokenMapCacheKey, hideErrors: true);
        if (tokenMap is not null && !string.IsNullOrWhiteSpace(tokenMap.RefreshTokenCacheKey))
        {
            await _refreshTokenCache.RemoveAsync(tokenMap.RefreshTokenCacheKey, hideErrors: true);
        }

        await _sessionTokenMapCache.RemoveAsync(sessionTokenMapCacheKey, hideErrors: true);
    }

    /// <summary>
    /// 根据刷新令牌获取缓存项
    /// </summary>
    public async Task<AuthRefreshTokenCacheItem?> GetRefreshTokenAsync(string refreshToken)
    {
        var cacheKey = BuildRefreshTokenCacheKey(refreshToken);
        return await _refreshTokenCache.GetAsync(cacheKey, hideErrors: true);
    }

    /// <summary>
    /// 直接移除指定刷新令牌缓存
    /// </summary>
    public async Task RemoveRefreshTokenDirectAsync(string refreshToken)
    {
        var cacheKey = BuildRefreshTokenCacheKey(refreshToken);
        await _refreshTokenCache.RemoveAsync(cacheKey, hideErrors: true);
    }

    /// <summary>
    /// 直接移除会话映射缓存
    /// </summary>
    public async Task RemoveSessionTokenMapDirectAsync(string sessionId)
    {
        var cacheKey = BuildSessionTokenMapCacheKey(sessionId);
        await _sessionTokenMapCache.RemoveAsync(cacheKey, hideErrors: true);
    }

    private static string BuildRefreshTokenCacheKey(string refreshToken)
    {
        var tokenBytes = Encoding.UTF8.GetBytes(refreshToken);
        var tokenHash = Convert.ToHexString(SHA256.HashData(tokenBytes));
        return SaasCacheKeys.AuthRefreshToken(tokenHash);
    }

    private static string BuildSessionTokenMapCacheKey(string sessionId)
    {
        return SaasCacheKeys.AuthSessionTokenMap(sessionId);
    }
}
