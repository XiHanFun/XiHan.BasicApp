// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.BasicApp.Runtime;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.BasicApp.Runtime.Schema;
using XiHan.Framework.Kernel.Hosting;
using XiHan.Framework.MultiTenancy.Abstractions.Sharding;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证一次升级里租户独立库的失败、拦截、预览、重试与升级锁
/// </summary>
/// <remarks>
/// 一个独立库的脚本失败：平台库照常升级、升级算成功，失败的库记进报告、台账与升级记录，这个租户的调用 503、健康项降级；
/// 预览列出要执行的脚本；修好脚本后在维护页重试即恢复。每一步记批次号与执行的 SQL；手动释放锁只放指定节点持着的锁
/// 平台库与独立库都是 SQLite：用例直接写脚本目录与租户目录
/// </remarks>
public sealed class SchemaRunsTests : IDisposable
{
    private const string Admin = "admin";

    private readonly SqliteDatabase _database = new();
    private readonly SqliteDatabase _shard = new();
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "xihan-schema-runs-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        _database.Dispose();
        _shard.Dispose();
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public async Task FailedTenantDatabase_IsReported_Gated_Previewed_AndRecoveredByRetry()
    {
        Directory.CreateDirectory(_directory);
        await using var server = await BasicAppTestServer.StartAsync(_database.ConnectionString, DatabaseProviders.Sqlite, [new(SchemaOptions.ConfigurationSection + ":Directory", _directory)]);
        await server.SeedDefaultsAsync(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [TenantShardKeys.ConnectionString] = _shard.ConnectionString,
            [TenantShardKeys.DatabaseProvider] = DatabaseProviders.Sqlite,
            [TenantSchemas.ReadyProperty] = "true",
        });
        await server.RunSeedersAsync();
        var platform = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;
        var upgrader = server.Services.GetRequiredService<SchemaUpgrader>();
        var health = server.Services.GetRequiredService<SchemaHealth>();
        (await server.Services.GetRequiredService<TenantSchemas>().InitializeAsync("acme", TestContext.Current.CancellationToken)).ValueOrThrow();
        (await upgrader.UpgradeAsync(TestContext.Current.CancellationToken)).ValueOrThrow();

        // 6.0.1：平台脚本能执行，租户脚本写错了
        Script("6.0.1/sqlite/0001-marker.sql", "CREATE TABLE IF NOT EXISTS demo_marker (id INTEGER PRIMARY KEY);");
        Script("6.0.1/sqlite/tenant/0001-note.sql", "THIS IS NOT SQL;");
        var report = (await upgrader.UpgradeAsync(TestContext.Current.CancellationToken)).ValueOrThrow();
        Assert.Equal("6.0.1", report.To);
        Assert.Contains("6.0.1/sqlite/0001-marker.sql", report.Applied);
        var failure = Assert.Single(report.Failures);
        Assert.Equal("acme", failure.Database);
        Assert.True(health.IsUnavailable("acme"));

        // 平台库的每一步记这次的批次号；脚本记原文，建表补列记实际发出的语句
        // 裸客户端只写 SQL：不带框架约定的实体映射会留在 SqlSugar 的全局缓存里，带坏后面的用例
        using (var client = Client(_database.ConnectionString))
        {
            var steps = (await client.Ado.GetDataTableAsync("SELECT script_name, sql_text FROM sys_migration_history WHERE LOWER(run_id) = @run ORDER BY rowid", new { run = report.RunId.ToString() })).Rows.Cast<System.Data.DataRow>().ToList();
            Assert.Equal(["sqlite/0001-marker.sql", SysMigrationHistory.OrmSync], steps.Select(static s => (string)s["script_name"]));
            Assert.Contains("demo_marker", (string)steps[0]["sql_text"], StringComparison.Ordinal);
        }

        await WaitOutMaintenanceAsync(server);

