// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using XiHan.BasicApp.Integration.Tests.Infrastructure;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证发给账号本人的安全通知
/// </summary>
/// <remarks>
/// 登录：别处都没登录着记一条「登录成功」，别的设备登录着就推给那些端，按设备标识分「其他设备」与「新设备」，投到那些会话各自所在的一侧；
/// 退出推给账号别的端，退出的这台收不到；管理员强制下线、改密、改用户名、API 凭证的新建滚动删除都落进本人的收件箱；这些只在收件箱里，不进通知公告管理与工作台的公告
/// </remarks>
public sealed class SecurityNoticeTests : IDisposable
{
    private const string Password = "Pass@123456";

    private const string Chrome = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36";

    private static readonly TimeSpan _wait = TimeSpan.FromSeconds(10);

    private readonly TestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    /// <summary>
    /// 登录与退出给本人记的提醒
    /// </summary>
    /// <remarks>
    /// 用例里每次登录都会留下一条（缺省区域 en-US），只核对别的通知时用它滤掉
    /// </remarks>
    internal static bool IsSignInNotice(JsonElement notice)
        => notice.GetProperty("title").GetString() is "Signed in successfully" or "Your account signed in on another device" or "Your account signed in on a new device" or "Your account signed out on another device";

    private static HubConnection Connect(BasicAppTestServer server, string token) => new HubConnectionBuilder()
        .WithUrl("http://localhost/hubs/notification", options =>
        {
            options.HttpMessageHandlerFactory = _ => server.Server.CreateHandler();
            options.Transports = HttpTransportType.LongPolling;
            options.AccessTokenProvider = () => Task.FromResult<string?>(token);
        })
        .Build();

