// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Data.Common;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlSugar;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.MultiTenancy.Abstractions.Sharding;
using XiHan.Framework.MultiTenancy.Abstractions.Tenants;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Runtime.Schema;

/// <summary>
/// 租户独立库的建库、建表与升级。
/// </summary>
/// <remarks>
/// 与 4.x 同语义（方案 5.10、5.14）：库隔离租户的独立库只建落租户库的实体（<see cref="EntityTypeCatalog.TenantScoped"/>），按当前实体建表即是最新结构，
/// 随即在该库自己的台账（<see cref="SysVersion"/>）登记当前结构版本；之后升级只跑比台账新的 <c>Schema/&lt;版本&gt;/tenant/*.sql</c>，没有租户脚本的版本只推进台账。
/// 都在该租户的分片连接上做：自建服务作用域、先经框架的 <see cref="TenantShardScope"/> 按租户目录绑定分片（不经管道，不看应用状态与租户是否启用），再取持久化客户端。
/// </remarks>
/// <param name="scopes">服务作用域工厂。</param>
/// <param name="entities">实体类型清单。</param>
/// <param name="options">结构升级配置。</param>
/// <param name="clock">时间源。</param>
/// <param name="logger">日志。</param>
public sealed partial class TenantSchemas(IServiceScopeFactory scopes, EntityTypeCatalog entities, SchemaOptions options, TimeProvider clock, ILogger<TenantSchemas> logger)
{
    /// <summary>
    /// 租户脚本所在的子目录名。
    /// </summary>
    public const string TenantDirectory = "tenant";

    /// <summary>
    /// 租户目录里标记独立库已就绪的属性。
    /// </summary>
    /// <remarks>
    /// 值为 <c>true</c> 时启动升级才去升这个租户的库（配了连接还没初始化的库可能还不存在）。
    /// </remarks>
    public const string ReadyProperty = "BasicApp.DatabaseReady";

    /// <summary>
    /// 在租户的分片上建库、建表并登记台账。
    /// </summary>
    /// <remarks>
    /// 幂等：库与表已有即不动，台账已有即保留。建库不能在事务里执行，框架的事务在第一次写入时才开，这里先于任何写入。
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

        var discovered = SchemaScripts.Discover(options.Directory);
        if (!discovered.TryGetValue(out var versions))
        {
            return discovered.Error!;
        }

