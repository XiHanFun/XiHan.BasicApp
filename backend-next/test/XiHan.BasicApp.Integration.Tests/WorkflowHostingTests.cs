// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Auditing.Domain.Services;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.Framework.Auditing.Database.Trail;
using XiHan.Framework.Messaging.Abstractions;
using XiHan.Framework.Messaging.Abstractions.Handlers;
using XiHan.Framework.Primitives;
using XiHan.Framework.Workflow.Abstractions.Definitions;
using XiHan.Framework.Workflow.Abstractions.Engine;
using XiHan.Framework.Workflow.Abstractions.Events;
using XiHan.Framework.Workflow.Abstractions.Instances;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 宿主装上数据库工作流之后的两项先行核对（方案 N3 第三批第一步）
/// </summary>
/// <remarks>
/// 一是框架在调用之外（到期书签的轮询）自己经完整管道开的调用：没有主体、没有操作描述，BasicApp 的会话门禁、访问策略、字段安全、
/// 审计与实时推送都要放行它，且不记进访问日志、异常日志与操作日志；二是 <c>workflow.*</c> 事件写进调用方事务里的发件箱，
/// 随调用提交才发出、回滚即不发，经内存代理与 RabbitMQ（环境变量 <c>XIHAN_TEST_RABBITMQ</c>，没设即跳过）都收得到
/// </remarks>
public sealed class WorkflowHostingTests : IDisposable
{
    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task FrameworkInvocations_PassTheHostPipeline_AndStayOutOfTheAppLogs()
    {
        var clock = new ManualClock(DateTimeOffset.UtcNow);
        var (server, platform, tenant) = await SettingsTestHost.StartAsync(_database.ConnectionString, configure: builder => builder.Services.AddSingleton<TimeProvider>(clock));
        await using var running = server;

        // 平台与租户两个分区各起一个停在延时上的实例
        var started = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var partition in new[] { "platform", "contoso" })
        {
            started[partition] = await SettingsTestHost.InvokeAsync(server, partition == "platform" ? null : partition, async ctx =>
            {
                await PublishAsync(ctx, WorkflowDefinitionBuilder.Create("hosting-delay").AddStart().AddDelay("wait", TimeSpan.FromMinutes(1)).AddEnd().AddTransition("start", "wait").AddTransition("wait", "end").Build());
                var instance = (await ctx.Services.GetRequiredService<IWorkflowEngine>().StartAsync(new WorkflowStartRequest { Name = "hosting-delay" }, ctx.CancellationToken)).ValueOrThrow();
                Assert.Equal(WorkflowInstanceStatus.Running, instance.Status);
                return instance.Id;
            });
        }

        // 到期之后在调用之外轮询：引擎为每个分区自己开调用，经宿主的全部中间件
        clock.Set(clock.GetUtcNow().AddMinutes(2));
        var poller = server.Services.GetRequiredService<DueBookmarkPoller>();
        foreach (var (partition, id) in started)
        {
            await EventuallyAsync(async () =>
            {
                await poller.PollOnceAsync(100, CancellationToken.None);
                var status = await SettingsTestHost.InvokeAsync(server, partition == "platform" ? null : partition, async ctx =>
                    (await ctx.Services.GetRequiredService<IWorkflowEngine>().FindInstanceAsync(id, ctx.CancellationToken)).ValueOrThrow().ValueOrThrow().Status);
                return status == WorkflowInstanceStatus.Completed;
            });
        }

