// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.BasicApp.Notification.Domain.Entities;
using XiHan.BasicApp.Notification.Infrastructure.Telegram;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// Telegram 机器人的交互（4.x 行为）：平台参数打开后 Webhook 注册（带密钥与订阅种类）、<c>getMe</c> 回填用户名、同步命令菜单；Webhook 的密钥校验与 200；
/// 内置 <c>/start</c> <c>/help</c> <c>/myid</c>、去重、群组白名单（fail-closed，常驻命令例外）、命令白名单、只给管理员的命令与回调、回调总会应答、内联查询、会话状态、兜底回复；
/// 出站重试与审计、最终失败告警管理员；Telegram 广播走同一出站；关掉总开关删 Webhook；长轮询。
/// </summary>
public sealed class TelegramTests : IDisposable
{
    private const string Token = "123456789:AAEexampleTOKEN_for-tests-only0";
    private const string Secret = "S3CR3T-token";

    private static readonly string[] _withBot = ["InApp", "Bot"];

    private readonly SqliteDatabase _database = new();
    private readonly FakeBotApi _api = new();

    public void Dispose() => _database.Dispose();

    private static async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(BasicAppTestServer server, HttpMethod method, string path, object body, string token)
    {
        var (response, payload) = await server.SendAsync(BasicAppTestServer.Request(method, path, body as string ?? JsonSerializer.Serialize(body), token));
        return (response.StatusCode, payload);
    }

    private static async Task<HttpStatusCode> WebhookAsync(BasicAppTestServer server, string bot, string body, string? secret = Secret)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/telegram-bot/webhook/{bot}") { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        if (secret is not null)
        {
            request.Headers.Add(TelegramWebhookEndpoints.SecretHeader, secret);
        }

