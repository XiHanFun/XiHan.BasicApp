// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using XiHan.BasicApp.Integration.Tests.Infrastructure;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证通知偏好的保存与发布时的筛选。
/// </summary>
/// <remarks>
/// 没保存过即缺省全收；整体覆盖保存；发布时站内信只发给愿意收这类通知的人（安全、待办各一类，其余算公告），强制阅读与紧急通知不筛（4.x 同）；
/// 关掉站内信即只剩强制与紧急。
/// </remarks>
public sealed class NotificationPreferenceTests : IDisposable
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
        var (status, body) = await SendAsync(server, HttpMethod.Post, "/api/users", JsonSerializer.Serialize(new { userName = name, displayName = name, password = "Pass@123456" }), token);
        Assert.Equal(HttpStatusCode.Created, status);
        return body.GetProperty("id").GetString()!;
    }

    /// <summary>
    /// 发一条给 ann 与 bob 的通知，返回实际进收件箱的人数。
    /// </summary>
    private static async Task<int> PublishAsync(BasicAppTestServer server, string token, string[] targets, object extra)
    {
        var request = JsonSerializer.SerializeToElement(extra).EnumerateObject().ToDictionary(static p => p.Name, static p => (object?)p.Value);
        request["title"] = "Hello";
        request["targetType"] = "User";
        request["targetIds"] = targets;
        var (created, createdBody) = await SendAsync(server, HttpMethod.Post, "/api/notifications", JsonSerializer.Serialize(request), token);
        Assert.Equal(HttpStatusCode.Created, created);
        var (published, publishedBody) = await SendAsync(server, HttpMethod.Post, $"/api/notifications/{createdBody.GetProperty("id").GetString()}:publish", "{}", token);
        Assert.Equal(HttpStatusCode.OK, published);
        return publishedBody.GetProperty("recipientCount").GetInt32();
    }

    private static string Preference(bool inApp, bool announcement) => $$"""{"channelInApp":{{(inApp ? "true" : "false")}},"channelEmail":true,"channelSms":false,"typeAnnouncement":{{(announcement ? "true" : "false")}},"typeTask":true,"typeSecurity":true}""";

    [Fact]
    public async Task Preferences_FilterTheInbox_ExceptMandatoryAndEmergency()
    {
        var (server, platform, _) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;
        string[] both = [await UserAsync(server, platform, "ann"), await UserAsync(server, platform, "bob")];
        var ann = (await AuthFlowTests.LoginAsync(server, "ann", "Pass@123456")).GetProperty("accessToken").GetString()!;

        // 没保存过：缺省全收
        var (_, defaults) = await SendAsync(server, HttpMethod.Get, "/api/profile:notification-preference", null, ann);
        Assert.True(defaults.EnumerateObject().All(static p => p.Value.GetBoolean()));

        // 整体覆盖保存：ann 不收公告
        var (saved, savedBody) = await SendAsync(server, HttpMethod.Post, "/api/profile:update-notification-preference", Preference(inApp: true, announcement: false), ann);
        Assert.Equal((HttpStatusCode.OK, false, false), (saved, savedBody.GetProperty("typeAnnouncement").GetBoolean(), savedBody.GetProperty("channelSms").GetBoolean()));
        var (_, reread) = await SendAsync(server, HttpMethod.Get, "/api/profile:notification-preference", null, ann);
        Assert.False(reread.GetProperty("typeAnnouncement").GetBoolean());

        // 公告只到 bob；安全通知两人都收；强制阅读与紧急通知不看偏好
        Assert.Equal(1, await PublishAsync(server, platform, both, new { notificationType = "System" }));
        Assert.Equal(2, await PublishAsync(server, platform, both, new { notificationType = "Security" }));
        Assert.Equal(2, await PublishAsync(server, platform, both, new { notificationType = "System", isMandatory = true }));
        Assert.Equal(2, await PublishAsync(server, platform, both, new { notificationType = "Emergency" }));

        // 关掉站内信：连安全通知也只到 bob，紧急照收
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, "/api/profile:update-notification-preference", Preference(inApp: false, announcement: true), ann)).Status);
        Assert.Equal(1, await PublishAsync(server, platform, both, new { notificationType = "Security" }));
        Assert.Equal(2, await PublishAsync(server, platform, both, new { notificationType = "Emergency" }));
        var (_, inbox) = await SendAsync(server, HttpMethod.Post, "/api/inbox:list", """{"page":{"index":1,"size":50}}""", ann);
        Assert.Equal(4, inbox.GetProperty("items").GetArrayLength());
    }
}
