// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Auditing.Domain.Services;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.Framework.Auditing.Database.Trail;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证个人中心的「登录设备」与「登录日志」。
/// </summary>
/// <remarks>
/// 只看本人（别人模仿本人开的会话也算本人的设备，4.x 同）；当前会话在最前、不能在这里下线，别人的会话即不存在；
/// 下线一台后它的令牌立即失效；下线其他全部设备连同本人发起的模仿会话；登录日志只有本人的，每页最多 50 条；活跃度按请求的时区分天、只算本人成功的登录。
/// </remarks>
public sealed class MySessionsTests : IDisposable
{
    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private async Task<BasicAppTestServer> StartAsync()
    {
        var server = await BasicAppTestServer.StartAsync(_database.ConnectionString, settings: [new("BasicApp:Seed:EnableDemoData", "true")]);
        await server.SeedDefaultsAsync();
        await server.RunSeedersAsync();
        return server;
    }

    private static async Task<(HttpStatusCode Status, JsonElement Body)> GetAsync(BasicAppTestServer server, string path, string token)
    {
        var (response, body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, path, token: token));
        return (response.StatusCode, body);
    }

    private static async Task<(HttpStatusCode Status, JsonElement Body)> PostAsync(BasicAppTestServer server, string path, string json, string token)
    {
        var (response, body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, path, json, token));
        return (response.StatusCode, body);
    }

    private static async Task<List<JsonElement>> DevicesAsync(BasicAppTestServer server, string token)
    {
        var (status, body) = await GetAsync(server, "/api/profile:sessions", token);
        Assert.Equal(HttpStatusCode.OK, status);
        return [.. body.GetProperty("items").EnumerateArray()];
    }

    /// <summary>
    /// 下线了的会话：令牌立即被会话门禁拒绝。
    /// </summary>
    private static async Task AssertSignedOutAsync(BasicAppTestServer server, string token)
    {
        var (status, body) = await GetAsync(server, "/api/auth:me", token);
        Assert.Equal((HttpStatusCode.Forbidden, "IDENTITY_DENIED_001"), (status, body.GetProperty("code").GetString()));
    }

    private static string CurrentOf(List<JsonElement> devices) => Assert.Single(devices, static d => d.GetProperty("isCurrent").GetBoolean()).GetProperty("id").GetString()!;

    [Fact]
    public async Task Devices_AndLoginLogs_AreTheCallersOwn()
    {
        await using var server = await StartAsync();
        var current = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;
        var second = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;
        var third = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;
        var demoLogin = await AuthFlowTests.LoginAsync(server, "admin", "Demo@123456", "demo");
        var demo = demoLogin.GetProperty("accessToken").GetString()!;
        var (started, startedBody) = await PostAsync(server, "/api/impersonation:start", $$"""{"targetUserId":"{{demoLogin.GetProperty("user").GetProperty("id").GetString()}}","tenantId":"demo"}""", current);
        Assert.Equal(HttpStatusCode.OK, started);
        var impersonating = startedBody.GetProperty("accessToken").GetString()!;

        // 本人三台设备，当前在最前、IP 不遮掩；演示管理员的设备里有超管模仿他开的会话
        var mine = await DevicesAsync(server, current);
        Assert.Equal(3, mine.Count);
        Assert.True(mine[0].GetProperty("isCurrent").GetBoolean());
        Assert.DoesNotContain("*", mine[0].GetProperty("ipAddress").GetString() ?? string.Empty, StringComparison.Ordinal);
        var theirs = await DevicesAsync(server, demo);
        Assert.Equal(2, theirs.Count);

        // 当前会话不能在这里下线；别人的会话即不存在
        var (self, selfBody) = await PostAsync(server, "/api/profile:revoke-session", $$"""{"sessionId":"{{CurrentOf(mine)}}"}""", current);
        Assert.Equal((HttpStatusCode.Conflict, "IDENTITY_CONFLICT_005"), (self, selfBody.GetProperty("code").GetString()));
        var (other, otherBody) = await PostAsync(server, "/api/profile:revoke-session", $$"""{"sessionId":"{{CurrentOf(theirs)}}"}""", current);
        Assert.Equal((HttpStatusCode.NotFound, "IDENTITY_NOTFOUND_002"), (other, otherBody.GetProperty("code").GetString()));

        // 下线第二台：它的令牌立即失效，剩两台
        var (revoked, revokedBody) = await PostAsync(server, "/api/profile:revoke-session", $$"""{"sessionId":"{{CurrentOf(await DevicesAsync(server, second))}}"}""", current);
        Assert.Equal((HttpStatusCode.OK, 2), (revoked, revokedBody.GetProperty("items").GetArrayLength()));
        await AssertSignedOutAsync(server, second);

        // 下线其他全部设备：第三台与本人发起的模仿会话都下线，只剩当前；演示管理员自己的会话不受影响
        var (others, othersBody) = await PostAsync(server, "/api/profile:revoke-other-sessions", "{}", current);
        Assert.Equal(HttpStatusCode.OK, others);
        Assert.True(Assert.Single(othersBody.GetProperty("items").EnumerateArray()).GetProperty("isCurrent").GetBoolean());
        await AssertSignedOutAsync(server, third);
        await AssertSignedOutAsync(server, impersonating);
        Assert.Equal(HttpStatusCode.OK, (await GetAsync(server, "/api/auth:me", demo)).Status);
        Assert.Single(await DevicesAsync(server, demo));

        // 登录日志只有本人的（超管的密码错也在，演示管理员看不到），按时间倒序，每页最多 50 条
        var (wrong, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:login", """{"userName":"superadmin","password":"nope-nope"}"""));
        Assert.Equal(HttpStatusCode.Forbidden, wrong.StatusCode);
        await server.Services.GetRequiredService<AuditLogWriter>().FlushAsync(CancellationToken.None);
        var (_, logs) = await GetAsync(server, "/api/my-login-logs?page=1&pageSize=50", current);
        var entries = logs.GetProperty("items").EnumerateArray().ToList();
        Assert.Equal(3, entries.Count(static e => e.GetProperty("event").GetString() == "Login" && e.GetProperty("succeeded").GetBoolean()));
        Assert.Equal("IDENTITY_DENIED_003", entries[0].GetProperty("errorCode").GetString());
        var occurred = entries.Select(static e => e.GetProperty("occurredAt").GetDateTimeOffset()).ToList();
        Assert.Equal(occurred.OrderDescending(), occurred);
        var (_, demoLogs) = await GetAsync(server, "/api/my-login-logs?page=1&pageSize=50", demo);
        Assert.Contains(demoLogs.GetProperty("items").EnumerateArray(), static e => e.GetProperty("event").GetString() == "Login" && e.GetProperty("succeeded").GetBoolean());
        Assert.DoesNotContain(demoLogs.GetProperty("items").EnumerateArray(), static e => e.GetProperty("errorCode").GetString() == "IDENTITY_DENIED_003");
        var (_, paged) = await GetAsync(server, "/api/my-login-logs?page=2&pageSize=2", current);
        Assert.Equal((2, logs.GetProperty("total").GetInt64(), 2), (paged.GetProperty("items").GetArrayLength(), paged.GetProperty("total").GetInt64(), paged.GetProperty("page").GetProperty("index").GetInt32()));
        Assert.Equal(HttpStatusCode.BadRequest, (await GetAsync(server, "/api/my-login-logs?page=1&pageSize=51", current)).Status);

        // 活跃度：今天的登录是本人成功的三次（密码错不算），访问按天进趋势；时区不认得即 400
        await server.Services.GetRequiredService<AuditLogWriter>().FlushAsync(CancellationToken.None);
        await server.Services.GetRequiredService<DatabaseAuditTrail>().FlushAsync(CancellationToken.None);
        var (activity, activityBody) = await GetAsync(server, "/api/my-activity:summary?timeZone=Asia/Shanghai", current);
        Assert.Equal(HttpStatusCode.OK, activity);
        Assert.Equal(3, activityBody.GetProperty("today").GetProperty("logins").GetInt32());
        Assert.True(activityBody.GetProperty("today").GetProperty("accesses").GetInt32() > 0);
        Assert.Contains(activityBody.GetProperty("trend").EnumerateArray(), static d => d.GetProperty("accesses").GetInt32() > 0);
        Assert.Equal(JsonValueKind.String, activityBody.GetProperty("lastLoginAt").ValueKind);
        var (_, demoActivity) = await GetAsync(server, "/api/my-activity:summary", demo);
        Assert.Equal(1, demoActivity.GetProperty("thisMonth").GetProperty("logins").GetInt32());
        var (badZone, badZoneBody) = await GetAsync(server, "/api/my-activity:summary?timeZone=Mars/Olympus", current);
        Assert.Equal((HttpStatusCode.BadRequest, "AUDITING_VALIDATION_005"), (badZone, badZoneBody.GetProperty("code").GetString()));
    }
}
