// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using XiHan.BasicApp.Integration.Tests.Infrastructure;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 通知公告与收件箱：草稿可改、发布后不能改也不能再发；按全部 / 用户 / 角色 / 部门找人且只留当前一侧的正常账号；发布后接收人实时收到推送；
/// 收件箱的已读、确认、横幅、弹窗与强制阅读，可见期（开始时间未到的看不到）；提醒、阅读统计与接收人；删除已发布的连同收件箱。
/// </summary>
public sealed class NotificationTests : IDisposable
{
    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private static async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(BasicAppTestServer server, HttpMethod method, string path, string? json, string? token)
    {
        var (response, body) = await server.SendAsync(BasicAppTestServer.Request(method, path, json, token));
        return (response.StatusCode, body);
    }

    private static async Task<string> UserAsync(BasicAppTestServer server, string token, string name)
    {
        var (status, body) = await SendAsync(server, HttpMethod.Post, "/api/users", JsonSerializer.Serialize(new { userName = name, displayName = name.ToUpperInvariant(), password = "Pass@123456" }), token);
        Assert.Equal(HttpStatusCode.Created, status);
        return body.GetProperty("id").GetString()!;
    }

    private static async Task<string> DraftAsync(BasicAppTestServer server, string token, object request)
    {
        var (status, body) = await SendAsync(server, HttpMethod.Post, "/api/notifications", JsonSerializer.Serialize(request), token);
        Assert.Equal(HttpStatusCode.Created, status);
        Assert.False(body.GetProperty("isPublished").GetBoolean());
        return body.GetProperty("id").GetString()!;
    }

    private static async Task<List<JsonElement>> InboxAsync(BasicAppTestServer server, string token, string filters = "[]")
        => (await SendAsync(server, HttpMethod.Post, "/api/inbox:list", $$"""{"page":{"index":1,"size":50},"filters":{{filters}}}""", token)).Body.GetProperty("items").EnumerateArray().ToList();

    private static async Task<int> ShowcaseAsync(BasicAppTestServer server, string token, string which)
        => (await SendAsync(server, HttpMethod.Get, $"/api/inbox:{which}", null, token)).Body.GetProperty("items").GetArrayLength();