        try
        {
            shard.DbMaintenance.CreateDatabase();
            var split = entities.TenantScoped.Where(static t => t.IsDefined(typeof(SplitTableAttribute), inherit: false)).ToArray();
            var plain = entities.TenantScoped.Except(split).Append(typeof(SysVersion)).Append(typeof(SysMigrationHistory)).ToArray();
            shard.CodeFirst.InitTables(plain);
            if (split.Length > 0)
            {
                shard.CodeFirst.SplitTables().InitTables(split);
            }

            var current = versions[^1].Name;
            if (!await shard.Queryable<SysVersion>().AnyAsync(v => v.Id == SysVersion.RowId, cancellationToken).ConfigureAwait(false))
            {
                await shard.Insertable(new SysVersion { Id = SysVersion.RowId, AppVersion = AppInfo.Version, DbVersion = current, UpgradedAt = clock.GetUtcNow(), ConcurrencyStamp = Guid.NewGuid().ToString("N") })
                    .ExecuteCommandAsync(cancellationToken).ConfigureAwait(false);
            }

            LogInitialized(logger, tenantId, current, plain.Length + split.Length);
            return current;
        }
        catch (Exception exception) when (exception is DbException or SqlSugarException)
        {
            return Error.Literal(RuntimeErrors.TenantDatabaseFailed, $"Initializing the database of tenant '{tenantId}' failed: {exception.Message}");
        }
    }

    /// <summary>
    /// 把租户的分片升到应用带的结构版本。
    /// </summary>
    /// <remarks>
    /// 只对已初始化的库调用；库比应用新即 <see cref="RuntimeErrors.SchemaNewerThanApp"/>。每个脚本一个事务，成功与失败都记进该库的升级记录，失败即停。
    /// </remarks>
    public async Task<Result<SchemaUpgradeReport>> UpgradeAsync(string tenantId, string node, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        await using var scope = scopes.CreateAsyncScope();
        var bound = await BindAsync(scope.ServiceProvider, tenantId, cancellationToken).ConfigureAwait(false);
        if (!bound.TryGetValue(out var shard))
        {
            return bound.Error!;
        }

        var discovered = SchemaScripts.Discover(options.Directory);
        if (!discovered.TryGetValue(out var versions))
        {
            return discovered.Error!;
        }

        try
        {
            var row = await shard.Queryable<SysVersion>().FirstAsync(v => v.Id == SysVersion.RowId, cancellationToken).ConfigureAwait(false);
            var from = row?.DbVersion;
            if (row is null || from is null || !System.Version.TryParse(from, out var current))
            {
                return Error.Literal(RuntimeErrors.TenantDatabaseFailed, $"The database of tenant '{tenantId}' has no schema version; initialize it first.");
            }

            if (current > versions[^1].Version)
            {
                return Error.Literal(RuntimeErrors.SchemaNewerThanApp, $"The schema version '{from}' of tenant '{tenantId}' is newer than the scripts this application ships (up to '{versions[^1].Name}').");
            }

            var applied = new List<string>();
            foreach (var version in versions.Where(v => v.Version > current))
            {
                var folder = Path.Combine(options.Directory, version.Name, TenantDirectory);
                var files = System.IO.Directory.Exists(folder) ? System.IO.Directory.EnumerateFiles(folder, "*.sql").Order(StringComparer.Ordinal).ToList() : [];
                foreach (var file in files)
                {
                    var failed = await ApplyAsync(shard, version.Name, file, node, cancellationToken).ConfigureAwait(false);
                    if (failed is not null)
                    {
                        return Error.Literal(RuntimeErrors.SchemaUpgradeFailed, $"Tenant '{tenantId}' schema script {version.Name}/{Path.GetFileName(file)} failed: {failed}");
                    }

                    applied.Add(version.Name + "/" + TenantDirectory + "/" + Path.GetFileName(file));
                }

                await shard.Updateable<SysVersion>().SetColumns(v => new SysVersion { DbVersion = version.Name }).Where(v => v.Id == SysVersion.RowId).ExecuteCommandAsync(cancellationToken).ConfigureAwait(false);
                current = version.Version;
            }

            var upgradedAt = clock.GetUtcNow();
            await shard.Updateable<SysVersion>().SetColumns(v => new SysVersion { AppVersion = AppInfo.Version, UpgradedAt = upgradedAt }).Where(v => v.Id == SysVersion.RowId).ExecuteCommandAsync(cancellationToken).ConfigureAwait(false);
            var to = versions.First(v => v.Version == current).Name;
            return new SchemaUpgradeReport(false, from, to, applied);
        }
        catch (Exception exception) when (exception is DbException or SqlSugarException)
        {
            return Error.Literal(RuntimeErrors.SchemaUpgradeFailed, $"Upgrading the database of tenant '{tenantId}' failed: {exception.Message}");
        }
    }

    /// <summary>
    /// 已就绪（建好库）的租户独立库。
    /// </summary>
    /// <remarks>
    /// 租户目录里标了 <see cref="ReadyProperty"/> 的；没有租户目录即没有。
    /// </remarks>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>租户 ID。</returns>
    public async Task<IReadOnlyList<string>> ReadyTenantsAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        if (scope.ServiceProvider.GetService<ITenantStore>() is not { } store)
        {
            return [];
        }

        return [.. (await store.ListAsync(cancellationToken).ConfigureAwait(false))
            .Where(static t => t.Properties.TryGetValue(ReadyProperty, out var ready) && string.Equals(ready, "true", StringComparison.Ordinal))
            .Select(static t => t.Id)];
    }

    /// <summary>
    /// 读租户独立库的版本台账。
    /// </summary>
    /// <remarks>
    /// 没有台账行为无值；连不上或读失败即 <see cref="RuntimeErrors.TenantDatabaseFailed"/>。
    /// </remarks>
    /// <param name="tenantId">租户 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>台账行。</returns>
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

    private async Task<string?> ApplyAsync(ISqlSugarClient shard, string version, string file, string node, CancellationToken cancellationToken)
    {
        var sql = await File.ReadAllTextAsync(file, cancellationToken).ConfigureAwait(false);
        var executedAt = clock.GetUtcNow();
        var watch = Stopwatch.StartNew();
        string? error = null;
        try
        {
            await shard.Ado.BeginTranAsync().ConfigureAwait(false);
            await shard.Ado.ExecuteCommandAsync(sql).ConfigureAwait(false);
            await shard.Ado.CommitTranAsync().ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is DbException or SqlSugarException)
        {
            await shard.Ado.RollbackTranAsync().ConfigureAwait(false);
            error = exception.Message;
        }

        await shard.Insertable(new SysMigrationHistory
        {
            Id = Guid.CreateVersion7(),
            ConcurrencyStamp = Guid.NewGuid().ToString("N"),
            Version = version,
            ScriptName = TenantDirectory + "/" + Path.GetFileName(file),
            ExecutedAt = executedAt,
            DurationMs = watch.ElapsedMilliseconds,
            Success = error is null,
            NodeName = node,
            ErrorMessage = error,
        }).ExecuteCommandAsync(cancellationToken).ConfigureAwait(false);
        return error;
    }

    /// <summary>
    /// 在作用域里绑定租户的分片并取分片连接。
    /// </summary>
    /// <remarks>
    /// 绑定要在作用域第一次取持久化客户端之前；租户目录里没有连接串（共库或未知租户）即 <see cref="RuntimeErrors.TenantShardMissing"/>。
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
