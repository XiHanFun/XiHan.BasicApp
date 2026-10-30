// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Data.Common;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlSugar;
using XiHan.BasicApp.Contracts;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Runtime.Schema;

/// <summary>
/// 一次结构升级的结果。
/// </summary>
/// <param name="Skipped">是否跳过（不是 PostgreSQL 库，脚本只为 PostgreSQL 写）。</param>
/// <param name="From">升级前库的结构版本；新库为 <see langword="null"/>。</param>
/// <param name="To">升级后库的结构版本。</param>
/// <param name="Applied">执行了的脚本（<c>版本/文件名</c>）。</param>
public sealed record SchemaUpgradeReport(bool Skipped, string? From, string? To, IReadOnlyList<string> Applied);

/// <summary>
/// 结构升级执行器。
/// </summary>
/// <remarks>
/// 方案 5.14：平台库按版本台账（<see cref="SysVersion"/>）执行 <see cref="SchemaOptions.Directory"/> 里还没执行的版本目录——新库从最低的基线建起，
/// 之后每版一个目录；每个脚本一个事务，成功与失败都记进 <see cref="SysMigrationHistory"/>，失败即停（调用方中断启动）。
/// 多节点经台账行的条件更新抢升级锁（时间按库的 now()），别的节点等它升级完再看还有没有要执行的。台账两张表先按实体建好（与 InitTables 同一结构），再抢锁。
/// 只支持 PostgreSQL：别的库（测试的 SQLite）跳过，由 InitTables 建表。租户库随库隔离落地后接进来。
/// </remarks>
/// <param name="scopes">作用域工厂。</param>
/// <param name="options">结构升级配置。</param>
/// <param name="clock">时间源。</param>
/// <param name="logger">日志。</param>
public sealed partial class SchemaUpgrader(IServiceScopeFactory scopes, SchemaOptions options, TimeProvider clock, ILogger<SchemaUpgrader> logger)
{
    private static readonly TimeSpan _retryDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// 执行平台库还没执行的升级脚本。
    /// </summary>
    public async Task<Result<SchemaUpgradeReport>> UpgradeAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var client = scope.ServiceProvider.GetRequiredService<SqlSugarUnitOfWork>().ConnectionFor(typeof(SysVersion));
        if (client.CurrentConnectionConfig.DbType != DbType.PostgreSQL)
        {
            LogSkipped(logger, client.CurrentConnectionConfig.DbType.ToString());
            return new SchemaUpgradeReport(true, null, null, []);
        }

        var discovered = SchemaScripts.Discover(options.Directory);
        if (!discovered.TryGetValue(out var versions))
        {
            return discovered.Error!;
        }

