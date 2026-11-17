// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Data.Common;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SqlSugar;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Runtime.Schema;

/// <summary>
/// 一个库的结构升级步骤
/// </summary>
/// <remarks>
/// 平台库（<see cref="SchemaUpgrader"/>）与租户独立库（<see cref="TenantSchemas"/>）共用：先按版本执行比台账新的脚本，再按实体建缺的表、补缺的列与部分唯一索引，每一步都记进该库的 <see cref="SysMigrationHistory"/>（带批次号与执行的 SQL）
/// 建表补列只在新库、执行了脚本或应用版本变了时做：同一版本的节点重启不再逐表核对。失败的原因与时间写进台账，成功即清空。调用方持有升级锁
/// </remarks>
internal static partial class SchemaMigration
{
    /// <summary>
    /// 按实体建缺的表、补缺的列，再补建软删除实体的部分唯一索引
    /// </summary>
    public static void Sync(ISqlSugarClient client, IReadOnlyCollection<Type> entityTypes)
    {
        SqlSugarConventions.InitTables(client, [.. entityTypes]);
        ActiveUniqueIndexes.Ensure(client, entityTypes);
    }

    /// <summary>
    /// 把一个库升到当前结构版本
    /// </summary>
    /// <param name="client">库的连接：台账与脚本都在它上面</param>
    /// <param name="sync">按实体建表补列（实体可能落在不同的连接上，由调用方分组）</param>
    /// <param name="versions">脚本目录里的版本（已按这个库的类型取了脚本）</param>
    /// <param name="tenant">是否租户独立库：执行 <see cref="SchemaVersionScripts.TenantFiles"/></param>
    /// <param name="run">这一次升级</param>
    /// <param name="clock">时间源</param>
    /// <param name="logger">日志</param>
    /// <param name="cancellationToken">取消令牌</param>
    public static async Task<Result<SchemaUpgradeReport>> ApplyAsync(
        ISqlSugarClient client,
        Action sync,
        IReadOnlyList<SchemaVersionScripts> versions,
        bool tenant,
        SchemaRun run,
        TimeProvider clock,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var row = await client.Queryable<SysVersion>().FirstAsync(v => v.Id == SysVersion.RowId, cancellationToken).ConfigureAwait(false);
        var outcome = await StepsAsync(client, sync, versions, tenant, run, row, clock, logger, cancellationToken).ConfigureAwait(false);
        if (outcome.IsFailure)
        {
            var failedAt = clock.GetUtcNow();
            var message = outcome.Error!.Message;
            await client.Updateable<SysVersion>()
                .SetColumns(v => new SysVersion { LastError = message, LastFailedAt = failedAt })
                .Where(v => v.Id == SysVersion.RowId)
                .ExecuteCommandAsync(cancellationToken).ConfigureAwait(false);
        }

        return outcome;
    }