        // 这个租户的调用 503，别的租户与平台照常；健康项降级
        var acme = TestTokens.Issue("root", "acme", Admin);
        var globex = TestTokens.Issue("root", "globex", Admin);
        var (blocked, blockedBody) = await SendAsync(server, HttpMethod.Post, "/api/users:list", new { }, acme);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, blocked);
        Assert.Equal(RuntimeErrors.TenantSchemaUnavailable, blockedBody.GetProperty("code").GetString());
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, "/api/users:list", new { }, globex)).Status);
        var status = Assert.Single(server.Services.GetServices<IHealthContributor>().OfType<SchemaHealthContributor>()).Check().Single();
        Assert.Equal(LifecycleState.Degraded, status.State);
        Assert.Equal("acme", status.Data["tenants"]);

        // 台账：独立库为失败并带原因，平台库为最新
        var (_, versions) = await SendAsync(server, HttpMethod.Post, "/api/versions:list", new { }, platform);
        var rows = versions.GetProperty("items").EnumerateArray().ToDictionary(static v => v.GetProperty("database").GetString()!);
        Assert.Equal("Current", rows["platform"].GetProperty("status").GetString());
        Assert.Equal("Failed", rows["acme"].GetProperty("status").GetString());
        Assert.Equal(DatabaseProviders.Sqlite, rows["acme"].GetProperty("provider").GetString());
        Assert.False(string.IsNullOrEmpty(rows["acme"].GetProperty("lastError").GetString()));

        // 预览：独立库还要执行那个脚本；平台库没有要做的
        var (_, plan) = await SendAsync(server, HttpMethod.Get, "/api/versions/acme:plan", null, platform);
        Assert.Equal("6.0.0", plan.GetProperty("from").GetString());
        Assert.Equal(["sqlite/tenant/0001-note.sql"], plan.GetProperty("scripts").EnumerateArray().Select(static s => s.GetProperty("script").GetString()));
        Assert.Equal("THIS IS NOT SQL;", plan.GetProperty("scripts")[0].GetProperty("sql").GetString());
        var (_, platformPlan) = await SendAsync(server, HttpMethod.Get, "/api/versions/platform:plan", null, platform);
        Assert.Equal(0, platformPlan.GetProperty("scripts").GetArrayLength());

        // 修好脚本后重试：这个租户恢复可用，升级记录按库与批次查得到
        Script("6.0.1/sqlite/tenant/0001-note.sql", "CREATE TABLE IF NOT EXISTS demo_note (id INTEGER PRIMARY KEY);");
        var (retried, retriedBody) = await SendAsync(server, HttpMethod.Post, "/api/versions/acme:upgrade", new { }, platform);
        Assert.True(retried == HttpStatusCode.OK, retriedBody.ToString());
        Assert.Contains("acme/6.0.1/sqlite/tenant/0001-note.sql", retriedBody.GetProperty("applied").EnumerateArray().Select(static a => a.GetString()));
        Assert.False(health.IsUnavailable("acme"));
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, "/api/users:list", new { }, acme)).Status);
        var runId = retriedBody.GetProperty("runId").GetString()!;
        var (_, records) = await SendAsync(server, HttpMethod.Post, "/api/migrations:list", new { filters = new[] { new { field = "database", @operator = "eq", value = "acme" }, new { field = "runId", @operator = "eq", value = runId } } }, platform);
        var record = Assert.Single(records.GetProperty("items").EnumerateArray(), static r => r.GetProperty("scriptName").GetString() == "sqlite/tenant/0001-note.sql");
        Assert.True(record.GetProperty("success").GetBoolean());
        Assert.Equal("acme", record.GetProperty("database").GetString());
        var (_, detail) = await SendAsync(server, HttpMethod.Get, $"/api/migrations/{record.GetProperty("id").GetString()}?database=acme", null, platform);
        Assert.Contains("demo_note", detail.GetProperty("sqlText").GetString(), StringComparison.Ordinal);
        var (_, recovered) = await SendAsync(server, HttpMethod.Get, "/api/versions/acme", null, platform);
        Assert.Equal("Current", recovered.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, recovered.GetProperty("lastError").ValueKind);

        // 平台库在节点启动时升级，不在这里重试
        Assert.Equal("MAINTENANCE_VALIDATION_007", (await SendAsync(server, HttpMethod.Post, "/api/versions/platform:upgrade", new { }, platform)).Body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task ReleaseLock_ReleasesOnlyTheNamedNodesLock()
    {
        await using var server = await BasicAppTestServer.StartAsync(_database.ConnectionString, DatabaseProviders.Sqlite);
        await server.SeedDefaultsAsync();
        await server.RunSeedersAsync();
        var platform = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;
        (await server.Services.GetRequiredService<SchemaUpgrader>().UpgradeAsync(TestContext.Current.CancellationToken)).ValueOrThrow();
        using (var client = Client(_database.ConnectionString))
        {
            // 一个早就不在的节点留下的锁（开始时间早于失效时间，维护模式不会因它打开）
            await client.Ado.ExecuteCommandAsync("UPDATE sys_version SET is_upgrading = 1, upgrade_node = 'ghost:1', upgrade_started_at = '2000-01-01 00:00:00'");
        }

        await WaitOutMaintenanceAsync(server);

        var (wrong, wrongBody) = await SendAsync(server, HttpMethod.Post, "/api/versions/platform:release-lock", new { node = "other:2" }, platform);
        Assert.Equal(HttpStatusCode.Conflict, wrong);
        Assert.Equal("MAINTENANCE_CONFLICT_005", wrongBody.GetProperty("code").GetString());
        var (released, releasedBody) = await SendAsync(server, HttpMethod.Post, "/api/versions/platform:release-lock", new { node = "ghost:1" }, platform);
        Assert.True(released == HttpStatusCode.OK, releasedBody.ToString());
        Assert.False(releasedBody.GetProperty("isUpgrading").GetBoolean());
        Assert.Equal("MAINTENANCE_NOTFOUND_002", (await SendAsync(server, HttpMethod.Post, "/api/versions/nowhere:release-lock", new { node = "ghost:1" }, platform)).Body.GetProperty("code").GetString());
    }

    /// <summary>
    /// 等维护模式结束
    /// </summary>
    /// <remarks>
    /// 用例在应用运行中升级（正式环境里启动升级在后台服务之前）：维护模式的轮询可能撞上持锁的那一段，等它下一轮读到锁已释放
    /// </remarks>
    private static async Task WaitOutMaintenanceAsync(BasicAppTestServer server)
    {
        var maintenance = server.Services.GetRequiredService<MaintenanceModeState>();
        for (var waited = TimeSpan.Zero; maintenance.IsActive && waited < MaintenanceModeState.PollInterval * 2; waited += TimeSpan.FromMilliseconds(200))
        {
            await Task.Delay(200, TestContext.Current.CancellationToken);
        }

        Assert.False(maintenance.IsActive);
    }

    private static SqlSugarClient Client(string connectionString)
        => new(new ConnectionConfig { DbType = DbType.Sqlite, ConnectionString = connectionString, IsAutoCloseConnection = true });

    private static async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(BasicAppTestServer server, HttpMethod method, string path, object? body, string? token)
    {
        var (response, json) = await server.SendAsync(BasicAppTestServer.Request(method, path, body is null ? null : JsonSerializer.Serialize(body), token));
        return (response.StatusCode, json);
    }

    private void Script(string relative, string sql)
    {
        var path = Path.Combine(_directory, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, sql);
    }
}
