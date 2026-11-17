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
/// <param name="Applied">执行了的步骤（<c>版本/库类型/脚本</c>，按实体建表补列为 <c>版本/orm-sync</c>；租户独立库的步骤前面带租户 ID）</param>
public sealed record SchemaUpgradeReport(string? From, string? To, IReadOnlyList<string> Applied)
{
    /// <summary>
    /// 这一次升级的批次号（升级记录的 <see cref="SysMigrationHistory.RunId"/>）
    /// </summary>
    public Guid RunId { get; init; }

    /// <summary>
    /// 没有升级成功的租户独立库
    /// </summary>
    /// <remarks>
    /// 平台库升级成功、只有租户独立库失败时升级照样算成功：这些租户在升级成功之前不可用，别的租户照常
    /// </remarks>
    public IReadOnlyList<SchemaUpgradeFailure> Failures { get; init; } = [];
}

/// <summary>
/// 结构升级执行器
/// </summary>
/// <remarks>
/// 方案 5.14：表结构由 ORM 按实体建出与补齐，PostgreSQL、MySQL、SQL Server、Oracle 同一条路径。新库按实体建全部表、登记当前结构版本；
/// 已有库先执行脚本目录里比台账新的、本库类型的脚本（只做 ORM 做不了的改名、改类型、数据迁移），再按实体补缺的表与列（<see cref="SchemaMigration"/>）
/// 多节点经台账行的条件更新抢升级锁（<see cref="SchemaLock"/>），别的节点等它升级完再看还有没有要做的；台账两张表先按实体建好再抢锁
/// 平台库升完后，在同一把锁里逐个升级已就绪的租户独立库（<see cref="TenantSchemas"/>，台账在各自的库里，停用的租户也升）：平台库失败即升级失败；
/// 一个租户库失败只记下（<see cref="SchemaHealth"/>），接着升下一个，这个租户在重试成功之前不可用
/// </remarks>
/// <param name="scopes">作用域工厂</param>
/// <param name="options">结构升级配置</param>
/// <param name="clock">时间源</param>
/// <param name="logger">日志</param>
/// <param name="tenantSchemas">租户独立库的升级</param>
/// <param name="entities">实体类型清单</param>
/// <param name="health">不可用的租户独立库</param>
public sealed partial class SchemaUpgrader(IServiceScopeFactory scopes, SchemaOptions options, TimeProvider clock, ILogger<SchemaUpgrader> logger, TenantSchemas tenantSchemas, EntityTypeCatalog entities, SchemaHealth health)
{
    /// <summary>
    /// 平台库的库名
    /// </summary>
    public const string PlatformDatabase = "platform";

