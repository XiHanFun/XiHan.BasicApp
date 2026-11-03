// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts.Realtime;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证实时推送的跨实例
/// </summary>
/// <remarks>
/// 两个宿主共用一个库：配了 Redis 时一个实例发出的推送经背板送到另一个实例上的连接，在线列表汇总两个实例的连接，
/// 吊销会话时别的实例上的连接收到 <c>ForceLogout</c> 并被移出全部组；再配了 RabbitMQ 时它还被所在的实例断开
/// 立即推送只能在事务之外用
/// </remarks>
public sealed class RealtimeScaleOutTests : IDisposable
{
    private const string RedisVariable = "XIHAN_TEST_REDIS";
    private const string RabbitMQVariable = "XIHAN_TEST_RABBITMQ";
    private static readonly TimeSpan _wait = TimeSpan.FromSeconds(10);
    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task WithRedis_PushesAndPresenceCrossInstances_AndRevokeDetachesTheRemoteConnection()
    {
        var redis = Require(RedisVariable);
        var (first, second, platform) = await StartPairAsync([new("XiHan:Bindings:cache:value", redis), new("XiHan:Bindings:queue:value", "")]);
        await using var runningFirst = first;
        await using var runningSecond = second;
        var other = await AuthFlowTests.LoginAsync(second, "superadmin", "SuperAdmin@123");
        var otherToken = other.GetProperty("accessToken").GetString()!;
        var otherSession = other.GetProperty("sessionId").GetString()!;

        await using var local = Connect(first, platform);
        await using var remote = Connect(second, otherToken);
        var forced = Expect(remote, RealtimeMethods.ForceLogout);
        var changes = Collect(remote, RealtimeMethods.UserSettingChanged);
        var localChanges = Collect(local, RealtimeMethods.UserSettingChanged);
        await local.StartAsync();
        await remote.StartAsync();

        // 在线状态：第一个实例的在线列表里也看得到连在第二个实例上的会话
        await WaitOnlineAsync(first, platform, 2);

        // 推送：第一个实例写用户设置，连在第二个实例上的另一端收到
        await SaveSettingAsync(first, platform, "dark");
        Assert.Equal("{\"theme\":\"dark\"}", (await changes.Reader.ReadAsync().AsTask().WaitAsync(_wait)).GetProperty("settingValue").GetString());

        // 强制下线：第一个实例吊销第二个实例上的会话，它收到 ForceLogout 并被移出全部组，之后的推送不再送到它
        var (_, revoked) = await first.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/sessions:revoke", JsonSerializer.Serialize(new { ids = new[] { otherSession } }), platform));
        Assert.Equal(otherSession, Assert.Single(revoked.GetProperty("succeeded").EnumerateArray()).GetString());
        Assert.Equal(otherSession, Assert.Single((await forced.Task.WaitAsync(_wait)).GetProperty("targetSessionIds").EnumerateArray()).GetString());
        await DrainAsync(localChanges);
        await SaveSettingAsync(first, platform, "light");
        Assert.Equal("{\"theme\":\"light\"}", (await localChanges.Reader.ReadAsync().AsTask().WaitAsync(_wait)).GetProperty("settingValue").GetString());
        await Task.Delay(TimeSpan.FromSeconds(1));
        Assert.False(changes.Reader.TryRead(out _));
    }

    [Fact]
    public async Task WithRedisAndRabbitMQ_RevokeClosesTheConnectionOnTheOtherInstance()
    {
        var redis = Require(RedisVariable);
        var rabbit = Require(RabbitMQVariable);
        var (first, second, platform) = await StartPairAsync([new("XiHan:Bindings:cache:value", redis), new("XiHan:Bindings:queue:value", rabbit)]);
        await using var runningFirst = first;
        await using var runningSecond = second;
        var other = await AuthFlowTests.LoginAsync(second, "superadmin", "SuperAdmin@123");
        var otherSession = other.GetProperty("sessionId").GetString()!;

        await using var remote = Connect(second, other.GetProperty("accessToken").GetString()!);
        var forced = Expect(remote, RealtimeMethods.ForceLogout);
        var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        remote.Closed += _ =>
        {
            closed.TrySetResult();
            return Task.CompletedTask;
        };
        await remote.StartAsync();
        await WaitOnlineAsync(first, platform, 1);

        var (_, revoked) = await first.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/sessions:revoke", JsonSerializer.Serialize(new { ids = new[] { otherSession } }), platform));
        Assert.Equal(otherSession, Assert.Single(revoked.GetProperty("succeeded").EnumerateArray()).GetString());
        await forced.Task.WaitAsync(_wait);
        await closed.Task.WaitAsync(_wait);
    }

