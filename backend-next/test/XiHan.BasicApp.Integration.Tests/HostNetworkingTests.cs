// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using XiHan.BasicApp.Host;
using XiHan.BasicApp.Integration.Tests.Infrastructure;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证宿主的反向代理转发头与跨域。
/// </summary>
/// <remarks>
/// 转发头只认受信代理转来的 <c>X-Forwarded-For</c>（登录自动封禁记下的是还原后的客户端地址）；跨域只对配置的来源放行，没配来源即不启用；两节配置写错启动即报错。
/// </remarks>
public sealed class HostNetworkingTests : IDisposable
{
    private const string ConnectionIpHeader = "X-Test-Client-Ip";
    private const string Proxy = "10.0.0.5";
    private const string Origin = "https://admin.example.com";
    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    // 测试服务器没有对端地址：按请求头给出连接上的地址，排在宿主的转发头之前
    private Task<BasicAppTestServer> StartAsync(params (string Key, string Value)[] settings)
        => BasicAppTestServer.StartAsync(
            _database.ConnectionString,
            settings: settings.Select(static s => new KeyValuePair<string, string?>(s.Key, s.Value)),
            configureApp: app => app.Use(async (http, next) =>
            {
                if (http.Request.Headers.TryGetValue(ConnectionIpHeader, out var ip))
                {
                    http.Connection.RemoteIpAddress = IPAddress.Parse(ip.ToString());
                }

                await next();
            }));

    private static async Task<string?> FailLoginAsync(BasicAppTestServer server, string connection, string? forwardedFor)
    {
        var request = BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:login", JsonSerializer.Serialize(new { userName = $"ghost-{Guid.NewGuid():N}", password = "wrong" }));
        request.Headers.Add(ConnectionIpHeader, connection);
        if (forwardedFor is not null)
        {
            request.Headers.Add("X-Forwarded-For", forwardedFor);
        }

        return (await server.SendAsync(request)).Body.GetProperty("code").GetString();
    }

    [Fact]
    public async Task ForwardedFor_IsTrustedOnlyFromKnownProxies()
    {
        var server = await StartAsync(($"{ForwardedHeadersSettings.ConfigurationSection}:KnownProxies:0", Proxy));
        await using var running = server;
        await server.SeedDefaultsAsync();
        await server.RunSeedersAsync();
        var admin = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;
        var settings = JsonSerializer.Serialize(new { isEnabled = true, windowMinutes = 10, maxFailures = 2, banMinutes = 5, maxBanMinutes = 20 });
        await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/configs", JsonSerializer.Serialize(new { key = "access.security.login-auto-ban", name = "Login auto-ban", dataType = "Json", value = settings }), admin));

        // 受信代理转来的：按转发头还原出客户端地址，封的是客户端
        Assert.Equal("IDENTITY_DENIED_003", await FailLoginAsync(server, Proxy, "203.0.113.7"));
        Assert.Equal("IDENTITY_DENIED_003", await FailLoginAsync(server, Proxy, "203.0.113.7"));

        // 不受信的来源自己写的转发头不认：封的是连接上的地址
        Assert.Equal("IDENTITY_DENIED_003", await FailLoginAsync(server, "198.51.100.30", "203.0.113.8"));
        Assert.Equal("IDENTITY_DENIED_003", await FailLoginAsync(server, "198.51.100.30", "203.0.113.8"));

        var (_, bans) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/login-bans", token: admin));
        Assert.Equal(["198.51.100.30", "203.0.113.7"], bans.GetProperty("items").EnumerateArray().Select(static b => b.GetProperty("ip").GetString()).Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task Cors_AllowsOnlyConfiguredOrigins_AndIsOffWithoutThem()
    {
        await using (var server = await StartAsync(($"{CorsSettings.ConfigurationSection}:AllowedOrigins:0", Origin)))
        {
            // 预检：配置的来源放行并允许凭据，别的来源不给跨域头
            var preflight = new HttpRequestMessage(HttpMethod.Options, BasicAppTestServer.ApiPrefix + "/auth:login");
            preflight.Headers.Add("Origin", Origin);
            preflight.Headers.Add("Access-Control-Request-Method", "POST");
            preflight.Headers.Add("Access-Control-Request-Headers", "content-type,authorization");
            var allowed = await server.Client.SendAsync(preflight, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.NoContent, allowed.StatusCode);
            Assert.Equal(Origin, Assert.Single(allowed.Headers.GetValues("Access-Control-Allow-Origin")));
            Assert.Equal("true", Assert.Single(allowed.Headers.GetValues("Access-Control-Allow-Credentials")));

            var other = new HttpRequestMessage(HttpMethod.Get, BasicAppTestServer.ApiPrefix + "/auth:login-config");
            other.Headers.Add("Origin", "https://evil.example.com");
            Assert.False((await server.Client.SendAsync(other, TestContext.Current.CancellationToken)).Headers.Contains("Access-Control-Allow-Origin"));

            var actual = new HttpRequestMessage(HttpMethod.Get, BasicAppTestServer.ApiPrefix + "/auth:login-config");
            actual.Headers.Add("Origin", Origin);
            var response = await server.Client.SendAsync(actual, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(Origin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        }

        // 没配来源：不启用跨域
        await using var plain = await StartAsync();
        var request = new HttpRequestMessage(HttpMethod.Get, BasicAppTestServer.ApiPrefix + "/auth:login-config");
        request.Headers.Add("Origin", Origin);
        Assert.False((await plain.Client.SendAsync(request, TestContext.Current.CancellationToken)).Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Theory]
    [InlineData("BasicApp:ForwardedHeaders:KnownProxies:0", "not-an-ip")]
    [InlineData("BasicApp:ForwardedHeaders:KnownNetworks:0", "10.0.0.5/8")]
    [InlineData("BasicApp:ForwardedHeaders:ForwardLimit", "0")]
    [InlineData("BasicApp:Cors:AllowedOrigins:0", "https://admin.example.com/app")]
    [InlineData("BasicApp:Cors:AllowAnyOrigin", "true")]
    public void Misconfiguration_FailsAtStartup(string key, string value)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection([new(key, value)]).Build();
        Assert.Throws<ArgumentException>(() =>
        {
            _ = ForwardedHeadersSettings.FromConfiguration(configuration);
            _ = CorsSettings.FromConfiguration(configuration);
        });
    }
}
