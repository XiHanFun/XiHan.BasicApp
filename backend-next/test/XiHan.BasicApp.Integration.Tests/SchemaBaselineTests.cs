// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using XiHan.BasicApp.Host;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.BasicApp.Runtime.Schema;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证结构升级的基线。
/// </summary>
/// <remarks>
/// 同一台 PostgreSQL 上两个空库：一个由 InitTables 建表，一个由升级执行器跑基线脚本建表，两边的表、列（类型、可空、缺省、自增）、约束与索引须完全一致；
/// 基线只执行一次（再跑没有要执行的脚本），台账记下版本与执行记录，跑完基线的库能正常启动。
/// 设 <c>XIHAN_UPDATE_SCHEMA_BASELINE=1</c> 时先把 InitTables 建出的结构渲染成基线脚本写回源码（实体改了之后用它重新生成，再提交）。
/// </remarks>
public sealed class SchemaBaselineTests
{
    private const string UpdateVariable = "XIHAN_UPDATE_SCHEMA_BASELINE";
    private const string BaselineVersion = "6.0.0";
    private const string BaselineScript = "0001-baseline.sql";

    [Fact]
    public async Task Baseline_BuildsTheSameTablesAsInitTables_AndRunsOnce()
    {
        var server = PostgresDatabase.RequireServer();
        var directory = Path.Combine(RepositoryRoot(), "src", "XiHan.BasicApp.Host", "Schema");

        // InitTables 建的库（启动即建表）
        await using var initTables = await PostgresDatabase.CreateAsync(server, "xihan_basicapp_test_inittables");
        await using (await BasicAppTestServer.StartAsync(initTables.ConnectionString, DatabaseProviders.PostgreSql))
        {
        }

        var expected = await PostgresSchemaSnapshot.ReadAsync(initTables.ConnectionString);
        if (Environment.GetEnvironmentVariable(UpdateVariable) == "1")
        {
            var target = Path.Combine(directory, BaselineVersion);
            Directory.CreateDirectory(target);
            await File.WriteAllTextAsync(Path.Combine(target, BaselineScript), expected.ToSql("-- XiHan.BasicApp 6.0.0 基线：由 SchemaBaselineTests 按 InitTables 建出的结构生成（XIHAN_UPDATE_SCHEMA_BASELINE=1），可重复执行。"));
        }

        // 基线建的库：开始监听之前执行升级，与宿主的启动顺序一致
        await using var baseline = await PostgresDatabase.CreateAsync(server, "xihan_basicapp_test_baseline");
        await using var app = BasicAppHost.Create([], builder =>
        {
            builder.WebHost.UseTestServer();
            builder.Configuration.AddInMemoryCollection(BasicAppTestServer.Settings(baseline.ConnectionString, DatabaseProviders.PostgreSql));
            builder.Configuration.AddInMemoryCollection([new("XiHan:Persistence:SqlSugar:InitTables", "false"), new(SchemaOptions.ConfigurationSection + ":Directory", directory)]);
        });
        var upgrader = app.Services.GetRequiredService<SchemaUpgrader>();
        var first = (await upgrader.UpgradeAsync(TestContext.Current.CancellationToken)).ValueOrThrow();
        Assert.False(first.Skipped);
        Assert.Null(first.From);
        Assert.Equal(BaselineVersion, first.To);
        Assert.Equal([$"{BaselineVersion}/{BaselineScript}"], first.Applied);
        var again = (await upgrader.UpgradeAsync(TestContext.Current.CancellationToken)).ValueOrThrow();
        Assert.Equal(BaselineVersion, again.From);
        Assert.Empty(again.Applied);

        var actual = await PostgresSchemaSnapshot.ReadAsync(baseline.ConnectionString);
        var differences = expected.Differences(actual);
        Assert.True(differences.Count == 0, "Baseline differs from InitTables:" + Environment.NewLine + string.Join(Environment.NewLine, differences));

        using var client = new SqlSugarClient(new ConnectionConfig { DbType = DbType.PostgreSQL, ConnectionString = baseline.ConnectionString, IsAutoCloseConnection = true });
        var history = await client.Ado.SqlQueryAsync<string>("SELECT script_name FROM sys_migration_history WHERE success ORDER BY executed_at");
        Assert.Equal([BaselineScript], history);
        Assert.Equal(BaselineVersion, await client.Ado.SqlQuerySingleAsync<string>("SELECT db_version FROM sys_version"));

        // 跑完基线的库能正常启动（调度、发件箱等 Provider 的表都在）；台账显示别的节点正在升级时进入维护模式：接口 503、健康检查照常
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
    public async Task Upgrade_IsSkipped_ForNonPostgresDatabases()
    {
        using var database = new SqliteDatabase();
        await using var server = await BasicAppTestServer.StartAsync(database.ConnectionString);
        var report = (await server.Services.GetRequiredService<SchemaUpgrader>().UpgradeAsync(TestContext.Current.CancellationToken)).ValueOrThrow();
        Assert.True(report.Skipped);
        Assert.Empty(report.Applied);
    }

    [Fact]
    public void Scripts_AreDiscoveredByVersion_AndBadDirectoriesFail()
    {
        var root = Path.Combine(Path.GetTempPath(), "xihan-schema-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "6.0.10"));
            Directory.CreateDirectory(Path.Combine(root, "6.0.2"));
            File.WriteAllText(Path.Combine(root, "6.0.2", "0002-b.sql"), "SELECT 1;");
            File.WriteAllText(Path.Combine(root, "6.0.2", "0001-a.sql"), "SELECT 1;");
            var versions = SchemaScripts.Discover(root).ValueOrThrow();
            Assert.Equal(["6.0.2", "6.0.10"], versions.Select(static v => v.Name));
            Assert.Equal(["0001-a.sql", "0002-b.sql"], versions[0].Files.Select(Path.GetFileName));

            Directory.CreateDirectory(Path.Combine(root, "next"));
            Assert.Equal(XiHan.BasicApp.Runtime.RuntimeErrors.SchemaScriptsInvalid, SchemaScripts.Discover(root).Error!.Code);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }

        Assert.Equal(XiHan.BasicApp.Runtime.RuntimeErrors.SchemaScriptsInvalid, SchemaScripts.Discover(root).Error!.Code);
    }

    private static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "XiHan.BasicApp.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("XiHan.BasicApp.slnx not found above the test output directory.");
    }
}
