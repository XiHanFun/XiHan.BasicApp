// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using SqlSugar;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Utils.Reflections;

namespace XiHan.BasicApp.Saas.Infrastructure.Migrations;

/// <summary>
/// 升级脚本文件
/// </summary>
/// <param name="Version">版本号，取自文件名（不含扩展名），如 <c>3.10.0</c></param>
/// <param name="ScriptName">脚本文件名，如 <c>3.10.0.sql</c></param>
/// <param name="FilePath">脚本文件的完整路径</param>
public sealed record MigrationScript(string Version, string ScriptName, string FilePath);

/// <summary>
/// 升级脚本执行器：按语义化版本顺序执行 <c>UpdateScripts/*.sql</c>，执行台账记入 <c>Sys_Migration_History</c>。
/// </summary>
/// <remarks>
/// 台账即状态：某版本是否已执行，只看库里有没有对应的成功记录，不依赖任何进程本地文件，
/// 因而容器重建、清空发布目录、还原数据库备份都不会让判断失真。
/// <para>
/// 空台账视为全新部署：只落一条基线记录（<see cref="BaselineScriptName"/>），不执行任何历史脚本，
/// 因为此时表结构由框架的建表流程按当前实体建成，早于基线的脚本没有意义。
/// </para>
/// <para>
/// PostgreSQL 下取 <c>pg_advisory_xact_lock</c> 事务级建议锁，并把本次全部脚本放进同一事务，
/// 多实例同时启动时只有一个执行、失败整体回滚。其余方言不加锁，需自行保证单实例执行。
/// </para>
/// </remarks>
public sealed class SqlScriptMigrationRunner
{
    /// <summary>
    /// 基线记录的脚本名。该记录不对应任何脚本文件，只标记「此版本及更早的脚本无需执行」。
    /// </summary>
    public const string BaselineScriptName = "(baseline)";

    /// <summary>
    /// 脚本目录名，位于 <see cref="AppContext.BaseDirectory"/> 下。
    /// </summary>
    public const string ScriptsDirectoryName = "UpdateScripts";

    /// <summary>
    /// 建议锁键值，取自表名的稳定散列，与其它用途的建议锁区分。
    /// </summary>
    private const long AdvisoryLockKey = 726_150_318_2026;

    private readonly ISqlSugarClientResolver _clientResolver;
    private readonly ILogger<SqlScriptMigrationRunner> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SqlScriptMigrationRunner(ISqlSugarClientResolver clientResolver, ILogger<SqlScriptMigrationRunner> logger)
    {
        _clientResolver = clientResolver;
        _logger = logger;
    }

    /// <summary>
    /// 选出待执行的脚本，按版本升序。
    /// </summary>
    /// <remarks>
    /// 纯函数，不触碰数据库与文件系统。三条排除规则依次为：已成功执行过、不高于基线、高于当前程序版本。
    /// 其中第一条保证同一版本无论启动多少次都只执行一次。
    /// </remarks>
    /// <param name="available">扫描到的全部脚本</param>
    /// <param name="appliedVersions">台账中已成功执行的版本号</param>
    /// <param name="baselineVersion">基线版本；为空表示无基线</param>
    /// <param name="currentVersion">当前程序版本</param>
    public static IReadOnlyList<MigrationScript> SelectPending(
        IEnumerable<MigrationScript> available,
        IReadOnlySet<string> appliedVersions,
        string? baselineVersion,
        string currentVersion)
    {
        ArgumentNullException.ThrowIfNull(available);
        ArgumentNullException.ThrowIfNull(appliedVersions);

        return [.. available
            .Where(script => !appliedVersions.Contains(script.Version))
            .Where(script => string.IsNullOrWhiteSpace(baselineVersion)
                             || CompareVersion(script.Version, baselineVersion) > 0)
            .Where(script => CompareVersion(script.Version, currentVersion) <= 0)
            .OrderBy(script => script.Version, Comparer<string>.Create(CompareVersion))];
    }

    /// <summary>
    /// 语义化版本比较：依次比较主/次/修订号，无法解析的按 0.0.0 参与比较。
    /// </summary>
    public static int CompareVersion(string left, string right)
    {
        var (leftMajor, leftMinor, leftPatch) = ParseVersion(left);
        var (rightMajor, rightMinor, rightPatch) = ParseVersion(right);

        if (leftMajor != rightMajor)
        {
            return leftMajor.CompareTo(rightMajor);
        }

        return leftMinor != rightMinor ? leftMinor.CompareTo(rightMinor) : leftPatch.CompareTo(rightPatch);
    }

    /// <summary>
    /// 扫描脚本目录，按版本升序返回；目录不存在时返回空。
    /// </summary>
    public static IReadOnlyList<MigrationScript> ScanScripts(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return [];
        }

