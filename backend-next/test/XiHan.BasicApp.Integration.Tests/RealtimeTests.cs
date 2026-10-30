// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using XiHan.BasicApp.Integration.Tests.Infrastructure;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证实时 Hub 与在线用户。
/// </summary>
/// <remarks>
/// Hub 只认本系统签发、会话仍有效的访问令牌；在线列表按会话列出并标出实时在线与当前会话；
/// 强制下线在提交后推 <c>ForceLogout</c> 给被下线的会话并断开它的连接，不能下线自己当前的会话；写用户设置推 <c>UserSettingChanged</c> 给同一用户的其他端。
/// </remarks>
public sealed class RealtimeTests : IDisposable
{
    private static readonly TimeSpan _wait = TimeSpan.FromSeconds(10);

    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private static HubConnection Connect(BasicAppTestServer server, string token) => new HubConnectionBuilder()
        .WithUrl("http://localhost/hubs/notification", options =>
        {
            options.HttpMessageHandlerFactory = _ => server.Server.CreateHandler();
            options.Transports = HttpTransportType.LongPolling;
            options.AccessTokenProvider = () => Task.FromResult<string?>(token);
        })
        .Build();

    private static TaskCompletionSource<JsonElement> Expect(HubConnection connection, string method)
    {
        var received = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.On<JsonElement>(method, payload => received.TrySetResult(payload));
        return received;
    }

    private static TaskCompletionSource Closed(HubConnection connection)
    {
        var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.Closed += _ =>
        {
            closed.TrySetResult();
            return Task.CompletedTask;
        };
        return closed;
    }

    /// <summary>
    /// 等这些会话在在线列表里都显示在线再推送。
    /// </summary>
    private static async Task WaitOnlineAsync(BasicAppTestServer server, string token, int count)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            var (_, summary) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/sessions:list", """{"page":{"index":1,"size":20}}""", token));
            if (summary.GetProperty("items").EnumerateArray().Count(static row => row.GetProperty("isRealtimeOnline").GetBoolean()) >= count)
            {
                return;
            }

            await Task.Delay(100);
        }

        throw new TimeoutException("Hub connections did not come online.");
    }

    [Fact]
    public async Task Hub_PushesAfterCommit_AndOnlineSessionsCanBeRevoked()
    {
        var (server, platform, tenant) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;
        var second = await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123");
        var secondToken = second.GetProperty("accessToken").GetString()!;
        var secondSession = second.GetProperty("sessionId").GetString()!;

        // 没带令牌：协商即被 ASP.NET Core 拒绝
        using (var anonymous = new HttpClient(server.Server.CreateHandler()) { BaseAddress = new Uri("http://localhost") })
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsync(new Uri("/hubs/notification/negotiate?negotiateVersion=1", UriKind.Relative), null)).StatusCode);
        }

        await using var first = Connect(server, platform);
        await using var other = Connect(server, secondToken);
        var settingChanged = Expect(other, "UserSettingChanged");
        var forced = Expect(other, "ForceLogout");
        var otherClosed = Closed(other);
        await first.StartAsync();
        await other.StartAsync();
        await WaitOnlineAsync(server, platform, 2);

        // 写用户设置：同一用户的其他端收到新值与发起端的客户端标识
        var (saved, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, "/api/user-settings", """{"scene":"Preference","key":"global","value":"{\"theme\":\"dark\"}","clientId":"tab-a","origin":"40,60"}""", platform));
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        var changed = await settingChanged.Task.WaitAsync(_wait);
        Assert.Equal(("Preference", "global", "{\"theme\":\"dark\"}", "tab-a", "40,60"), (changed.GetProperty("scene").GetString(), changed.GetProperty("settingKey").GetString(), changed.GetProperty("settingValue").GetString(), changed.GetProperty("sourceClientId").GetString(), changed.GetProperty("origin").GetString()));

        // 在线列表：两条平台会话都实时在线，调用方自己的会话标为当前；租户一侧看不到平台的会话
        var (listed, list) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/sessions:list", """{"page":{"index":1,"size":20}}""", platform));
        Assert.Equal(HttpStatusCode.OK, listed.StatusCode);
        var rows = list.GetProperty("items").EnumerateArray().ToList();
        Assert.Equal(2, rows.Count);
        Assert.All(rows, row => Assert.True(row.GetProperty("isRealtimeOnline").GetBoolean()));
        Assert.All(rows, row => Assert.Equal("superadmin", row.GetProperty("userName").GetString()));
        var current = Assert.Single(rows, row => row.GetProperty("isCurrent").GetBoolean());
        Assert.NotEqual(secondSession, current.GetProperty("id").GetString());
        var (_, summary) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/sessions:summary", token: platform));
        Assert.Equal((2, 1, 1), (summary.GetProperty("activeSessions").GetInt32(), summary.GetProperty("activeUsers").GetInt32(), summary.GetProperty("realtimeOnlineUsers").GetInt32()));
        var tenantRows = (await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/sessions:list", """{"page":{"index":1,"size":20}}""", tenant))).Body.GetProperty("items").EnumerateArray().ToList();
        Assert.Equal("admin", Assert.Single(tenantRows).GetProperty("userName").GetString());

        // 不能下线自己当前的会话；下线另一条：它收到 ForceLogout（带原因与目标会话）后被断开，令牌随即失效
        var (_, self) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/sessions:revoke", JsonSerializer.Serialize(new { ids = new[] { current.GetProperty("id").GetString() } }), platform));
        Assert.Equal("IDENTITY_CONFLICT_005", self.GetProperty("failed")[0].GetProperty("code").GetString());
        var (_, revoked) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/sessions:revoke", JsonSerializer.Serialize(new { ids = new[] { secondSession, Guid.NewGuid().ToString() }, reason = "maintenance" }), platform));
        Assert.Equal(secondSession, Assert.Single(revoked.GetProperty("succeeded").EnumerateArray()).GetString());
        Assert.Equal("IDENTITY_NOTFOUND_002", Assert.Single(revoked.GetProperty("failed").EnumerateArray()).GetProperty("code").GetString());
        var logout = await forced.Task.WaitAsync(_wait);
        Assert.Equal("maintenance", logout.GetProperty("reason").GetString());
        Assert.Equal(secondSession, Assert.Single(logout.GetProperty("targetSessionIds").EnumerateArray()).GetString());
        await otherClosed.Task.WaitAsync(_wait);
        Assert.Equal("IDENTITY_DENIED_001", (await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:me", token: secondToken))).Body.GetProperty("code").GetString());
        Assert.Equal(HubConnectionState.Connected, first.State);

        // 已吊销的会话再连 Hub：令牌本身还没过期，认证时的会话门禁也拒绝（协商即 401，连接建立不起来）
        await using var stale = Connect(server, secondToken);
        var refused = await Assert.ThrowsAsync<HttpRequestException>(() => stale.StartAsync());
        Assert.Equal(HttpStatusCode.Unauthorized, refused.StatusCode);
        var (_, after) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/sessions:summary", token: platform));
        Assert.Equal((1, 1), (after.GetProperty("activeSessions").GetInt32(), after.GetProperty("realtimeOnlineUsers").GetInt32()));
    }
}
