// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SqlSugar;
using XiHan.BasicApp.Host;
using XiHan.BasicApp.Host.Commands;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.BasicApp.Runtime;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.BasicApp.Runtime.Schema;
using XiHan.Framework.Hosting.CommandLine;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证结构升级：按实体建表、版本脚本与升级锁
/// </summary>
/// <remarks>
/// 新库由升级执行器按实体建出全部表，与框架 InitTables 建出的结构一致（PostgreSQL 上逐表比对），登记为当前结构版本；同一应用版本重跑不再建表；
/// 比台账新的、本库类型的脚本先于建表补列执行，成功与失败都记进升级记录，失败即停；台账显示别的节点正在升级时进入维护模式
/// </remarks>
public sealed class SchemaUpgradeTests
{
    private const string Baseline = "6.0.0";

    [Fact]
    public async Task UpgradeCommand_BuildsAnEmptyDatabase_WithoutStartingTheApp()
    {
        await using var database = await PostgresDatabase.CreateAsync(PostgresDatabase.RequireServer(), "xihan_basicapp_test_schema_command");
        IEnumerable<KeyValuePair<string, string?>> settings = [new("XiHan:Persistence:SqlSugar:InitTables", "false"), new(SchemaOptions.ConfigurationSection + ":Directory", NoScripts())];
        await using var app = Create(database.ConnectionString, DatabaseProviders.PostgreSql, settings, commands: true);

        // 空库上启动应用会失败（Feature 启动就要读写自己的表），所以这条命令不启动
        Assert.True(SchemaCommand.IsUpgrade(["schema", "upgrade"]));
        Assert.False(SchemaCommand.IsUpgrade(["schema"]));
        Assert.False(SchemaCommand.IsUpgrade(["seed", "upgrade"]));
        using var output = new StringWriter();
        using var error = new StringWriter();
        var exitCode = await SchemaCommand.UpgradeBeforeStartAsync(app.Services, output, error, TestContext.Current.CancellationToken);
        Assert.True(exitCode == CommandLineExitCodes.Success, error.ToString());
        var report = JsonElement.Parse(output.ToString());
        Assert.Equal(JsonValueKind.Null, report.GetProperty("from").ValueKind);
        Assert.Equal(Baseline, report.GetProperty("to").GetString());
        Assert.Equal($"{Baseline}/{SysMigrationHistory.OrmSync}", Assert.Single(report.GetProperty("applied").EnumerateArray()).GetString());

        // 升级之后同一个库能正常启动，表都在（还没执行种子，登录是账号不存在）
        await using var server = await BasicAppTestServer.StartAsync(database.ConnectionString, DatabaseProviders.PostgreSql, settings);
        var (login, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:login", """{"userName":"superadmin","password":"SuperAdmin@123"}"""));
        Assert.Equal(HttpStatusCode.Forbidden, login.StatusCode);
    }

    [Fact]
    public async Task UpgradeCommand_ReportsAMissingDatabase_AsACommandFailure()
    {
        var missing = new NpgsqlConnectionStringBuilder(PostgresDatabase.RequireServer()) { Database = $"xihan_basicapp_test_missing_{Guid.NewGuid():N}" }.ConnectionString;
        await using var app = Create(missing, DatabaseProviders.PostgreSql, [new("XiHan:Persistence:SqlSugar:InitTables", "false")], commands: true);

        using var output = new StringWriter();
        using var error = new StringWriter();
        var exitCode = await SchemaCommand.UpgradeBeforeStartAsync(app.Services, output, error, TestContext.Current.CancellationToken);

        Assert.NotEqual(CommandLineExitCodes.Success, exitCode);
        Assert.Empty(output.ToString());
        Assert.Equal("RUNTIME_UNAVAILABLE_001", JsonElement.Parse(error.ToString()).GetProperty("code").GetString());
    }

    [Fact]
    public async Task UpgradeCommand_CreatesAMissingDatabase_WhenCreateDatabaseIsOn()
    {
        await using var database = PostgresDatabase.Reserve(PostgresDatabase.RequireServer(), "xihan_basicapp_test_schema_create");
        IEnumerable<KeyValuePair<string, string?>> settings =
        [
            new("XiHan:Persistence:SqlSugar:InitTables", "false"),
            new(SchemaOptions.ConfigurationSection + ":Directory", NoScripts()),
            new(SchemaOptions.ConfigurationSection + ":CreateDatabase", "true"),
        ];
        await using var app = Create(database.ConnectionString, DatabaseProviders.PostgreSql, settings, commands: true);

        // 库不存在：先建库，再按实体建表
        using var output = new StringWriter();
        using var error = new StringWriter();
        var exitCode = await SchemaCommand.UpgradeBeforeStartAsync(app.Services, output, error, TestContext.Current.CancellationToken);
        Assert.True(exitCode == CommandLineExitCodes.Success, error.ToString());
        Assert.Equal($"{Baseline}/{SysMigrationHistory.OrmSync}", Assert.Single(JsonElement.Parse(output.ToString()).GetProperty("applied").EnumerateArray()).GetString());
    }

    [Fact]
    public async Task Upgrader_BuildsTheSameTablesAsInitTables_AndSyncsOncePerAppVersion()
    {
        var server = PostgresDatabase.RequireServer();

        // 框架 InitTables 建的库（启动即建表）
        await using var initTables = await PostgresDatabase.CreateAsync(server, "xihan_basicapp_test_inittables");
        await using (await BasicAppTestServer.StartAsync(initTables.ConnectionString, DatabaseProviders.PostgreSql))
        {
        }

        var expected = await PostgresSchemaSnapshot.ReadAsync(initTables.ConnectionString);

        // 升级执行器建的库：开始监听之前执行升级，与宿主的启动顺序一致
        await using var upgraded = await PostgresDatabase.CreateAsync(server, "xihan_basicapp_test_upgraded");
        await using var app = Create(upgraded.ConnectionString, DatabaseProviders.PostgreSql, [new("XiHan:Persistence:SqlSugar:InitTables", "false"), new(SchemaOptions.ConfigurationSection + ":Directory", NoScripts())]);
        var upgrader = app.Services.GetRequiredService<SchemaUpgrader>();
        var first = (await upgrader.UpgradeAsync(TestContext.Current.CancellationToken)).ValueOrThrow();
        Assert.Null(first.From);
        Assert.Equal(Baseline, first.To);
        Assert.Equal([$"{Baseline}/{SysMigrationHistory.OrmSync}"], first.Applied);

        // 同一应用版本再升一次：不再逐表核对
        var again = (await upgrader.UpgradeAsync(TestContext.Current.CancellationToken)).ValueOrThrow();
        Assert.Equal(Baseline, again.From);
        Assert.Empty(again.Applied);

        var actual = await PostgresSchemaSnapshot.ReadAsync(upgraded.ConnectionString);
        var differences = expected.Differences(actual);
        Assert.True(differences.Count == 0, "Upgrader differs from InitTables:" + Environment.NewLine + string.Join(Environment.NewLine, differences));

        using var client = new SqlSugarClient(new ConnectionConfig { DbType = DbType.PostgreSQL, ConnectionString = upgraded.ConnectionString, IsAutoCloseConnection = true });
        Assert.Equal([SysMigrationHistory.OrmSync], await client.Ado.SqlQueryAsync<string>("SELECT script_name FROM sys_migration_history WHERE success ORDER BY executed_at"));
        Assert.Equal(Baseline, await client.Ado.SqlQuerySingleAsync<string>("SELECT db_version FROM sys_version"));

        // 建好的库能正常启动（调度、发件箱等 Provider 的表都在）；台账显示别的节点正在升级时进入维护模式：接口 503、健康检查照常
        await client.Ado.ExecuteCommandAsync("UPDATE sys_version SET is_upgrading = TRUE, upgrade_node = 'other-node', upgrade_started_at = now()");
        await app.StartAsync(TestContext.Current.CancellationToken);
        using var http = app.GetTestServer().CreateClient();
        var status = HttpStatusCode.OK;
        for (var attempt = 0; attempt < 50 && status != HttpStatusCode.ServiceUnavailable; attempt++)
        {
            await Task.Delay(100, TestContext.Current.CancellationToken);
            status = (await http.GetAsync(new Uri(BasicAppTestServer.ApiPrefix + "/auth:login-config", UriKind.Relative), TestContext.Current.CancellationToken)).StatusCode;
        }

        Assert.Equal(HttpStatusCode.ServiceUnavailable, status);
        Assert.NotEqual(HttpStatusCode.ServiceUnavailable, (await http.GetAsync(new Uri("/health/live", UriKind.Relative), TestContext.Current.CancellationToken)).StatusCode);
        await app.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Upgrader_RunsPendingScriptsOfItsDatabaseType_BeforeSyncing_AndStopsAtAFailure()
    {
        using var database = new SqliteDatabase();
        var directory = EmptyDirectory();
        try
        {
            // 新库：按实体建表，登记为最早的结构版本
            var created = await UpgradeAsync(database, directory);
            Assert.Equal([$"{Baseline}/{SysMigrationHistory.OrmSync}"], created.ValueOrThrow().Applied);

            // 6.0.1 带 SQLite 与 PostgreSQL 的脚本：只执行本库类型的平台脚本（租户脚本留给独立库），再补表补列
            Script(directory, "6.0.1/sqlite/0001-marker.sql", "CREATE TABLE IF NOT EXISTS demo_marker (id INTEGER PRIMARY KEY);");
            Script(directory, "6.0.1/sqlite/tenant/0001-tenant.sql", "THIS IS NOT SQL;");
            Script(directory, "6.0.1/postgresql/0001-marker.sql", "THIS IS NOT SQL EITHER;");
            var upgraded = (await UpgradeAsync(database, directory)).ValueOrThrow();
            Assert.Equal(Baseline, upgraded.From);
            Assert.Equal("6.0.1", upgraded.To);
            Assert.Equal(["6.0.1/sqlite/0001-marker.sql", $"6.0.1/{SysMigrationHistory.OrmSync}"], upgraded.Applied);
            Assert.Empty((await UpgradeAsync(database, directory)).ValueOrThrow().Applied);

            // 失败的脚本：升级失败、记下失败与原因，台账停在上一个版本
            Script(directory, "6.0.2/sqlite/0001-broken.sql", "THIS IS NOT SQL;");
            var failed = await UpgradeAsync(database, directory);
            Assert.Equal(RuntimeErrors.SchemaUpgradeFailed, failed.Error?.Code);

            using var client = new SqlSugarClient(new ConnectionConfig { DbType = DbType.Sqlite, ConnectionString = database.ConnectionString, IsAutoCloseConnection = true });
            Assert.Equal(1, await client.Ado.GetIntAsync("SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'demo_marker'"));
            Assert.Equal("6.0.1", await client.Ado.SqlQuerySingleAsync<string>("SELECT db_version FROM sys_version"));
            var history = (await client.Ado.GetDataTableAsync("SELECT version, script_name, success, error_message FROM sys_migration_history ORDER BY rowid")).Rows.Cast<System.Data.DataRow>().ToList();
            Assert.Equal([SysMigrationHistory.OrmSync, "sqlite/0001-marker.sql", SysMigrationHistory.OrmSync, "sqlite/0001-broken.sql"], history.Select(static r => (string)r["script_name"]));
            var broken = history[^1];
            Assert.Equal(0L, Convert.ToInt64(broken["success"], System.Globalization.CultureInfo.InvariantCulture));
            Assert.Equal("6.0.2", broken["version"]);
            Assert.False(string.IsNullOrEmpty(broken["error_message"] as string));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Scripts_AreDiscoveredByVersionAndDatabaseType_AndBadDirectoriesFail()
    {
        var root = EmptyDirectory();
        try
        {
            Assert.Empty(SchemaScripts.Discover(Path.Combine(root, "missing"), DatabaseProviders.PostgreSql).ValueOrThrow());
            Assert.Equal(new Version(6, 0, 0), SchemaScripts.Current([]));

            Script(root, "6.0.10/postgresql/0001-c.sql", "SELECT 1;");
            Script(root, "6.0.2/postgresql/0002-b.sql", "SELECT 1;");
            Script(root, "6.0.2/postgresql/0001-a.sql", "SELECT 1;");
            Script(root, "6.0.2/postgresql/tenant/0001-t.sql", "SELECT 1;");
            Script(root, "6.0.2/mysql/0001-m.sql", "SELECT 1;");
            var versions = SchemaScripts.Discover(root, DatabaseProviders.PostgreSql).ValueOrThrow();
            Assert.Equal(["6.0.2", "6.0.10"], versions.Select(static v => v.Name));
            Assert.Equal(["0001-a.sql", "0002-b.sql"], versions[0].Files.Select(Path.GetFileName));
            Assert.Equal(["0001-t.sql"], versions[0].TenantFiles.Select(Path.GetFileName));
            Assert.Equal(new Version(6, 0, 10), SchemaScripts.Current(versions));
            Assert.All(SchemaScripts.Discover(root, DatabaseProviders.Oracle).ValueOrThrow(), static v => Assert.Empty(v.Files));

            // 脚本没放进库类型目录、库类型写错、目录名不是版本：都报错，不跳过
            Script(root, "6.0.3/0001-loose.sql", "SELECT 1;");
            Assert.Equal(RuntimeErrors.SchemaScriptsInvalid, SchemaScripts.Discover(root, DatabaseProviders.PostgreSql).Error!.Code);
            File.Delete(Path.Combine(root, "6.0.3", "0001-loose.sql"));
            Script(root, "6.0.3/postgres/0001-typo.sql", "SELECT 1;");
            Assert.Equal(RuntimeErrors.SchemaScriptsInvalid, SchemaScripts.Discover(root, DatabaseProviders.PostgreSql).Error!.Code);
            Directory.Delete(Path.Combine(root, "6.0.3"), recursive: true);
            Directory.CreateDirectory(Path.Combine(root, "next"));
            Assert.Equal(RuntimeErrors.SchemaScriptsInvalid, SchemaScripts.Discover(root, DatabaseProviders.PostgreSql).Error!.Code);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static async Task<XiHan.Framework.Primitives.Result<SchemaUpgradeReport>> UpgradeAsync(SqliteDatabase database, string directory)
    {
        await using var app = Create(database.ConnectionString, DatabaseProviders.Sqlite, [new("XiHan:Persistence:SqlSugar:InitTables", "false"), new(SchemaOptions.ConfigurationSection + ":Directory", directory)]);
        return await app.Services.GetRequiredService<SchemaUpgrader>().UpgradeAsync(TestContext.Current.CancellationToken);
    }

    private static WebApplication Create(string connectionString, string provider, IEnumerable<KeyValuePair<string, string?>> settings, bool commands = false)
        => BasicAppHost.Create([], builder =>
        {
            builder.WebHost.UseTestServer();
            if (commands)
            {
                builder.Services.AddBasicAppCommands();
            }

            builder.Configuration.AddInMemoryCollection(BasicAppTestServer.Settings(connectionString, provider));
            builder.Configuration.AddInMemoryCollection(settings);
        });

    /// <summary>
    /// 不存在的脚本目录：没有脚本
    /// </summary>
    private static string NoScripts() => Path.Combine(Path.GetTempPath(), "xihan-schema-none-" + Guid.NewGuid().ToString("N"));

    private static string EmptyDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "xihan-schema-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void Script(string root, string relative, string sql)
    {
        var path = Path.Combine(root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, sql);
    }
}
