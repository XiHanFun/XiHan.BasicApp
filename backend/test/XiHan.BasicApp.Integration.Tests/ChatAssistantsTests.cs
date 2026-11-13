// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.AI;
using XiHan.BasicApp.Chat;
using XiHan.BasicApp.Contracts.Realtime;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.Framework.Security.Abstractions.Permissions;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证聊天里的助手
/// </summary>
/// <remarks>
/// 打开会话与开场白；回复的增量顺序到达、落库与两条推送；上下文取自库；检索增强带引用、只在调用方一侧；
/// 失败以 Problem Details 结束并立即推送（方案 D13）；工具以调用方身份执行，没有权限时模型收到拒绝；AI 关掉后聊天照常、助手操作明确不可用
/// 模型端点是仿真的 OpenAI 兼容端点，向量库用内存连接器
/// </remarks>
public sealed class ChatAssistantsTests : IDisposable
{
    private const string Chatter = "chatter";
    private const string Admin = "ai-admin";
    private const string Password = "Chat@123456";
    private static readonly TimeSpan _wait = TimeSpan.FromSeconds(10);

    private readonly SqliteDatabase _database = new();
    private readonly FakeOpenAIHandler _model = new();

    public void Dispose()
    {
        _database.Dispose();
        _model.Dispose();
    }

    private async Task<BasicAppTestServer> StartAsync(bool vectorStore = true, IEnumerable<KeyValuePair<string, string?>>? settings = null)
    {
        var server = await BasicAppTestServer.StartAsync(
            _database.ConnectionString,
            settings: [new("XiHan:AI:Knowledge:Dimensions", FakeOpenAIHandler.DefaultDimensions.ToString(CultureInfo.InvariantCulture)), .. settings ?? []],
            configure: builder =>
            {
                _model.Attach(builder);
                if (vectorStore)
                {
                    builder.Services.AddInMemoryVectorStore();
                }
            });
        await server.SeedDefaultsAsync();
        await server.RunSeedersAsync();
        await server.SeedAsync([], [.. AIPermissions.All.SelectMany(static p => TestGrants.BothSides(GrantSubject.ForRole(Admin), p.Code))]);
        return server;
    }

    private static string Platform() => TestTokens.Issue("root", null, Admin);

    /// <summary>
    /// 建一个真实用户并按用户授予聊天权限（令牌里的角色不替真实用户授权），返回它的令牌
    /// </summary>
    private static async Task<string> UserAsync(BasicAppTestServer server, string name, string? tenant = "acme", params string[] extraPermissions)
        => (await CreateUserAsync(server, name, tenant, extraPermissions)).Token;

    private static async Task<(string Id, string Token)> CreateUserAsync(BasicAppTestServer server, string name, string? tenant = "acme", params string[] extraPermissions)
    {
        var creator = tenant is null ? await SuperAdminAsync(server) : TestTokens.Issue("root", tenant, "admin");
        var (created, user) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users", JsonSerializer.Serialize(new { userName = name, displayName = name, password = Password }), creator));
        Assert.True(created.StatusCode == HttpStatusCode.Created, user.GetRawText());
        var id = user.GetProperty("id").GetString()!;
        await server.SeedAsync([], [.. ChatPermissions.All.Select(static p => p.Code).Concat(extraPermissions).Select(code => new PermissionGrant(GrantSubject.ForUser(id), code, tenant))]);
        return (id, TestTokens.Issue(id, tenant, Chatter));
    }

