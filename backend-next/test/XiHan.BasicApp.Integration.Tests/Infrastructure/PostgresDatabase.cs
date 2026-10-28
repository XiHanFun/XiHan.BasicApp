// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;

namespace XiHan.BasicApp.Integration.Tests.Infrastructure;

/// <summary>
/// 测试用的临时 PostgreSQL 库：经维护库 <c>postgres</c> 建库，释放时 <c>DROP DATABASE … WITH (FORCE)</c>。服务器连接串来自环境变量 <see cref="Variable"/>（不含 Database）。
/// </summary>
public sealed class PostgresDatabase : IAsyncDisposable
{
    /// <summary>
    /// 服务器连接串所在的环境变量；没设即跳过需要真库的用例。
    /// </summary>
    public const string Variable = "XIHAN_TEST_POSTGRES";

    private readonly string _server;
    private readonly string _name;

    private PostgresDatabase(string server, string name)
    {
        _server = server;
        _name = name;
        ConnectionString = $"{server.TrimEnd(';')};Database={name}";
    }

    /// <summary>
    /// 指向这个临时库的连接串。
    /// </summary>
    public string ConnectionString { get; }

    /// <summary>
    /// 环境变量里的服务器连接串；没设即跳过当前用例。
    /// </summary>
    public static string RequireServer()
    {
        var server = Environment.GetEnvironmentVariable(Variable);
        Assert.SkipWhen(string.IsNullOrWhiteSpace(server), $"{Variable} is not set; this case only runs against a real PostgreSQL server.");
        return server!;
    }

    /// <summary>
    /// 建一个名字唯一的空库。
    /// </summary>
    public static async Task<PostgresDatabase> CreateAsync(string server, string prefix)
    {
        var name = $"{prefix}_{Guid.NewGuid():N}";
        using var client = Maintenance(server);
        await client.Ado.ExecuteCommandAsync($"CREATE DATABASE \"{name}\"");
        return new PostgresDatabase(server, name);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        using var client = Maintenance(_server);
        await client.Ado.ExecuteCommandAsync($"DROP DATABASE IF EXISTS \"{_name}\" WITH (FORCE)");
    }

    private static SqlSugarClient Maintenance(string server)
        => new(new ConnectionConfig { DbType = DbType.PostgreSQL, ConnectionString = $"{server.TrimEnd(';')};Database=postgres", IsAutoCloseConnection = true });
}