    [Fact]
    public async Task Notifications_ReachTheirRecipients_AndTheInboxTracksReading()
    {
        var (server, platform, tenant) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;
        var ann = await UserAsync(server, platform, "ann");
        await UserAsync(server, platform, "bob");
        var annToken = (await AuthFlowTests.LoginAsync(server, "ann", "Pass@123456")).GetProperty("accessToken").GetString()!;
        var bobToken = (await AuthFlowTests.LoginAsync(server, "bob", "Pass@123456")).GetProperty("accessToken").GetString()!;

        // 校验：按角色要给目标、平台一侧没有部门、到期须晚于开始
        Assert.Equal("NOTIFICATION_VALIDATION_001", (await SendAsync(server, HttpMethod.Post, "/api/notifications", """{"title":"x","targetType":"Role"}""", platform)).Body.GetProperty("code").GetString());
        Assert.Equal("NOTIFICATION_VALIDATION_002", (await SendAsync(server, HttpMethod.Post, "/api/notifications", $$"""{"title":"x","targetType":"Department","targetIds":["{{Guid.NewGuid()}}"]}""", platform)).Body.GetProperty("code").GetString());
        Assert.Equal("NOTIFICATION_VALIDATION_003", (await SendAsync(server, HttpMethod.Post, "/api/notifications", """{"title":"x","startsAt":"2030-01-02T00:00:00Z","expiresAt":"2030-01-01T00:00:00Z"}""", platform)).Body.GetProperty("code").GetString());

        // 全部：草稿可改；发布即平台一侧的三个账号收到，ann 的连接实时收到推送；发布后不能改也不能再发
        await using var hub = new HubConnectionBuilder()
            .WithUrl("http://localhost/hubs/notification", options =>
            {
                options.HttpMessageHandlerFactory = _ => server.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
                options.AccessTokenProvider = () => Task.FromResult<string?>(annToken);
            })
            .Build();
        var pushed = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        hub.On<JsonElement>("ReceiveNotification", payload => pushed.TrySetResult(payload));
        await hub.StartAsync();
        for (var attempt = 0; attempt < 100 && (await SendAsync(server, HttpMethod.Get, "/api/sessions:summary", null, platform)).Body.GetProperty("realtimeOnlineUsers").GetInt32() == 0; attempt++)
        {
            await Task.Delay(100);
        }

        var all = await DraftAsync(server, platform, new { title = "Maintenance", content = "**tonight**", notificationType = "Emergency" });
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Put, $"/api/notifications/{all}", """{"title":"Maintenance window","content":"**tonight**","notificationType":"Emergency"}""", platform)).Status);
        var (_, publishedBody) = await SendAsync(server, HttpMethod.Post, $"/api/notifications/{all}:publish", "{}", platform);
        Assert.Equal(3, publishedBody.GetProperty("recipientCount").GetInt32());
        var push = await pushed.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(("Error", "Maintenance window", "Markdown", all), (push.GetProperty("type").GetString(), push.GetProperty("title").GetString(), push.GetProperty("contentFormat").GetString(), push.GetProperty("notificationId").GetString()));
        Assert.Equal("NOTIFICATION_CONFLICT_001", (await SendAsync(server, HttpMethod.Put, $"/api/notifications/{all}", """{"title":"again"}""", platform)).Body.GetProperty("code").GetString());
        Assert.Equal("NOTIFICATION_CONFLICT_001", (await SendAsync(server, HttpMethod.Post, $"/api/notifications/{all}:publish", "{}", platform)).Body.GetProperty("code").GetString());
        Assert.Empty(await InboxAsync(server, tenant));

        // 指定用户 + 横幅 / 弹窗 / 强制阅读 / 需要确认：只到 ann；开始时间未到的看不到
        var special = await DraftAsync(server, platform, new { title = "Policy", targetType = "User", targetIds = new[] { ann }, isBanner = true, isPopup = true, isMandatory = true, needConfirm = true });
        await SendAsync(server, HttpMethod.Post, $"/api/notifications/{special}:publish", "{}", platform);
        var later = await DraftAsync(server, platform, new { title = "Later", targetType = "User", targetIds = new[] { ann }, startsAt = DateTimeOffset.UtcNow.AddDays(1) });
        await SendAsync(server, HttpMethod.Post, $"/api/notifications/{later}:publish", "{}", platform);
        var annInbox = await InboxAsync(server, annToken);
        Assert.Equal(["Policy", "Maintenance window"], annInbox.Select(static i => i.GetProperty("title").GetString()).ToList());
        Assert.Single(await InboxAsync(server, bobToken));
        Assert.Equal((1, 1, 1), (await ShowcaseAsync(server, annToken, "banners"), await ShowcaseAsync(server, annToken, "popups"), await ShowcaseAsync(server, annToken, "mandatory")));
        var policyItem = annInbox[0].GetProperty("id").GetString()!;
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, $"/api/inbox/{policyItem}:mark-popup-shown", "{}", annToken)).Status);
        Assert.Equal(0, await ShowcaseAsync(server, annToken, "popups"));
        Assert.Equal(2, (await InboxAsync(server, annToken, """[{"field":"pending","operator":"eq","value":"true"}]""")).Count);
        Assert.Equal("NOTIFICATION_NOTFOUND_002", (await SendAsync(server, HttpMethod.Post, $"/api/inbox/{policyItem}:confirm", "{}", bobToken)).Body.GetProperty("code").GetString());
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, $"/api/inbox/{policyItem}:confirm", "{}", annToken)).Status);
        Assert.Equal((0, 0), (await ShowcaseAsync(server, annToken, "banners"), await ShowcaseAsync(server, annToken, "mandatory")));
        var confirmed = (await InboxAsync(server, annToken))[0];
        Assert.Equal("Read", confirmed.GetProperty("status").GetString());
        Assert.NotEqual(JsonValueKind.Null, confirmed.GetProperty("confirmedAt").ValueKind);

        // 统计、接收人与提醒：ann 读了全部，bob 与超管未读
        Assert.Equal(1, (await SendAsync(server, HttpMethod.Post, "/api/inbox:mark-all-read", "{}", annToken)).Body.GetProperty("count").GetInt32());
        var stats = (await SendAsync(server, HttpMethod.Get, $"/api/notifications/{all}:read-stats", null, platform)).Body;
        Assert.Equal((3, 1, 2), (stats.GetProperty("recipientCount").GetInt32(), stats.GetProperty("readCount").GetInt32(), stats.GetProperty("unreadCount").GetInt32()));
        var unread = (await SendAsync(server, HttpMethod.Post, $"/api/notifications/{all}:recipients", """{"filter":"Unread"}""", platform)).Body;
        Assert.Equal(["BOB", "Super Admin"], unread.GetProperty("items").EnumerateArray().Select(static r => r.GetProperty("displayName").GetString()).Order(StringComparer.Ordinal).ToList());
        Assert.Equal(2, (await SendAsync(server, HttpMethod.Post, $"/api/notifications/{all}:remind", "{}", platform)).Body.GetProperty("recipientCount").GetInt32());
        var draft = await DraftAsync(server, platform, new { title = "Draft" });
        Assert.Equal("NOTIFICATION_CONFLICT_002", (await SendAsync(server, HttpMethod.Post, $"/api/notifications/{draft}:remind", "{}", platform)).Body.GetProperty("code").GetString());

        // 租户一侧：按角色与部门找人，只到本租户的账号
        var tenantRoles = (await SendAsync(server, HttpMethod.Post, "/api/roles:list", "{}", tenant)).Body.GetProperty("items").EnumerateArray().ToList();
        var adminRole = tenantRoles.Single(static r => r.GetProperty("code").GetString() == "admin").GetProperty("id").GetString()!;
        var byRole = await DraftAsync(server, tenant, new { title = "Team", targetType = "Role", targetIds = new[] { adminRole } });
        Assert.Equal(1, (await SendAsync(server, HttpMethod.Post, $"/api/notifications/{byRole}:publish", "{}", tenant)).Body.GetProperty("recipientCount").GetInt32());
        var carol = await UserAsync(server, tenant, "carol");
        var hq = (await SendAsync(server, HttpMethod.Post, "/api/departments", """{"code":"hq","name":"HQ"}""", tenant)).Body.GetProperty("id").GetString()!;
        var rd = (await SendAsync(server, HttpMethod.Post, "/api/departments", $$"""{"code":"rd","name":"R&D","parentId":"{{hq}}"}""", tenant)).Body.GetProperty("id").GetString()!;
        Assert.Equal(HttpStatusCode.Created, (await SendAsync(server, HttpMethod.Post, "/api/memberships", $$"""{"userId":"{{carol}}","departmentId":"{{rd}}"}""", tenant)).Status);
        var byDepartment = await DraftAsync(server, tenant, new { title = "HQ news", targetType = "Department", targetIds = new[] { hq } });
        Assert.Equal(1, (await SendAsync(server, HttpMethod.Post, $"/api/notifications/{byDepartment}:publish", "{}", tenant)).Body.GetProperty("recipientCount").GetInt32());
        var carolToken = (await AuthFlowTests.LoginAsync(server, "carol", "Pass@123456", "contoso")).GetProperty("accessToken").GetString()!;
        Assert.Equal(["HQ news"], (await InboxAsync(server, carolToken)).Select(static i => i.GetProperty("title").GetString()).ToList());
        var nobody = await DraftAsync(server, tenant, new { title = "Nobody", targetType = "User", targetIds = new[] { ann } });
        Assert.Equal("NOTIFICATION_VALIDATION_004", (await SendAsync(server, HttpMethod.Post, $"/api/notifications/{nobody}:publish", "{}", tenant)).Body.GetProperty("code").GetString());
        Assert.Equal("NOTIFICATION_NOTFOUND_001", (await SendAsync(server, HttpMethod.Get, $"/api/notifications/{all}", null, tenant)).Body.GetProperty("code").GetString());

        // 删除：收件箱自己删一条不影响别人；删除已发布的通知连同所有收件箱
        var bobItem = (await InboxAsync(server, bobToken))[0].GetProperty("id").GetString()!;
        Assert.Single((await SendAsync(server, HttpMethod.Delete, $"/api/inbox?ids={bobItem}", null, bobToken)).Body.GetProperty("succeeded").EnumerateArray());
        Assert.Empty(await InboxAsync(server, bobToken));
        Assert.Equal(2, (await InboxAsync(server, annToken)).Count);
        Assert.Empty((await SendAsync(server, HttpMethod.Delete, $"/api/notifications?ids={all}", null, platform)).Body.GetProperty("failed").EnumerateArray());
        Assert.Equal(["Policy"], (await InboxAsync(server, annToken)).Select(static i => i.GetProperty("title").GetString()).ToList());
        var listed = (await SendAsync(server, HttpMethod.Post, "/api/notifications:list", """{"page":{"index":1,"size":20},"filters":[{"field":"isPublished","operator":"eq","value":"true"}]}""", platform)).Body;
        Assert.Equal(2, listed.GetProperty("total").GetInt32());
    }
}
