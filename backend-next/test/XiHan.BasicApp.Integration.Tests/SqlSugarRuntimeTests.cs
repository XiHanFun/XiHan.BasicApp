// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SqlSugar;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Kernel.Hosting;
using XiHan.Framework.MultiTenancy.Abstractions.Sharding;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证数据库连接的应用配置
/// </summary>
/// <remarks>
/// 从库只给平台库：带分片的调用另建 <c>shard</c> 配置，平台绑定的属性还会并入分片绑定，从库串绝不能跑到租户库上
/// SQL 诊断日志按配置记录语句、慢 SQL 与错误，配置写错启动即失败
/// </remarks>
public sealed class SqlSugarRuntimeTests : IDisposable
{
    private const string Platform = "Host=platform;Database=main";
    private const string Tenant = "Host=tenant;Database=iso";

    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private static IConfiguration ConfigurationOf(params (string Key, string Value)[] settings)
        => new ConfigurationBuilder().AddInMemoryCollection(settings.Select(static s => new KeyValuePair<string, string?>(s.Key, s.Value))).Build();

    private static Maybe<BindingValue> Binding(string connectionString, params (string Key, string Value)[] properties)
    {
        var values = properties.ToDictionary(static p => p.Key, static p => p.Value, StringComparer.Ordinal);
        values.TryAdd(TenantShardKeys.ProviderProperty, "postgresql");
        return Maybe.Some(new BindingValue(SqlSugarRuntime.BindingName, "connection:relational", connectionString, values));
    }

    private static ConnectionConfig ConfigFor(Action<Maybe<BindingValue>, ConnectionConfig> configure, string configId, Maybe<BindingValue> binding)
    {
        var config = new ConnectionConfig { ConfigId = configId };
        configure(binding, config);
        return config;
    }

    [Fact]
    public void Replicas_GoToThePlatformDatabaseOnly()
    {
        var configure = SqlSugarRuntime.ConfigureWith(ConfigurationOf(
            ("BasicApp:Database:Replicas:0:ConnectionString", "Host=replica-a;Database=main"),
            ("BasicApp:Database:Replicas:1:ConnectionString", "Host=replica-b;Database=main"),
            ("BasicApp:Database:Replicas:1:HitRate", "30")));

        var platform = ConfigFor(configure, SqlSugarConnections.Default, Binding(Platform));
        Assert.Equal(Platform, platform.ConnectionString);
        Assert.Equal(
            [("Host=replica-a;Database=main", DatabaseOptions.DefaultHitRate), ("Host=replica-b;Database=main", 30)],
            platform.SlaveConnectionConfigs.Select(static r => (r.ConnectionString, r.HitRate)));

        // 分片绑定带着平台绑定并入的属性与租户标记：连接串是租户自己的，没有从库
        var shard = ConfigFor(configure, SqlSugarConnections.Shard, Binding(Tenant, (TenantShardKeys.TenantProperty, "iso")));
        Assert.Equal(Tenant, shard.ConnectionString);
        Assert.True(shard.SlaveConnectionConfigs is null or { Count: 0 });

        // 命名连接同样不带
        var named = ConfigFor(configure, "reports", Binding("Host=reports;Database=main"));
        Assert.True(named.SlaveConnectionConfigs is null or { Count: 0 });

        // 不配即单库
        var single = ConfigFor(SqlSugarRuntime.ConfigureWith(ConfigurationOf()), SqlSugarConnections.Default, Binding(Platform));
        Assert.True(single.SlaveConnectionConfigs is null or { Count: 0 });
    }

    [Theory]
    [InlineData("BasicApp:Database:Replicas:0:HitRate", "10")]
    [InlineData("BasicApp:Database:LogSql", "maybe")]
    [InlineData("BasicApp:Database:SlowSqlThresholdMilliseconds", "0")]
    public void InvalidSettings_FailAtStartup(string key, string value)
    {
        var invalid = ConfigurationOf((key, value));
        Assert.Throws<ArgumentException>(() => DatabaseOptions.FromConfiguration(invalid));
    }

    [Fact]
    public void ReplicaHitRate_MustBePositive()
    {
        var invalid = ConfigurationOf(("BasicApp:Database:Replicas:0:ConnectionString", "Host=r"), ("BasicApp:Database:Replicas:0:HitRate", "0"));
        Assert.Throws<ArgumentException>(() => DatabaseOptions.FromConfiguration(invalid));
    }

    [Fact]
    public async Task SqlDiagnostics_LogByConfiguration()
    {
        var logger = new RecordingLogger();
        var options = DatabaseOptions.FromConfiguration(ConfigurationOf(("BasicApp:Database:LogSql", "true"), ("BasicApp:Database:SlowSqlThresholdMilliseconds", "1")));
        using var client = NewClient(_database.ConnectionString, new SqlDiagnostics(options, logger));

        await client.Ado.ExecuteCommandAsync("CREATE TABLE probe (id INTEGER)");
        Assert.Contains(logger.Entries, static e => e.Level == LogLevel.Information && e.Message.Contains("CREATE TABLE probe", StringComparison.Ordinal));

        await Assert.ThrowsAnyAsync<Exception>(() => client.Ado.ExecuteCommandAsync("SELECT * FROM missing_table"));
        Assert.Contains(logger.Entries, static e => e.Level == LogLevel.Error && e.Message.Contains("missing_table", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SqlDiagnostics_StayQuietByDefault()
    {
        var logger = new RecordingLogger();
        var options = DatabaseOptions.FromConfiguration(ConfigurationOf());
        using var client = NewClient(_database.ConnectionString, new SqlDiagnostics(options, logger));

        await client.Ado.ExecuteCommandAsync("CREATE TABLE quiet (id INTEGER)");
        Assert.Empty(logger.Entries);
    }

    [Fact]
    public async Task SqlDiagnostics_SkipErrorsTheCallerExpects()
    {
        var logger = new RecordingLogger();
        var options = DatabaseOptions.FromConfiguration(ConfigurationOf());
        using var client = NewClient(_database.ConnectionString, new SqlDiagnostics(options, logger));

        using (SqlDiagnostics.ExpectErrors())
        {
            await Assert.ThrowsAnyAsync<Exception>(() => client.Ado.ExecuteCommandAsync("SELECT * FROM missing_table"));
        }

        Assert.Empty(logger.Entries);
        await Assert.ThrowsAnyAsync<Exception>(() => client.Ado.ExecuteCommandAsync("SELECT * FROM missing_table"));
        Assert.Single(logger.Entries);
    }

    private static SqlSugarClient NewClient(string connectionString, SqlDiagnostics diagnostics)
    {
        var config = new ConnectionConfig { ConnectionString = connectionString, DbType = DbType.Sqlite, IsAutoCloseConnection = true, AopEvents = new AopEvents() };
        diagnostics.Attach(config.AopEvents);
        return new SqlSugarClient(config);
    }

    private sealed class RecordingLogger : ILogger
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, formatter(state, exception)));
    }
}