        // 框架自己的调用不进 BasicApp 的访问日志、异常日志与操作日志（框架的审计记录照记，目标是 workflow.poll.*）
        await server.Services.GetRequiredService<DatabaseAuditTrail>().FlushAsync(CancellationToken.None);
        await server.Services.GetRequiredService<AuditLogWriter>().FlushAsync(CancellationToken.None);
        foreach (var token in new[] { platform, tenant })
        {
            foreach (var resource in new[] { "access-logs", "exception-logs", "operation-logs" })
            {
                Assert.DoesNotContain(await ListAsync(server, resource, token), entry => entry.ToString().Contains("workflow", StringComparison.OrdinalIgnoreCase));
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WorkflowEvents_LeaveThroughTheOutbox_OnlyWhenTheCallCommits(bool rabbit)
    {
        List<KeyValuePair<string, string?>> settings = [];
        if (rabbit)
        {
            var connection = Environment.GetEnvironmentVariable("XIHAN_TEST_RABBITMQ");
            Assert.SkipWhen(string.IsNullOrWhiteSpace(connection), "XIHAN_TEST_RABBITMQ is not set; this case only runs against a real server.");
            settings.Add(new("XiHan:Bindings:queue:value", connection));
        }

        var received = new ConcurrentQueue<MessageDelivery>();
        await using var server = await BasicAppTestServer.StartAsync(
            _database.ConnectionString,
            settings: settings,
            configure: builder => builder.Services.AddSingleton(received).AddMessageHandler<RecordingHandler>(WorkflowTopics.InstanceCompleted));
        (await server.InvokeAsync(async ctx =>
        {
            await PublishAsync(ctx, WorkflowDefinitionBuilder.Create("hosting-instant").AddStart().AddEnd().AddTransition("start", "end").Build());
            return Result.Success();
        })).ThrowIfFailure();

        // 发起即完成：提交的调用发出 workflow.instance.completed，回滚的调用连实例带事件都不留
        string? committed = null;
        string? rolledBack = null;
        (await server.InvokeAsync(async ctx =>
        {
            committed = (await ctx.Services.GetRequiredService<IWorkflowEngine>().StartAsync(new WorkflowStartRequest { Name = "hosting-instant" }, ctx.CancellationToken)).ValueOrThrow().Id;
            return Result.Success();
        })).ThrowIfFailure();
        var failed = await server.InvokeAsync(async ctx =>
        {
            rolledBack = (await ctx.Services.GetRequiredService<IWorkflowEngine>().StartAsync(new WorkflowStartRequest { Name = "hosting-instant" }, ctx.CancellationToken)).ValueOrThrow().Id;
            // 业务在流程操作之后失败：整个调用回滚
            return Error.Validation("IDENTITY_VALIDATION_001", "identity.user.tenant_required");
        });
        Assert.True(failed.IsFailure);

        await EventuallyAsync(() => Task.FromResult(received.Any(d => d.Envelope.Subject == committed)));
        var completed = Assert.Single(received, d => d.Envelope.Subject == committed);
        Assert.Equal(committed, JsonDocument.Parse(completed.Envelope.Data.ToArray()).RootElement.GetProperty("instanceId").GetString());
        // 再给发件箱一个派发周期：回滚的实例始终没有事件
        await Task.Delay(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        Assert.DoesNotContain(received, d => d.Envelope.Subject == rolledBack);
        var kept = await SettingsTestHost.InvokeAsync(server, null, async ctx =>
            (await ctx.Services.GetRequiredService<IWorkflowEngine>().FindInstanceAsync(rolledBack!, ctx.CancellationToken)).ValueOrThrow().HasValue);
        Assert.False(kept);
    }

    private static async Task PublishAsync(InvocationContext context, WorkflowDefinition definition)
    {
        var catalog = context.Services.GetRequiredService<IWorkflowDefinitionCatalog>();
        var draft = (await catalog.CreateAsync(definition, context.CancellationToken)).ValueOrThrow();
        (await catalog.PublishAsync(draft.Id, context.CancellationToken)).ValueOrThrow();
    }

    private static async Task<List<JsonElement>> ListAsync(BasicAppTestServer server, string resource, string token)
    {
        var (response, json) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/{resource}:list", """{"page":{"index":1,"size":200}}""", token));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return json.GetProperty("items").EnumerateArray().ToList();
    }

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
    /// 把收到的消息记进共享队列的测试处理器
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