    private static async Task<Result<SchemaUpgradeReport>> StepsAsync(
        ISqlSugarClient client,
        Action sync,
        IReadOnlyList<SchemaVersionScripts> versions,
        bool tenant,
        SchemaRun run,
        SysVersion row,
        TimeProvider clock,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var from = row.DbVersion;
        var target = SchemaScripts.Current(versions);
        var targetName = SchemaScripts.NameOf(target);
        Version? current = null;
        if (from is not null && (!System.Version.TryParse(from, out current) || current > target))
        {
            return Error.Literal(RuntimeErrors.SchemaNewerThanApp, $"The database schema version '{from}' is newer than this application ({targetName}).");
        }

        var provider = DatabaseProviders.NameOf(client.CurrentConnectionConfig.DbType);
        var applied = new List<string>();
        if (current is not null)
        {
            foreach (var version in versions.Where(v => v.Version > current))
            {
                foreach (var file in tenant ? version.TenantFiles : version.Files)
                {
                    var script = provider + "/" + (tenant ? SchemaScripts.TenantDirectory + "/" : string.Empty) + Path.GetFileName(file);
                    var failed = await RunScriptAsync(client, version.Name, script, file, run, clock, cancellationToken).ConfigureAwait(false);
                    if (failed is not null)
                    {
                        LogStepFailed(logger, version.Name, script, failed);
                        return Error.Literal(RuntimeErrors.SchemaUpgradeFailed, $"Schema script {version.Name}/{script} failed: {failed}");
                    }

                    LogStepApplied(logger, version.Name, script);
                    applied.Add(version.Name + "/" + script);
                }

                await client.Updateable<SysVersion>().SetColumns(v => new SysVersion { DbVersion = version.Name }).Where(v => v.Id == SysVersion.RowId).ExecuteCommandAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        if (from is null || applied.Count > 0 || !string.Equals(row.AppVersion, AppInfo.Version, StringComparison.Ordinal))
        {
            var failed = await RunSyncAsync(client, sync, targetName, run, clock, cancellationToken).ConfigureAwait(false);
            if (failed is not null)
            {
                LogStepFailed(logger, targetName, SysMigrationHistory.OrmSync, failed);
                return Error.Literal(RuntimeErrors.SchemaUpgradeFailed, $"Creating tables and columns from the entities failed: {failed}");
            }

            LogStepApplied(logger, targetName, SysMigrationHistory.OrmSync);
            applied.Add(targetName + "/" + SysMigrationHistory.OrmSync);
        }

        var upgradedAt = clock.GetUtcNow();
        await client.Updateable<SysVersion>()
            .SetColumns(v => new SysVersion { DbVersion = targetName, AppVersion = AppInfo.Version, UpgradedAt = upgradedAt, LastError = null, LastFailedAt = null })
            .Where(v => v.Id == SysVersion.RowId)
            .ExecuteCommandAsync(cancellationToken).ConfigureAwait(false);
        return new SchemaUpgradeReport(from, targetName, applied);
    }

    /// <summary>
    /// 执行一个脚本并记下这一步
    /// </summary>
    /// <remarks>
    /// 一个脚本一个事务；MySQL 与 Oracle 的 DDL 会隐式提交、回滚不了，所以脚本要写成可重复执行。Oracle 不能一次执行多条语句：脚本里的语句以只有 <c>/</c> 的一行分隔
    /// </remarks>
    /// <returns>失败的原因；成功为 <see langword="null"/></returns>
    private static async Task<string?> RunScriptAsync(ISqlSugarClient client, string version, string script, string file, SchemaRun run, TimeProvider clock, CancellationToken cancellationToken)
    {
        var sql = await File.ReadAllTextAsync(file, cancellationToken).ConfigureAwait(false);
        var executedAt = clock.GetUtcNow();
        var watch = Stopwatch.StartNew();
        string? error = null;
        try
        {
            await client.Ado.BeginTranAsync().ConfigureAwait(false);
            foreach (var statement in Statements(sql, client.CurrentConnectionConfig.DbType))
            {
                await client.Ado.ExecuteCommandAsync(statement).ConfigureAwait(false);
            }

            await client.Ado.CommitTranAsync().ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is DbException or SqlSugarException)
        {
            await client.Ado.RollbackTranAsync().ConfigureAwait(false);
            error = exception.Message;
        }

        await RecordAsync(client, new Step(version, script, executedAt, watch.ElapsedMilliseconds, error, sql), run, cancellationToken).ConfigureAwait(false);
        return error;
    }

    /// <summary>
    /// 按实体建表补列并记下这一步
    /// </summary>
    /// <returns>失败的原因；成功为 <see langword="null"/></returns>
    private static async Task<string?> RunSyncAsync(ISqlSugarClient client, Action sync, string version, SchemaRun run, TimeProvider clock, CancellationToken cancellationToken)
    {
        var executedAt = clock.GetUtcNow();
        var watch = Stopwatch.StartNew();
        string? error = null;
        IReadOnlyList<string> statements;
        using (var capture = DdlCapture.Begin())
        {
            try
            {
                sync();
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // 建表失败的原因各式各样（数据库异常、不支持的库类型、映射错误），都记下来交给调用方中断升级
                error = exception.Message;
            }

            statements = capture.Statements;
        }

        var sql = statements.Count == 0 ? null : string.Join(";\n", statements) + ";";
        await RecordAsync(client, new Step(version, SysMigrationHistory.OrmSync, executedAt, watch.ElapsedMilliseconds, error, sql), run, cancellationToken).ConfigureAwait(false);
        return error;
    }

    private static async Task RecordAsync(ISqlSugarClient client, Step step, SchemaRun run, CancellationToken cancellationToken)
        => await client.Insertable(new SysMigrationHistory
        {
            Id = Guid.CreateVersion7(),
            ConcurrencyStamp = Guid.NewGuid().ToString("N"),
            Version = step.Version,
            ScriptName = step.Script,
            ExecutedAt = step.ExecutedAt,
            DurationMs = step.DurationMs,
            Success = step.Error is null,
            NodeName = run.Node,
            ErrorMessage = step.Error,
            RunId = run.Id,
            SqlText = step.Sql,
        }).ExecuteCommandAsync(cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// 执行完的一步
    /// </summary>
    private sealed record Step(string Version, string Script, DateTimeOffset ExecutedAt, long DurationMs, string? Error, string? Sql);

    /// <summary>
    /// 把脚本拆成逐条执行的语句
    /// </summary>
    /// <remarks>
    /// Oracle 按只有 <c>/</c> 的行拆开；别的库整份一次执行
    /// </remarks>
    internal static IReadOnlyList<string> Statements(string sql, DbType dbType)
    {
        if (dbType != DbType.Oracle)
        {
            return string.IsNullOrWhiteSpace(sql) ? [] : [sql];
        }

        var statements = new List<string>();
        var current = new List<string>();
        foreach (var line in sql.Split('\n'))
        {
            if (line.Trim() == "/")
            {
                Flush();
                continue;
            }

            current.Add(line.TrimEnd('\r'));
        }

        Flush();
        return statements;

        void Flush()
        {
            var statement = string.Join('\n', current).Trim();
            if (statement.Length > 0)
            {
                statements.Add(statement);
            }

            current.Clear();
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Schema step {Version}/{Step} applied.")]
    private static partial void LogStepApplied(ILogger logger, string version, string step);

    [LoggerMessage(Level = LogLevel.Error, Message = "Schema step {Version}/{Step} failed: {Error}")]
    private static partial void LogStepFailed(ILogger logger, string version, string step, string error);
}
