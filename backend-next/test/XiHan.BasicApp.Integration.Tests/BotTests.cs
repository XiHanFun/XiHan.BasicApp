// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using System.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.BasicApp.Notification.Domain.Entities;
using XiHan.BasicApp.Notification.Infrastructure;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证群机器人的配置约束、通知投递与各通道载荷。
/// </summary>
/// <remarks>
/// Webhook 只认各服务商的官方 https 地址；每个服务商一个缺省，缺省的不能删、停用或换服务商；签名密钥不回显；
/// 通知的机器人渠道发到本侧每个服务商的缺省机器人（经发件箱，由发送循环发出），租户没有自己的机器人即不可用（不借平台的）；
/// Telegram 机器人只在平台、令牌不回显，广播到每个启用的机器人的每个广播群；三家群机器人通道的地址、签名与载荷（Telegram 的出站在 TelegramTests）。
/// </remarks>
public sealed class BotTests : IDisposable
{
    private const string TelegramToken = "123456789:AAEexampleTOKEN_for-tests-only0";

    private static readonly string[] _withBot = ["InApp", "Bot"];

    private readonly SqliteDatabase _database = new();
    private readonly ConcurrentQueue<(BotEndpoint Endpoint, string Text)> _sent = new();

    public void Dispose() => _database.Dispose();

    private static async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(BasicAppTestServer server, HttpMethod method, string path, object? body, string token)
    {
        var json = body switch
        {
            null => null,
            string text => text,
            _ => JsonSerializer.Serialize(body),
        };
        var (response, payload) = await server.SendAsync(BasicAppTestServer.Request(method, path, json, token));
        return (response.StatusCode, payload);
    }

    private static async Task<JsonElement> CreatedAsync(BasicAppTestServer server, string path, object body, string token)
    {
        var (status, created) = await SendAsync(server, HttpMethod.Post, path, body, token);
        Assert.True(status == HttpStatusCode.Created, created.ToString());
        return created;
    }