    private static ChannelReader<JsonElement> Notifications(HubConnection connection)
    {
        var received = Channel.CreateUnbounded<JsonElement>();
        connection.On<JsonElement>("ReceiveNotification", payload => received.Writer.TryWrite(payload));
        return received.Reader;
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
    /// 等这个会话在它那一侧的在线列表里显示实时在线
    /// </summary>
    private static async Task WaitOnlineAsync(BasicAppTestServer server, string adminToken, string sessionId)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            var (_, page) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/sessions:list", """{"page":{"index":1,"size":100}}""", adminToken));
            if (page.GetProperty("items").EnumerateArray().Any(row => row.GetProperty("id").GetString() == sessionId && row.GetProperty("isRealtimeOnline").GetBoolean()))
            {
                return;
            }

            await Task.Delay(100);
        }

        throw new TimeoutException($"Session {sessionId} did not come online.");
    }

    private static async Task<JsonElement> LoginAsync(BasicAppTestServer server, string userName, string? tenant = null, string? deviceId = null)
    {
        var request = BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:login", JsonSerializer.Serialize(new { userName, password = Password, tenant, deviceId }), acceptLanguage: "en-US");
        request.Headers.UserAgent.ParseAdd(Chrome);
        var (response, body) = await server.SendAsync(request);
        Assert.True(response.StatusCode == HttpStatusCode.OK, body.ToString());
        return body.GetProperty("token");
    }

    private static async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(BasicAppTestServer server, HttpMethod method, string path, string? json, string? token)
    {
        var (response, body) = await server.SendAsync(BasicAppTestServer.Request(method, path, json, token, "en-US"));
        return (response.StatusCode, body);
    }

    private static async Task<string> CreateUserAsync(BasicAppTestServer server, string adminToken, string userName, string? homeTenantId = null)
    {
        var (status, body) = await SendAsync(server, HttpMethod.Post, "/api/users", JsonSerializer.Serialize(new { userName, displayName = userName, password = Password, homeTenantId }), adminToken);
        Assert.True(status == HttpStatusCode.Created, body.ToString());
        return body.GetProperty("id").GetString()!;
    }

    private static async Task<List<(string Title, string Content)>> SecurityInboxAsync(BasicAppTestServer server, string token)
    {
        var (status, body) = await SendAsync(server, HttpMethod.Post, "/api/inbox:list", """{"page":{"index":1,"size":50}}""", token);
        Assert.True(status == HttpStatusCode.OK, body.ToString());
        return body.GetProperty("items").EnumerateArray()
            .Where(static n => n.GetProperty("notificationType").GetString() == "Security")
            .Select(static n => (n.GetProperty("title").GetString()!, n.GetProperty("content").GetString()!))
            .ToList();
    }

    private static async Task<JsonElement> NextAsync(ChannelReader<JsonElement> notifications, string title)
    {
        using var timeout = new CancellationTokenSource(_wait);
        while (true)
        {
            var payload = await notifications.ReadAsync(timeout.Token);
            if (payload.GetProperty("title").GetString() == title)
            {
                return payload;
            }
        }
    }

    [Fact]
    public async Task Login_TellsTheSignedInDevices_NewOrKnownDevice()
    {
        var (server, platform, _) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;
        await CreateUserAsync(server, platform, "carol");

        // 别处都没登录着：这一侧记一条「登录成功」，带浏览器与系统
        var first = await LoginAsync(server, "carol", deviceId: "dfp_0a1b2c3d_4e5f6a7b");
        var firstToken = first.GetProperty("accessToken").GetString()!;
        var signedIn = Assert.Single(await SecurityInboxAsync(server, firstToken));
        Assert.Equal("Signed in successfully", signedIn.Title);
        Assert.Contains("Chrome · Windows", signedIn.Content, StringComparison.Ordinal);

        await using var hub = Connect(server, firstToken);
        var notifications = Notifications(hub);
        await hub.StartAsync();
        await WaitOnlineAsync(server, platform, first.GetProperty("sessionId").GetString()!);

        // 另一台没登录过的设备登录：已登录的端实时收到「新设备登录」（安全通知是警告色调）
        await LoginAsync(server, "carol", deviceId: "dfp_11111111_22222222");
        var newDevice = await NextAsync(notifications, "Your account signed in on a new device");
        Assert.Equal("Warning", newDevice.GetProperty("type").GetString());
        Assert.Contains("Chrome · Windows", newDevice.GetProperty("content").GetString(), StringComparison.Ordinal);

        // 同一台设备再登录：设备登录过，是「其他设备登录」；不报设备标识的当作新设备
        await LoginAsync(server, "carol", deviceId: "dfp_11111111_22222222");
        await NextAsync(notifications, "Your account signed in on another device");
        await LoginAsync(server, "carol");
        await NextAsync(notifications, "Your account signed in on a new device");
        var titles = (await SecurityInboxAsync(server, firstToken)).Select(static n => n.Title).Order(StringComparer.Ordinal).ToList();
        Assert.Equal(["Signed in successfully", "Your account signed in on a new device", "Your account signed in on a new device", "Your account signed in on another device"], titles);

        // 设备标识只收字母、数字、下划线与连字符
        var (invalid, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:login", JsonSerializer.Serialize(new { userName = "carol", password = Password, deviceId = "not a device id" })));
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }

    [Fact]
    public async Task Notices_StayOutOfNotificationManagement_AndWorkbenchAnnouncements()
    {
        var (server, platform, _) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;
        await CreateUserAsync(server, platform, "carol");
        var carol = (await LoginAsync(server, "carol")).GetProperty("accessToken").GetString()!;
        var (_, inbox) = await SendAsync(server, HttpMethod.Post, "/api/inbox:list", """{"page":{"index":1,"size":50}}""", carol);
        var notice = Assert.Single(inbox.GetProperty("items").EnumerateArray(), IsSignInNotice).GetProperty("notificationId").GetString();

        // 系统投递只在本人收件箱里：通知公告的列表不列出，按 ID 查看与删除都当作不存在
        var (listed, list) = await SendAsync(server, HttpMethod.Post, "/api/notifications:list", """{"page":{"index":1,"size":50}}""", platform);
        Assert.Equal(HttpStatusCode.OK, listed);
        Assert.DoesNotContain(list.GetProperty("items").EnumerateArray(), static n => IsSignInNotice(n));
        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(server, HttpMethod.Get, $"/api/notifications/{notice}", null, platform)).Status);
        var (_, deleted) = await SendAsync(server, HttpMethod.Delete, $"/api/notifications?ids={notice}", null, platform);
        Assert.Equal("NOTIFICATION_NOTFOUND_001", Assert.Single(deleted.GetProperty("failed").EnumerateArray()).GetProperty("code").GetString());
        Assert.Contains((await SendAsync(server, HttpMethod.Post, "/api/inbox:list", """{"page":{"index":1,"size":50}}""", carol)).Body.GetProperty("items").EnumerateArray(), IsSignInNotice);

        // 工作台的公告只取公告：收件箱里自己登录时记的提醒不算
        var (published, publishedBody) = await SendAsync(server, HttpMethod.Post, "/api/notifications", """{"title":"Release notes","publishImmediately":true}""", platform);
        Assert.True(published == HttpStatusCode.Created, publishedBody.ToString());
        var (_, platformInbox) = await SendAsync(server, HttpMethod.Post, "/api/inbox:list", """{"page":{"index":1,"size":50}}""", platform);
        Assert.Contains(platformInbox.GetProperty("items").EnumerateArray(), IsSignInNotice);
        var (_, widget) = await SendAsync(server, HttpMethod.Get, "/api/workbench:widget-data?key=notification.announcements", null, platform);
        Assert.Equal(["Release notes"], widget.GetProperty("items").EnumerateArray().Select(static i => i.GetProperty("title").GetString()));
    }

    [Fact]
    public async Task Notices_ReachTheTenantOfEachSession_AndLogoutSkipsTheDeviceLeaving()
    {
        var (server, platform, contoso) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;
        var enterprise = await EditionsTests.EditionIdAsync(server, platform, "enterprise");
        var (created, createdBody) = await SendAsync(server, HttpMethod.Post, "/api/tenants", $$"""{"tenantId":"fabrikam","name":"Fabrikam","editionId":"{{enterprise}}"}""", platform);
        Assert.True(created == HttpStatusCode.Created, createdBody.ToString());
        var (initialized, initializedBody) = await SendAsync(server, HttpMethod.Post, $"/api/tenants/{createdBody.GetProperty("id").GetString()}:initialize-admin", """{"userName":"admin","password":"Admin@123456"}""", platform);
        Assert.True(initialized == HttpStatusCode.OK, initializedBody.ToString());
        var fabrikam = (await AuthFlowTests.LoginAsync(server, "admin", "Admin@123456", "fabrikam")).GetProperty("accessToken").GetString()!;
        var alice = await CreateUserAsync(server, platform, "alice", "fabrikam");
        var (added, addedBody) = await SendAsync(server, HttpMethod.Post, "/api/tenant-members", JsonSerializer.Serialize(new { tenantId = "contoso", userId = alice }), platform);
        Assert.True(added == HttpStatusCode.Created, addedBody.ToString());

        // 第一台设备的会话切进 contoso：它此刻在 contoso 一侧
        var first = await LoginAsync(server, "alice", "fabrikam", "dfp_aaaaaaaa_bbbbbbbb");
        var (switched, switchedBody) = await SendAsync(server, HttpMethod.Post, "/api/auth:switch-tenant", """{"tenantId":"contoso"}""", first.GetProperty("accessToken").GetString());
        Assert.True(switched == HttpStatusCode.OK, switchedBody.ToString());
        var inContoso = switchedBody.GetProperty("accessToken").GetString()!;
        await using var contosoHub = Connect(server, inContoso);
        var contosoNotifications = Notifications(contosoHub);
        var contosoClosed = Closed(contosoHub);
        await contosoHub.StartAsync();
        await WaitOnlineAsync(server, contoso, first.GetProperty("sessionId").GetString()!);

        // 注册地 fabrikam 的新登录（匿名调用）投到那个会话所在的 contoso：实时收到，收件箱也在 contoso
        var second = await LoginAsync(server, "alice", "fabrikam", "dfp_aaaaaaaa_bbbbbbbb");
        await NextAsync(contosoNotifications, "Your account signed in on another device");
        Assert.Contains(await SecurityInboxAsync(server, inContoso), static n => n.Title == "Your account signed in on another device");

        var fabrikamToken = second.GetProperty("accessToken").GetString()!;
        await using var fabrikamHub = Connect(server, fabrikamToken);
        var fabrikamNotifications = Notifications(fabrikamHub);
        await fabrikamHub.StartAsync();
        await WaitOnlineAsync(server, fabrikam, second.GetProperty("sessionId").GetString()!);

        // contoso 那一端退出：fabrikam 的端实时收到「其他设备退出登录」；退出的这一端先被断开，收不到它
        var (loggedOut, loggedOutBody) = await SendAsync(server, HttpMethod.Post, "/api/auth:logout", "{}", inContoso);
        Assert.True(loggedOut == HttpStatusCode.OK, loggedOutBody.ToString());
        var logout = await NextAsync(fabrikamNotifications, "Your account signed out on another device");
        Assert.Contains("Chrome · Windows", logout.GetProperty("content").GetString(), StringComparison.Ordinal);
        await contosoClosed.Task.WaitAsync(_wait);
        while (contosoNotifications.TryRead(out var late))
        {
            Assert.NotEqual("Your account signed out on another device", late.GetProperty("title").GetString());
        }

        Assert.Contains(await SecurityInboxAsync(server, fabrikamToken), static n => n.Title == "Your account signed out on another device");
    }

    [Fact]
    public async Task Logout_InATenant_ReachesThePlatformDevices_OfASupportMember()
    {
        var (server, platform, contoso) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;
        var ops = await CreateUserAsync(server, platform, "ops");
        var (added, addedBody) = await SendAsync(server, HttpMethod.Post, "/api/tenant-members", JsonSerializer.Serialize(new { tenantId = "contoso", userId = ops, memberType = "Support" }), platform);
        Assert.True(added == HttpStatusCode.Created, addedBody.ToString());
        var first = await LoginAsync(server, "ops");
        var (switched, switchedBody) = await SendAsync(server, HttpMethod.Post, "/api/auth:switch-tenant", """{"tenantId":"contoso"}""", first.GetProperty("accessToken").GetString());
        Assert.True(switched == HttpStatusCode.OK, switchedBody.ToString());
        var second = await LoginAsync(server, "ops");
        var platformToken = second.GetProperty("accessToken").GetString()!;
        await using var platformHub = Connect(server, platformToken);
        var platformNotifications = Notifications(platformHub);
        await platformHub.StartAsync();
        await WaitOnlineAsync(server, platform, second.GetProperty("sessionId").GetString()!);

        // 在 contoso 一侧退出：平台一侧另起一次平台调用投递，平台的端实时收到
        var (loggedOut, loggedOutBody) = await SendAsync(server, HttpMethod.Post, "/api/auth:logout", "{}", switchedBody.GetProperty("accessToken").GetString());
        Assert.True(loggedOut == HttpStatusCode.OK, loggedOutBody.ToString());
        await NextAsync(platformNotifications, "Your account signed out on another device");
        Assert.Contains(await SecurityInboxAsync(server, platformToken), static n => n.Title == "Your account signed out on another device");
    }

    [Fact]
    public async Task AdminRevoke_TellsTheOwner_WithTheDeviceAndReason()
    {
        var (server, _, contoso) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;
        await CreateUserAsync(server, contoso, "dave");
        var revokedSession = (await LoginAsync(server, "dave", "contoso")).GetProperty("sessionId").GetString()!;
        var kept = await LoginAsync(server, "dave", "contoso");
        var keptToken = kept.GetProperty("accessToken").GetString()!;
        await using var hub = Connect(server, keptToken);
        var notifications = Notifications(hub);
        await hub.StartAsync();
        await WaitOnlineAsync(server, contoso, kept.GetProperty("sessionId").GetString()!);

        var (revoked, revokedBody) = await SendAsync(server, HttpMethod.Post, "/api/sessions:revoke", JsonSerializer.Serialize(new { ids = new[] { revokedSession }, reason = "maintenance" }), contoso);
        Assert.True(revoked == HttpStatusCode.OK, revokedBody.ToString());
        var notice = await NextAsync(notifications, "A session was signed out by an administrator");
        Assert.Contains("(reason: maintenance)", notice.GetProperty("content").GetString(), StringComparison.Ordinal);
        Assert.Contains(await SecurityInboxAsync(server, keptToken), static n => n.Title == "A session was signed out by an administrator");
    }

    [Fact]
    public async Task SelfServiceChanges_LeaveANoticeInTheInbox()
    {
        var (server, platform, _) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;
        await CreateUserAsync(server, platform, "erin");
        var token = (await LoginAsync(server, "erin")).GetProperty("accessToken").GetString()!;

        var (created, createdBody) = await SendAsync(server, HttpMethod.Post, "/api/my-api-credentials", """{"name":"ci"}""", token);
        Assert.True(created == HttpStatusCode.OK || created == HttpStatusCode.Created, createdBody.ToString());
        var credential = createdBody.GetProperty("id").GetString()!;
        var appKey = createdBody.GetProperty("appKey").GetString()!;
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, $"/api/my-api-credentials/{credential}:rotate-secret", "{}", token)).Status);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Delete, $"/api/my-api-credentials?ids={credential}", null, token)).Status);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, "/api/profile:change-username", JsonSerializer.Serialize(new { userName = "erin2", password = Password }), token)).Status);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, "/api/profile:change-password", JsonSerializer.Serialize(new { currentPassword = Password, newPassword = "Pass@654321" }), token)).Status);

        var inbox = await SecurityInboxAsync(server, token);
        Assert.Contains(inbox, n => n.Title == "An API credential was created" && n.Content.Contains($"\"ci\" ({appKey})", StringComparison.Ordinal));
        Assert.Contains(inbox, static n => n.Title == "An API credential secret was rotated");
        Assert.Contains(inbox, static n => n.Title == "An API credential was deleted");
        Assert.Contains(inbox, static n => n.Title == "Your username was changed" && n.Content.Contains("\"erin2\"", StringComparison.Ordinal));
        Assert.Contains(inbox, static n => n.Title == "Your password was changed");
    }
}
