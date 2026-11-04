// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Channels;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts.Realtime;
using XiHan.BasicApp.Integration.Tests.Infrastructure;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证后台任务进度的推送
/// </summary>
/// <remarks>
/// 导出与通知发布把进度经 <c>TaskProgress</c> 推给发起人（别人收不到）：导出从「准备中」到完成或失败，
/// 文案按提交时的区域；发布先推进行中，成功带接收人数与链接，失败写明原因，事务开着之后才失败也要收尾
/// </remarks>
public sealed class TaskProgressTests : IDisposable
{
    private static readonly TimeSpan _wait = TimeSpan.FromSeconds(15);

    private readonly SqliteDatabase _database = new();
    private readonly string _root = Path.Combine(Path.GetTempPath(), "xihan-progress-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        _database.Dispose();
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private void UseLocalFiles(WebApplicationBuilder builder) => builder.Configuration.AddInMemoryCollection([new("BasicApp:Files:LocalRootPath", _root)]);

    /// <summary>
    /// 一条 Hub 连接收到的进度
    /// </summary>
    private sealed class Listener(BasicAppTestServer server, string token) : IAsyncDisposable
    {
        private readonly Channel<JsonElement> _received = Channel.CreateUnbounded<JsonElement>();

        private readonly HubConnection _connection = new HubConnectionBuilder()
            .WithUrl("http://localhost/hubs/notification", options =>
            {
                options.HttpMessageHandlerFactory = _ => server.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
                options.AccessTokenProvider = () => Task.FromResult<string?>(token);
            })
            .Build();

        /// <summary>
        /// 连上并等到在线列表里出现这条连接，之后的推送一定收得到
        /// </summary>
        /// <param name="watcher">看得到在线列表的令牌</param>
        /// <param name="online">这一侧应有的在线会话数</param>
        public async Task StartAsync(string watcher, int online)
        {
            _connection.On<JsonElement>(RealtimeMethods.TaskProgress, payload => _received.Writer.TryWrite(payload));
            await _connection.StartAsync();
            for (var attempt = 0; attempt < 100; attempt++)
            {
                var (_, sessions) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/sessions:list", """{"page":{"index":1,"size":20}}""", watcher));
                if (sessions.GetProperty("items").EnumerateArray().Count(static row => row.GetProperty("isRealtimeOnline").GetBoolean()) >= online)
                {
                    return;
                }

                await Task.Delay(100);
            }

            throw new TimeoutException("The hub connection did not come online.");
        }

        /// <summary>
        /// 读到终态为止（包括那一条）
        /// </summary>
        public async Task<List<JsonElement>> UntilFinishedAsync()
        {
            using var timeout = new CancellationTokenSource(_wait);
            var messages = new List<JsonElement>();
            while (true)
            {
                var message = await _received.Reader.ReadAsync(timeout.Token);
                messages.Add(message);
                if (message.GetProperty("state").GetString() != "loading")
                {
                    return messages;
                }
            }
        }

        public bool IsSilent => !_received.Reader.TryPeek(out _);

        public ValueTask DisposeAsync() => _connection.DisposeAsync();
    }

    private static async Task<Listener> ListenAsync(BasicAppTestServer server, string token, string? watcher = null, int online = 1)
    {
        var listener = new Listener(server, token);
        await listener.StartAsync(watcher ?? token, online);
        return listener;
    }

    private static string SubmitBody(string name) => JsonSerializer.Serialize(new
    {
        businessType = "users.list",
        format = "Csv",
        scope = "SearchResult",
        taskName = name,
        querySnapshot = JsonSerializer.Serialize(new { page = new { index = 1, size = 20 }, filters = Array.Empty<object>(), sorts = Array.Empty<object>() }),
        columns = new object[] { new { key = "userName", title = "Login" } },
    });

    private static async Task<string> UserTokenAsync(BasicAppTestServer server, string platform, string name)
    {
        var (created, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users", JsonSerializer.Serialize(new { userName = name, displayName = name, password = "Pass@123456" }), platform));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        return (await AuthFlowTests.LoginAsync(server, name, "Pass@123456")).GetProperty("accessToken").GetString()!;
    }

    private static async Task<string> DraftAsync(BasicAppTestServer server, string token, string title)
    {
        var (created, body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/notifications", JsonSerializer.Serialize(new { title }), token));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        return body.GetProperty("id").GetString()!;
    }

    [Fact]
    public async Task Export_ReportsProgressToTheRequester_InTheLocaleOfTheSubmission()
    {
        var (server, platform, tenant) = await SettingsTestHost.StartAsync(_database.ConnectionString, configure: UseLocalFiles);
        await using var running = server;
        await using var requester = await ListenAsync(server, platform);
        await using var tenantRequester = await ListenAsync(server, tenant);

        var (submitted, task) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/export-tasks:submit", SubmitBody("users"), platform, acceptLanguage: "ja-JP"));
        Assert.Equal(HttpStatusCode.OK, submitted.StatusCode);
        var messages = await requester.UntilFinishedAsync();
        var taskId = $"export:{task.GetProperty("id").GetString()}";
        Assert.All(messages, message => Assert.Equal(taskId, message.GetProperty("taskId").GetString()));

        // 领到任务：准备中，进度 0，文案按提交时的区域
        var first = messages[0];
        Assert.Equal(("loading", 0, "準備中…"), (first.GetProperty("state").GetString(), first.GetProperty("progress").GetInt32(), first.GetProperty("detail").GetString()));
        Assert.Equal("users をエクスポート中", first.GetProperty("label").GetString());

        // 写完到 99%：带已写行数
        var rows = (await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/export-tasks/{task.GetProperty("id").GetString()}", token: platform))).Body.GetProperty("processedCount").GetInt64();
        var writing = messages[^2];
        Assert.Equal(("loading", 99, $"{rows}/{rows} 行"), (writing.GetProperty("state").GetString(), writing.GetProperty("progress").GetInt32(), writing.GetProperty("detail").GetString()));

        // 完成：进度 100，点击去导出中心
        var last = messages[^1];
        Assert.Equal(("success", 100, "/files/export-center"), (last.GetProperty("state").GetString(), last.GetProperty("progress").GetInt32(), last.GetProperty("link").GetString()));
        Assert.Equal("users のエクスポートが完了しました", last.GetProperty("label").GetString());
        Assert.Contains(rows.ToString(System.Globalization.CultureInfo.InvariantCulture), last.GetProperty("detail").GetString(), StringComparison.Ordinal);

        // 租户一侧的导出只推给租户一侧的发起人；进度已经写进任务表，平台的发起人这边不再有新消息
        var (tenantSubmitted, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/export-tasks:submit", SubmitBody("tenant-users"), tenant));
        Assert.Equal(HttpStatusCode.OK, tenantSubmitted.StatusCode);
        Assert.Equal("success", (await tenantRequester.UntilFinishedAsync())[^1].GetProperty("state").GetString());
        Assert.True(requester.IsSilent);
    }

    [Fact]
    public async Task Export_WhenItFails_TellsTheRequesterWhy()
    {
        var (server, platform, _) = await SettingsTestHost.StartAsync(_database.ConnectionString, configure: UseLocalFiles);
        await using var running = server;
        var viewer = await UserTokenAsync(server, platform, "viewer");
        await using var admin = await ListenAsync(server, platform);
        await using var requester = await ListenAsync(server, viewer, watcher: platform, online: 2);

        // 发起人没有列表操作的权限：领到任务之后第一页就被拒绝
        var (submitted, task) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/export-tasks:submit", SubmitBody("denied"), viewer, acceptLanguage: "zh-CN"));
        Assert.Equal(HttpStatusCode.OK, submitted.StatusCode);
        var failed = (await requester.UntilFinishedAsync())[^1];
        Assert.Equal(("error", "denied 导出失败"), (failed.GetProperty("state").GetString(), failed.GetProperty("label").GetString()));
        Assert.Equal(JsonValueKind.Null, failed.GetProperty("progress").ValueKind);
        Assert.Equal(JsonValueKind.Null, failed.GetProperty("link").ValueKind);

        // 副文本就是记到任务上的原因
        var (_, recorded) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/export-tasks/{task.GetProperty("id").GetString()}", token: viewer));
        Assert.Equal("Failed", recorded.GetProperty("status").GetString());
        Assert.Equal(recorded.GetProperty("errorMessage").GetString(), failed.GetProperty("detail").GetString());

        // 只推给发起人
        Assert.True(admin.IsSilent);
    }

    [Fact]
    public async Task Publish_ReportsToThePublisher_AndAFailureClosesTheTask()
    {
        var (server, platform, _) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;
        var viewer = await UserTokenAsync(server, platform, "viewer");
        await using var publisher = await ListenAsync(server, platform);
        await using var other = await ListenAsync(server, viewer, watcher: platform, online: 2);
        var draft = await DraftAsync(server, platform, "Maintenance");

        // 成功：先进行中，再带接收人数与链接；同一个任务标识
        var (published, delivery) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/notifications/{draft}:publish", "{}", platform, acceptLanguage: "en-US"));
        Assert.Equal(HttpStatusCode.OK, published.StatusCode);
        var messages = await publisher.UntilFinishedAsync();
        Assert.Equal(2, messages.Count);
        var (start, done) = (messages[0], messages[1]);
        Assert.Equal(("loading", "Publishing announcement…"), (start.GetProperty("state").GetString(), start.GetProperty("label").GetString()));
        Assert.Equal(JsonValueKind.Null, start.GetProperty("progress").ValueKind);
        Assert.Equal(("success", "Announcement published", 100, "/notification/notifications"), (done.GetProperty("state").GetString(), done.GetProperty("label").GetString(), done.GetProperty("progress").GetInt32(), done.GetProperty("link").GetString()));
        Assert.Equal($"Sent to {delivery.GetProperty("recipientCount").GetInt32()} users", done.GetProperty("detail").GetString());
        Assert.StartsWith("notification-publish:", start.GetProperty("taskId").GetString(), StringComparison.Ordinal);
        Assert.Equal(start.GetProperty("taskId").GetString(), done.GetProperty("taskId").GetString());

        // 重复发布：操作失败，那一条收尾并写明原因
        var (repeated, conflict) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/notifications/{draft}:publish", "{}", platform, acceptLanguage: "en-US"));
        Assert.Equal(HttpStatusCode.Conflict, repeated.StatusCode);
        var failed = (await publisher.UntilFinishedAsync())[^1];
        Assert.Equal(("error", "Failed to publish announcement"), (failed.GetProperty("state").GetString(), failed.GetProperty("label").GetString()));
        Assert.Equal(conflict.GetProperty("detail").GetString(), failed.GetProperty("detail").GetString());

        // 没有发布权限：授权就拒绝了，什么也不推；只有发布操作才推，建好即发布的创建不推
        var (denied, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/notifications/{await DraftAsync(server, platform, "Denied")}:publish", "{}", viewer));
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        var (immediate, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/notifications", JsonSerializer.Serialize(new { title = "Now", publishImmediately = true }), platform));
        Assert.Equal(HttpStatusCode.Created, immediate.StatusCode);
        Assert.True(other.IsSilent);
        Assert.True(publisher.IsSilent);
    }

    [Fact]
    public async Task Publish_FailingAfterTheFirstWrite_RollsBackAndStillClosesTheTask()
    {
        var (server, platform, _) = await SettingsTestHost.StartAsync(_database.ConnectionString, configure: static builder => builder.Services.AddScoped<IRealtimePush, BrokenInboxPush>());
        await using var running = server;
        await using var publisher = await ListenAsync(server, platform);
        var draft = await DraftAsync(server, platform, "Maintenance");

        // 收件箱已写、事务开着时推送抛出：回滚之后才推失败，事务开着时立即推送会抛出
        var (response, body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/notifications/{draft}:publish", "{}", platform, acceptLanguage: "en-US"));
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("KERNEL_UNEXPECTED_001", body.GetProperty("code").GetString());
        var messages = await publisher.UntilFinishedAsync();
        Assert.Equal(("loading", "error"), (messages[0].GetProperty("state").GetString(), messages[^1].GetProperty("state").GetString()));
        Assert.Equal("Failed to publish announcement", messages[^1].GetProperty("label").GetString());
        Assert.Equal(JsonValueKind.Null, messages[^1].GetProperty("detail").ValueKind);

        // 回滚了：还是草稿
        var (_, notification) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/notifications/{draft}", token: platform));
        Assert.False(notification.GetProperty("isPublished").GetBoolean());
    }

    /// <summary>
    /// 排在真实推送之前的推送：发收件箱推送时抛出，别的什么也不做
    /// </summary>
    private sealed class BrokenInboxPush : IRealtimePush
    {
        public ValueTask SendAsync<T>(string hub, RealtimeAudience audience, string method, T payload, JsonTypeInfo<T> payloadInfo, CancellationToken cancellationToken)
            => method == RealtimeMethods.ReceiveNotification ? throw new InvalidOperationException("The inbox push is broken.") : ValueTask.CompletedTask;

        public ValueTask SendNowAsync<T>(string hub, RealtimeAudience audience, string method, T payload, JsonTypeInfo<T> payloadInfo, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;

        public ValueTask DisconnectAsync(IReadOnlyCollection<Guid> sessionIds, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }
}
