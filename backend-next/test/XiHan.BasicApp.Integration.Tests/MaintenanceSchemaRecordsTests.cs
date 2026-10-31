// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.BasicApp.Runtime.Schema;
using XiHan.Framework.Persistence.Abstractions.Repositories;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证版本台账与升级记录的查询。
/// </summary>
/// <remarks>
/// 台账与执行记录由升级执行器写（这里直接写进测试库）；列表、筛选、缺省排序与按 ID 取，找不到即 404。
/// </remarks>
public sealed class MaintenanceSchemaRecordsTests : IDisposable
{
    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private static async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(BasicAppTestServer server, HttpMethod method, string path, object? body, string? token)
    {
        var (response, json) = await server.SendAsync(BasicAppTestServer.Request(method, path, body is null ? null : JsonSerializer.Serialize(body), token));
        return (response.StatusCode, json);
    }

    [Fact]
    public async Task Versions_AndUpgradeRecords_AreListedAndFetched()
    {
        var (server, platform, _) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;
        var now = DateTimeOffset.UtcNow;
        var failedId = Guid.CreateVersion7();
        (await server.InvokeAsync(async ctx =>
        {
            (await ctx.Services.GetRequiredService<IRepository<SysVersion, Guid>>().AddAsync(new SysVersion { Id = SysVersion.RowId, AppVersion = "6.0.0-preview.1", DbVersion = "6.0.0", UpgradedAt = now }, ctx.CancellationToken)).ThrowIfFailure();
            var history = ctx.Services.GetRequiredService<IRepository<SysMigrationHistory, Guid>>();
            (await history.AddAsync(new SysMigrationHistory { Id = Guid.CreateVersion7(), Version = "6.0.0", ScriptName = "0001-baseline.sql", ExecutedAt = now.AddMinutes(-2), DurationMs = 812, Success = true, NodeName = "node-a" }, ctx.CancellationToken)).ThrowIfFailure();
            return await history.AddAsync(new SysMigrationHistory { Id = failedId, Version = "6.0.1", ScriptName = "0001-users.sql", ExecutedAt = now.AddMinutes(-1), DurationMs = 15, Success = false, NodeName = "node-a", ErrorMessage = "relation \"sys_users\" does not exist" }, ctx.CancellationToken);
        })).ThrowIfFailure();

        var (listed, versions) = await SendAsync(server, HttpMethod.Post, "/api/versions:list", new { }, platform);
        Assert.Equal(HttpStatusCode.OK, listed);
        var version = Assert.Single(versions.GetProperty("items").EnumerateArray());
        Assert.Equal("platform", version.GetProperty("database").GetString());
        Assert.Equal("6.0.0", version.GetProperty("dbVersion").GetString());
        Assert.False(version.GetProperty("isUpgrading").GetBoolean());
        // 按库名取：平台库是 platform，不认识的库名找不到
        Assert.Equal("6.0.0-preview.1", (await SendAsync(server, HttpMethod.Get, "/api/versions/platform", null, platform)).Body.GetProperty("appVersion").GetString());
        Assert.Equal("MAINTENANCE_NOTFOUND_002", (await SendAsync(server, HttpMethod.Get, "/api/versions/nowhere", null, platform)).Body.GetProperty("code").GetString());

        // 缺省按执行时间倒序；按结果筛选；按 ID 取到失败原因
        var (_, records) = await SendAsync(server, HttpMethod.Post, "/api/migrations:list", new { }, platform);
        Assert.Equal(["0001-users.sql", "0001-baseline.sql"], records.GetProperty("items").EnumerateArray().Select(static r => r.GetProperty("scriptName").GetString()));
        var (_, failed) = await SendAsync(server, HttpMethod.Post, "/api/migrations:list", new { filters = new[] { new { field = "success", @operator = "eq", value = "false" } } }, platform);
        Assert.Equal(1, failed.GetProperty("total").GetInt64());
        var (got, record) = await SendAsync(server, HttpMethod.Get, $"/api/migrations/{failedId}", null, platform);
        Assert.Equal(HttpStatusCode.OK, got);
        Assert.Contains("sys_users", record.GetProperty("errorMessage").GetString(), StringComparison.Ordinal);
        Assert.Equal("MAINTENANCE_NOTFOUND_002", (await SendAsync(server, HttpMethod.Get, $"/api/migrations/{Guid.CreateVersion7()}", null, platform)).Body.GetProperty("code").GetString());
        Assert.Equal("MAINTENANCE_VALIDATION_001", (await SendAsync(server, HttpMethod.Post, "/api/migrations:list", new { sorts = new[] { new { field = "nodeName", descending = false } } }, platform)).Body.GetProperty("code").GetString());
    }
}