    private static readonly TimeSpan _retryDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// 把平台库与已就绪的租户独立库升到当前结构版本
    /// </summary>
    /// <returns>平台库失败即失败；租户独立库的失败在 <see cref="SchemaUpgradeReport.Failures"/> 里</returns>
    public async Task<Result<SchemaUpgradeReport>> UpgradeAsync(CancellationToken cancellationToken)
    {
        var run = SchemaRun.Start();
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

            var locked = await SchemaLock.AcquireAsync(client, run.Node, options.LockExpiry, options.LockWait, clock, logger, cancellationToken).ConfigureAwait(false);
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

                var platform = await SchemaMigration.ApplyAsync(client, SyncAll, versions, tenant: false, run, clock, logger, cancellationToken).ConfigureAwait(false);
                if (!platform.TryGetValue(out var report))
                {
                    return platform;
                }

                LogUpgraded(logger, report.From ?? "(new)", report.To ?? "(none)", report.Applied.Count);
                var tenants = await UpgradeTenantsAsync(run, cancellationToken).ConfigureAwait(false);
                return tenants.TryGetValue(out var outcome)
                    ? report with { RunId = run.Id, Applied = [.. report.Applied, .. outcome.Applied], Failures = outcome.Failures }
                    : tenants.Error!;
            }
            finally
            {
                await SchemaLock.ReleaseAsync(client, run.Node, CancellationToken.None).ConfigureAwait(false);
            }
        }
        catch (Exception exception) when (exception is DbException or SqlSugarException)
        {
            return Error.Literal(RuntimeErrors.SchemaUpgradeFailed, $"Schema upgrade failed: {exception.Message}");
        }
    }

    /// <summary>
    /// 重试升级一个租户独立库
    /// </summary>
    /// <remarks>
    /// 不碰平台库的锁、别的节点不进维护模式：只锁这个租户库自己的台账行，有别的节点正在升级它即 <see cref="RuntimeErrors.SchemaUpgradeLocked"/>、不等
    /// 成功即这个租户恢复可用；失败照样记下原因
    /// </remarks>
    /// <param name="tenantId">租户 ID（已就绪的独立库）</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task<Result<SchemaUpgradeReport>> UpgradeTenantAsync(string tenantId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        var run = SchemaRun.Start();
        var upgraded = await tenantSchemas.UpgradeAsync(tenantId, run, TimeSpan.Zero, cancellationToken).ConfigureAwait(false);
        if (upgraded.TryGetValue(out var report))
        {
            health.Recover(tenantId);
            LogTenantUpgraded(logger, tenantId, report.To ?? "(none)", report.Applied.Count);
            return report with { RunId = run.Id, Applied = [.. report.Applied.Select(s => tenantId + "/" + s)] };
        }

        if (upgraded.Error!.Code != RuntimeErrors.SchemaUpgradeLocked)
        {
            health.Fail(new SchemaUpgradeFailure(tenantId, upgraded.Error.Code, upgraded.Error.Message, clock.GetUtcNow()));
        }

        return upgraded.Error;
    }

    /// <summary>
    /// 手动释放升级锁
    /// </summary>
    /// <remarks>
    /// 持锁节点崩溃后锁要等 <see cref="SchemaOptions.LockExpiry"/> 才失效；确认那个节点已经不在了，按它的节点名释放。只在锁确实还在这个节点手里时释放，不会抢走别的节点刚拿到的锁
    /// </remarks>
    /// <param name="database">库名：平台库为 <see cref="PlatformDatabase"/>，独立库为租户 ID</param>
    /// <param name="node">持锁的节点（版本台账上显示的）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>释放了为 <see langword="true"/>；锁不在这个节点手里（已释放、已换了节点）为 <see langword="false"/></returns>
    public async Task<Result<bool>> ReleaseLockAsync(string database, string node, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(database);
        ArgumentException.ThrowIfNullOrWhiteSpace(node);
        if (!string.Equals(database, PlatformDatabase, StringComparison.Ordinal))
        {
            return await tenantSchemas.ReleaseLockAsync(database, node, cancellationToken).ConfigureAwait(false);
        }

        await using var scope = scopes.CreateAsyncScope();
        var client = scope.ServiceProvider.GetRequiredService<SqlSugarUnitOfWork>().ConnectionFor(typeof(SysVersion));
        try
        {
            var released = await SchemaLock.ReleaseAsync(client, node, cancellationToken).ConfigureAwait(false);
            if (released)
            {
                LogLockReleased(logger, database, node);
            }

            return released;
        }
        catch (Exception exception) when (exception is DbException or SqlSugarException)
        {
            return Error.Literal(RuntimeErrors.SchemaUpgradeFailed, $"Releasing the schema upgrade lock failed: {exception.Message}");
        }
    }

    /// <summary>
    /// 预览平台库与已就绪的租户独立库要做的结构变更
    /// </summary>
    /// <remarks>
    /// 只读，不抢锁、不执行。读不到的租户库给不出预览，原因在 <see cref="SchemaPlanResult.Failures"/> 里
    /// </remarks>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task<Result<SchemaPlanResult>> PlanAsync(CancellationToken cancellationToken)
    {
        var platform = await PlanAsync(PlatformDatabase, cancellationToken).ConfigureAwait(false);
        if (!platform.TryGetValue(out var plan))
        {
            return platform.Error!;
        }

        var ready = await tenantSchemas.ReadyTenantsAsync(cancellationToken).ConfigureAwait(false);
        if (!ready.TryGetValue(out var tenantIds))
        {
            return ready.Error!;
        }

        var plans = new List<SchemaPlan> { plan };
        var failures = new List<SchemaUpgradeFailure>();
        foreach (var tenantId in tenantIds)
        {
            var tenant = await tenantSchemas.PlanAsync(tenantId, cancellationToken).ConfigureAwait(false);
            if (tenant.TryGetValue(out var tenantPlan))
            {
                plans.Add(tenantPlan);
            }
            else
            {
                failures.Add(new SchemaUpgradeFailure(tenantId, tenant.Error!.Code, tenant.Error.Message, clock.GetUtcNow()));
            }
        }

        return new SchemaPlanResult(plans, failures);
    }

    /// <summary>
    /// 预览一个库要做的结构变更
    /// </summary>
    /// <param name="database">库名：平台库为 <see cref="PlatformDatabase"/>，独立库为租户 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task<Result<SchemaPlan>> PlanAsync(string database, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(database);
        if (!string.Equals(database, PlatformDatabase, StringComparison.Ordinal))
        {
            return await tenantSchemas.PlanAsync(database, cancellationToken).ConfigureAwait(false);
        }

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
            return SchemaPlanning.Plan(PlatformDatabase, client, entities.All.GroupBy(unitOfWork.ConnectionFor), versions, tenant: false);
        }
        catch (Exception exception) when (exception is DbException or SqlSugarException || exception.GetType() == typeof(Exception))
        {
            // SqlSugar 把连不上库包成裸的 Exception
            return Error.Literal(RuntimeErrors.SchemaUpgradeFailed, $"Reading the platform database schema failed: {exception.Message}");
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

    /// <summary>
    /// 逐个升级已就绪的租户独立库
    /// </summary>
    /// <remarks>
    /// 一个租户库失败不停：记下原因、标为不可用，接着升下一个。读不出租户目录（平台库的问题）才算失败
    /// </remarks>
    private async Task<Result<TenantOutcome>> UpgradeTenantsAsync(SchemaRun run, CancellationToken cancellationToken)
    {
        var ready = await tenantSchemas.ReadyTenantsAsync(cancellationToken).ConfigureAwait(false);
        if (!ready.TryGetValue(out var tenantIds))
        {
            return ready.Error!;
        }

        var applied = new List<string>();
        var failures = new List<SchemaUpgradeFailure>();
        foreach (var tenantId in tenantIds)
        {
            var upgraded = await tenantSchemas.UpgradeAsync(tenantId, run, options.LockWait, cancellationToken).ConfigureAwait(false);
            if (upgraded.TryGetValue(out var report))
            {
                health.Recover(tenantId);
                applied.AddRange(report.Applied.Select(s => tenantId + "/" + s));
                continue;
            }

            var failure = new SchemaUpgradeFailure(tenantId, upgraded.Error!.Code, upgraded.Error.Message, clock.GetUtcNow());
            health.Fail(failure);
            failures.Add(failure);
            LogTenantFailed(logger, tenantId, failure.Code, failure.Message);
        }

        return new TenantOutcome(applied, failures);
    }

    /// <summary>
    /// 租户独立库这一轮的结果
    /// </summary>
    private sealed record TenantOutcome(IReadOnlyList<string> Applied, IReadOnlyList<SchemaUpgradeFailure> Failures);

    [LoggerMessage(Level = LogLevel.Information, Message = "CreateDatabase is on but Oracle databases are users created by a DBA; connecting to the configured user as is.")]
    private static partial void LogOracleDatabaseNotCreated(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Database schema upgraded from {From} to {To} ({Count} steps).")]
    private static partial void LogUpgraded(ILogger logger, string from, string to, int count);

    [LoggerMessage(Level = LogLevel.Error, Message = "Database of tenant '{TenantId}' failed to upgrade ({Code}): {Message}; the tenant is unavailable until it is upgraded.")]
    private static partial void LogTenantFailed(ILogger logger, string tenantId, string code, string message);

    [LoggerMessage(Level = LogLevel.Information, Message = "Database of tenant '{TenantId}' upgraded to {To} ({Count} steps).")]
    private static partial void LogTenantUpgraded(ILogger logger, string tenantId, string to, int count);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Schema upgrade lock of database '{Database}' held by node '{Node}' was released by hand.")]
    private static partial void LogLockReleased(ILogger logger, string database, string node);
}