        using var response = await server.Client.SendAsync(request);
        return response.StatusCode;
    }

    private static string Message(long updateId, long chatId, long userId, string text, string chatType = "private", long messageId = 10)
        => JsonSerializer.Serialize(new { update_id = updateId, message = new { message_id = messageId, chat = new { id = chatId, type = chatType }, from = new { id = userId }, text } });

    private async Task<List<JsonObject>> DeliverAsync(BasicAppTestServer server, string body)
    {
        var before = _api.Calls.Count;
        Assert.Equal(HttpStatusCode.OK, await WebhookAsync(server, "ops", body));
        await server.Services.GetRequiredService<TelegramBotManager>().DrainAsync();
        return _api.Calls.Skip(before).Select(static c => c.Payload).ToList();
    }

    private static List<string?> Texts(IEnumerable<JsonObject> calls) => calls.Where(static c => c.ContainsKey("text")).Select(static c => c["text"]?.GetValue<string>()).ToList();

    // 长轮询在后台线程里记调用：读的时候与记录同一把锁
    private bool Polled()
    {
        lock (_api.Calls)
        {
            return _api.Calls.Any(static c => c.Method == "getUpdates" && c.Payload["allowed_updates"] is JsonArray);
        }
    }

    private bool SentTo(long chatId)
    {
        lock (_api.Calls)
        {
            return _api.Calls.Any(c => c.Method == "sendMessage" && c.Payload["chat_id"]!.GetValue<long>() == chatId);
        }
    }

    [Fact]
    public async Task Bots_AnswerThroughTheWebhook_WithFourXGuardsAndRouting()
    {
        var (server, platform, _) = await SettingsTestHost.StartAsync(_database.ConnectionString, configure: builder =>
        {
            builder.Configuration.AddInMemoryCollection(
            [
                new("BasicApp:Notification:Telegram:Retry:BaseDelayMs", "1"),
                new("BasicApp:Notification:Telegram:Retry:MaxDelayMs", "2"),
                new("BasicApp:Notification:DispatchInterval", "00:00:00.100"),
            ]);
            builder.Services.AddSingleton<ITelegramHttp>(_api);
            builder.Services.AddScoped<ITelegramCommandHandler, SecretCommand>();
            builder.Services.AddScoped<ITelegramCommandHandler, AskCommand>();
            builder.Services.AddScoped<ITelegramCallbackHandler, ApproveCallback>();
            builder.Services.AddScoped<ITelegramInlineQueryHandler, EchoInline>();
            builder.Services.AddScoped<ITelegramStateHandler, NameState>();
        });
        await using var running = server;
        var manager = server.Services.GetRequiredService<TelegramBotManager>();

        // 平台参数与机器人
        var (settingsStatus, settingsBody) = await SendAsync(server, HttpMethod.Post, "/api/configs", new { key = TelegramSettings.ConfigKey, name = "Telegram", dataType = "Json", value = """{"enabled":true,"webhookBaseUrl":"https://bot.example.com/","managerRefreshSeconds":0}""" }, platform);
        Assert.True(settingsStatus == HttpStatusCode.Created, settingsBody.ToString());
        Assert.Equal(HttpStatusCode.Created, (await SendAsync(server, HttpMethod.Post, "/api/configs", new { key = TelegramSettings.SecretConfigKey, name = "Telegram secret", value = Secret, isEncrypted = true }, platform)).Status);
        var (botStatus, bot) = await SendAsync(server, HttpMethod.Post, "/api/telegram-bots", new { name = "ops", token = Token, adminUsers = "1001", allowedGroupChatIds = "-100500", enableFallbackReply = true, broadcastChatIds = "-100777" }, platform);
        Assert.True(botStatus == HttpStatusCode.Created, bot.ToString());
        var botId = bot.GetProperty("id").GetString()!;
        Assert.True(await manager.RefreshAsync(CancellationToken.None));

        // 启动：getMe、setWebhook（地址、密钥、订阅种类）、两个作用域的命令菜单（按登记顺序、不含只给管理员的，短说明补齐；测试的处理器先于内置的登记）
        Assert.Equal(["getMe", "setWebhook", "setMyCommands", "setMyCommands"], _api.Calls.Select(static c => c.Method).ToList());
        var webhook = _api.Calls[1].Payload;
        Assert.Equal(("https://bot.example.com/api/telegram-bot/webhook/ops", Secret), (webhook["url"]!.GetValue<string>(), webhook["secret_token"]!.GetValue<string>()));
        Assert.Equal(TelegramUpdate.AllowedUpdates, webhook["allowed_updates"]!.AsArray().Select(static n => n!.GetValue<string>()).ToList());
        var menu = _api.Calls[2].Payload["commands"]!.AsArray().Select(static c => c!["command"]!.GetValue<string>()).ToList();
        Assert.Equal(["ask", "start", "help", "myid"], menu);
        Assert.Equal("问 / /ask", _api.Calls[2].Payload["commands"]![0]!["description"]!.GetValue<string>());
        Assert.Equal(["ask", "start", "h", "id"], _api.Calls[3].Payload["commands"]!.AsArray().Select(static c => c!["command"]!.GetValue<string>()).ToList());
        Assert.Equal("all_group_chats", _api.Calls[3].Payload["scope"]!["type"]!.GetValue<string>());

        // Webhook：没带或带错密钥 401；JSON 坏、机器人不存在一律 200 且不处理
        Assert.Equal(HttpStatusCode.Unauthorized, await WebhookAsync(server, "ops", Message(1, 1001, 1001, "/start"), secret: null));
        Assert.Equal(HttpStatusCode.Unauthorized, await WebhookAsync(server, "ops", Message(1, 1001, 1001, "/start"), secret: "s3cr3t-token"));
        Assert.Equal(HttpStatusCode.OK, await WebhookAsync(server, "ops", "{not json"));
        Assert.Equal(HttpStatusCode.OK, await WebhookAsync(server, "nobody", Message(1, 1001, 1001, "/start")));
        await manager.DrainAsync();
        Assert.Equal(4, _api.Calls.Count);

        // /start：欢迎语用 getMe 的用户名，引用触发消息；同一个 update 只处理一次
        var start = Assert.Single(await DeliverAsync(server, Message(1, 1001, 1001, "/start@ops_bot")));
        Assert.Equal(("你好，欢迎使用 ops_bot！发送 /help 查看可用命令。", 10), (start["text"]!.GetValue<string>(), start["reply_parameters"]!["message_id"]!.GetValue<int>()));
        Assert.Empty(await DeliverAsync(server, Message(1, 1001, 1001, "/start")));

        // /help：非管理员不列只给管理员的；/myid；发给别的机器人的命令不处理
        Assert.Equal([$"可用命令：{Environment.NewLine}/ask - 问{Environment.NewLine}/start - 开始使用机器人{Environment.NewLine}/help - 查看帮助{Environment.NewLine}/myid - 查看我的 Telegram Id"], Texts(await DeliverAsync(server, Message(2, 2002, 2002, "/h"))));
        Assert.Contains("/secret - 管理员", Texts(await DeliverAsync(server, Message(3, 1001, 1001, "/help")))[0], StringComparison.Ordinal);
        Assert.Equal(["ChatId：-100500\nUserId：2002"], Texts(await DeliverAsync(server, Message(4, -100500, 2002, "/id", "supergroup"))));
        Assert.Empty(await DeliverAsync(server, Message(5, 2002, 2002, "/myid@other_bot")));

        // 群组白名单：不在白名单的群静默丢弃，常驻命令例外；白名单里的群认不出的消息兜底回复
        Assert.Empty(await DeliverAsync(server, Message(6, -100999, 2002, "hello", "group")));
        Assert.Single(await DeliverAsync(server, Message(7, -100999, 2002, "/help", "group")));
        Assert.Equal(["暂不支持处理该消息，发送 /help 查看可用命令。"], Texts(await DeliverAsync(server, Message(8, -100500, 2002, "hello", "supergroup"))));

        // 只给管理员的命令
        Assert.Equal(["该命令仅管理员可用。"], Texts(await DeliverAsync(server, Message(9, 2002, 2002, "/secret"))));
        Assert.Equal(["secret ok"], Texts(await DeliverAsync(server, Message(10, 1001, 1001, "/secret"))));

        // 回调：只给管理员的以消息提示且应答不带文字；管理员的带处理器定制的应答；没登记的 action 也应答
        static string Callback(long updateId, long userId, string data) => JsonSerializer.Serialize(new { update_id = updateId, callback_query = new { id = $"cb{updateId}", from = new { id = userId }, message = new { message_id = 20, chat = new { id = userId, type = "private" } }, data } });
        var denied = await DeliverAsync(server, Callback(11, 2002, "approve:42"));
        Assert.Equal(["该操作仅管理员可用。"], Texts(denied));
        Assert.Equal("cb11", _api.Calls.Last().Payload["callback_query_id"]!.GetValue<string>());
        Assert.False(_api.Calls.Last().Payload.ContainsKey("text"));
        await DeliverAsync(server, Callback(12, 1001, "APPROVE:42"));
        Assert.Equal(("answerCallbackQuery", "approved APPROVE:42"), (_api.Calls.Last().Method, _api.Calls.Last().Payload["text"]!.GetValue<string>()));
        Assert.Single(await DeliverAsync(server, Callback(13, 2002, "nope:1")));
        Assert.Equal("answerCallbackQuery", _api.Calls.Last().Method);

        // 内联查询：去空格后交给处理器，缓存 300 秒
        await DeliverAsync(server, JsonSerializer.Serialize(new { update_id = 14, inline_query = new { id = "iq1", from = new { id = 2002 }, query = "  hi " } }));
        var inline = _api.Calls.Last();
        Assert.Equal(("answerInlineQuery", "iq1", 300, "echo: hi"), (inline.Method, inline.Payload["inline_query_id"]!.GetValue<string>(), inline.Payload["cache_time"]!.GetValue<int>(), inline.Payload["results"]![0]!["title"]!.GetValue<string>()));

        // 会话状态：/ask 设状态，下一条普通消息交给状态处理器并清掉，再下一条回到兜底
        Assert.Equal(["What is your name?"], Texts(await DeliverAsync(server, Message(15, 2002, 2002, "/ask"))));
        Assert.Equal(["Hello Alice"], Texts(await DeliverAsync(server, Message(16, 2002, 2002, "Alice"))));
        Assert.Equal(["暂不支持处理该消息，发送 /help 查看可用命令。"], Texts(await DeliverAsync(server, Message(17, 2002, 2002, "Bob"))));

        // 命令白名单：不在白名单的命令回「未开启」，列表与菜单随之过滤
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Put, $"/api/telegram-bots/{botId}", new { name = "ops", adminUsers = "1001", allowedGroupChatIds = "-100500", allowedCommands = "start,help", enableFallbackReply = true, broadcastChatIds = "-100777" }, platform)).Status);
        Assert.True(await manager.RefreshAsync(CancellationToken.None));
        Assert.Equal(["该命令未开启。"], Texts(await DeliverAsync(server, Message(18, 2002, 2002, "/myid"))));
        Assert.Equal([$"可用命令：{Environment.NewLine}/start - 开始使用机器人{Environment.NewLine}/help - 查看帮助"], Texts(await DeliverAsync(server, Message(19, 2002, 2002, "/help"))));

        // 出站：5xx 与 429 重试后成功记一条审计；400 不重试、最终失败记一条并私发管理员告警
        var ops = server.Services.GetRequiredService<TelegramRuntime>().Find("ops")!;
        var notifier = server.Services.GetRequiredService<TelegramNotifier>();
        _api.Fail("sendMessage", 500, 1);
        _api.Fail("sendMessage", 429, 1);
        Assert.True((await notifier.SendTextAsync(ops, 5005, "retried")).Ok);
        _api.Fail("sendMessage", 400, 1);
        Assert.False((await notifier.SendTextAsync(ops, 6006, "refused")).Ok);
        Assert.Contains(_api.Calls, static c => c.Method == "sendMessage" && c.Payload["chat_id"]!.GetValue<long>() == 1001 && c.Payload["text"]!.GetValue<string>().StartsWith("消息发送失败告警\nBot: ops\nChatId: 6006\nApi: sendMessage\nError: ", StringComparison.Ordinal));
        var audits = await SettingsTestHost.InvokeAsync(server, null, async ctx => await ctx.Services.GetRequiredService<IRepository<SysTelegramMessage, Guid>>().ListAsync(EntityQuery.All<SysTelegramMessage>(), ctx.CancellationToken));
        var retried = Assert.Single(audits, static a => a.ChatId == 5005);
        Assert.Equal((true, "sendMessage", "text", "None", "retried"), (retried.Success, retried.ApiMethod, retried.MessageType, retried.ParseMode, retried.Content));
        var refused = Assert.Single(audits, static a => a.ChatId == 6006);
        Assert.Equal((false, 400), (refused.Success, refused.ErrorCode));
        Assert.Equal(4, _api.Calls.Count(static c => c.Method == "sendMessage" && c.Payload["chat_id"]!.GetValue<long>() is 5005 or 6006));

        // 广播：平台按机器人渠道发布的通知经同一出站发到广播群
        var notification = (await SendAsync(server, HttpMethod.Post, "/api/notifications", new { title = "Release", content = "v6", deliveryChannels = _withBot }, platform)).Body.GetProperty("id").GetString()!;
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, $"/api/notifications/{notification}:publish", "{}", platform)).Status);
        for (var attempt = 0; attempt < 100 && !_api.Calls.Any(static c => c.Method == "sendMessage" && c.Payload["chat_id"]!.GetValue<long>() == -100777); attempt++)
        {
            await Task.Delay(100);
        }

        Assert.Equal("Release\n\nv6", _api.Calls.Single(static c => c.Method == "sendMessage" && c.Payload["chat_id"]!.GetValue<long>() == -100777).Payload["text"]!.GetValue<string>());

        // 关掉总开关：删 Webhook，之后收到的 Update 不处理
        var configs = (await SendAsync(server, HttpMethod.Post, "/api/configs:list", """{"page":{"index":1,"size":50}}""", platform)).Body.GetProperty("items").EnumerateArray().ToList();
        var settingsId = configs.Single(static c => c.GetProperty("key").GetString() == TelegramSettings.ConfigKey).GetProperty("id").GetString()!;
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Put, $"/api/configs/{settingsId}", new { name = "Telegram", dataType = "Json", value = """{"enabled":false,"webhookBaseUrl":"https://bot.example.com"}""" }, platform)).Status);
        Assert.True(await manager.RefreshAsync(CancellationToken.None));
        Assert.Equal("deleteWebhook", _api.Calls.Last().Method);
        Assert.Empty(await DeliverAsync(server, Message(20, 1001, 1001, "/start")));

        // 长轮询：没有 Webhook 根地址即先删 Webhook 再拉 Update
        _api.Enqueue(JsonNode.Parse(Message(21, 4004, 4004, "/start"))!.AsObject());
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Put, $"/api/configs/{settingsId}", new { name = "Telegram", dataType = "Json", value = """{"enabled":true}""" }, platform)).Status);
        Assert.True(await manager.RefreshAsync(CancellationToken.None));
        // 假的 Bot API 在一次空轮询之后才记下 getUpdates：两样都等到再判（Release 下回复比第二次轮询快）
        for (var attempt = 0; attempt < 100 && !(Polled() && SentTo(4004)); attempt++)
        {
            await Task.Delay(100);
        }

        Assert.True(SentTo(4004));
        Assert.True(Polled());
    }

    /// <summary>
    /// 假的 Bot API：记下每次调用，按方法回应；可排失败与长轮询的 Update。
    /// </summary>
    private sealed class FakeBotApi : HttpMessageHandler, ITelegramHttp
    {
        private readonly ConcurrentQueue<(string Method, int Code)> _failures = new();
        private readonly ConcurrentQueue<JsonObject> _updates = new();
        private long _messageId = 100;

        public List<(string Method, JsonObject Payload)> Calls { get; } = [];

        public HttpClient ClientFor(TelegramNetwork network) => new(this, disposeHandler: false);

        public void Fail(string method, int code, int count)
        {
            for (var i = 0; i < count; i++)
            {
                _failures.Enqueue((method, code));
            }
        }

        public void Enqueue(JsonObject update) => _updates.Enqueue(update);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var method = request.RequestUri!.Segments[^1];
            Assert.Contains($"/bot{Token}/", request.RequestUri.AbsolutePath, StringComparison.Ordinal);
            var payload = JsonNode.Parse(await request.Content!.ReadAsStringAsync(cancellationToken))!.AsObject();
            if (method == "getUpdates")
            {
                // 长轮询：有排好的 Update 就给，否则稍等再回空数组
                if (_updates.TryDequeue(out var update))
                {
                    return Respond("""{"ok":true,"result":[""" + update.ToJsonString() + "]}");
                }

                await Task.Delay(50, cancellationToken);
                lock (Calls)
                {
                    if (!Calls.Any(static c => c.Method == "getUpdates"))
                    {
                        Calls.Add((method, payload));
                    }
                }

                return Respond("""{"ok":true,"result":[]}""");
            }

            lock (Calls)
            {
                Calls.Add((method, payload));
            }

            if (_failures.TryPeek(out var failure) && failure.Method == method && _failures.TryDequeue(out _))
            {
                return failure.Code == 429
                    ? Respond("""{"ok":false,"error_code":429,"description":"Too Many Requests: retry after 0","parameters":{"retry_after":0}}""")
                    : Respond($$"""{"ok":false,"error_code":{{failure.Code}},"description":"failure {{failure.Code}}"}""");
            }

            return method switch
            {
                "getMe" => Respond("""{"ok":true,"result":{"id":999,"is_bot":true,"username":"ops_bot"}}"""),
                "sendMessage" or "editMessageText" => Respond($$$"""{"ok":true,"result":{"message_id":{{{Interlocked.Increment(ref _messageId)}}}}}"""),
                _ => Respond("""{"ok":true,"result":true}"""),
            };
        }

        private static HttpResponseMessage Respond(string json) => new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }

    private sealed class SecretCommand : ITelegramCommandHandler
    {
        public TelegramCommand Command { get; } = new("/secret", "管理员", AdminOnly: true);

        public Task HandleAsync(TelegramContext context, IReadOnlyList<string> arguments) => context.ReplyAsync("secret ok");
    }

    private sealed class AskCommand : ITelegramCommandHandler
    {
        public TelegramCommand Command { get; } = new("ask", "问");

        public async Task HandleAsync(TelegramContext context, IReadOnlyList<string> arguments)
        {
            await context.SetStateAsync("await_name");
            await context.ReplyAsync("What is your name?");
        }
    }

    private sealed class NameState : ITelegramStateHandler
    {
        public int Order => 0;

        public bool CanHandle(string currentStep) => currentStep == "await_name";

        public async Task HandleAsync(TelegramContext context, TelegramConversationState state)
        {
            await context.ClearStateAsync();
            await context.ReplyAsync($"Hello {context.Update.Text}");
        }
    }

    private sealed class ApproveCallback : ITelegramCallbackHandler
    {
        public string Action => "approve";

        public bool AdminOnly => true;

        public Task HandleAsync(TelegramContext context, string data)
        {
            context.SetCallbackAnswer($"approved {data}");
            return Task.CompletedTask;
        }
    }

    private sealed class EchoInline : ITelegramInlineQueryHandler
    {
        public int Order => 0;

        public bool CanHandle(string query) => query.Length > 0;

        public Task<JsonArray> HandleAsync(TelegramContext context, string query)
            => Task.FromResult(new JsonArray(new JsonObject { ["type"] = "article", ["id"] = "1", ["title"] = $"echo: {query}", ["input_message_content"] = new JsonObject { ["message_text"] = query } }));
    }
}
