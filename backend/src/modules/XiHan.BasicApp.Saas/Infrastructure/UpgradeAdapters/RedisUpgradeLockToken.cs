#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RedisUpgradeLockToken
// Guid:c3d4e5f6-7890-1234-cdef-234567890123
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/20 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.Framework.Caching.Distributed.Abstracts;
using XiHan.Framework.Upgrade.Abstractions;

namespace XiHan.BasicApp.Saas.Infrastructure.UpgradeAdapters;

public partial class RedisUpgradeLockProvider
{
    /// <summary>
    /// 升级锁令牌实现
    /// </summary>
    private sealed class RedisUpgradeLockToken : IUpgradeLockToken
    {
        private readonly IDistributedCache<DistributedUpgradeLockCacheItem, string> _distributedCache;
        private readonly ILogger _logger;
        private int _released;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="resourceKey">资源键</param>
        /// <param name="lockId">锁标识</param>
        /// <param name="distributedCache">分布式缓存</param>
        /// <param name="logger">日志记录器</param>
        public RedisUpgradeLockToken(
            string resourceKey,
            string lockId,
            IDistributedCache<DistributedUpgradeLockCacheItem, string> distributedCache,
            ILogger logger)
        {
            ResourceKey = resourceKey;
            LockId = lockId;
            _distributedCache = distributedCache;
            _logger = logger;
        }

        /// <summary>
        /// 资源键
        /// </summary>
        public string ResourceKey { get; }

        /// <summary>
        /// 锁标识
        /// </summary>
        public string LockId { get; }

        /// <summary>
        /// 是否已释放
        /// </summary>
        public bool IsReleased => Volatile.Read(ref _released) == 1;

        /// <summary>
        /// 释放升级锁
        /// </summary>
        /// <returns></returns>
        public async Task ReleaseAsync()
        {
            if (Interlocked.Exchange(ref _released, 1) == 1)
            {
                return;
            }

            try
            {
                await _distributedCache.ScriptEvaluateAsync(
                    ReleaseLuaScript,
                    [ResourceKey],
                    [LockId],
                    hideErrors: false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "释放分布式升级锁失败，资源键: {ResourceKey}, 锁ID: {LockId}",
                    ResourceKey,
                    LockId);
            }
        }

        /// <summary>
        /// 异步释放升级锁
        /// </summary>
        /// <returns></returns>
        public async ValueTask DisposeAsync()
        {
            await ReleaseAsync();
        }
    }
}
