// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Data.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlSugar;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Runtime.Schema;

/// <summary>
/// 一次结构升级的结果
/// </summary>
/// <param name="From">升级前库的结构版本；新库为 <see langword="null"/></param>
/// <param name="To">升级后库的结构版本</param>
/// <param name="Applied">执行了的步骤（<c>版本/库类型/脚本</c>，按实体建表补列为 <c>版本/orm-sync</c>）</param>
public sealed record SchemaUpgradeReport(string? From, string? To, IReadOnlyList<string> Applied);

/// <summary>
/// 结构升级执行器
/// </summary>
/// <remarks>
/// 方案 5.14：表结构由 ORM 按实体建出与补齐，PostgreSQL、MySQL、SQL Server、Oracle 同一条路径。新库按实体建全部表、登记当前结构版本；
/// 已有库先执行脚本目录里比台账新的、本库类型的脚本（只做 ORM 做不了的改名、改类型、数据迁移），再按实体补缺的表与列（<see cref="SchemaMigration"/>）
/// 多节点经台账行的条件更新抢升级锁，别的节点等它升级完再看还有没有要做的；台账两张表先按实体建好再抢锁
/// 平台库升完后，在同一把锁里逐个升级已就绪的租户独立库（<see cref="TenantSchemas"/>，台账在各自的库里，停用的租户也升）
/// </remarks>
/// <param name="scopes">作用域工厂</param>
/// <param name="options">结构升级配置</param>
/// <param name="clock">时间源</param>
/// <param name="logger">日志</param>
/// <param name="tenantSchemas">租户独立库的升级</param>
/// <param name="entities">实体类型清单</param>
public sealed partial class SchemaUpgrader(IServiceScopeFactory scopes, SchemaOptions options, TimeProvider clock, ILogger<SchemaUpgrader> logger, TenantSchemas tenantSchemas, EntityTypeCatalog entities)
{
    private static readonly TimeSpan _retryDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// 把平台库与已就绪的租户独立库升到当前结构版本
    /// </summary>
    public async Task<Result<SchemaUpgradeReport>> UpgradeAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<SqlSugarUnitOfWork>();
        var client = unitOfWork.ConnectionFor(typeof(SysVersion));
        var discovered = SchemaScripts.Discover(options.Directory, DatabaseProviders.NameOf(client.CurrentConnectionConfig.DbType));
        if (!discovered.TryGetValue(out var versions))
        {
            return discovered.Error!;
        }

        try
        {
            var database = EnsureDatabase(client);
            if (database.IsFailure)
            {
                return database.Error!;
            }

            var snapshot = SnapshotReads.Ensure(client, RuntimeErrors.SchemaUpgradeFailed);
            if (snapshot.IsFailure)
            {
                return snapshot.Error!;
            }
            var ledger = await EnsureLedgerAsync(client, cancellationToken).ConfigureAwait(false);
            if (ledger.IsFailure)
            {
                return ledger.Error!;
            }

            var node = $"{Environment.MachineName}:{Environment.ProcessId}";
            var locked = await AcquireAsync(client, node, cancellationToken).ConfigureAwait(false);
            if (locked.IsFailure)
            {
                return locked.Error!;
            }

            try
            {
                // 与框架建表同一分法：实体按落点分到各自的连接上建
                void SyncAll()
                {
                    foreach (var group in entities.All.GroupBy(unitOfWork.ConnectionFor))
                    {
                        SchemaMigration.Sync(group.Key, [.. group]);
                    }
                }

                var platform = await SchemaMigration.ApplyAsync(client, SyncAll, versions, tenant: false, node, clock, logger, cancellationToken).ConfigureAwait(false);
                if (!platform.TryGetValue(out var report))
                {
                    return platform;
                }

                LogUpgraded(logger, report.From ?? "(new)", report.To ?? "(none)", report.Applied.Count);
                var tenants = await UpgradeTenantsAsync(node, cancellationToken).ConfigureAwait(false);
                return tenants.TryGetValue(out var applied) ? report with { Applied = [.. report.Applied, .. applied] } : tenants.Error!;
            }
            finally
            {
                await client.Updateable<SysVersion>()
                    .SetColumns(static v => new SysVersion { IsUpgrading = false })
                    .Where(v => v.Id == SysVersion.RowId && v.UpgradeNode == node)
                    .ExecuteCommandAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }
        catch (Exception exception) when (exception is DbException or SqlSugarException)
        {
            return Error.Literal(RuntimeErrors.SchemaUpgradeFailed, $"Schema upgrade failed: {exception.Message}");
        }
    }

