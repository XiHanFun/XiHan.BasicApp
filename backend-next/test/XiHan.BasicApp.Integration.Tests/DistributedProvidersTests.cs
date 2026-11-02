// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using XiHan.BasicApp.Host;
using XiHan.BasicApp.Identity;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.BasicApp.Runtime;
using XiHan.Framework.Caching.Abstractions.Caches;
using XiHan.Framework.Caching.Abstractions.Stores;
using XiHan.Framework.Caching.Hybrid;
using XiHan.Framework.Caching.Redis;
using XiHan.Framework.Localization.Abstractions.Messages;
using XiHan.Framework.Localization.Database;
using XiHan.Framework.Localization.Database.Translations;
using XiHan.Framework.Messaging.Abstractions;
using XiHan.Framework.Messaging.Abstractions.Envelopes;
using XiHan.Framework.Messaging.Abstractions.Handlers;
using XiHan.Framework.Messaging.Abstractions.Publishing;
using XiHan.Framework.Messaging.InMemory;
using XiHan.Framework.Messaging.RabbitMQ;
using XiHan.Framework.Primitives;
using XiHan.Framework.Security.Abstractions.Permissions;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证宿主按配置装配的分布式 Provider。
/// </summary>
/// <remarks>
/// 没配连接时是内存实现、不映射 CloudEvents 入站端点；数据库里的译文覆盖嵌入的语言文件；入站端点打开后只收持有入站权限码的调用方。
/// 配了 Redis（环境变量 <c>XIHAN_TEST_REDIS</c>，如 <c>localhost:6379</c>）与 RabbitMQ（<c>XIHAN_TEST_RABBITMQ</c>，如 <c>amqp://guest:guest@localhost:5672</c>）时，
/// 两个宿主实例共享缓存、互相失效本机的 L1，一个实例发布的消息由另一个实例消费；没设环境变量即跳过。
/// </remarks>
public sealed class DistributedProvidersTests : IDisposable
{
    private const string RedisVariable = "XIHAN_TEST_REDIS";
    private const string RabbitMQVariable = "XIHAN_TEST_RABBITMQ";
    private const string Topic = "basicapp.tests.ping";
    private const string CacheBinding = "XiHan:Bindings:cache:value";
    private const string QueueBinding = "XiHan:Bindings:queue:value";
    private readonly SqliteDatabase _first = new();
    private readonly SqliteDatabase _second = new();

    public void Dispose()
    {
        _first.Dispose();
        _second.Dispose();
    }

