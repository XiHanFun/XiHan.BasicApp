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
using XiHan.BasicApp.Upgrade.Domain.Entities;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Upgrade.Abstractions;
using XiHan.Framework.Upgrade.Options;

namespace XiHan.BasicApp.Upgrade.Infrastructure.Adapters;

/// <summary>
/// 基于 SqlSugar 的升级分布式锁
/// </summary>
public class SqlSugarUpgradeLockProvider : IUpgradeLockProvider
{
    private readonly ISqlSugarClientProvider _clientProvider;
    private readonly XiHanUpgradeOptions _options;
    private readonly ILogger<SqlSugarUpgradeLockProvider> _logger;

    public SqlSugarUpgradeLockProvider(
        ISqlSugarClientProvider clientProvider,
        IOptions<XiHanUpgradeOptions> options,
        ILogger<SqlSugarUpgradeLockProvider> logger)
    {
        _clientProvider = clientProvider;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IUpgradeLockToken?> TryAcquireLockAsync(string resourceKey, TimeSpan expiry, string nodeName, CancellationToken cancellationToken = default)
    {
        var db = _clientProvider.GetClient(_options.ConnectionConfigId);
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

    internal async Task ReleaseAsync(string resourceKey, string lockId)
    {
        var db = _clientProvider.GetClient(_options.ConnectionConfigId);
        await db.Deleteable<SysUpgradeLockEntity>()
            .Where(l => l.ResourceKey == resourceKey && l.LockId == lockId)
            .ExecuteCommandAsync();
    }

    private sealed class SqlSugarUpgradeLockToken : IUpgradeLockToken
    {
        private readonly SqlSugarUpgradeLockProvider _provider;
        private bool _isReleased;

        public SqlSugarUpgradeLockToken(string resourceKey, string lockId, SqlSugarUpgradeLockProvider provider)
        {
            ResourceKey = resourceKey;
            LockId = lockId;
            _provider = provider;
        }

        public string ResourceKey { get; }
        public string LockId { get; }
        public bool IsReleased => _isReleased;

        public async Task ReleaseAsync()
        {
            if (_isReleased)
            {
                return;
            }

            await _provider.ReleaseAsync(ResourceKey, LockId);
            _isReleased = true;
        }

        public async ValueTask DisposeAsync()
        {
            await ReleaseAsync();
        }
    }
}
