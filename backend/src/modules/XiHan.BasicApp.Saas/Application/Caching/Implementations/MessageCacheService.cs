#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MessageCacheService
// Guid:640dd2dd-b9dd-49b4-852e-3562b9fc0710
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 13:50:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using XiHan.BasicApp.Saas.Constants.Caching;
using XiHan.BasicApp.Saas.Constants.Settings;
using XiHan.Framework.Caching.Distributed.Abstracts;

namespace XiHan.BasicApp.Saas.Application.Caching.Implementations;

/// <summary>
/// 消息缓存服务实现
/// </summary>
public class MessageCacheService : IMessageCacheService
{
    private static readonly DistributedCacheEntryOptions VersionCacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30)
    };

    private readonly IDistributedCache<MessageCacheVersionItem> _versionCache;
    private readonly IDistributedCache<MessageUnreadCountCacheItem> _unreadCountCache;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// 构造函数
    /// </summary>
    public MessageCacheService(
        IDistributedCache<MessageCacheVersionItem> versionCache,
        IDistributedCache<MessageUnreadCountCacheItem> unreadCountCache,
        IConfiguration configuration)
    {
        _versionCache = versionCache;
        _unreadCountCache = unreadCountCache;
        _configuration = configuration;
    }

    /// <summary>
    /// 获取用户未读数量
    /// </summary>
    public async Task<int> GetUnreadCountAsync(
        long userId,
        long? tenantId,
        Func<CancellationToken, Task<int>> factory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(factory);
        if (userId <= 0)
        {
            return 0;
        }

        var version = await GetUnreadVersionAsync(tenantId, cancellationToken);
        var cacheKey = BuildUnreadUserKey(tenantId, userId, version);
        var item = await _unreadCountCache.GetOrAddAsync(
            cacheKey,
            async () => new MessageUnreadCountCacheItem
            {
                Count = await factory(cancellationToken)
            },
            optionsFactory: BuildUnreadCacheOptions,
            token: cancellationToken);

        return item?.Count ?? 0;
    }

    /// <summary>
    /// 失效未读缓存
    /// </summary>
    public async Task InvalidateUnreadCountAsync(long? tenantId, CancellationToken cancellationToken = default)
    {
        var versionKey = BuildUnreadVersionKey(tenantId);
        var version = await GetUnreadVersionAsync(tenantId, cancellationToken);
        await _versionCache.SetAsync(
            versionKey,
            new MessageCacheVersionItem { Version = version + 1 },
            options: VersionCacheOptions,
            considerUow: true,
            token: cancellationToken);
    }

    /// <summary>
    /// 获取未读缓存版本
    /// </summary>
    public async Task<long> GetUnreadVersionAsync(long? tenantId, CancellationToken cancellationToken = default)
    {
        var item = await _versionCache.GetOrAddAsync(
            BuildUnreadVersionKey(tenantId),
            () => Task.FromResult(new MessageCacheVersionItem { Version = 1 }),
            optionsFactory: () => VersionCacheOptions,
            token: cancellationToken);

        return item?.Version > 0 ? item.Version : 1;
    }

    private static string BuildUnreadVersionKey(long? tenantId)
    {
        return SaasCacheKeys.MessageUnreadVersion(tenantId);
    }

    private static string BuildUnreadUserKey(long? tenantId, long userId, long version)
    {
        return SaasCacheKeys.MessageUnread(tenantId, userId, version);
    }

    private DistributedCacheEntryOptions BuildUnreadCacheOptions()
    {
        var absoluteMinutes = Math.Clamp(
            _configuration.GetValue(SaasSettingKeys.Caching.MessageUnreadAbsoluteExpirationMinutes, 10),
            1,
            1440);
        var slidingMinutes = Math.Clamp(
            _configuration.GetValue(SaasSettingKeys.Caching.MessageUnreadSlidingExpirationMinutes, 3),
            1,
            absoluteMinutes);

        return new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(absoluteMinutes),
            SlidingExpiration = TimeSpan.FromMinutes(slidingMinutes)
        };
    }
}
