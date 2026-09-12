// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Caching.Distributed;
using System.Security.Cryptography;
using System.Text;
using XiHan.Framework.Authentication.Jwt;

namespace XiHan.BasicApp.Saas.Infrastructure.Auth;

/// <summary>
/// SaaS 刷新令牌存储：使用应用配置的分布式缓存实现跨实例共享与自动过期
/// </summary>
public sealed class SaasRefreshTokenStore : IRefreshTokenStore
{
    private const string CacheKeyPrefix = "basicapp:auth:refresh-token:";

    private readonly IDistributedCache _cache;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="cache">分布式缓存</param>
    public SaasRefreshTokenStore(IDistributedCache cache)
    {
        _cache = cache;
    }

    /// <inheritdoc />
    public void Save(string refreshToken, string? subject, DateTime expiresAt)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        var expiration = ToUtcOffset(expiresAt);
        if (expiration <= DateTimeOffset.UtcNow)
        {
            _cache.Remove(BuildCacheKey(refreshToken));
            return;
        }

        _cache.SetString(
            BuildCacheKey(refreshToken),
            subject ?? string.Empty,
            new DistributedCacheEntryOptions { AbsoluteExpiration = expiration });
    }

    /// <inheritdoc />
    public bool Validate(string refreshToken, string? subject = null)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return false;
        }

        var storedSubject = _cache.GetString(BuildCacheKey(refreshToken));
        return storedSubject is not null
               && (string.IsNullOrWhiteSpace(subject)
                   || string.Equals(storedSubject, subject, StringComparison.Ordinal));
    }

    /// <inheritdoc />
    public void Remove(string refreshToken)
    {
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            _cache.Remove(BuildCacheKey(refreshToken));
        }
    }

    private static string BuildCacheKey(string refreshToken)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return CacheKeyPrefix + Convert.ToHexString(hash);
    }

    private static DateTimeOffset ToUtcOffset(DateTime value)
    {
        return value.Kind == DateTimeKind.Unspecified
            ? new DateTimeOffset(value, TimeSpan.Zero)
            : new DateTimeOffset(value.ToUniversalTime());
    }
}
