#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SqlSugarUpgradeLockProvider
// Guid:f9383e62-8f7c-4d1b-8f1e-7b6d8fb9f7a1
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/01 16:49:40
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SqlSugar;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Upgrade.Abstractions;
using XiHan.Framework.Upgrade.Options;

namespace XiHan.BasicApp.Saas.Infrastructure.UpgradeAdapters;

/// <summary>
/// 基于 SqlSugar 的升级分布式锁
/// </summary>
public partial class SqlSugarUpgradeLockProvider : IUpgradeLockProvider
{
    private readonly ISqlSugarDbContext _dbContext;
    private readonly XiHanUpgradeOptions _options;
    private readonly ILogger<SqlSugarUpgradeLockProvider> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext">SqlSugar 数据上下文</param>
    /// <param name="options">升级选项</param>
    /// <param name="logger">日志记录器</param>
    public SqlSugarUpgradeLockProvider(
        ISqlSugarDbContext dbContext,
        IOptions<XiHanUpgradeOptions> options,
        ILogger<SqlSugarUpgradeLockProvider> logger)
    {
        _dbContext = dbContext;
        _options = options.Value;
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
    public async Task<IUpgradeLockToken?> TryAcquireLockAsync(string resourceKey, TimeSpan expiry, string nodeName, CancellationToken cancellationToken = default)
    {
        var db = _dbContext.GetClient(_options.ConnectionConfigId);
        var lockId = Guid.NewGuid().ToString("N");
        var now = DateTime.UtcNow;
        var expiryTime = now.Add(expiry);

        var updated = await db.Updateable<SysUpgradeLockEntity>()
            .SetColumns(l => new SysUpgradeLockEntity
            {
                LockId = lockId,
                ExpiryTime = expiryTime,
                OwnerNode = nodeName,
                UpdatedTime = now
            })
            .Where(l => l.ResourceKey == resourceKey && l.ExpiryTime < now)
            .ExecuteCommandAsync();

        if (updated > 0)
        {
            return new SqlSugarUpgradeLockToken(resourceKey, lockId, this);
        }

        try
        {
            var inserted = await db.Insertable(new SysUpgradeLockEntity
            {
                ResourceKey = resourceKey,
                LockId = lockId,
                ExpiryTime = expiryTime,
                OwnerNode = nodeName,
                CreatedTime = now,
                UpdatedTime = now
            }).ExecuteCommandAsync();

            if (inserted > 0)
            {
                return new SqlSugarUpgradeLockToken(resourceKey, lockId, this);
            }
        }
        catch (SqlSugarException ex)
        {
            _logger.LogDebug(ex, "升级锁占用: {ResourceKey}", resourceKey);
        }

        return null;
    }

    /// <summary>
    /// 释放升级锁
    /// </summary>
    /// <param name="resourceKey">资源键</param>
    /// <param name="lockId">锁标识</param>
    /// <returns></returns>
    internal async Task ReleaseAsync(string resourceKey, string lockId)
    {
        var db = _dbContext.GetClient(_options.ConnectionConfigId);
        await db.Deleteable<SysUpgradeLockEntity>()
            .Where(l => l.ResourceKey == resourceKey && l.LockId == lockId)
            .ExecuteCommandAsync();
    }
}