        return [.. Directory.GetFiles(directory, "*.sql")
            .Select(path => new MigrationScript(Path.GetFileNameWithoutExtension(path), Path.GetFileName(path), path))
            .OrderBy(script => script.Version, Comparer<string>.Create(CompareVersion))];
    }

    /// <summary>
    /// 执行升级。任何一个脚本失败即回滚本次全部改动、写入失败台账并抛出，阻止应用带着半吊子表结构启动。
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var db = _clientResolver.GetCurrentClient();
        var tableName = db.EntityMaintenance.GetTableName<SysMigrationHistory>();

        if (!db.DbMaintenance.IsAnyTable(tableName, false))
        {
            throw new InvalidOperationException(
                $"升级台账表 {tableName} 不存在，无法判断已执行到哪个版本。请先完成建表（XiHan:Data:SqlSugarCore:EnableTableInitialization）。");
        }

        var currentVersion = GetCurrentProgramVersion();
        var history = await db.Queryable<SysMigrationHistory>()
            .Where(record => record.Success)
            .Select(record => new { record.Version, record.ScriptName })
            .ToListAsync(cancellationToken);

        if (history.Count == 0)
        {
            await WriteHistoryAsync(db, currentVersion, BaselineScriptName, true, null, cancellationToken);
            _logger.LogInformation("升级台账为空，按全新部署落基线 {Version}，不执行历史脚本。", currentVersion);
            return;
        }

        var appliedVersions = history.Select(record => record.Version).ToHashSet(StringComparer.Ordinal);
        var baselineVersion = history
            .Where(record => record.ScriptName == BaselineScriptName)
            .Select(record => record.Version)
            .Order(Comparer<string>.Create(CompareVersion))
            .LastOrDefault();

        var scripts = ScanScripts(Path.Combine(AppContext.BaseDirectory, ScriptsDirectoryName));
        var pending = SelectPending(scripts, appliedVersions, baselineVersion, currentVersion);

        if (pending.Count == 0)
        {
            _logger.LogInformation("升级脚本无待执行项（当前版本 {Version}，基线 {Baseline}）。", currentVersion, baselineVersion ?? "无");
            return;
        }

        await ExecutePendingAsync(db, pending, cancellationToken);
    }

    /// <summary>
    /// 在单个事务内执行全部待执行脚本并写入台账。
    /// </summary>
    private async Task ExecutePendingAsync(ISqlSugarClient db, IReadOnlyList<MigrationScript> pending, CancellationToken cancellationToken)
    {
        var isPostgreSql = db.CurrentConnectionConfig.DbType == DbType.PostgreSQL;
        if (!isPostgreSql)
        {
            _logger.LogWarning(
                "当前数据库类型 {DbType} 未取建议锁，多实例同时启动可能重复执行升级脚本，请确保仅单实例执行。",
                db.CurrentConnectionConfig.DbType);
        }

        var executing = string.Empty;
        try
        {
            await db.Ado.BeginTranAsync();

            if (isPostgreSql)
            {
                // 事务级建议锁：事务结束自动释放，不受 IsAutoCloseConnection 影响
                _ = await db.Ado.ExecuteCommandAsync("SELECT pg_advisory_xact_lock(@key)", new { key = AdvisoryLockKey });
            }

            foreach (var script in pending)
            {
                executing = script.ScriptName;
                _logger.LogInformation("执行升级脚本 {Script}。", script.ScriptName);

                var sql = await File.ReadAllTextAsync(script.FilePath, cancellationToken);
                if (!string.IsNullOrWhiteSpace(sql))
                {
                    _ = await db.Ado.ExecuteCommandAsync(sql);
                }

                await WriteHistoryAsync(db, script.Version, script.ScriptName, true, null, cancellationToken);
            }

            await db.Ado.CommitTranAsync();
            _logger.LogInformation("升级脚本执行完成，共 {Count} 个。", pending.Count);
        }
        catch (Exception ex)
        {
            await db.Ado.RollbackTranAsync();

            // 失败台账须在事务之外写，否则随回滚一并丢弃
            try
            {
                await WriteHistoryAsync(db, executing, executing, false, ex.Message, cancellationToken);
            }
            catch (Exception recordEx)
            {
                _logger.LogError(recordEx, "写入升级失败台账时再次出错。");
            }

            _logger.LogError(ex, "升级脚本 {Script} 执行失败，本次改动已整体回滚。", executing);
            throw;
        }
    }

    /// <summary>
    /// 写入一条台账记录。主键与创建审计列由框架的写入拦截器补齐。
    /// </summary>
    private static async Task WriteHistoryAsync(
        ISqlSugarClient db,
        string version,
        string scriptName,
        bool success,
        string? errorMessage,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var record = new SysMigrationHistory
        {
            Version = version,
            ScriptName = scriptName,
            ExecutedTime = DateTimeOffset.UtcNow,
            Success = success,
            NodeName = Environment.MachineName,
            ErrorMessage = errorMessage?.Length > 1024 ? errorMessage[..1024] : errorMessage
        };

        _ = await db.Insertable(record).ExecuteCommandAsync();
    }

    /// <summary>
    /// 当前程序版本（入口程序集版本，取 x.y.z 三段）。
    /// </summary>
    private static string GetCurrentProgramVersion()
    {
        return ReflectionHelper.GetEntryAssemblyVersion()?.ToString(3) ?? "0.0.0";
    }

    /// <summary>
    /// 解析 x.y.z，缺省段按 0 处理，无法解析按 0.0.0。
    /// </summary>
    private static (int Major, int Minor, int Patch) ParseVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return (0, 0, 0);
        }

        var parts = version.Trim().Split('.');
        _ = int.TryParse(parts.ElementAtOrDefault(0), out var major);
        _ = int.TryParse(parts.ElementAtOrDefault(1), out var minor);
        _ = int.TryParse(parts.ElementAtOrDefault(2), out var patch);
        return (major, minor, patch);
    }
}
