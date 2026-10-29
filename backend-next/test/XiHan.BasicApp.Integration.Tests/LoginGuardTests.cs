// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Auditing.Domain.Services;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.Framework.Auditing.Database.Trail;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证登录守卫的策略判定与登录失败自动封禁。
/// </summary>
/// <remarks>
/// 凭据都核对之后、签发令牌之前按登录时机的访问策略判定（拒绝即不签发，命中记为登录时）；
/// 登录失败自动封禁按来源地址数认证失败，达到上限即封、封禁中的地址一切调用 503，平台可查看与解封；参数缺省关。
/// </remarks>
public sealed class LoginGuardTests : IDisposable
{
    private const string ClientIpHeader = "X-Test-Client-Ip";
    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    // 用例按请求头给出来源地址（测试服务器没有远端地址）
    private static Task<BasicAppTestServer> StartAsync(SqliteDatabase database)
        => BasicAppTestServer.StartAsync(database.ConnectionString, configureApp: app => app.Use(async (http, next) =>
        {
            if (http.Request.Headers.TryGetValue(ClientIpHeader, out var ip))
            {
                http.Connection.RemoteIpAddress = IPAddress.Parse(ip.ToString());
            }

            await next();
        }));

    private static async Task<(HttpStatusCode Status, JsonElement Body)> LoginFromAsync(BasicAppTestServer server, string ip, string userName, string password)
    {
        var request = BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:login", JsonSerializer.Serialize(new { userName, password }));
        request.Headers.Add(ClientIpHeader, ip);
        var (response, body) = await server.SendAsync(request);
        return (response.StatusCode, body);
    }