    private static async Task<string> SuperAdminAsync(BasicAppTestServer server)
        => (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;

    private static Task<(HttpResponseMessage Response, JsonElement Body)> SendAsync(BasicAppTestServer server, HttpMethod method, string path, object? body, string token, string? locale = null)
        => server.SendAsync(BasicAppTestServer.Request(method, path, body is null ? null : JsonSerializer.Serialize(body), token, locale));

    private static async Task ProviderAsync(BasicAppTestServer server)
    {
        var (created, body) = await SendAsync(server, HttpMethod.Post, "/api/ai-providers", new { code = "model", name = "Model", vendor = "OpenAI", chatModel = "chat-a", embeddingModel = "embed-a", endpoint = FakeOpenAIHandler.Endpoint, apiKey = "test-key", isDefault = true }, Platform());
        Assert.True(created.StatusCode == HttpStatusCode.Created, body.GetRawText());
    }

    private static async Task<string> AssistantAsync(BasicAppTestServer server, string code = "helper", bool enableKnowledge = false, string? greeting = null, int historyRounds = 10, string? promptCode = null, string[]? tools = null)
    {
        var (created, body) = await SendAsync(server, HttpMethod.Post, "/api/ai-assistants", new { code, name = "Helper", greeting, enableKnowledge, historyRounds, promptCode, tools }, Platform());
        Assert.True(created.StatusCode == HttpStatusCode.Created, body.GetRawText());
        return body.GetProperty("id").GetString()!;
    }

    private static async Task<JsonElement> OpenAsync(BasicAppTestServer server, string assistantId, string token)
    {
        var (opened, body) = await SendAsync(server, HttpMethod.Post, "/api/chat-conversations:open-assistant", new { assistantId }, token);
        Assert.True(opened.IsSuccessStatusCode, body.GetRawText());
        return body;
    }

    private static async Task SayAsync(BasicAppTestServer server, string conversationId, string content, string token)
    {
        var (sent, body) = await SendAsync(server, HttpMethod.Post, "/api/chat-messages:send", new { conversationId, messageType = "Text", content }, token);
        Assert.True(sent.StatusCode == HttpStatusCode.OK, body.GetRawText());
    }

    private static Task<(HttpResponseMessage Response, JsonElement Body)> ReplyAsync(BasicAppTestServer server, string conversationId, string token, string? locale = null, string replyId = "r-1")
        => SendAsync(server, HttpMethod.Post, "/api/chat-messages:reply-assistant", new { conversationId, replyId }, token, locale);

    private static async Task<List<JsonElement>> HistoryAsync(BasicAppTestServer server, string conversationId, string token)
    {
        var (listed, body) = await SendAsync(server, HttpMethod.Get, $"/api/chat-messages:history?conversationId={conversationId}&take=50", null, token);
        Assert.True(listed.StatusCode == HttpStatusCode.OK, body.GetRawText());
        return [.. body.GetProperty("items").EnumerateArray()];
    }

    private static HubConnection Connect(BasicAppTestServer server, string token) => new HubConnectionBuilder()
        .WithUrl("http://localhost" + RealtimeHubs.Chat, options =>
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

    private IReadOnlyList<(string Path, System.Text.Json.Nodes.JsonNode Body)> ChatRequests()
        => [.. _model.Requests.Where(static r => r.Path.EndsWith("/chat/completions", StringComparison.Ordinal))];

    [Fact]
    public async Task OpenAndReply_StreamsDeltas_SavesTheMessage_AndPushesCompletion()
    {
        await using var server = await StartAsync();
        await ProviderAsync(server);
        var assistant = await AssistantAsync(server, greeting: "Hi, ask me anything.");
        var token = await UserAsync(server, "alice");

        // 可用助手：两侧的聊天都能用
        var (listed, listedBody) = await SendAsync(server, HttpMethod.Get, "/api/chat-conversations:assistants", null, token);
        Assert.Equal(HttpStatusCode.OK, listed.StatusCode);
        Assert.Equal("helper", Assert.Single(listedBody.GetProperty("items").EnumerateArray()).GetProperty("code").GetString());

        // 打开：一个用户对一个助手只有一个会话，新建时发开场白
        var opened = await OpenAsync(server, assistant, token);
        Assert.True(opened.GetProperty("created").GetBoolean());
        var conversation = opened.GetProperty("conversationId").GetString()!;
        var again = await OpenAsync(server, assistant, token);
        Assert.False(again.GetProperty("created").GetBoolean());
        Assert.Equal(conversation, again.GetProperty("conversationId").GetString());
        var greeting = Assert.Single(await HistoryAsync(server, conversation, token));
        Assert.Equal("Assistant", greeting.GetProperty("messageType").GetString());
        Assert.Equal("Hi, ask me anything.", greeting.GetProperty("content").GetString());

        await using var hub = Connect(server, (await AuthFlowTests.LoginAsync(server, "alice", Password, "acme")).GetProperty("accessToken").GetString()!);
        var deltas = new ConcurrentQueue<JsonElement>();
        hub.On<JsonElement>(ChatRealtimeMethods.ChatAssistantDelta, payload => deltas.Enqueue(payload));
        var message = Expect(hub, ChatRealtimeMethods.ReceiveChatMessage);
        var completed = Expect(hub, ChatRealtimeMethods.ChatAssistantCompleted);
        await hub.StartAsync();

        // StartAsync 在握手完成时就返回，这时服务端可能还没把连接加进用户组；调用排在 OnConnectedAsync 之后，走一个来回再发
        await hub.InvokeAsync(ChatRealtimeMethods.JoinConversation, conversation);

        await SayAsync(server, conversation, "What is the refund window?", token);
        _model.Reply = "Thirty days.";
        var (replied, repliedBody) = await ReplyAsync(server, conversation, token, locale: "en-US");
        Assert.True(replied.StatusCode == HttpStatusCode.OK, repliedBody.GetRawText());
        var messageId = repliedBody.GetProperty("messageId").GetString()!;

        // 完成：落库之后推送，带消息
        var done = await completed.Task.WaitAsync(_wait);
        Assert.Equal(conversation, done.GetProperty("conversationId").GetString());
        Assert.Equal("r-1", done.GetProperty("replyId").GetString());
        Assert.Equal(messageId, done.GetProperty("messageId").GetString());
        Assert.Equal(JsonValueKind.Null, done.GetProperty("errorCode").ValueKind);
        _ = await message.Task.WaitAsync(_wait);

        // 增量顺序到达，拼起来就是全文
        Assert.Equal("Thirty days.", string.Concat(deltas.Select(static d => d.GetProperty("delta").GetString())));
        Assert.All(deltas, static d => Assert.Equal("r-1", d.GetProperty("replyId").GetString()));

        // 落库：助手消息、会话预览，助手不是成员，不算未读
        var history = await HistoryAsync(server, conversation, token);
        var saved = history.Single(m => m.GetProperty("messageId").GetString() == messageId);
        Assert.Equal("Assistant", saved.GetProperty("messageType").GetString());
        Assert.Equal("Thirty days.", saved.GetProperty("content").GetString());
        var (_, mine) = await SendAsync(server, HttpMethod.Get, "/api/chat-conversations:mine", null, token);
        var item = mine.GetProperty("items").EnumerateArray().Single(c => c.GetProperty("conversationId").GetString() == conversation);
        Assert.Equal("Assistant", item.GetProperty("conversationType").GetString());
        Assert.Equal(assistant, item.GetProperty("assistantId").GetString());
        Assert.Equal("Thirty days.", item.GetProperty("lastMessagePreview").GetString());
        Assert.Equal(0, item.GetProperty("unreadCount").GetInt32());

        // 没配提示词：缺省系统提示词按调用方区域取
        var request = Assert.Single(ChatRequests()).Body;
        var messages = request["messages"]!.AsArray();
        Assert.Equal("system", messages[0]!["role"]!.GetValue<string>());
        Assert.StartsWith("You are an enterprise knowledge assistant", messages[0]!["content"]!.ToJsonString().Trim('"'), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Reply_ReadsTheContextFromTheDatabase_AndConfiguredPromptVerbatim()
    {
        await using var server = await StartAsync();
        await ProviderAsync(server);
        var (prompt, promptBody) = await SendAsync(server, HttpMethod.Post, "/api/ai-prompts", new { code = "pirate", name = "Pirate", content = "Talk like a pirate." }, Platform());
        Assert.True(prompt.StatusCode == HttpStatusCode.Created, promptBody.GetRawText());
        var assistant = await AssistantAsync(server, historyRounds: 2, promptCode: "pirate");
        var token = await UserAsync(server, "alice");
        var conversation = (await OpenAsync(server, assistant, token)).GetProperty("conversationId").GetString()!;

        await SayAsync(server, conversation, "first", token);
        _model.Reply = "one";
        Assert.Equal(HttpStatusCode.OK, (await ReplyAsync(server, conversation, token)).Response.StatusCode);
        await SayAsync(server, conversation, "second", token);
        _model.Reply = "two";
        Assert.Equal(HttpStatusCode.OK, (await ReplyAsync(server, conversation, token)).Response.StatusCode);

        // 只带最近两条：上一轮的回复与本轮提问
        var messages = ChatRequests()[^1].Body["messages"]!.AsArray();
        Assert.Equal(["system", "assistant", "user"], messages.Select(static m => m!["role"]!.GetValue<string>()));
        Assert.Contains("Talk like a pirate.", messages[0]!["content"]!.ToJsonString(), StringComparison.Ordinal);
        Assert.Contains("second", messages[2]!["content"]!.ToJsonString(), StringComparison.Ordinal);

        // 停用引用的提示词：回复失败、不回落到缺省，也不留消息（方案 D8）
        var (_, promptNow) = await SendAsync(server, HttpMethod.Get, $"/api/ai-prompts/{promptBody.GetProperty("id").GetString()}", null, Platform());
        var (disabled, _) = await SendAsync(server, HttpMethod.Post, $"/api/ai-prompts/{promptBody.GetProperty("id").GetString()}:set-status", new { concurrencyStamp = promptNow.GetProperty("concurrencyStamp").GetString(), status = "Disabled" }, Platform());
        Assert.Equal(HttpStatusCode.OK, disabled.StatusCode);
        var before = (await HistoryAsync(server, conversation, token)).Count;
        await SayAsync(server, conversation, "third", token);
        var (failed, failedBody) = await ReplyAsync(server, conversation, token);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, failed.StatusCode);
        Assert.Equal(AIErrors.AssistantPromptDisabled, failedBody.GetProperty("code").GetString());
        Assert.Equal(before + 1, (await HistoryAsync(server, conversation, token)).Count);
    }

    [Fact]
    public async Task Reply_AugmentsTheQuestion_FromTheCallersSideOnly()
    {
        await using var server = await StartAsync();
        await ProviderAsync(server);
        var assistant = await AssistantAsync(server, enableKnowledge: true);
        var token = await UserAsync(server, "alice");
        foreach (var (text, owner) in new[] { ("Acme secret text.", TestTokens.Issue("root", "acme", Admin)), ("Platform secret text.", Platform()) })
        {
            var (ingested, body) = await SendAsync(server, HttpMethod.Post, "/api/knowledge-documents:ingest", new { title = text, text }, owner);
            Assert.True(ingested.IsSuccessStatusCode, body.GetRawText());
            Assert.Equal("Indexed", body.GetProperty("status").GetString());
        }

        var conversation = (await OpenAsync(server, assistant, token)).GetProperty("conversationId").GetString()!;
        await SayAsync(server, conversation, "What is the secret?", token);
        var (replied, repliedBody) = await ReplyAsync(server, conversation, token);
        Assert.True(replied.StatusCode == HttpStatusCode.OK, repliedBody.GetRawText());

        var question = ChatRequests()[^1].Body["messages"]!.AsArray()[^1]!["content"]!.ToJsonString();
        Assert.Contains("[1]", question, StringComparison.Ordinal);
        Assert.Contains("Acme secret text.", question, StringComparison.Ordinal);
        Assert.DoesNotContain("Platform secret text.", question, StringComparison.Ordinal);

        // 平台一侧同样能和助手聊天，只命中平台的文档
        var ops = await UserAsync(server, "ops", tenant: null);
        var own = (await OpenAsync(server, assistant, ops)).GetProperty("conversationId").GetString()!;
        await SayAsync(server, own, "What is the secret?", ops);
        var (platformReply, platformBody) = await ReplyAsync(server, own, ops);
        Assert.True(platformReply.StatusCode == HttpStatusCode.OK, platformBody.GetRawText());
        var platformQuestion = ChatRequests()[^1].Body["messages"]!.AsArray()[^1]!["content"]!.ToJsonString();
        Assert.Contains("Platform secret text.", platformQuestion, StringComparison.Ordinal);
        Assert.DoesNotContain("Acme secret text.", platformQuestion, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Failures_EndInProblemDetails_AndArePushedRightAway()
    {
        await using var server = await StartAsync();
        await ProviderAsync(server);
        var assistant = await AssistantAsync(server);
        var token = await UserAsync(server, "alice");
        var conversation = (await OpenAsync(server, assistant, token)).GetProperty("conversationId").GetString()!;

        // 没有提问
        var (noQuestion, noQuestionBody) = await ReplyAsync(server, conversation, token);
        Assert.Equal(HttpStatusCode.BadRequest, noQuestion.StatusCode);
        Assert.Equal(ChatErrors.NoQuestion, noQuestionBody.GetProperty("code").GetString());

        await SayAsync(server, conversation, "Hello?", token);
        var before = (await HistoryAsync(server, conversation, token)).Count;

        await using var hub = Connect(server, (await AuthFlowTests.LoginAsync(server, "alice", Password, "acme")).GetProperty("accessToken").GetString()!);
        var completions = new ConcurrentQueue<JsonElement>();
        hub.On<JsonElement>(ChatRealtimeMethods.ChatAssistantCompleted, payload => completions.Enqueue(payload));
        await hub.StartAsync();
        await hub.InvokeAsync(ChatRealtimeMethods.JoinConversation, conversation);

        // 模型不可达
        _model.Failing = true;
        var (failed, failedBody) = await ReplyAsync(server, conversation, token, replyId: "r-fail");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, failed.StatusCode);
        Assert.Equal(ChatErrors.AssistantReplyFailed, failedBody.GetProperty("code").GetString());
        // 原因只有异常类型：调用方是租户用户，模型服务的响应与地址不外露，完整异常在日志里
        Assert.DoesNotContain("fake failure", failedBody.GetProperty("detail").GetString(), StringComparison.Ordinal);
        _model.Failing = false;

        // 空输出
        _model.Reply = string.Empty;
        var (empty, emptyBody) = await ReplyAsync(server, conversation, token, replyId: "r-empty");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, empty.StatusCode);
        Assert.Equal(ChatErrors.AssistantReplyEmpty, emptyBody.GetProperty("code").GetString());

        // 敏感词
        var (config, configBody) = await SettingsTestHost.SetBuiltInConfigAsync(server, await SuperAdminAsync(server), "chat.policy", """{"retentionDays":365,"sensitiveWords":["forbidden"]}""");
        Assert.True(config == HttpStatusCode.OK, configBody.GetRawText());
        _model.Reply = "This is forbidden content.";
        var (blocked, blockedBody) = await ReplyAsync(server, conversation, token, replyId: "r-word");
        Assert.Equal(HttpStatusCode.BadRequest, blocked.StatusCode);
        Assert.Equal(ChatErrors.SensitiveWord, blockedBody.GetProperty("code").GetString());

        // 每次失败都立即推 Completed 带错误，消息一条都没留
        await EventuallyAsync(() => Task.FromResult(completions.Count >= 3));
        var failure = completions.First(c => c.GetProperty("replyId").GetString() == "r-fail");
        Assert.Equal(ChatErrors.AssistantReplyFailed, failure.GetProperty("errorCode").GetString());
        Assert.False(string.IsNullOrEmpty(failure.GetProperty("error").GetString()));
        Assert.Equal(JsonValueKind.Null, failure.GetProperty("messageId").ValueKind);
        Assert.Equal(before, (await HistoryAsync(server, conversation, token)).Count);

        // 不是成员不能请求；普通会话不能请求助手回复
        var (bobId, bob) = await CreateUserAsync(server, "bob");
        var (notMember, notMemberBody) = await ReplyAsync(server, conversation, bob);
        Assert.Equal(HttpStatusCode.Forbidden, notMember.StatusCode);
        Assert.Equal(ChatErrors.NotMember, notMemberBody.GetProperty("code").GetString());
        var (_, single) = await SendAsync(server, HttpMethod.Post, "/api/chat-conversations:open-single", new { peerUserId = bobId }, token);
        var (notAssistant, notAssistantBody) = await ReplyAsync(server, single.GetProperty("conversationId").GetString()!, token);
        Assert.Equal(HttpStatusCode.BadRequest, notAssistant.StatusCode);
        Assert.Equal(ChatErrors.NotAssistantConversation, notAssistantBody.GetProperty("code").GetString());
    }

    [Fact]
    public async Task KnowledgeWithoutAVectorStore_FailsTheReply()
    {
        await using var server = await StartAsync(vectorStore: false);
        await ProviderAsync(server);
        var assistant = await AssistantAsync(server, enableKnowledge: true);
        var token = await UserAsync(server, "alice");
        var conversation = (await OpenAsync(server, assistant, token)).GetProperty("conversationId").GetString()!;
        await SayAsync(server, conversation, "Hello?", token);

        // 检索失败不退化成纯模型回答（方案 D7）
        var (failed, body) = await ReplyAsync(server, conversation, token);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, failed.StatusCode);
        Assert.Equal("AI_UNAVAILABLE_002", body.GetProperty("code").GetString());
        Assert.Empty(ChatRequests());
    }

    [Fact]
    public async Task Tools_RunAsTheCaller_AndAreDeniedWithoutPermission()
    {
        await using var server = await StartAsync();
        await ProviderAsync(server);
        var assistant = await AssistantAsync(server, tools: ["users_list"]);

        // 调用方没有 identity.users.read：工具被拒，模型收到拒绝，回复本身不失败
        var denied = await UserAsync(server, "alice");
        var conversation = (await OpenAsync(server, assistant, denied)).GetProperty("conversationId").GetString()!;
        await SayAsync(server, conversation, "List the users.", denied);
        _model.ToolCall = ("users_list", """{"request":{"page":{"index":1,"size":5}}}""");
        var (replied, repliedBody) = await ReplyAsync(server, conversation, denied);
        Assert.True(replied.StatusCode == HttpStatusCode.OK, repliedBody.GetRawText());
        var declared = ChatRequests()[0].Body["tools"]!.AsArray();
        Assert.Equal("users_list", Assert.Single(declared)!["function"]!["name"]!.GetValue<string>());
        var deniedResult = ChatRequests()[^1].Body["messages"]!.AsArray().Last(static m => m!["role"]!.GetValue<string>() == "tool")!["content"]!.ToJsonString();
        Assert.Contains("SECURITY_DENIED", deniedResult, StringComparison.Ordinal);

        // 调用方有权限：工具照常执行，模型收到结果
        var count = ChatRequests().Count;
        var allowed = await UserAsync(server, "bob", "acme", "identity.users.read");
        var own = (await OpenAsync(server, assistant, allowed)).GetProperty("conversationId").GetString()!;
        await SayAsync(server, own, "List the users.", allowed);
        var (ok, okBody) = await ReplyAsync(server, own, allowed);
        Assert.True(ok.StatusCode == HttpStatusCode.OK, okBody.GetRawText());
        Assert.True(ChatRequests().Count > count);
        var allowedResult = ChatRequests()[^1].Body["messages"]!.AsArray().Last(static m => m!["role"]!.GetValue<string>() == "tool")!["content"]!.ToJsonString();
        Assert.DoesNotContain("SECURITY_DENIED", allowedResult, StringComparison.Ordinal);
        Assert.Contains("items", allowedResult, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DisabledAssistant_DisappearsFromChat_AndExistingConversationsStopAnswering()
    {
        await using var server = await StartAsync();
        await ProviderAsync(server);
        var assistant = await AssistantAsync(server);
        var token = await UserAsync(server, "alice");
        var conversation = (await OpenAsync(server, assistant, token)).GetProperty("conversationId").GetString()!;
        await SayAsync(server, conversation, "Hello?", token);

        var (_, current) = await SendAsync(server, HttpMethod.Get, $"/api/ai-assistants/{assistant}", null, Platform());
        var (disabled, _) = await SendAsync(server, HttpMethod.Post, $"/api/ai-assistants/{assistant}:set-status", new { concurrencyStamp = current.GetProperty("concurrencyStamp").GetString(), status = "Disabled" }, Platform());
        Assert.Equal(HttpStatusCode.OK, disabled.StatusCode);

        var (_, listed) = await SendAsync(server, HttpMethod.Get, "/api/chat-conversations:assistants", null, token);
        Assert.Empty(listed.GetProperty("items").EnumerateArray());
        var (open, openBody) = await SendAsync(server, HttpMethod.Post, "/api/chat-conversations:open-assistant", new { assistantId = assistant }, token);
        Assert.Equal(HttpStatusCode.NotFound, open.StatusCode);
        Assert.Equal(AIErrors.AssistantNotFound, openBody.GetProperty("code").GetString());
        var (reply, replyBody) = await ReplyAsync(server, conversation, token);
        Assert.Equal(HttpStatusCode.NotFound, reply.StatusCode);
        Assert.Equal(AIErrors.AssistantNotFound, replyBody.GetProperty("code").GetString());
    }

    [Fact]
    public async Task WithoutTheAIModule_AssistantOperationsAreUnavailable_AndChatStillWorks()
    {
        await using var server = await StartAsync(vectorStore: false, settings: [new("BasicApp:Modules:AI:Enabled", "false")]);
        var token = await UserAsync(server, "alice");

        var (listed, listedBody) = await SendAsync(server, HttpMethod.Get, "/api/chat-conversations:assistants", null, token);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, listed.StatusCode);
        Assert.Equal(ChatErrors.AssistantUnavailable, listedBody.GetProperty("code").GetString());
        var (open, openBody) = await SendAsync(server, HttpMethod.Post, "/api/chat-conversations:open-assistant", new { assistantId = Guid.CreateVersion7() }, token);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, open.StatusCode);
        Assert.Equal(ChatErrors.AssistantUnavailable, openBody.GetProperty("code").GetString());
        var (reply, replyBody) = await ReplyAsync(server, Guid.CreateVersion7().ToString(), token);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, reply.StatusCode);
        Assert.Equal(ChatErrors.AssistantUnavailable, replyBody.GetProperty("code").GetString());

        // 聊天其余功能照常：会话列表、消息接口都在，AI 的接口不在
        var (mine, _) = await SendAsync(server, HttpMethod.Get, "/api/chat-conversations:mine", null, token);
        Assert.Equal(HttpStatusCode.OK, mine.StatusCode);
        var (assistants, _) = await SendAsync(server, HttpMethod.Post, "/api/ai-assistants:list", new { page = new { index = 1, size = 10 } }, Platform());
        Assert.Equal(HttpStatusCode.NotFound, assistants.StatusCode);
    }

    private static async Task EventuallyAsync(Func<Task<bool>> condition)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            if (await condition())
            {
                return;
            }

            await Task.Delay(100);
        }

        Assert.Fail("The condition was not met in time.");
    }
}
