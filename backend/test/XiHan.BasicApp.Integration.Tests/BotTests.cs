// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.BasicApp.Notification.Domain.Entities;
using XiHan.BasicApp.Notification.Domain.Services;
using XiHan.BasicApp.Notification.Infrastructure;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.BasicApp.Runtime.Security;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证群机器人的配置约束、通知投递与各通道载荷
/// </summary>
/// <remarks>
/// Webhook 只认各服务商的官方 https 地址；每个服务商一个缺省，缺省的不能删、停用或换服务商；签名密钥不回显；
/// Webhook 地址加密存储，投递时解开，解不开就不发、不当明文用，别的一侧读不到也用不了；
/// 通知的机器人渠道发到本侧每个服务商的缺省机器人（经发件箱，由发送循环发出），租户没有自己的机器人即不可用（不借平台的）；
/// Telegram 机器人只在平台、令牌不回显，广播到每个启用的机器人的每个广播群；三家群机器人通道的地址、签名与载荷（Telegram 的出站在 TelegramTests）
/// </remarks>
public sealed class BotTests : IDisposable
{
    private const string TelegramToken = "123456789:AAEexampleTOKEN_for-tests-only0";
    private const string WebhookToken = "plain-webhook-token";
    private const string Webhook = "https://oapi.dingtalk.com/robot/send?access_token=" + WebhookToken;
    private const string MaskedWebhook = "https://oapi.dingtalk.com/***";

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

        // 每个服务商的第一个即缺省；密钥与地址里的令牌不回显；缺省的不能删、停用或换服务商
        var ding = await CreatedAsync(server, "/api/bot-configs", new { code = "ding", name = "Ding", provider = "DingTalk", webhookUrl = "https://oapi.dingtalk.com/robot/send?access_token=one", secret = "SEC-secret-1", keyword = "XiHan" }, platform);
        Assert.True(ding.GetProperty("isDefault").GetBoolean());
        Assert.True(ding.GetProperty("hasSecret").GetBoolean());
        Assert.DoesNotContain("SEC-secret-1", ding.ToString(), StringComparison.Ordinal);
        Assert.Equal("https://oapi.dingtalk.com/***", ding.GetProperty("webhookUrl").GetString());
        var dingId = ding.GetProperty("id").GetString()!;
        var ding2 = await CreatedAsync(server, "/api/bot-configs", new { code = "ding2", name = "Ding 2", provider = "DingTalk", webhookUrl = "https://oapi.dingtalk.com/robot/send?access_token=two" }, platform);
        Assert.False(ding2.GetProperty("isDefault").GetBoolean());
        var wecom = await CreatedAsync(server, "/api/bot-configs", new { code = "wecom", name = "WeCom", provider = "WeCom", webhookUrl = "https://qyapi.weixin.qq.com/cgi-bin/webhook/send?key=k1" }, platform);
        Assert.True(wecom.GetProperty("isDefault").GetBoolean());
        Assert.Equal("NOTIFICATION_CONFLICT_005", (await SendAsync(server, HttpMethod.Delete, $"/api/bot-configs?ids={dingId}", null, platform)).Body.GetProperty("failed")[0].GetProperty("code").GetString());
        Assert.Equal("NOTIFICATION_CONFLICT_005", (await SendAsync(server, HttpMethod.Put, $"/api/bot-configs/{dingId}", new { name = "Ding", provider = "DingTalk", webhookUrl = "https://oapi.dingtalk.com/robot/send?access_token=one", isEnabled = false }, platform)).Body.GetProperty("code").GetString());
        Assert.Equal("NOTIFICATION_CONFLICT_005", (await SendAsync(server, HttpMethod.Put, $"/api/bot-configs/{dingId}", new { name = "Ding", provider = "Lark", webhookUrl = "https://open.feishu.cn/open-apis/bot/v2/hook/x" }, platform)).Body.GetProperty("code").GetString());

