#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RedisUpgradeLockProvider
// Guid:b2c3d4e5-6f78-9012-bcde-f12345678901
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/20 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using XiHan.Framework.Caching.Distributed.Abstracts;
using XiHan.Framework.Upgrade.Abstractions;

namespace XiHan.BasicApp.Saas.Infrastructure.UpgradeAdapters;

/// <summary>
/// 分布式升级锁提供者（基于 Redis Lua 脚本）
/// </summary>
public partial class RedisUpgradeLockProvider : IUpgradeLockProvider
{
    private const string AcquireLuaScript = """
                                          if redis.call('exists', KEYS[1]) == 0 then
                                              redis.call('set', KEYS[1], ARGV[1], 'PX', ARGV[2])
                                              return 1
                                          end
                                          return 0
                                          """;

    private const string ReleaseLuaScript = """
                                          if redis.call('get', KEYS[1]) == ARGV[1] then
                                              return redis.call('del', KEYS[1])
                                          end
                                          return 0
                                          """;

    private readonly IDistributedCache<DistributedUpgradeLockCacheItem, string> _distributedCache;
    private readonly ILogger<RedisUpgradeLockProvider> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="distributedCache">分布式缓存</param>
    /// <param name="logger">日志记录器</param>
    public RedisUpgradeLockProvider(
        IDistributedCache<DistributedUpgradeLockCacheItem, string> distributedCache,
        ILogger<RedisUpgradeLockProvider> logger)
    {
        _distributedCache = distributedCache;
        _logger = logger;
    }

    /// <summary>
    /// 尝试获取升级锁
    /// </summary>
    /// <param name="resourceKey">资源键</param>
    /// <param name="expiry">锁的过期时间</param>
    /// <param name="nodeName">节点名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>返回升级锁令牌，如果获取失败则返回 null</returns>
    public async Task<IUpgradeLockToken?> TryAcquireLockAsync(
        string resourceKey,
        TimeSpan expiry,
        string nodeName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceKey);

        if (expiry <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(expiry), "锁过期时间必须大于零。");
        }

        var lockId = Guid.NewGuid().ToString("N");
        var expiryMilliseconds = Math.Max(1L, (long)expiry.TotalMilliseconds);

        try
        {
            var result = await _distributedCache.ScriptEvaluateAsync(
                AcquireLuaScript,
                [resourceKey.Trim()],
                [lockId, expiryMilliseconds],
                hideErrors: false,
                token: cancellationToken);

            if (!IsAcquired(result))
            {
                return null;
            }

            _logger.LogInformation("升级锁已获取，资源键: {ResourceKey}, 节点: {NodeName}", resourceKey, nodeName);
            return new RedisUpgradeLockToken(resourceKey.Trim(), lockId, _distributedCache, _logger);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "尝试获取分布式升级锁失败，资源键: {ResourceKey}, 节点: {NodeName}",
                resourceKey,
                nodeName);

            return null;
        }
    }

    /// <summary>
    /// 判断锁是否获取成功
    /// </summary>
    /// <param name="result">脚本执行结果</param>
    /// <returns>成功返回 true</returns>
    private static bool IsAcquired(RedisResult? result)
    {
        if (result is null)
        {
            return false;
        }

        var responseText = result.ToString();

        return result.Resp2Type switch
        {
            ResultType.Integer => (long)result == 1,
            ResultType.SimpleString => string.Equals(responseText, "OK", StringComparison.OrdinalIgnoreCase),
            _ => string.Equals(responseText, "1", StringComparison.Ordinal)
        };
    }
}
