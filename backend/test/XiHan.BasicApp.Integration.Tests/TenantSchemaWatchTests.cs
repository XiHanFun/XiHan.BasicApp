// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Host;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.BasicApp.Runtime;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.BasicApp.Runtime.Schema;
using XiHan.Framework.MultiTenancy.Abstractions.Sharding;
using XiHan.Framework.MultiTenancy.Abstractions.Tenants;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证租户独立库在启动后的核对
/// </summary>
/// <remarks>
/// 内核进入服务状态之后才读租户目录：新库上起宿主不再因目录表还没建而告警；已就绪的独立库照样在启动后核对一遍，落后的标为不可用；
/// 宿主启动之前的启动升级记下的失败经过这一遍仍然不可用，升级成功的照常可用
/// 平台库与独立库都是 SQLite：用例直接写脚本目录与独立库
/// </remarks>
public sealed class TenantSchemaWatchTests : IDisposable
{
    private readonly SqliteDatabase _database = new();
    private readonly SqliteDatabase _acme = new();
    private readonly SqliteDatabase _globex = new();
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "xihan-schema-watch-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        _database.Dispose();
        _acme.Dispose();
        _globex.Dispose();
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public async Task DirectoryIsReadOnceServing_AndReadyDatabasesAreCheckedAfterStart()
    {
        await PrepareAsync();

        // 6.0.1 加了独立库脚本，两个独立库的台账还在 6.0.0；宿主不跑启动升级（关掉自动升级、由运维另跑命令时）
        Script("6.0.1/sqlite/tenant/0001-note.sql", "CREATE TABLE IF NOT EXISTS demo_note (id INTEGER PRIMARY KEY);");
        var logs = new LogCapture();
        await using var server = await BasicAppTestServer.StartAsync(_database.ConnectionString, DatabaseProviders.Sqlite, Settings(), builder => builder.Logging.AddProvider(logs));
        await FirstPassAsync(server.Services);

        var failures = server.Services.GetRequiredService<SchemaHealth>().Failures;
        Assert.Equal(["acme", "globex"], failures.Select(static f => f.Database));
        Assert.All(failures, static f => Assert.Contains("needs 6.0.1", f.Message, StringComparison.Ordinal));
        Assert.Equal(2, logs.Entries.Count(static e => e.Category == typeof(TenantSchemaWatch).FullName && e.Level == LogLevel.Error));
        AssertDirectoryReadInTime(logs);
    }

    [Fact]
    public async Task StartupUpgradeFailures_StayUnavailableThroughTheFirstCheck()
    {
        await PrepareAsync();

        // acme 的库里已有同名表，6.0.1 的独立库脚本只在它上面失败
        Execute(_acme.ConnectionString, "CREATE TABLE demo_note (id INTEGER PRIMARY KEY)");
        Script("6.0.1/sqlite/tenant/0001-note.sql", "CREATE TABLE demo_note (id INTEGER PRIMARY KEY);");

        // 与宿主同一顺序：开始监听之前升级，再启动
        var logs = new LogCapture();
        await using var app = BasicAppHost.Create([], builder =>
        {
            builder.WebHost.UseTestServer();
            builder.Configuration.AddInMemoryCollection(BasicAppTestServer.Settings(_database.ConnectionString, DatabaseProviders.Sqlite));
            builder.Configuration.AddInMemoryCollection([.. Settings(), new("XiHan:Persistence:SqlSugar:InitTables", "false")]);
            builder.Logging.AddProvider(logs);
        });
        Assert.True(await SchemaStartup.UpgradeAsync(app));
        var health = app.Services.GetRequiredService<SchemaHealth>();
        Assert.Equal("acme", Assert.Single(health.Failures).Database);

        await app.StartAsync(TestContext.Current.CancellationToken);
        await FirstPassAsync(app.Services);

        // 按 acme 自己的台账核对：上次升级失败，仍然不可用；globex 已升到 6.0.1，照常可用
        var failure = Assert.Single(health.Failures);
        Assert.Equal(("acme", RuntimeErrors.SchemaUpgradeFailed), (failure.Database, failure.Code));
        Assert.Contains("0001-note.sql", failure.Message, StringComparison.Ordinal);
        AssertDirectoryReadInTime(logs);
        await app.StopAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// 在新库上起一次宿主，登记两个就绪的独立库
    /// </summary>
    /// <remarks>
    /// 新库的表在内核启动时才建：第一遍核对等到服务状态才读目录，读得到、没有就绪的库，不告警。之后 acme 与 globex 的独立库初始化在 6.0.0
    /// </remarks>
    private async Task PrepareAsync()
    {
        Directory.CreateDirectory(_directory);
        var logs = new LogCapture();
        await using var server = await BasicAppTestServer.StartAsync(_database.ConnectionString, DatabaseProviders.Sqlite, Settings(), builder => builder.Logging.AddProvider(logs));
        await FirstPassAsync(server.Services);
        Assert.Empty(server.Services.GetRequiredService<SchemaHealth>().Failures);
        AssertDirectoryReadInTime(logs);

        await server.SeedAsync([Dedicated("acme", _acme), Dedicated("globex", _globex)], []);
        var schemas = server.Services.GetRequiredService<TenantSchemas>();
        foreach (var tenant in new[] { "acme", "globex" })
        {
            Assert.Equal("6.0.0", (await schemas.InitializeAsync(tenant, TestContext.Current.CancellationToken)).ValueOrThrow());
        }
    }

    private static Task FirstPassAsync(IServiceProvider services)
        => services.GetRequiredService<TenantSchemaWatch>().FirstPass.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);

    /// <summary>
    /// 没有提前读租户目录
    /// </summary>
    /// <remarks>
    /// 目录表还没建时读：框架的目录读取记「Reading the tenant catalog failed」，核对记「Tenant databases were not checked」
    /// </remarks>
    private static void AssertDirectoryReadInTime(LogCapture logs)
        => Assert.DoesNotContain(logs.Entries, static e => e.Category.EndsWith(".CatalogInvocation", StringComparison.Ordinal) || e.Message.StartsWith("Tenant databases were not checked", StringComparison.Ordinal));

    private static TenantInfo Dedicated(string id, SqliteDatabase database) => new()
    {
        Id = id,
        DisplayName = id,
        Properties = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [TenantShardKeys.ConnectionString] = database.ConnectionString,
            [TenantShardKeys.DatabaseProvider] = DatabaseProviders.Sqlite,
            [TenantSchemas.ReadyProperty] = "true",
        },
    };

    private static void Execute(string connectionString, string sql)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private IEnumerable<KeyValuePair<string, string?>> Settings() => [new(SchemaOptions.ConfigurationSection + ":Directory", _directory)];

    private void Script(string relative, string sql)
    {
        var path = Path.Combine(_directory, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, sql);
    }
}