    [Fact]
    public async Task WithoutConnections_RunsOnInMemoryProviders_AndMapsNoCloudEventsEndpoint()
    {
        // 显式清空两个绑定：跑全量时环境变量里配了 Redis / RabbitMQ 也不影响这一例
        await using var server = await BasicAppTestServer.StartAsync(_first.ConnectionString, settings: [new(CacheBinding, ""), new(QueueBinding, "")]);
        Assert.Null(server.Services.GetService<RedisCacheStore>());
        Assert.Null(server.Services.GetService<HybridCache>());
        Assert.IsType<InMemoryMessageBroker>(server.Services.GetRequiredService<IMessageBroker>());
        Assert.IsType<DatabaseMessageLocalizer>(server.Services.GetRequiredService<IMessageLocalizer>());

        var health = await HealthAsync(server);
        Assert.DoesNotContain(RedisCachingFeature.HealthName, health.Keys);
        Assert.DoesNotContain(RabbitMQMessagingFeature.HealthName, health.Keys);
        Assert.Contains(DatabaseLocalizationFeature.HealthName, health.Keys);

        var (response, _) = await server.SendAsync(CloudEvent(TestTokens.Issue("svc", null)));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DatabaseTranslation_OverridesTheEmbeddedText_ForItsLocaleOnly()
    {
        await using var server = await BasicAppTestServer.StartAsync(_first.ConnectionString);
        var english = await LoginFailureAsync(server, "en-US");
        Assert.Equal("用户名或密码不正确。", await LoginFailureAsync(server, "zh-CN"));

        // 译文写在调用的工作单元里，提交之后重载快照才生效
        var written = await server.InvokeAsync(async ctx =>
        {
            var results = await ctx.Services.GetRequiredService<MessageTranslationStore>().UpsertAsync([new MessageTranslationEntry(IdentityMessages.InvalidCredentials, "zh-CN", "账号或密码有误，请重试。")], ctx.CancellationToken);
            return Assert.Single(results).Outcome;
        });
        written.ThrowIfFailure();
        (await server.Services.GetRequiredService<MessageTranslationLoader>().ReloadAsync(TestContext.Current.CancellationToken)).ThrowIfFailure();

        Assert.Equal("账号或密码有误，请重试。", await LoginFailureAsync(server, "zh-CN"));
        Assert.Equal(english, await LoginFailureAsync(server, "en-US"));
    }

    [Fact]
    public async Task CloudEvents_WhenEnabled_AcceptsOnlyCallersHoldingTheInboundPermission_AndDispatchesToHandlers()
    {
        var received = new ConcurrentQueue<MessageDelivery>();
        await using var server = await BasicAppTestServer.StartAsync(
            _first.ConnectionString,
            settings: [new($"{CloudEventsSettings.ConfigurationSection}:Enabled", "true")],
            configure: builder => builder.Services.AddSingleton(received).AddMessageHandler<RecordingHandler>(Topic));

        // 入站权限码只授给平台的服务角色
        await server.SeedAsync([], [new PermissionGrant(GrantSubject.ForRole("event-relay"), RuntimePermissions.CloudEventsDeliver)]);

        var (anonymous, anonymousBody) = await server.SendAsync(CloudEvent(token: null));
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.Equal("SECURITY_DENIED_001", anonymousBody.GetProperty("code").GetString());
        Assert.Empty(received);

        // 登录过的普通用户：没有入站权限码即 403，事件不进处理器
        var (denied, deniedBody) = await server.SendAsync(CloudEvent(TestTokens.Issue("alice", null)));
        Assert.Equal((HttpStatusCode.Forbidden, "SECURITY_DENIED_004"), (denied.StatusCode, deniedBody.GetProperty("code").GetString()));
        Assert.Empty(received);

        var (accepted, _) = await server.SendAsync(CloudEvent(TestTokens.Issue("svc", null, "event-relay")));
        Assert.Equal(HttpStatusCode.Accepted, accepted.StatusCode);
        Assert.Equal(Topic, Assert.Single(received).Topic);
    }

    [Fact]
    public async Task Redis_IsSharedByInstances_AndInvalidatesTheirLocalCopies()
    {
        var redis = Require(RedisVariable);
        var prefix = $"xihan-basicapp-test-{Guid.NewGuid():N}:";
        KeyValuePair<string, string?>[] settings = [new(CacheBinding, redis), new("XiHan:Caching:Redis:KeyPrefix", prefix), new(QueueBinding, "")];
        await using var first = await BasicAppTestServer.StartAsync(_first.ConnectionString, settings: settings);
        await using var second = await BasicAppTestServer.StartAsync(_second.ConnectionString, settings: settings);
        foreach (var server in new[] { first, second })
        {
            Assert.NotNull(server.Services.GetService<RedisCacheStore>());
            Assert.IsType<HybridCache>(server.Services.GetRequiredService<ICache>());
            await EventuallyAsync(async () => (await HealthAsync(server)).GetValueOrDefault(RedisCachingFeature.HealthName) == "Ready");
        }

        // 一个实例写、另一个实例读，值落在 Redis 里（带键前缀）
        var shared = new CacheKey("tests:shared");
        var options = CacheEntryOptions.Expires(TimeSpan.FromMinutes(1));
        await first.Services.GetRequiredService<ISharedCache>().SetAsync(shared, "from-first", options, CancellationToken.None);
        Assert.Equal("from-first", (await second.Services.GetRequiredService<ISharedCache>().GetAsync<string>(shared, CancellationToken.None)).ValueOrThrow());
        Assert.True(await first.Services.GetRequiredService<IConnectionMultiplexer>().GetDatabase().KeyExistsAsync(prefix + shared.Value));

        // 两级缓存：第二个实例先读进本机 L1，第一个实例改写后经 Redis 的变更频道失效它
        var local = new CacheKey("tests:local");
        var firstCache = first.Services.GetRequiredService<ICache>();
        var secondCache = second.Services.GetRequiredService<ICache>();
        await firstCache.SetAsync(local, "v1", options, CancellationToken.None);
        Assert.Equal("v1", (await secondCache.GetAsync<string>(local, CancellationToken.None)).ValueOrThrow());
        await firstCache.SetAsync(local, "v2", options, CancellationToken.None);
        await EventuallyAsync(async () => (await secondCache.GetAsync<string>(local, CancellationToken.None)).ValueOr(string.Empty) == "v2");
    }

    [Fact]
    public async Task RabbitMQ_DeliversWhatOneInstancePublishes_ToAnother()
    {
        var rabbit = Require(RabbitMQVariable);
        KeyValuePair<string, string?>[] settings = [new(QueueBinding, rabbit), new("XiHan:Messaging:RabbitMQ:QueuePrefix", $"xihan-basicapp-test-{Guid.NewGuid():N}."), new(CacheBinding, "")];
        var received = new ConcurrentQueue<MessageDelivery>();
        await using var publisher = await BasicAppTestServer.StartAsync(_first.ConnectionString, settings: settings);
        await using var consumer = await BasicAppTestServer.StartAsync(
            _second.ConnectionString,
            settings: settings,
            configure: builder => builder.Services.AddSingleton(received).AddMessageHandler<RecordingHandler>(Topic));
        foreach (var server in new[] { publisher, consumer })
        {
            Assert.IsType<RabbitMQMessageBroker>(server.Services.GetRequiredService<IMessageBroker>());
            await EventuallyAsync(async () => (await HealthAsync(server)).GetValueOrDefault(RabbitMQMessagingFeature.HealthName) == "Ready");
        }

        // 调用里发布的先进发件箱、随调用提交，再由发件箱的后台循环经 RabbitMQ 发出；调用之外的直接发给代理
        (await publisher.InvokeAsync(async ctx => await ctx.Services.GetRequiredService<IMessagePublisher>().PublishAsync(Topic, Envelope("in-call"), ctx.CancellationToken))).ThrowIfFailure();
        await using (var scope = publisher.Services.CreateAsyncScope())
        {
            (await scope.ServiceProvider.GetRequiredService<IMessagePublisher>().PublishAsync(Topic, Envelope("direct"), CancellationToken.None)).ThrowIfFailure();
        }

        await EventuallyAsync(() => Task.FromResult(received.Count >= 2));
        Assert.Equal(["direct", "in-call"], received.Select(static d => d.Envelope.Id).Order(StringComparer.Ordinal));
    }

    private static string Require(string variable)
    {
        var value = Environment.GetEnvironmentVariable(variable);
        Assert.SkipWhen(string.IsNullOrWhiteSpace(value), $"{variable} is not set; this case only runs against a real server.");
        return value!;
    }

    private static async Task<Dictionary<string, string>> HealthAsync(BasicAppTestServer server)
    {
        var (_, body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/health/ready"));
        return body.GetProperty("contributions").EnumerateArray().ToDictionary(static c => c.GetProperty("id").GetString()!, static c => c.GetProperty("state").GetString()!, StringComparer.Ordinal);
    }

    private static async Task<string> LoginFailureAsync(BasicAppTestServer server, string locale)
    {
        var body = JsonSerializer.Serialize(new { userName = $"ghost-{Guid.NewGuid():N}", password = "wrong-12345" });
        var (response, problem) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:login", body, acceptLanguage: locale));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("IDENTITY_DENIED_003", problem.GetProperty("code").GetString());
        return problem.GetProperty("detail").GetString()!;
    }

    private static HttpRequestMessage CloudEvent(string? token)
    {
        var request = BasicAppTestServer.Request(HttpMethod.Post, CloudEventsSettings.Path, token: token);
        var json = JsonSerializer.Serialize(new { specversion = "1.0", id = Guid.NewGuid().ToString("N"), source = "/tests", type = Topic, datacontenttype = "application/json", data = new { ping = 1 } });
        request.Content = new StringContent(json, Encoding.UTF8, "application/cloudevents+json");
        return request;
    }

    private static MessageEnvelope Envelope(string id) => new()
    {
        Id = id,
        Source = "/tests",
        Type = Topic,
        DataContentType = "application/json",
        Data = Encoding.UTF8.GetBytes("""{"ping":1}"""),
    };

    private static async Task EventuallyAsync(Func<Task<bool>> condition)
    {
        for (var attempt = 0; attempt < 150; attempt++)
        {
            if (await condition())
            {
                return;
            }

            await Task.Delay(100, TestContext.Current.CancellationToken);
        }

        Assert.Fail("The condition was not met within 15 seconds.");
    }

    /// <summary>
    /// 把收到的消息记进共享队列的测试处理器。
    /// </summary>
    private sealed class RecordingHandler(ConcurrentQueue<MessageDelivery> received) : IMessageHandler
    {
        /// <inheritdoc />
        public ValueTask<Result> HandleAsync(MessageDelivery delivery, InvocationContext context, CancellationToken cancellationToken)
        {
            received.Enqueue(delivery);
            return ValueTask.FromResult(Result.Success());
        }
    }
}
