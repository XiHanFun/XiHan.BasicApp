// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Data.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlSugar;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Runtime.MultiTenancy;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.MultiTenancy.Abstractions.Sharding;
using XiHan.Framework.MultiTenancy.Abstractions.Tenants;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Runtime.Schema;

/// <summary>
/// 租户独立库的建库、建表与升级
/// </summary>
/// <remarks>
/// 方案 5.10、5.14：库隔离租户的独立库只建落租户库的实体（<see cref="EntityTypeCatalog.TenantScoped"/>），按当前实体建表即是最新结构，
/// 随即在该库自己的台账（<see cref="SysVersion"/>）登记当前结构版本；之后升级与平台库同一套（<see cref="SchemaMigration"/>）：先跑比台账新的、本库类型的
/// <c>Schema/&lt;版本&gt;/&lt;库类型&gt;/tenant/*.sql</c>，再按实体补缺的表与列。独立库的库类型可以与平台库不同，脚本按它自己的类型取
/// 都在该租户的分片连接上做：自建服务作用域、先经框架的 <see cref="TenantShardScope"/> 按租户目录绑定分片（不经管道，不看应用状态与租户是否启用），再取持久化客户端
/// </remarks>
/// <param name="scopes">服务作用域工厂</param>
/// <param name="entities">实体类型清单</param>
/// <param name="options">结构升级配置</param>
/// <param name="clock">时间源</param>
/// <param name="logger">日志</param>
public sealed partial class TenantSchemas(IServiceScopeFactory scopes, EntityTypeCatalog entities, SchemaOptions options, TimeProvider clock, ILogger<TenantSchemas> logger)
{
    /// <summary>
    /// 租户目录里标记独立库已就绪的属性
    /// </summary>
    /// <remarks>
    /// 值为 <c>true</c> 时启动升级才去升这个租户的库（配了连接还没初始化的库可能还不存在）
    /// </remarks>
    public const string ReadyProperty = "BasicApp.DatabaseReady";

    /// <summary>
    /// 在租户的分片上建库、建表并登记台账
    /// </summary>
    /// <remarks>
    /// 幂等：库与表已有即不动，台账已有即保留。建库不能在事务里执行，框架的事务在第一次写入时才开，这里先于任何写入
    /// </remarks>
    public async Task<Result<string>> InitializeAsync(string tenantId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        await using var scope = scopes.CreateAsyncScope();
        var bound = await BindAsync(scope.ServiceProvider, tenantId, cancellationToken).ConfigureAwait(false);
        if (!bound.TryGetValue(out var shard))
        {
            return bound.Error!;
        }

        var discovered = SchemaScripts.Discover(options.Directory, DatabaseProviders.NameOf(shard.CurrentConnectionConfig.DbType));
        if (!discovered.TryGetValue(out var versions))
        {
            return discovered.Error!;
        }

        try
        {
            // Oracle 的库是 DBA 事先建好的用户，不建库
            if (shard.CurrentConnectionConfig.DbType != DbType.Oracle)
            {
                shard.DbMaintenance.CreateDatabase();
            }

            var snapshot = SnapshotReads.Ensure(shard, RuntimeErrors.TenantDatabaseFailed);
            if (snapshot.IsFailure)
            {
                return snapshot.Error!;
            }

            var types = entities.TenantScoped.Append(typeof(SysVersion)).Append(typeof(SysMigrationHistory)).ToArray();
            SchemaMigration.Sync(shard, types);
            var current = SchemaScripts.NameOf(SchemaScripts.Current(versions));
            if (!await shard.Queryable<SysVersion>().AnyAsync(v => v.Id == SysVersion.RowId, cancellationToken).ConfigureAwait(false))
            {
                await shard.Insertable(new SysVersion { Id = SysVersion.RowId, AppVersion = AppInfo.Version, DbVersion = current, UpgradedAt = clock.GetUtcNow(), ConcurrencyStamp = Guid.NewGuid().ToString("N") })
                    .ExecuteCommandAsync(cancellationToken).ConfigureAwait(false);
            }

            LogInitialized(logger, tenantId, current, types.Length);
            return current;
        }
        catch (Exception exception) when (exception is DbException or SqlSugarException or NotSupportedException or FrameworkException)
        {
            return Error.Literal(RuntimeErrors.TenantDatabaseFailed, $"Initializing the database of tenant '{tenantId}' failed: {exception.Message}");
        }
    }