        // 交回遮过的地址即保留原地址（后面的发送用的还是它）
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Put, $"/api/bot-configs/{dingId}", new { name = "Ding", provider = "DingTalk", webhookUrl = "https://oapi.dingtalk.com/***", keyword = "XiHan" }, platform)).Status);

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
    public async Task WebhookUrl_IsEncryptedAtRest_AndDeliveredDecrypted()
    {
        var (server, platform, _) = await StartWithFakeTransportsAsync();
        await using var running = server;
        var created = await CreatedAsync(server, "/api/bot-configs", new { code = "ding", name = "Ding", provider = "DingTalk", webhookUrl = Webhook }, platform);
        var id = created.GetProperty("id").GetString()!;

        // 库里是密文，换个用途解不开；响应照旧只给主机
        var stored = await StoredWebhookAsync(server, id);
        Assert.StartsWith("dp:", stored, StringComparison.Ordinal);
        Assert.DoesNotContain(WebhookToken, stored, StringComparison.Ordinal);
        var secrets = server.Services.GetRequiredService<ISecretProtector>();
        Assert.True(secrets.TryUnprotect(BotConfigService.WebhookPurpose, stored, out var opened));
        Assert.Equal(Webhook, opened);
        Assert.False(secrets.TryUnprotect(BotConfigService.SecretPurpose, stored, out _));
        Assert.Equal(MaskedWebhook, created.GetProperty("webhookUrl").GetString());
        Assert.Equal(MaskedWebhook, (await SendAsync(server, HttpMethod.Get, $"/api/bot-configs/{id}", null, platform)).Body.GetProperty("webhookUrl").GetString());
        Assert.Equal(MaskedWebhook, (await SendAsync(server, HttpMethod.Post, "/api/bot-configs:list", "{}", platform)).Body.GetProperty("items")[0].GetProperty("webhookUrl").GetString());

        // 交回遮过的值或原地址都不改库里的值（差异日志没有变化）
        foreach (var same in new[] { MaskedWebhook, Webhook })
        {
            Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Put, $"/api/bot-configs/{id}", new { name = "Ding", provider = "DingTalk", webhookUrl = same }, platform)).Status);
            Assert.Equal(stored, await StoredWebhookAsync(server, id));
        }

        // 遮过的值不是地址：新建不接受，主机对不上也不算保留；错误里不带地址里的令牌（错误参数会进操作日志）
        Assert.Equal("NOTIFICATION_VALIDATION_011", (await SendAsync(server, HttpMethod.Post, "/api/bot-configs", new { code = "masked", name = "Masked", provider = "DingTalk", webhookUrl = MaskedWebhook }, platform)).Body.GetProperty("code").GetString());
        Assert.Equal("NOTIFICATION_VALIDATION_011", (await SendAsync(server, HttpMethod.Put, $"/api/bot-configs/{id}", new { name = "Ding", provider = "DingTalk", webhookUrl = "https://open.feishu.cn/***" }, platform)).Body.GetProperty("code").GetString());
        var refused = (await SendAsync(server, HttpMethod.Post, "/api/bot-configs", new { code = "wrong", name = "Wrong", provider = "Lark", webhookUrl = Webhook }, platform)).Body;
        Assert.Equal("NOTIFICATION_VALIDATION_011", refused.GetProperty("code").GetString());
        Assert.DoesNotContain(WebhookToken, refused.ToString(), StringComparison.Ordinal);
        Assert.Equal(stored, await StoredWebhookAsync(server, id));

        // 发送用解开的地址
        await PublishAsync(server, "First", platform);
        Assert.Equal(Webhook, (await WaitSentAsync("First")).WebhookUrl);

        // 换地址即重新加密，之后按新地址发
        const string next = "https://oapi.dingtalk.com/robot/send?access_token=next-token";
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Put, $"/api/bot-configs/{id}", new { name = "Ding", provider = "DingTalk", webhookUrl = next }, platform)).Status);
        var replaced = await StoredWebhookAsync(server, id);
        Assert.StartsWith("dp:", replaced, StringComparison.Ordinal);
        Assert.NotEqual(stored, replaced);
        Assert.DoesNotContain("next-token", replaced, StringComparison.Ordinal);
        await PublishAsync(server, "Second", platform);
        Assert.Equal(next, (await WaitSentAsync("Second")).WebhookUrl);

        // 请求限 500 字符，全是三字节字符的地址加密后也放得下
        var longest = "https://oapi.dingtalk.com/" + new string('中', 500 - 26);
        var column = typeof(SysBotConfig).GetProperty(nameof(SysBotConfig.WebhookUrl))!.GetCustomAttribute<StringLengthAttribute>()!.MaximumLength;
        Assert.InRange(secrets.Protect(BotConfigService.WebhookPurpose, longest).Length, 501, column);
    }

    [Theory]
    [InlineData(Webhook)]
    [InlineData("dp:not-a-ciphertext")]
    public async Task WebhookUrl_ThatCannotBeDecrypted_IsReported_AndNeverUsed(string column)
    {
        var (server, platform, _) = await StartWithFakeTransportsAsync();
        await using var running = server;
        var id = (await CreatedAsync(server, "/api/bot-configs", new { code = "ding", name = "Ding", provider = "DingTalk", webhookUrl = Webhook }, platform)).GetProperty("id").GetString()!;
        var key = Guid.Parse(id);
        var overwritten = await StoreScope.WriteAsync(server.Services.GetRequiredService<IServiceScopeFactory>(), async (services, ct) =>
        {
            var unitOfWork = services.GetRequiredService<SqlSugarUnitOfWork>();
            await unitOfWork.EnsureTransactionAsync(ct);
            await unitOfWork.ConnectionFor(typeof(SysBotConfig)).Updateable<SysBotConfig>().SetColumns(c => new SysBotConfig { WebhookUrl = column }).Where(c => c.Id == key).ExecuteCommandAsync(ct);
            return Result.Success();
        }, CancellationToken.None);
        Assert.True(overwritten.IsSuccess, overwritten.Error?.Code);

        // 列表与详情照常，地址只给 ***，这一行还能改
        Assert.Equal("***", (await SendAsync(server, HttpMethod.Get, $"/api/bot-configs/{id}", null, platform)).Body.GetProperty("webhookUrl").GetString());
        Assert.Equal("***", (await SendAsync(server, HttpMethod.Post, "/api/bot-configs:list", "{}", platform)).Body.GetProperty("items")[0].GetProperty("webhookUrl").GetString());

        // 发送不用库里的值，发送记录写明原因
        await PublishAsync(server, "Blocked", platform);
        var error = await WaitBotErrorAsync(server, static row => row.Content == "Blocked");
        Assert.Contains("ding", error, StringComparison.Ordinal);
        Assert.DoesNotContain(WebhookToken, error, StringComparison.Ordinal);
        Assert.Empty(_sent);

        // 交回遮过的值保留不了，明确报错
        foreach (var masked in new[] { MaskedWebhook, "***" })
        {
            var (status, body) = await SendAsync(server, HttpMethod.Put, $"/api/bot-configs/{id}", new { name = "Ding", provider = "DingTalk", webhookUrl = masked }, platform);
            Assert.Equal((HttpStatusCode.ServiceUnavailable, "NOTIFICATION_UNAVAILABLE_004"), (status, body.GetProperty("code").GetString()));
        }

        // 重新填完整地址即恢复
        Assert.Equal(MaskedWebhook, (await SendAsync(server, HttpMethod.Put, $"/api/bot-configs/{id}", new { name = "Ding", provider = "DingTalk", webhookUrl = Webhook }, platform)).Body.GetProperty("webhookUrl").GetString());
        Assert.StartsWith("dp:", await StoredWebhookAsync(server, id), StringComparison.Ordinal);
        await PublishAsync(server, "Fixed", platform);
        Assert.Equal(Webhook, (await WaitSentAsync("Fixed")).WebhookUrl);
    }

    [Fact]
    public async Task WebhookUrl_OfAnotherSide_CannotBeReadOrUsed()
    {
        var (server, platform, contoso) = await StartWithFakeTransportsAsync();
        await using var running = server;
        var fabrikam = await OpenTenantAsync(server, platform, "fabrikam");
        var id = (await CreatedAsync(server, "/api/bot-configs", new { code = "ding", name = "Ding", provider = "DingTalk", webhookUrl = Webhook }, contoso)).GetProperty("id").GetString()!;

        // 别的租户与平台：读、改、设缺省、删都当不存在，列表里没有
        foreach (var other in new[] { fabrikam, platform })
        {
            Assert.Equal("NOTIFICATION_NOTFOUND_008", (await SendAsync(server, HttpMethod.Get, $"/api/bot-configs/{id}", null, other)).Body.GetProperty("code").GetString());
            Assert.Equal("NOTIFICATION_NOTFOUND_008", (await SendAsync(server, HttpMethod.Put, $"/api/bot-configs/{id}", new { name = "Mine", provider = "DingTalk", webhookUrl = MaskedWebhook }, other)).Body.GetProperty("code").GetString());
            Assert.Equal("NOTIFICATION_NOTFOUND_008", (await SendAsync(server, HttpMethod.Post, $"/api/bot-configs/{id}:set-default", "{}", other)).Body.GetProperty("code").GetString());
            Assert.Equal("NOTIFICATION_NOTFOUND_008", (await SendAsync(server, HttpMethod.Delete, $"/api/bot-configs?ids={id}", null, other)).Body.GetProperty("failed")[0].GetProperty("code").GetString());
            Assert.Equal(0, (await SendAsync(server, HttpMethod.Post, "/api/bot-configs:list", "{}", other)).Body.GetProperty("total").GetInt32());
        }

        // 所属租户发得出去；别的租户没有自己的机器人，渠道不可用
        await PublishAsync(server, "Own", contoso);
        Assert.Equal(Webhook, (await WaitSentAsync("Own")).WebhookUrl);
        var draft = await CreatedAsync(server, "/api/notifications", new { title = "Other", deliveryChannels = _withBot }, fabrikam);
        Assert.Equal("NOTIFICATION_UNAVAILABLE_003", (await SendAsync(server, HttpMethod.Post, $"/api/notifications/{draft.GetProperty("id").GetString()}:publish", "{}", fabrikam)).Body.GetProperty("code").GetString());

        // 就算发件箱里有一条别的租户的消息指向这个配置，也解不到它的地址
        var key = Guid.Parse(id);
        var forged = await StoreScope.WriteAsync(server.Services.GetRequiredService<IServiceScopeFactory>(), async (services, ct) =>
            await services.GetRequiredService<IRepository<SysBotMessage, Guid>>().AddAsync(new SysBotMessage { Id = Guid.NewGuid(), OwnerTenantId = "fabrikam", Channel = BotChannel.DingTalk, ConfigId = key, Content = "Forged", MaxAttempts = 1 }, ct), CancellationToken.None);
        Assert.True(forged.IsSuccess, forged.Error?.Code);
        Assert.Equal("The bot configuration was disabled or deleted.", await WaitBotErrorAsync(server, static row => row.Content == "Forged"));
        Assert.DoesNotContain(_sent, static s => s.Text == "Forged");
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

    private Task<(BasicAppTestServer Server, string Platform, string Tenant)> StartWithFakeTransportsAsync() => SettingsTestHost.StartAsync(_database.ConnectionString, configure: builder =>
    {
        builder.Configuration.AddInMemoryCollection([new("BasicApp:Notification:DispatchInterval", "00:00:00.100")]);
        foreach (var channel in Enum.GetValues<BotChannel>())
        {
            builder.Services.AddSingleton<IBotTransport>(new FakeBotTransport(channel, _sent));
        }
    });

    private static async Task<string> OpenTenantAsync(BasicAppTestServer server, string platform, string tenantId)
    {
        var enterprise = await EditionsTests.EditionIdAsync(server, platform, "enterprise");
        var tenant = await CreatedAsync(server, "/api/tenants", new { tenantId, name = tenantId, editionId = enterprise }, platform);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, $"/api/tenants/{tenant.GetProperty("id").GetString()}:initialize-admin", new { userName = "admin", password = "Admin@123456" }, platform)).Status);
        return (await AuthFlowTests.LoginAsync(server, "admin", "Admin@123456", tenantId)).GetProperty("accessToken").GetString()!;
    }

    private static Task<string> StoredWebhookAsync(BasicAppTestServer server, string id)
        => SettingsTestHost.InvokeAsync(server, null, async ctx => (await ctx.Services.GetRequiredService<IRepository<SysBotConfig, Guid>>().FindAsync(Guid.Parse(id), ctx.CancellationToken)).ValueOrThrow().WebhookUrl);

    private static async Task PublishAsync(BasicAppTestServer server, string title, string token)
    {
        var notification = await CreatedAsync(server, "/api/notifications", new { title, deliveryChannels = _withBot }, token);
        var (status, body) = await SendAsync(server, HttpMethod.Post, $"/api/notifications/{notification.GetProperty("id").GetString()}:publish", "{}", token);
        Assert.True(status == HttpStatusCode.OK, body.ToString());
    }

    private async Task<BotEndpoint> WaitSentAsync(string text)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            if (_sent.FirstOrDefault(s => s.Text == text) is { Endpoint: { } endpoint })
            {
                return endpoint;
            }

            await Task.Delay(100);
        }

        throw new TimeoutException($"Nothing was sent for '{text}'.");
    }

    private static async Task<string> WaitBotErrorAsync(BasicAppTestServer server, Func<SysBotMessage, bool> match)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            var rows = await SettingsTestHost.InvokeAsync(server, null, async ctx => await ctx.Services.GetRequiredService<IRepository<SysBotMessage, Guid>>().ListAsync(EntityQuery.All<SysBotMessage>(), ctx.CancellationToken));
            if (rows.FirstOrDefault(match) is { ErrorMessage: { } error })
            {
                return error;
            }

            await Task.Delay(100);
        }

        throw new TimeoutException("The bot message was not attempted.");
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