    private static async Task<List<JsonElement>> HitsAsync(BasicAppTestServer server, string token, string policyCode)
    {
        await server.Services.GetRequiredService<DatabaseAuditTrail>().FlushAsync(CancellationToken.None);
        await server.Services.GetRequiredService<AuditLogWriter>().FlushAsync(CancellationToken.None);
        var (_, hits) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/condition-hits:list", $$"""{"page":{"index":1,"size":50},"filters":[{"field":"policyCode","operator":"eq","value":"{{policyCode}}"}]}""", token));
        return [.. hits.GetProperty("items").EnumerateArray()];
    }

    [Fact]
    public async Task LoginTimePolicies_RefuseTheToken_AndRecordALoginHit()
    {
        var server = await StartAsync(_database);
        await using var running = server;
        await server.SeedDefaultsAsync();
        await server.RunSeedersAsync();
        var admin = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;
        await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users", """{"userName":"dana","displayName":"Dana","password":"Dana@123456"}""", admin));

        // 只引用登录名的平台策略：登录请求本身是匿名的，中间件里不命中，签发前按账号判定才拦下
        var (_, created) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/policies", """{"code":"no-dana","name":"No Dana","conditions":[{"attributeName":"subject.userName","operator":"Equals","value":"dana"}]}""", admin));
        var policyId = created.GetProperty("id").GetString()!;
        await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/policies/{policyId}:set-action", """{"action":"Deny"}""", admin));

        var (denied, deniedBody) = await LoginFromAsync(server, "198.51.100.10", "dana", "Dana@123456");
        Assert.Equal(HttpStatusCode.Forbidden, denied);
        Assert.Equal("ACCESS_DENIED_005", deniedBody.GetProperty("code").GetString());
        Assert.Equal(HttpStatusCode.OK, (await LoginFromAsync(server, "198.51.100.10", "superadmin", "SuperAdmin@123")).Status);

        var hit = Assert.Single(await HitsAsync(server, admin, "no-dana"));
        Assert.Equal(("Login", true), (hit.GetProperty("phase").GetString(), hit.GetProperty("isBlocked").GetBoolean()));

        // 改回只记录：照常签发，命中照记不拦
        await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/policies/{policyId}:set-action", """{"action":"LogOnly"}""", admin));
        Assert.Equal(HttpStatusCode.OK, (await LoginFromAsync(server, "198.51.100.10", "dana", "Dana@123456")).Status);
        Assert.Contains(await HitsAsync(server, admin, "no-dana"), static h => h.GetProperty("action").GetString() == "LogOnly" && !h.GetProperty("isBlocked").GetBoolean());
    }

    [Fact]
    public async Task AutoBan_CountsAuthenticationFailuresPerAddress_BlocksEveryCall_AndCanBeLifted()
    {
        var server = await StartAsync(_database);
        await using var running = server;
        await server.SeedDefaultsAsync();
        await server.RunSeedersAsync();
        var admin = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;
        const string attacker = "198.51.100.20";

        // 失败分散在不同的登录名上，免得先撞上按账号的登录节流
        var guess = 0;
        async Task<string?> FailAsync() => (await LoginFromAsync(server, attacker, $"ghost-{++guess}", "wrong")).Body.GetProperty("code").GetString();

        // 缺省关：失败照常只回答凭据错误
        for (var attempt = 0; attempt < 3; attempt++)
        {
            Assert.Equal("IDENTITY_DENIED_003", await FailAsync());
        }

        var (_, empty) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/login-bans", token: admin));
        Assert.False(empty.GetProperty("settings").GetProperty("isEnabled").GetBoolean());
        Assert.Empty(empty.GetProperty("items").EnumerateArray());

        // 打开：同一地址两次认证失败即封（账号不存在、密码错都算），封禁中连正确的密码也 503
        var settings = JsonSerializer.Serialize(new { isEnabled = true, windowMinutes = 10, maxFailures = 2, banMinutes = 5, maxBanMinutes = 20 });
        var (configured, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/configs", JsonSerializer.Serialize(new { key = "access.security.login-auto-ban", name = "Login auto-ban", dataType = "Json", value = settings }), admin));
        Assert.Equal(HttpStatusCode.Created, configured.StatusCode);
        Assert.Equal("IDENTITY_DENIED_003", await FailAsync());
        Assert.Equal("IDENTITY_DENIED_003", (await LoginFromAsync(server, attacker, "superadmin", "wrong")).Body.GetProperty("code").GetString());
        var (banned, bannedBody) = await LoginFromAsync(server, attacker, "superadmin", "SuperAdmin@123");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, banned);
        Assert.Equal("ACCESS_UNAVAILABLE_001", bannedBody.GetProperty("code").GetString());
        Assert.Equal(HttpStatusCode.OK, (await LoginFromAsync(server, "198.51.100.21", "superadmin", "SuperAdmin@123")).Status);

        // 平台查看：参数与当前封禁
        var (_, listed) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/login-bans", token: admin));
        Assert.True(listed.GetProperty("settings").GetProperty("isEnabled").GetBoolean());
        var ban = Assert.Single(listed.GetProperty("items").EnumerateArray());
        Assert.Equal(attacker, ban.GetProperty("ip").GetString());
        Assert.InRange(ban.GetProperty("remainingSeconds").GetInt32(), 1, 300);
        var banHit = Assert.Single(await HitsAsync(server, admin, "login-auto-ban"));
        Assert.Equal(("Deny", true, "BeforeAuthentication"), (banHit.GetProperty("action").GetString(), banHit.GetProperty("isBlocked").GetBoolean(), banHit.GetProperty("phase").GetString()));

        // 解封：立即可登录；再解封即没封着；地址写错
        var (lifted, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/login-bans:lift", $$"""{"ip":"{{attacker}}"}""", admin));
        Assert.Equal(HttpStatusCode.OK, lifted.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await LoginFromAsync(server, attacker, "superadmin", "SuperAdmin@123")).Status);
        var (_, again) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/login-bans:lift", $$"""{"ip":"{{attacker}}"}""", admin));
        Assert.Equal("ACCESS_NOTFOUND_009", again.GetProperty("code").GetString());
        var (_, invalid) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/login-bans:lift", """{"ip":"not-an-ip"}""", admin));
        Assert.Equal("ACCESS_VALIDATION_036", invalid.GetProperty("code").GetString());

        // 24 小时内再犯：封禁时长翻倍
        await FailAsync();
        await FailAsync();
        var (_, doubled) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/login-bans", token: admin));
        Assert.InRange(Assert.Single(doubled.GetProperty("items").EnumerateArray()).GetProperty("remainingSeconds").GetInt32(), 301, 600);
    }
}
