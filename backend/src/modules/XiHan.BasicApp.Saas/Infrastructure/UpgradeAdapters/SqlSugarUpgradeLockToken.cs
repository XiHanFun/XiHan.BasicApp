#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SqlSugarUpgradeLockToken
// Guid:d7c28d3c-d957-4fad-87fe-64c2e80fdb18
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/01 16:49:40
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.Framework.Upgrade.Abstractions;

namespace XiHan.BasicApp.Saas.Infrastructure.UpgradeAdapters;

public partial class SqlSugarUpgradeLockProvider
{
    /// <summary>
    /// 升级锁令牌实现
    /// </summary>
    private sealed class SqlSugarUpgradeLockToken : IUpgradeLockToken
    {
        private readonly SqlSugarUpgradeLockProvider _provider;
        private bool _isReleased;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="resourceKey">资源键</param>
        /// <param name="lockId">锁标识</param>
        /// <param name="provider">升级锁提供者</param>
        public SqlSugarUpgradeLockToken(string resourceKey, string lockId, SqlSugarUpgradeLockProvider provider)
        {
            ResourceKey = resourceKey;
            LockId = lockId;
            _provider = provider;
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
        public bool IsReleased => _isReleased;

        /// <summary>
        /// 释放升级锁
        /// </summary>
        /// <returns></returns>
        public async Task ReleaseAsync()
        {
            if (_isReleased)
            {
                return;
            }

            await _provider.ReleaseAsync(ResourceKey, LockId);
            _isReleased = true;
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