    [Fact]
    public async Task SendNow_RefusesToRunInsideATransaction()
    {
        await using var server = await BasicAppTestServer.StartAsync(_database.ConnectionString);
        var refused = await server.InvokeAsync(async ctx =>
        {
            var push = ctx.Services.GetRequiredService<IRealtimePush>();
            await Assert.ThrowsAsync<InvalidOperationException>(() => push.SendNowAsync(RealtimeHubs.Notification, RealtimeAudience.Users([Guid.NewGuid()]), "Ping", "now", TestJsonContext.Default.String, ctx.CancellationToken).AsTask());
            return Result.Success();
        });
        refused.ThrowIfFailure();

        // 事务之外照常发出；没登记的 Hub 即抛出
        await using var scope = server.Services.CreateAsyncScope();
        var outside = scope.ServiceProvider.GetRequiredService<IRealtimePush>();
        await outside.SendNowAsync(RealtimeHubs.Notification, RealtimeAudience.Users([Guid.NewGuid()]), "Ping", "now", TestJsonContext.Default.String, CancellationToken.None);
        await Assert.ThrowsAsync<InvalidOperationException>(() => outside.SendNowAsync("/hubs/unknown", RealtimeAudience.Users([Guid.NewGuid()]), "Ping", "now", TestJsonContext.Default.String, CancellationToken.None).AsTask());
    }

    private static string Require(string variable)
    {
        var value = Environment.GetEnvironmentVariable(variable);
        Assert.SkipWhen(string.IsNullOrWhiteSpace(value), $"{variable} is not set; this case only runs against a real server.");
        return value!;
    }

    /// <summary>
    /// 起两个共用一个库、一个 Redis 键前缀的实例
    /// </summary>
    private async Task<(BasicAppTestServer First, BasicAppTestServer Second, string Platform)> StartPairAsync(KeyValuePair<string, string?>[] providers)
    {
        KeyValuePair<string, string?>[] settings = [.. providers, new("XiHan:Caching:Redis:KeyPrefix", $"xihan-basicapp-test-{Guid.NewGuid():N}:")];
        var (first, platform, _) = await SettingsTestHost.StartAsync(_database.ConnectionString, configure: builder => builder.Configuration.AddInMemoryCollection(settings));
        var second = await BasicAppTestServer.StartAsync(_database.ConnectionString, settings: settings);
        return (first, second, platform);
    }

    private static HubConnection Connect(BasicAppTestServer server, string token) => new HubConnectionBuilder()
        .WithUrl("http://localhost" + RealtimeHubs.Notification, options =>
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

    private static System.Threading.Channels.Channel<JsonElement> Collect(HubConnection connection, string method)
    {
        var received = System.Threading.Channels.Channel.CreateUnbounded<JsonElement>();
        connection.On<JsonElement>(method, payload => received.Writer.TryWrite(payload));
        return received;
    }

    private static async Task DrainAsync(System.Threading.Channels.Channel<JsonElement> channel)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(300));
        while (channel.Reader.TryRead(out _))
        {
        }
    }

    private static async Task SaveSettingAsync(BasicAppTestServer server, string token, string theme)
    {
        var (saved, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, "/api/user-settings", $$"""{"scene":"Preference","key":"global","value":"{\"theme\":\"{{theme}}\"}","clientId":"tab-a"}""", token));
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
    }

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
}

/// <summary>
/// 用例载荷的源生成序列化上下文
/// </summary>
[System.Text.Json.Serialization.JsonSerializable(typeof(string))]
internal sealed partial class TestJsonContext : System.Text.Json.Serialization.JsonSerializerContext;