    /// <summary>
    /// 把租户的分片升到应用的结构版本
    /// </summary>
    /// <remarks>
    /// 只对已初始化的库调用；库比应用新即 <see cref="RuntimeErrors.SchemaNewerThanApp"/>。每一步成功与失败都记进该库的升级记录，失败即停
    /// 升级期间锁这个库自己的台账行（<see cref="SchemaLock"/>）：启动升级与手动重试不会同时升同一个库
    /// </remarks>
    /// <param name="tenantId">租户 ID</param>
    /// <param name="run">这一次升级</param>
    /// <param name="wait">别的节点正在升级这个库时最多等多久</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task<Result<SchemaUpgradeReport>> UpgradeAsync(string tenantId, SchemaRun run, TimeSpan wait, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        await using var scope = scopes.CreateAsyncScope();
        var bound = await BindAsync(scope.ServiceProvider, tenantId, cancellationToken).ConfigureAwait(false);
        if (!bound.TryGetValue(out var shard))
        {
            return bound.Error!;
        }

        var discovered = SchemaScripts.Discover(options.Directory, DatabaseProviders.NameOf(shard.CurrentConnectionConfig.DbType));
        if (!discovered.TryGetValue(out var versions))
        {
            return discovered.Error!;
        }

        try
        {
            var snapshot = SnapshotReads.Ensure(shard, RuntimeErrors.TenantDatabaseFailed);
            if (snapshot.IsFailure)
            {
                return snapshot.Error!;
            }

            var row = await shard.Queryable<SysVersion>().FirstAsync(v => v.Id == SysVersion.RowId, cancellationToken).ConfigureAwait(false);
            if (row?.DbVersion is null)
            {
                return Error.Literal(RuntimeErrors.TenantDatabaseFailed, $"The database of tenant '{tenantId}' has no schema version; initialize it first.");
            }

            var locked = await SchemaLock.AcquireAsync(shard, run.Node, options.LockExpiry, wait, clock, logger, cancellationToken).ConfigureAwait(false);
            if (locked.IsFailure)
            {
                return Error.Literal(locked.Error!.Code, $"Tenant '{tenantId}': {locked.Error.Message}");
            }

            try
            {
                var types = entities.TenantScoped.Append(typeof(SysVersion)).Append(typeof(SysMigrationHistory)).ToArray();
                var upgraded = await SchemaMigration.ApplyAsync(shard, () => SchemaMigration.Sync(shard, types), versions, tenant: true, run, clock, logger, cancellationToken).ConfigureAwait(false);
                return upgraded.IsFailure
                    ? Error.Literal(upgraded.Error!.Code, $"Tenant '{tenantId}': {upgraded.Error.Text ?? upgraded.Error.Code}")
                    : upgraded;
            }
            finally
            {
                await SchemaLock.ReleaseAsync(shard, run.Node, CancellationToken.None).ConfigureAwait(false);
            }
        }
        catch (Exception exception) when (exception is DbException or SqlSugarException)
        {
            return Error.Literal(RuntimeErrors.SchemaUpgradeFailed, $"Upgrading the database of tenant '{tenantId}' failed: {exception.Message}");
        }
    }

    /// <summary>
    /// 释放租户独立库的升级锁
    /// </summary>
    /// <param name="tenantId">租户 ID</param>
    /// <param name="node">持锁的节点</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>锁确实在这个节点手里、已释放为 <see langword="true"/></returns>
    public Task<Result<bool>> ReleaseLockAsync(string tenantId, string node, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(node);
        return ReadAsync(tenantId, (shard, ct) => SchemaLock.ReleaseAsync(shard, node, ct), cancellationToken);
    }

    /// <summary>
    /// 预览租户独立库要做的结构变更
    /// </summary>
    /// <param name="tenantId">租户 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task<Result<SchemaPlan>> PlanAsync(string tenantId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        await using var scope = scopes.CreateAsyncScope();
        var bound = await BindAsync(scope.ServiceProvider, tenantId, cancellationToken).ConfigureAwait(false);
        if (!bound.TryGetValue(out var shard))
        {
            return bound.Error!;
        }

        var discovered = SchemaScripts.Discover(options.Directory, DatabaseProviders.NameOf(shard.CurrentConnectionConfig.DbType));
        if (!discovered.TryGetValue(out var versions))
        {
            return discovered.Error!;
        }

        try
        {
            var types = entities.TenantScoped.Append(typeof(SysVersion)).Append(typeof(SysMigrationHistory));
            return SchemaPlanning.Plan(tenantId, shard, types.GroupBy(_ => shard), versions, tenant: true);
        }
        catch (Exception exception) when (exception is DbException or SqlSugarException || exception.GetType() == typeof(Exception))
        {
            return Error.Literal(RuntimeErrors.TenantDatabaseFailed, $"Reading the database schema of tenant '{tenantId}' failed: {exception.Message}");
        }
    }

    /// <summary>
    /// 核对租户独立库是否在当前结构版本
    /// </summary>
    /// <remarks>
    /// 读不到、没有台账、上次升级失败或结构版本不是应用的版本，即给出原因
    /// </remarks>
    /// <param name="tenantId">租户 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>不可用的原因；在当前版本为无值</returns>
    public async Task<Maybe<SchemaUpgradeFailure>> CheckAsync(string tenantId, CancellationToken cancellationToken)
    {
        var checkedAt = clock.GetUtcNow();
        var read = await ReadAsync(tenantId, async (shard, ct) =>
        {
            var discovered = SchemaScripts.Discover(options.Directory, DatabaseProviders.NameOf(shard.CurrentConnectionConfig.DbType));
            if (!discovered.TryGetValue(out var versions))
            {
                return Maybe.Some(new SchemaUpgradeFailure(tenantId, discovered.Error!.Code, discovered.Error.Message, checkedAt));
            }

            var target = SchemaScripts.NameOf(SchemaScripts.Current(versions));
            var row = await shard.Queryable<SysVersion>().FirstAsync(v => v.Id == SysVersion.RowId, ct).ConfigureAwait(false);
            if (row is null)
            {
                return Maybe.Some(new SchemaUpgradeFailure(tenantId, RuntimeErrors.TenantDatabaseFailed, $"The database of tenant '{tenantId}' has no schema version ledger.", checkedAt));
            }

            if (row.LastError is { } error)
            {
                return Maybe.Some(new SchemaUpgradeFailure(tenantId, RuntimeErrors.SchemaUpgradeFailed, error, row.LastFailedAt ?? checkedAt));
            }

            return string.Equals(row.DbVersion, target, StringComparison.Ordinal)
                ? Maybe.None<SchemaUpgradeFailure>()
                : Maybe.Some(new SchemaUpgradeFailure(tenantId, RuntimeErrors.SchemaUpgradeFailed, $"The database of tenant '{tenantId}' is at schema {row.DbVersion ?? "(none)"}; the application needs {target}.", checkedAt));
        }, cancellationToken).ConfigureAwait(false);
        return read.TryGetValue(out var verdict) ? verdict : Maybe.Some(new SchemaUpgradeFailure(tenantId, read.Error!.Code, read.Error.Message, checkedAt));
    }

    /// <summary>
    /// 在租户独立库上读
    /// </summary>
    /// <remarks>
    /// 给维护页读独立库自己的台账与升级记录：在自建作用域里绑定分片，连不上或读失败即 <see cref="RuntimeErrors.TenantDatabaseFailed"/>
    /// </remarks>
    /// <param name="tenantId">租户 ID</param>
    /// <param name="read">在分片连接上读</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task<Result<T>> ReadAsync<T>(string tenantId, Func<ISqlSugarClient, CancellationToken, Task<T>> read, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(read);
        await using var scope = scopes.CreateAsyncScope();
        var bound = await BindAsync(scope.ServiceProvider, tenantId, cancellationToken).ConfigureAwait(false);
        if (!bound.TryGetValue(out var shard))
        {
            return bound.Error!;
        }

        try
        {
            return await read(shard, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is DbException or SqlSugarException)
        {
            return Error.Literal(RuntimeErrors.TenantDatabaseFailed, $"Reading the database of tenant '{tenantId}' failed: {exception.Message}");
        }
    }

    /// <summary>
    /// 已就绪（建好库）的租户独立库
    /// </summary>
    /// <remarks>
    /// 租户目录里标了 <see cref="ReadyProperty"/> 的；没有租户目录即没有
    /// </remarks>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户 ID</returns>
    public async Task<Result<IReadOnlyList<string>>> ReadyTenantsAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        if (scope.ServiceProvider.GetService<ITenantStore>() is not { } store)
        {
            return Array.Empty<string>();
        }

        var listed = await store.ListAllAsync(cancellationToken).ConfigureAwait(false);
        if (!listed.TryGetValue(out var tenants))
        {
            return listed.Error!;
        }

        return tenants
            .Where(static t => t.Properties.TryGetValue(ReadyProperty, out var ready) && string.Equals(ready, "true", StringComparison.Ordinal))
            .Select(static t => t.Id)
            .ToList();
    }

    /// <summary>
    /// 读租户独立库的版本台账
    /// </summary>
    /// <remarks>
    /// 没有台账行为无值；连不上或读失败即 <see cref="RuntimeErrors.TenantDatabaseFailed"/>
    /// </remarks>
    /// <param name="tenantId">租户 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>台账行</returns>
    public async Task<Result<Maybe<SysVersion>>> ReadVersionAsync(string tenantId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        await using var scope = scopes.CreateAsyncScope();
        var bound = await BindAsync(scope.ServiceProvider, tenantId, cancellationToken).ConfigureAwait(false);
        if (!bound.TryGetValue(out var shard))
        {
            return bound.Error!;
        }

        try
        {
            var row = await shard.Queryable<SysVersion>().FirstAsync(v => v.Id == SysVersion.RowId, cancellationToken).ConfigureAwait(false);
            return row is null ? Maybe.None<SysVersion>() : Maybe.Some(row);
        }
        catch (Exception exception) when (exception is DbException or SqlSugarException)
        {
            return Error.Literal(RuntimeErrors.TenantDatabaseFailed, $"Reading the schema version of tenant '{tenantId}' failed: {exception.Message}");
        }
    }

    /// <summary>
    /// 在作用域里绑定租户的分片并取分片连接
    /// </summary>
    /// <remarks>
    /// 绑定要在作用域第一次取持久化客户端之前；租户目录里没有连接串（共库或未知租户）即 <see cref="RuntimeErrors.TenantShardMissing"/>
    /// </remarks>
    private static async Task<Result<ISqlSugarClient>> BindAsync(IServiceProvider services, string tenantId, CancellationToken cancellationToken)
    {
        var bound = await services.GetRequiredService<TenantShardScope>().BindTenantAsync(tenantId, cancellationToken).ConfigureAwait(false);
        if (!bound.TryGetValue(out var shard))
        {
            return bound.Error!;
        }

        return shard.HasValue && services.GetRequiredService<ISqlSugarClient>() is SqlSugarClient client && client.IsAnyConnection(SqlSugarConnections.Shard)
            ? client.GetConnection(SqlSugarConnections.Shard)
            : Error.Literal(RuntimeErrors.TenantShardMissing, $"Tenant '{tenantId}' has no database of its own.");
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Database of tenant '{TenantId}' initialized at schema {Version} ({Count} tables).")]
    private static partial void LogInitialized(ILogger logger, string tenantId, string version, int count);
}