        try
        {
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
                return await ApplyAsync(client, versions, node, cancellationToken).ConfigureAwait(false);
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

    private async Task<Result> EnsureLedgerAsync(ISqlSugarClient client, CancellationToken cancellationToken)
    {
        // 两个节点同时建台账表时后建的可能撞上前一个的建表，再试一次即见到已建好的表
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                client.CodeFirst.InitTables(typeof(SysVersion), typeof(SysMigrationHistory));
                break;
            }
            catch (Exception exception) when (attempt < 3 && exception is DbException or SqlSugarException)
            {
                await Task.Delay(_retryDelay, clock, cancellationToken).ConfigureAwait(false);
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
            // 时间取库的 now()：各节点的时钟不必对齐，也不经驱动的时区换算
            var taken = await client.Ado.ExecuteCommandAsync(
                "UPDATE sys_version SET is_upgrading = TRUE, upgrade_node = @node, upgrade_started_at = now() "
                + "WHERE id = @id AND (NOT is_upgrading OR upgrade_started_at IS NULL OR upgrade_started_at < now() - make_interval(secs => @expiry))",
                new SugarParameter("@node", node),
                new SugarParameter("@id", SysVersion.RowId),
                new SugarParameter("@expiry", options.LockExpiry.TotalSeconds)).ConfigureAwait(false);
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

    private async Task<Result<SchemaUpgradeReport>> ApplyAsync(ISqlSugarClient client, IReadOnlyList<SchemaVersionScripts> versions, string node, CancellationToken cancellationToken)
    {
        var row = await client.Queryable<SysVersion>().FirstAsync(v => v.Id == SysVersion.RowId, cancellationToken).ConfigureAwait(false);
        var from = row.DbVersion;
        Version? current = null;
        if (from is not null && (!System.Version.TryParse(from, out current) || current > versions[^1].Version))
        {
            return Error.Literal(RuntimeErrors.SchemaNewerThanApp, $"The database schema version '{from}' is newer than the scripts this application ships (up to '{versions[^1].Name}').");
        }

        var applied = new List<string>();
        foreach (var version in versions.Where(v => current is null || v.Version > current))
        {
            foreach (var file in version.Files)
            {
                var script = Path.GetFileName(file);
                var sql = await File.ReadAllTextAsync(file, cancellationToken).ConfigureAwait(false);
                var executedAt = clock.GetUtcNow();
                var watch = Stopwatch.StartNew();
                string? error = null;
                try
                {
                    await client.Ado.BeginTranAsync().ConfigureAwait(false);
                    await client.Ado.ExecuteCommandAsync(sql).ConfigureAwait(false);
                    await client.Ado.CommitTranAsync().ConfigureAwait(false);
                }
                catch (Exception exception) when (exception is DbException or SqlSugarException)
                {
                    await client.Ado.RollbackTranAsync().ConfigureAwait(false);
                    error = exception.Message;
                }

                await client.Insertable(new SysMigrationHistory
                {
                    Id = Guid.CreateVersion7(),
                    ConcurrencyStamp = Guid.NewGuid().ToString("N"),
                    Version = version.Name,
                    ScriptName = script,
                    ExecutedAt = executedAt,
                    DurationMs = watch.ElapsedMilliseconds,
                    Success = error is null,
                    NodeName = node,
                    ErrorMessage = error,
                }).ExecuteCommandAsync(cancellationToken).ConfigureAwait(false);
                if (error is not null)
                {
                    LogScriptFailed(logger, version.Name, script, error);
                    return Error.Literal(RuntimeErrors.SchemaUpgradeFailed, $"Schema script {version.Name}/{script} failed: {error}");
                }

                LogScriptApplied(logger, version.Name, script, watch.ElapsedMilliseconds);
                applied.Add(version.Name + "/" + script);
            }

            await client.Updateable<SysVersion>()
                .SetColumns(v => new SysVersion { DbVersion = version.Name })
                .Where(v => v.Id == SysVersion.RowId)
                .ExecuteCommandAsync(cancellationToken).ConfigureAwait(false);
            current = version.Version;
        }

        var to = current is null ? from : versions.First(v => v.Version == current).Name;
        var upgradedAt = clock.GetUtcNow();
        await client.Updateable<SysVersion>()
            .SetColumns(v => new SysVersion { AppVersion = AppInfo.Version, UpgradedAt = upgradedAt })
            .Where(v => v.Id == SysVersion.RowId)
            .ExecuteCommandAsync(cancellationToken).ConfigureAwait(false);
        LogUpgraded(logger, from ?? "(new)", to ?? "(none)", applied.Count);
        return new SchemaUpgradeReport(false, from, to, applied);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Schema upgrade skipped: the database is {DbType}; schema scripts are written for PostgreSQL and tables come from InitTables.")]
    private static partial void LogSkipped(ILogger logger, string dbType);

    [LoggerMessage(Level = LogLevel.Information, Message = "Another node is upgrading the database schema; waiting.")]
    private static partial void LogWaiting(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Schema script {Version}/{Script} applied in {Elapsed} ms.")]
    private static partial void LogScriptApplied(ILogger logger, string version, string script, long elapsed);

    [LoggerMessage(Level = LogLevel.Error, Message = "Schema script {Version}/{Script} failed: {Error}")]
    private static partial void LogScriptFailed(ILogger logger, string version, string script, string error);

    [LoggerMessage(Level = LogLevel.Information, Message = "Database schema upgraded from {From} to {To} ({Count} scripts).")]
    private static partial void LogUpgraded(ILogger logger, string from, string to, int count);
}