    [Fact]
    public async Task Bots_ReceiveNotificationsOfTheirOwnSide_ThroughTheOutbox()
    {
        var (server, platform, tenant) = await SettingsTestHost.StartAsync(_database.ConnectionString, configure: builder =>
        {
            builder.Configuration.AddInMemoryCollection([new("BasicApp:Notification:DispatchInterval", "00:00:00.100")]);
            foreach (var channel in Enum.GetValues<BotChannel>())
            {
                builder.Services.AddSingleton<IBotTransport>(new FakeBotTransport(channel, _sent));
            }
        });
        await using var running = server;

        // 地址：别的主机、http 与带端口的都拒绝
        foreach (var url in new[] { "https://example.com/robot/send?access_token=x", "http://oapi.dingtalk.com/robot/send?access_token=x", "https://oapi.dingtalk.com:8443/robot/send?access_token=x" })
        {
            Assert.Equal("NOTIFICATION_VALIDATION_011", (await SendAsync(server, HttpMethod.Post, "/api/bot-configs", new { code = "bad", name = "Bad", provider = "DingTalk", webhookUrl = url }, platform)).Body.GetProperty("code").GetString());
        }

        Assert.Equal("NOTIFICATION_VALIDATION_011", (await SendAsync(server, HttpMethod.Post, "/api/bot-configs", new { code = "bad", name = "Bad", provider = "Lark", webhookUrl = "https://oapi.dingtalk.com/robot/send?access_token=x" }, platform)).Body.GetProperty("code").GetString());

        // 每个服务商的第一个即缺省；密钥不回显；缺省的不能删、停用或换服务商
        var ding = await CreatedAsync(server, "/api/bot-configs", new { code = "ding", name = "Ding", provider = "DingTalk", webhookUrl = "https://oapi.dingtalk.com/robot/send?access_token=one", secret = "SEC-secret-1", keyword = "XiHan" }, platform);
        Assert.True(ding.GetProperty("isDefault").GetBoolean());
        Assert.True(ding.GetProperty("hasSecret").GetBoolean());
        Assert.DoesNotContain("SEC-secret-1", ding.ToString(), StringComparison.Ordinal);
        var dingId = ding.GetProperty("id").GetString()!;
        var ding2 = await CreatedAsync(server, "/api/bot-configs", new { code = "ding2", name = "Ding 2", provider = "DingTalk", webhookUrl = "https://oapi.dingtalk.com/robot/send?access_token=two" }, platform);
        Assert.False(ding2.GetProperty("isDefault").GetBoolean());
        var wecom = await CreatedAsync(server, "/api/bot-configs", new { code = "wecom", name = "WeCom", provider = "WeCom", webhookUrl = "https://qyapi.weixin.qq.com/cgi-bin/webhook/send?key=k1" }, platform);
        Assert.True(wecom.GetProperty("isDefault").GetBoolean());
        Assert.Equal("NOTIFICATION_CONFLICT_005", (await SendAsync(server, HttpMethod.Delete, $"/api/bot-configs?ids={dingId}", null, platform)).Body.GetProperty("failed")[0].GetProperty("code").GetString());
        Assert.Equal("NOTIFICATION_CONFLICT_005", (await SendAsync(server, HttpMethod.Put, $"/api/bot-configs/{dingId}", new { name = "Ding", provider = "DingTalk", webhookUrl = "https://oapi.dingtalk.com/robot/send?access_token=one", isEnabled = false }, platform)).Body.GetProperty("code").GetString());
        Assert.Equal("NOTIFICATION_CONFLICT_005", (await SendAsync(server, HttpMethod.Put, $"/api/bot-configs/{dingId}", new { name = "Ding", provider = "Lark", webhookUrl = "https://open.feishu.cn/open-apis/bot/v2/hook/x" }, platform)).Body.GetProperty("code").GetString());

        // 修改不填密钥即保留；设缺省只换同一服务商的
        var (updatedStatus, updated) = await SendAsync(server, HttpMethod.Put, $"/api/bot-configs/{dingId}", new { name = "Ding renamed", provider = "DingTalk", webhookUrl = "https://oapi.dingtalk.com/robot/send?access_token=one", keyword = "XiHan" }, platform);
        Assert.Equal(HttpStatusCode.OK, updatedStatus);
        Assert.True(updated.GetProperty("hasSecret").GetBoolean());
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, $"/api/bot-configs/{ding2.GetProperty("id").GetString()}:set-default", "{}", platform)).Status);
        var listed = (await SendAsync(server, HttpMethod.Post, "/api/bot-configs:list", """{"page":{"index":1,"size":20}}""", platform)).Body.GetProperty("items").EnumerateArray()
            .ToDictionary(static c => c.GetProperty("code").GetString()!, static c => c.GetProperty("isDefault").GetBoolean());
        Assert.Equal(new Dictionary<string, bool> { ["ding"] = false, ["ding2"] = true, ["wecom"] = true }, listed);
        Assert.Single((await SendAsync(server, HttpMethod.Post, "/api/bot-configs:list", """{"page":{"index":1,"size":20},"filters":[{"field":"provider","operator":"eq","value":"WeCom"}]}""", platform)).Body.GetProperty("items").EnumerateArray());

        // Telegram（平台托管）：令牌格式、ID 与命令列表校验并规整，名称唯一，令牌不回显、修改不填即保留；停用的不广播；租户看不到
        Assert.Equal("NOTIFICATION_VALIDATION_012", (await SendAsync(server, HttpMethod.Post, "/api/telegram-bots", new { name = "ops", token = "not-a-token" }, platform)).Body.GetProperty("code").GetString());
        var (idsStatus, idsBody) = await SendAsync(server, HttpMethod.Post, "/api/telegram-bots", new { name = "ops", token = TelegramToken, adminUsers = "1001,abc" }, platform);
        Assert.Equal((HttpStatusCode.BadRequest, "NOTIFICATION_VALIDATION_013"), (idsStatus, idsBody.GetProperty("code").GetString()));
        Assert.Equal("NOTIFICATION_VALIDATION_014", (await SendAsync(server, HttpMethod.Post, "/api/telegram-bots", new { name = "ops", token = TelegramToken, allowedCommands = "help,bad cmd" }, platform)).Body.GetProperty("code").GetString());
        var ops = await CreatedAsync(server, "/api/telegram-bots", new { name = "ops", token = TelegramToken, adminUsers = "1001, 1002,1001", allowedCommands = "/Start,help", broadcastChatIds = "-100123,-100456" }, platform);
        Assert.Equal((true, "1001,1002", "start,help", "-100123,-100456"), (ops.GetProperty("hasToken").GetBoolean(), ops.GetProperty("adminUsers").GetString(), ops.GetProperty("allowedCommands").GetString(), ops.GetProperty("broadcastChatIds").GetString()));
        Assert.DoesNotContain(TelegramToken, ops.ToString(), StringComparison.Ordinal);
        Assert.Equal("NOTIFICATION_CONFLICT_008", (await SendAsync(server, HttpMethod.Post, "/api/telegram-bots", new { name = "ops", token = TelegramToken }, platform)).Body.GetProperty("code").GetString());
        var opsId = ops.GetProperty("id").GetString()!;
        var (renamedStatus, renamed) = await SendAsync(server, HttpMethod.Put, $"/api/telegram-bots/{opsId}", new { name = "ops-main", adminUsers = "1001", broadcastChatIds = "-100123,-100456" }, platform);
        Assert.Equal((HttpStatusCode.OK, true, "ops-main"), (renamedStatus, renamed.GetProperty("hasToken").GetBoolean(), renamed.GetProperty("name").GetString()));
        await CreatedAsync(server, "/api/telegram-bots", new { name = "idle", token = TelegramToken, broadcastChatIds = "-100999", isEnabled = false }, platform);
        Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(server, HttpMethod.Post, "/api/telegram-bots:list", "{}", tenant)).Status);

        // 发布：本侧每个服务商的缺省机器人与（平台一侧）每个启用的 Telegram 机器人的每个广播群各收到一条纯文本（标题、正文与链接）
        var notification = await CreatedAsync(server, "/api/notifications", new { title = "Release", content = "v6 is out", link = "/notification/inbox", deliveryChannels = _withBot }, platform);
        Assert.Equal(["InApp", "Bot"], notification.GetProperty("deliveryChannels").EnumerateArray().Select(static c => c.GetString()).ToList());
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, $"/api/notifications/{notification.GetProperty("id").GetString()}:publish", "{}", platform)).Status);
        for (var attempt = 0; attempt < 100 && _sent.Count < 4; attempt++)
        {
            await Task.Delay(100);
        }

        var sent = _sent.OrderBy(static s => s.Endpoint.Channel).ThenBy(static s => s.Endpoint.ChatId, StringComparer.Ordinal).ToList();
        Assert.Equal([BotChannel.DingTalk, BotChannel.WeCom, BotChannel.Telegram, BotChannel.Telegram], sent.Select(static s => s.Endpoint.Channel).ToList());
        Assert.Equal(("https://oapi.dingtalk.com/robot/send?access_token=two", null), (sent[0].Endpoint.WebhookUrl, sent[0].Endpoint.Secret));
        Assert.Equal([("-100123", TelegramToken), ("-100456", TelegramToken)], sent.Skip(2).Select(static s => (s.Endpoint.ChatId!, s.Endpoint.Secret!)).ToList());
        Assert.All(sent, static s => Assert.Equal("Release\n\nv6 is out\n\n/notification/inbox", s.Text));

        // 正文按机器人渠道的模板 notification-bot 渲染：没有正文与链接就只有标题；平台改了模板即按新的发
        var botTemplate = (await SendAsync(server, HttpMethod.Post, "/api/message-templates:list", """{"filters":[{"field":"channel","operator":"eq","value":"Bot"}]}""", platform)).Body.GetProperty("items")[0];
        Assert.Equal("notification-bot", botTemplate.GetProperty("code").GetString());
        var botTemplateId = botTemplate.GetProperty("id").GetString()!;
        var ping = await CreatedAsync(server, "/api/notifications", new { title = "Ping", deliveryChannels = _withBot }, platform);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, $"/api/notifications/{ping.GetProperty("id").GetString()}:publish", "{}", platform)).Status);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Put, $"/api/message-templates/{botTemplateId}", new { name = "Notice", content = "Notice: {{title}}" }, platform)).Status);
        var pong = await CreatedAsync(server, "/api/notifications", new { title = "Pong", content = "ignored", deliveryChannels = _withBot }, platform);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, $"/api/notifications/{pong.GetProperty("id").GetString()}:publish", "{}", platform)).Status);
        for (var attempt = 0; attempt < 100 && _sent.Count < 12; attempt++)
        {
            await Task.Delay(100);
        }

        Assert.Equal(4, _sent.Count(static s => s.Text == "Ping"));
        Assert.Equal(4, _sent.Count(static s => s.Text == "Notice: Pong"));

        // 模板停用了，机器人渠道即不可用（与邮件、短信同，不悄悄改发纯文本）
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Put, $"/api/message-templates/{botTemplateId}", new { name = "Notice", content = "{{title}}", isEnabled = false }, platform)).Status);
        var muted = await CreatedAsync(server, "/api/notifications", new { title = "Muted", deliveryChannels = _withBot }, platform);
        Assert.Equal("NOTIFICATION_UNAVAILABLE_003", (await SendAsync(server, HttpMethod.Post, $"/api/notifications/{muted.GetProperty("id").GetString()}:publish", "{}", platform)).Body.GetProperty("code").GetString());

        // 租户没有自己的机器人：机器人渠道不可用，不借平台的（也不发平台的 Telegram）
        var tenantDraft = await CreatedAsync(server, "/api/notifications", new { title = "Team", deliveryChannels = _withBot }, tenant);
        Assert.Equal("NOTIFICATION_UNAVAILABLE_003", (await SendAsync(server, HttpMethod.Post, $"/api/notifications/{tenantDraft.GetProperty("id").GetString()}:publish", "{}", tenant)).Body.GetProperty("code").GetString());
        Assert.Equal(0, (await SendAsync(server, HttpMethod.Post, "/api/bot-configs:list", "{}", tenant)).Body.GetProperty("total").GetInt32());
        Assert.Equal("NOTIFICATION_NOTFOUND_008", (await SendAsync(server, HttpMethod.Get, $"/api/bot-configs/{dingId}", null, tenant)).Body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Transports_SignAndShapeTheirRequests()
    {
        var clock = new ManualClock(DateTimeOffset.FromUnixTimeMilliseconds(1_700_000_000_123));

        // 钉钉：地址后加毫秒时间戳与「密钥对 时间戳\n密钥 的 HMAC-SHA256」签名，关键词放在第一行
        var dingHandler = new RecordingHandler("""{"errcode":0,"errmsg":"ok"}""");
        var ding = await new DingTalkBotTransport(new Factory(dingHandler), clock).SendAsync(new BotEndpoint(BotChannel.DingTalk, "https://oapi.dingtalk.com/robot/send?access_token=t", "SEC1", "XiHan"), "hello", CancellationToken.None);
        Assert.True(ding.Succeeded);
        var (dingUri, dingBody) = Assert.Single(dingHandler.Requests);
        var query = HttpUtility.ParseQueryString(dingUri.Query);
        var expectedSign = Convert.ToBase64String(System.Security.Cryptography.HMACSHA256.HashData("SEC1"u8.ToArray(), System.Text.Encoding.UTF8.GetBytes("1700000000123\nSEC1")));
        Assert.Equal(("t", "1700000000123", expectedSign), (query["access_token"], query["timestamp"], query["sign"]));
        Assert.Equal("""{"msgtype":"text","text":{"content":"XiHan\nhello"}}""", dingBody);

        // 飞书：签名在载荷里（秒级时间戳；以 时间戳\n密钥 为密钥对空串做 HMAC-SHA256）；返回 code 非 0 即失败
        var larkHandler = new RecordingHandler("""{"code":19021,"msg":"sign match fail"}""");
        var lark = await new LarkBotTransport(new Factory(larkHandler), clock).SendAsync(new BotEndpoint(BotChannel.Lark, "https://open.feishu.cn/open-apis/bot/v2/hook/h", "SEC2", null), "hello", CancellationToken.None);
        Assert.Equal((false, "19021: sign match fail"), (lark.Succeeded, lark.Error));
        using var larkBody = JsonDocument.Parse(Assert.Single(larkHandler.Requests).Body);
        var larkSign = Convert.ToBase64String(System.Security.Cryptography.HMACSHA256.HashData(System.Text.Encoding.UTF8.GetBytes("1700000000\nSEC2"), Array.Empty<byte>()));
        Assert.Equal(("text", "hello", "1700000000", larkSign), (larkBody.RootElement.GetProperty("msg_type").GetString(), larkBody.RootElement.GetProperty("content").GetProperty("text").GetString(), larkBody.RootElement.GetProperty("timestamp").GetString(), larkBody.RootElement.GetProperty("sign").GetString()));

        // 企业微信：地址原样、没有签名；errcode 非 0 即失败
        var wecomHandler = new RecordingHandler("""{"errcode":93000,"errmsg":"invalid webhook url"}""");
        var wecom = await new WeComBotTransport(new Factory(wecomHandler)).SendAsync(new BotEndpoint(BotChannel.WeCom, "https://qyapi.weixin.qq.com/cgi-bin/webhook/send?key=k", null, null), "hello", CancellationToken.None);
        Assert.Equal((false, "93000: invalid webhook url"), (wecom.Succeeded, wecom.Error));
        Assert.Equal(("https://qyapi.weixin.qq.com/cgi-bin/webhook/send?key=k", """{"msgtype":"text","text":{"content":"hello"}}"""), (Assert.Single(wecomHandler.Requests).Uri.ToString(), wecomHandler.Requests[0].Body));
    }

    private sealed class FakeBotTransport(BotChannel channel, ConcurrentQueue<(BotEndpoint Endpoint, string Text)> sent) : IBotTransport
    {
        public BotChannel Channel => channel;

        public Task<TransportResult> SendAsync(BotEndpoint endpoint, string text, CancellationToken cancellationToken)
        {
            sent.Enqueue((endpoint, text));
            return Task.FromResult(TransportResult.Sent(null));
        }
    }

    private sealed class RecordingHandler(string response) : HttpMessageHandler
    {
        public List<(Uri Uri, string Body)> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add((request.RequestUri!, await request.Content!.ReadAsStringAsync(cancellationToken)));
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(response) };
        }
    }

    private sealed class Factory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }
}