    private Result EnsureDatabase(ISqlSugarClient client)
    {
        if (!options.CreateDatabase)
        {
            return Result.Success();
        }

        if (client.CurrentConnectionConfig.DbType == DbType.Oracle)
        {
            // Oracle 的「库」是用户（schema），要由 DBA 事先建好并授权；连接不上时下一步建台账即报错
            LogOracleDatabaseNotCreated(logger);
            return Result.Success();
        }

        try
        {
            // 库已存在时什么都不做；不存在时经各库的维护库（postgres、master、mysql 的服务器连接）建库
            client.DbMaintenance.CreateDatabase();
            return Result.Success();
        }
        catch (Exception exception) when (exception is DbException or SqlSugarException || exception.GetType() == typeof(Exception))
        {
            return Error.Literal(RuntimeErrors.SchemaUpgradeFailed, $"Schema upgrade failed: creating the database failed: {exception.Message}");
        }
    }

    private async Task<Result> EnsureLedgerAsync(ISqlSugarClient client, CancellationToken cancellationToken)
    {
        // 两个节点同时建台账表时后建的可能撞上前一个的建表，再试一次即见到已建好的表
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                SqlSugarConventions.InitTables(client, typeof(SysVersion), typeof(SysMigrationHistory));
                break;
            }
            catch (Exception exception) when (attempt < 3 && exception is DbException or SqlSugarException)
            {
                await Task.Delay(_retryDelay, clock, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception.GetType() == typeof(Exception))
            {
                // InitTables 把连不上库（库不存在、地址错）包成裸的 Exception，外层只认数据库异常
                return Error.Literal(RuntimeErrors.SchemaUpgradeFailed, $"Schema upgrade failed: {exception.Message}");
            }
        }

        if (!await client.Queryable<SysVersion>().AnyAsync(v => v.Id == SysVersion.RowId, cancellationToken).ConfigureAwait(false))
        {
            try
            {
                await client.Insertable(new SysVersion { Id = SysVersion.RowId, AppVersion = AppInfo.Version, ConcurrencyStamp = Guid.NewGuid().ToString("N") }).ExecuteCommandAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is DbException or SqlSugarException)
            {
                // 别的节点先插了台账行即可接着走；还是没有就是真的写不进去
                if (!await client.Queryable<SysVersion>().AnyAsync(v => v.Id == SysVersion.RowId, cancellationToken).ConfigureAwait(false))
                {
                    throw;
                }
            }
        }

        return Result.Success();
    }

    private async Task<Result> AcquireAsync(ISqlSugarClient client, string node, CancellationToken cancellationToken)
    {
        var deadline = clock.GetUtcNow() + options.LockWait;
        while (true)
        {
            // 条件更新是原子的：只有一个节点改得动。时间取节点时钟，各节点的偏差远小于锁的失效时间
            var now = clock.GetUtcNow();
            var expired = now - options.LockExpiry;
            var taken = await client.Updateable<SysVersion>()
                .SetColumns(v => new SysVersion { IsUpgrading = true, UpgradeNode = node, UpgradeStartedAt = now })
                .Where(v => v.Id == SysVersion.RowId && (!v.IsUpgrading || v.UpgradeStartedAt == null || v.UpgradeStartedAt < expired))
                .ExecuteCommandAsync(cancellationToken).ConfigureAwait(false);
            if (taken > 0)
            {
                return Result.Success();
            }

            if (clock.GetUtcNow() >= deadline)
            {
                return Error.Literal(RuntimeErrors.SchemaUpgradeLocked, $"Another node is still upgrading the database schema after {options.LockWait.TotalMinutes} minutes.");
            }

            LogWaiting(logger);
            await Task.Delay(_retryDelay, clock, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 逐个升级已就绪的租户独立库
    /// </summary>
    /// <returns>执行了的租户步骤（<c>租户/版本/…</c>）</returns>
    private async Task<Result<IReadOnlyList<string>>> UpgradeTenantsAsync(string node, CancellationToken cancellationToken)
    {
        var applied = new List<string>();
        var ready = await tenantSchemas.ReadyTenantsAsync(cancellationToken).ConfigureAwait(false);
        if (!ready.TryGetValue(out var tenantIds))
        {
            return ready.Error!;
        }

        foreach (var tenantId in tenantIds)
        {
            var upgraded = await tenantSchemas.UpgradeAsync(tenantId, node, cancellationToken).ConfigureAwait(false);
            if (!upgraded.TryGetValue(out var report))
            {
                return upgraded.Error!;
            }

            applied.AddRange(report.Applied.Select(s => tenantId + "/" + s));
        }

        return applied;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Another node is upgrading the database schema; waiting.")]
    private static partial void LogWaiting(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "CreateDatabase is on but Oracle databases are users created by a DBA; connecting to the configured user as is.")]
    private static partial void LogOracleDatabaseNotCreated(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Database schema upgraded from {From} to {To} ({Count} steps).")]
    private static partial void LogUpgraded(ILogger logger, string from, string to, int count);
}
