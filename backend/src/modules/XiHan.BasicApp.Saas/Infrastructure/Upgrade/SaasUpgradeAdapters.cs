// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using SqlSugar;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Upgrade.Abstractions;

namespace XiHan.BasicApp.Saas.Infrastructure.Upgrade;

/// <summary>
/// 升级分布式锁：以 <see cref="SysUpgradeLock"/> 之外的既有版本行承载租约，避免为一把锁单开一张表。
/// </summary>
/// <remarks>
/// 采用「条件更新抢占 + 到期自动失效」的租约模型，而非数据库会话级建议锁：
/// 连接配置 IsAutoCloseConnection=true 会在每条命令后归还连接，会话级锁随之释放，
/// 而引擎需要在整个升级过程（跨多条命令、可能跨事务）持有它。
/// <para>
/// 抢占条件为「当前无人持有」或「持有者已超过租约时长仍未释放」，后者用于回收崩溃节点遗留的锁。
/// </para>
/// </remarks>
public sealed class SaasUpgradeLockProvider : IUpgradeLockProvider
{
    private readonly ISqlSugarClientResolver _clientResolver;
    private readonly ILogger<SaasUpgradeLockProvider> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SaasUpgradeLockProvider(ISqlSugarClientResolver clientResolver, ILogger<SaasUpgradeLockProvider> logger)
    {
        _clientResolver = clientResolver;
        _logger = logger;
    }

    /// <summary>
    /// 尝试获取锁
    /// </summary>
    /// <param name="resourceKey"></param>
    /// <param name="expiry"></param>
    /// <param name="nodeName"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IUpgradeLockToken?> TryAcquireLockAsync(string resourceKey, TimeSpan expiry, string nodeName, CancellationToken cancellationToken = default)
    {
        var db = _clientResolver.GetCurrentClient();
        var now = DateTimeOffset.UtcNow;
        var staleBefore = now - expiry;

        // 单条条件更新即抢占：数据库保证同一行的并发更新串行化，只有一个节点能把 IsUpgrading 从 false 翻成 true
        var affected = await db.Updateable<SysVersion>()
            .SetColumns(row => new SysVersion
            {
                IsUpgrading = true,
                UpgradeNode = nodeName,
                UpgradeStartTime = now
            })
            .Where(row => !row.IsUpgrading || row.UpgradeStartTime == null || row.UpgradeStartTime < staleBefore)
            .ExecuteCommandAsync(cancellationToken);

        if (affected == 0)
        {
            _logger.LogWarning("升级锁 {ResourceKey} 已被其它节点持有且未到期。", resourceKey);
            return null;
        }

        return new SaasUpgradeLockToken(db, resourceKey, nodeName);
    }

    /// <summary>
    /// 租约令牌：释放即把版本行的升级中标记清回 false。
    /// </summary>
    private sealed class SaasUpgradeLockToken : IUpgradeLockToken
    {
        private readonly ISqlSugarClient _db;

        public SaasUpgradeLockToken(ISqlSugarClient db, string resourceKey, string nodeName)
        {
            _db = db;
            ResourceKey = resourceKey;
            LockId = nodeName;
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
        public bool IsReleased { get; private set; }

        /// <summary>
        /// 释放锁
        /// </summary>
        public async Task ReleaseAsync()
        {
            if (IsReleased)
            {
                return;
            }

            _ = await _db.Updateable<SysVersion>()
                .SetColumns(row => new SysVersion { IsUpgrading = false })
                .Where(row => row.UpgradeNode == LockId)
                .ExecuteCommandAsync();

            IsReleased = true;
        }

        /// <summary>
        /// 异步释放占用的资源
        /// </summary>
        /// <returns>释放任务</returns>
        public async ValueTask DisposeAsync()
        {
            await ReleaseAsync();
        }
    }
}

/// <summary>
/// 升级租户分发：列出需要随平台库一同升级的库隔离租户。
/// </summary>
/// <remarks>
/// 只取已完成初始化（Configured）的库隔离租户；未完成初始化者其独立库可能尚不存在，
/// 待其初始化流程建表后下次启动自然纳入。字段隔离租户与平台库共库，无需单独升级。
/// </remarks>
public sealed class SaasUpgradeTenantProvider : IUpgradeTenantProvider
{
    private readonly ISqlSugarClientResolver _clientResolver;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SaasUpgradeTenantProvider(ISqlSugarClientResolver clientResolver)
    {
        _clientResolver = clientResolver;
    }

    /// <summary>
    /// 获取租户列表
    /// </summary>
    public IReadOnlyList<BasicTenantInfo> GetTenants()
    {
        // 整行取实体后在内存里投影：SqlSugar 物化结果时调用目标类型的无参构造函数，
        // BasicTenantInfo 只有带参构造函数，直接投影会取不到构造器而抛异常。
        var tenants = _clientResolver.GetCurrentClient()
            .Queryable<SysTenant>()
            .Where(tenant => !tenant.IsDeleted)
            .Where(tenant => tenant.IsolationMode == TenantIsolationMode.Database)
            .Where(tenant => tenant.ConfigStatus == TenantConfigStatus.Configured)
            .ToList();

        // 平台库自身以 null 租户参与升级，排在各租户库之前
        return [new BasicTenantInfo(null, "平台库"), .. tenants.Select(tenant => new BasicTenantInfo(tenant.BasicId, tenant.TenantName))];
    }
}

/// <summary>
/// 升级迁移执行器：在当前租户上下文解析到的库上执行脚本。
/// </summary>
public sealed class SaasUpgradeMigrationExecutor : IUpgradeMigrationExecutor
{
    private readonly ISqlSugarClientResolver _clientResolver;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SaasUpgradeMigrationExecutor(ISqlSugarClientResolver clientResolver)
    {
        _clientResolver = clientResolver;
    }

    /// <summary>
    /// 执行迁移脚本（内部保证事务）
    /// </summary>
    /// <param name="sql"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task ExecuteAsync(string sql, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return;
        }

        _ = await _clientResolver.GetCurrentClient().Ado.ExecuteCommandAsync(sql);
    }
}
