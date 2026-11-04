// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using XiHan.BasicApp.Integration.Tests.Infrastructure;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证服务监控
/// </summary>
/// <remarks>
/// 运行信息取自框架的 IHostMonitor（运行时、处理器、内存、磁盘、网卡、显卡、主板），健康项取自各 Provider 的最近一次检查；只在平台侧
/// </remarks>
public sealed class MaintenanceServerTests : IDisposable
{
    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task ServerInfo_HasTheHostRuntimeAndTheProviderHealthChecks()
    {
        var (server, platform, tenant) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;

        var (response, body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/server", token: platform));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(string.IsNullOrEmpty(body.GetProperty("runtime").GetProperty("framework").GetString()));
        Assert.False(string.IsNullOrEmpty(body.GetProperty("runtime").GetProperty("osDescription").GetString()));
        Assert.Equal(JsonValueKind.Object, body.GetProperty("processor").ValueKind);
        Assert.Equal(JsonValueKind.Object, body.GetProperty("memory").ValueKind);
        Assert.Equal(JsonValueKind.Array, body.GetProperty("disks").ValueKind);
        Assert.Equal(JsonValueKind.Array, body.GetProperty("networkInterfaces").ValueKind);
        Assert.Equal(JsonValueKind.Object, body.GetProperty("board").ValueKind);
        var health = body.GetProperty("health").EnumerateArray().ToList();
        Assert.NotEmpty(health);
        Assert.All(health, item => Assert.False(string.IsNullOrEmpty(item.GetProperty("name").GetString())));
        Assert.Equal(health.Select(static h => h.GetProperty("name").GetString()).Order(StringComparer.Ordinal), health.Select(static h => h.GetProperty("name").GetString()));

        // 租户主体没有这个平台侧权限；没登录 401
        Assert.Equal(HttpStatusCode.Forbidden, (await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/server", token: tenant))).Response.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/server"))).Response.StatusCode);
    }

    [Fact]
    public async Task Packages_ListTheThirdPartyAssemblies_SortedAndDistinct_OnThePlatformSide()
    {
        var (server, platform, tenant) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;

        var (response, body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/server:packages", token: platform));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = body.GetProperty("items").EnumerateArray().ToList();
        Assert.NotEmpty(items);
        var names = items.Select(static i => i.GetProperty("packageName").GetString()!).ToList();
        Assert.Equal(names.Order(StringComparer.Ordinal), names);
        Assert.Equal(names.Count, names.Distinct(StringComparer.Ordinal).Count());
        Assert.DoesNotContain(names, static n => n.StartsWith("XiHan", StringComparison.OrdinalIgnoreCase) || n.StartsWith("Microsoft", StringComparison.OrdinalIgnoreCase) || n.StartsWith("System", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("SqlSugar", names);
        Assert.All(items, static i => Assert.True(Version.TryParse(i.GetProperty("packageVersion").GetString(), out _)));

        // 同 server.get：租户主体没有这个平台侧权限；没登录 401
        Assert.Equal(HttpStatusCode.Forbidden, (await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/server:packages", token: tenant))).Response.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/server:packages"))).Response.StatusCode);
    }
}
