// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Auditing.Domain.Services;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.Framework.Auditing.Database.Trail;
using XiHan.Framework.Kernel.Hosting;
using XiHan.Framework.Kernel.Pipeline;
using XiHan.Framework.Primitives;
using XiHan.Framework.Workflow.Abstractions.Definitions;
using XiHan.Framework.Workflow.Abstractions.Engine;
using XiHan.Framework.Workflow.Abstractions.Instances;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证访问策略只管外部协议
/// </summary>
/// <remarks>
/// 平台层一条拒绝策略拦下除测试客户端外的一切调用：内部调用（作业、消息处理、工作流）都没有 User-Agent。
/// 它们照常运行、不写条件命中；HTTP、gRPC、MCP 照常求值，没有来源地址的 HTTP 请求也一样
/// </remarks>
public sealed class AccessPolicyProtocolsTests : IDisposable
{
    private const string TestClient = "xihan-test-client";
    private const string DenyAll = "deny-all";

    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private static HttpRequestMessage Request(HttpMethod method, string path, string? json, string? token, string? userAgent = TestClient)
    {
        var request = BasicAppTestServer.Request(method, path, json, token);
        if (userAgent is not null)
        {
            request.Headers.TryAddWithoutValidation("User-Agent", userAgent);
        }

        return request;
    }

    private static async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(BasicAppTestServer server, HttpMethod method, string path, object? body, string? token, string? userAgent = TestClient)
    {
        var (response, json) = await server.SendAsync(Request(method, path, body is null ? null : JsonSerializer.Serialize(body), token, userAgent));
        return (response.StatusCode, json);
    }

    /// <summary>
    /// 拦下除测试客户端外的一切调用（测试客户端留着才改得了策略、看得了结果）
    /// </summary>
    private static async Task DenyAllButTheTestClientAsync(BasicAppTestServer server, string platform)
    {
        var conditions = new object[]
        {
            new { groupNo = 0, attributeName = "environment.client.userAgent", @operator = "IsNull" },
            new { groupNo = 1, attributeName = "environment.client.userAgent", @operator = "NotEquals", value = TestClient },
        };
        var (created, policy) = await SendAsync(server, HttpMethod.Post, "/api/policies", new { code = DenyAll, name = DenyAll, conditions }, platform);
        Assert.True(created == HttpStatusCode.Created, policy.ToString());
        var (switched, switchedBody) = await SendAsync(server, HttpMethod.Post, $"/api/policies/{policy.GetProperty("id").GetString()}:set-action", new { action = "Deny" }, platform);
        Assert.True(switched == HttpStatusCode.OK, switchedBody.ToString());
    }

    private static async Task<List<JsonElement>> HitsAsync(BasicAppTestServer server, string platform, string policyCode)
    {
        await server.Services.GetRequiredService<DatabaseAuditTrail>().FlushAsync(CancellationToken.None);
        await server.Services.GetRequiredService<AuditLogWriter>().FlushAsync(CancellationToken.None);
        var (status, hits) = await SendAsync(server, HttpMethod.Post, "/api/condition-hits:list", new { page = new { index = 1, size = 50 }, filters = new[] { new { field = "policyCode", @operator = "eq", value = policyCode } } }, platform);
        Assert.True(status == HttpStatusCode.OK, hits.ToString());
        return [.. hits.GetProperty("items").EnumerateArray()];
    }

    private static async Task AssertNoHitsAsync(BasicAppTestServer server, string platform)
    {
        Assert.Empty(await HitsAsync(server, platform, DenyAll));

        // 测试宿主启动时的 HTTP 请求不带 User-Agent，会命中只记录的预设；内部调用没有 HTTP 方法，一条都不该有
        Assert.DoesNotContain(await HitsAsync(server, platform, "preset.block-scripts"), static h => h.GetProperty("method").ValueKind == JsonValueKind.Null);
    }

    private static async Task EventuallyAsync(Func<Task<bool>> condition, string what)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            if (await condition())
            {
                return;
            }

            await Task.Delay(200);
        }

        Assert.Fail(what);
    }

    [Fact]
    public async Task Jobs_RunUnderADenyPolicy_AndWriteNoHits()
    {
        var (server, platform, _) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;
        await DenyAllButTheTestClientAsync(server, platform);

        var (triggered, run) = await SendAsync(server, HttpMethod.Post, "/api/jobs/openplatform.oauth-cleanup:trigger", new { }, platform);
        Assert.True(triggered == HttpStatusCode.OK, run.ToString());
        Assert.False(string.IsNullOrEmpty(run.GetProperty("trigger").GetString()));
        var state = "(no run)";
        for (var attempt = 0; attempt < 150 && state is "(no run)" or "Running"; attempt++)
        {
            await Task.Delay(200, TestContext.Current.CancellationToken);
            var (_, runs) = await SendAsync(server, HttpMethod.Post, "/api/job-runs:list", new { filters = new[] { new { field = "job", @operator = "eq", value = "openplatform.oauth-cleanup" } } }, platform);
            var found = runs.GetProperty("items").EnumerateArray().FirstOrDefault();
            state = found.ValueKind == JsonValueKind.Undefined ? "(no run)" : found.GetProperty("state").GetString()!;
        }

        Assert.Equal("Succeeded", state);
        await AssertNoHitsAsync(server, platform);
    }

    [Fact]
    public async Task Messages_AreHandledUnderADenyPolicy_AndWriteNoHits()
    {
        var (server, platform, _) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;
        var (created, alice) = await SendAsync(server, HttpMethod.Post, "/api/users", new { userName = "alice", displayName = "Alice", password = "Pass@123456" }, platform);
        Assert.Equal(HttpStatusCode.Created, created);
        var aliceToken = (await AuthFlowTests.LoginAsync(server, "alice", "Pass@123456")).GetProperty("accessToken").GetString()!;
        var document = JsonSerializer.Serialize(new
        {
            id = "x",
            name = "protocols-flow",
            displayName = "Flow",
            category = "Tests",
            nodes = new object[]
            {
                new { id = "start", activityType = "Start" },
                new { id = "approve", activityType = "UserTask", properties = new { AssigneeRules = new[] { new { type = "User", values = new[] { alice.GetProperty("id").GetString() } } }, Title = "Review" } },
                new { id = "end", activityType = "End" },
            },
            transitions = new[] { new { id = "t1", sourceNodeId = "start", targetNodeId = "approve" }, new { id = "t2", sourceNodeId = "approve", targetNodeId = "end" } },
        });
        var (_, draft) = await SendAsync(server, HttpMethod.Post, "/api/workflow-definitions", new { document }, platform);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, $"/api/workflow-definitions/{draft.GetProperty("id").GetString()}:publish", new { }, platform)).Status);
        await DenyAllButTheTestClientAsync(server, platform);

        // 流程在 HTTP 请求里启动；提醒受理人的是发件箱派发的消息处理器
        var (started, instance) = await SendAsync(server, HttpMethod.Post, "/api/workflow-instances:start", new { name = "protocols-flow", displayName = "Flow" }, platform);
        Assert.True(started == HttpStatusCode.OK, instance.ToString());
        await EventuallyAsync(async () =>
        {
            var (_, inbox) = await SendAsync(server, HttpMethod.Post, "/api/inbox:list", new { page = new { index = 1, size = 20 } }, aliceToken);
            return inbox.GetProperty("items").GetArrayLength() > 0;
        }, "the message handler did not notify the assignee");
        await AssertNoHitsAsync(server, platform);
    }

    [Fact]
    public async Task WorkflowTimers_FireUnderADenyPolicy_AndWriteNoHits()
    {
        var clock = new ManualClock(DateTimeOffset.UtcNow);
        var (server, platform, _) = await SettingsTestHost.StartAsync(_database.ConnectionString, configure: builder => builder.Services.AddSingleton<TimeProvider>(clock));
        await using var running = server;

        // 策略生效之前先起一个停在延时上的实例
        var id = await SettingsTestHost.InvokeAsync(server, null, async ctx =>
        {
            var catalog = ctx.Services.GetRequiredService<IWorkflowDefinitionCatalog>();
            var definition = (await catalog.CreateAsync(WorkflowDefinitionBuilder.Create("protocols-delay").AddStart().AddDelay("wait", TimeSpan.FromMinutes(1)).AddEnd().AddTransition("start", "wait").AddTransition("wait", "end").Build(), ctx.CancellationToken)).ValueOrThrow();
            (await catalog.PublishAsync(definition.Id, ctx.CancellationToken)).ValueOrThrow();
            return (await ctx.Services.GetRequiredService<IWorkflowEngine>().StartAsync(new WorkflowStartRequest { Name = "protocols-delay" }, ctx.CancellationToken)).ValueOrThrow().Id;
        });
        await DenyAllButTheTestClientAsync(server, platform);

        clock.Set(clock.GetUtcNow().AddMinutes(2));
        var poller = server.Services.GetRequiredService<DueBookmarkPoller>();
        await poller.PollOnceAsync(100, CancellationToken.None);

        var (read, instance) = await SendAsync(server, HttpMethod.Get, $"/api/workflow-instances/{WorkflowIds(id)}", null, platform);
        Assert.True(read == HttpStatusCode.OK, instance.ToString());
        Assert.Equal(nameof(WorkflowInstanceStatus.Completed), instance.GetProperty("instance").GetProperty("status").GetString());
        await AssertNoHitsAsync(server, platform);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(InvocationProtocols.Messaging, true)]
    [InlineData(InvocationProtocols.Job, true)]
    [InlineData(InvocationProtocols.Workflow, true)]
    [InlineData(InvocationProtocols.CommandLine, true)]
    [InlineData(InvocationProtocols.Http, false)]
    [InlineData(InvocationProtocols.Grpc, false)]
    [InlineData(InvocationProtocols.Mcp, false)]
    public async Task Policies_AreEvaluatedByProtocol(string? protocol, bool passes)
    {
        var (server, platform, _) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;
        await DenyAllButTheTestClientAsync(server, platform);

        await using var scope = server.Services.GetRequiredService<IServiceScopeFactory>().CreateAsyncScope();
        var context = App.CreateContext(scope.ServiceProvider, CancellationToken.None);
        if (protocol is not null)
        {
            context.SetOrigin(new InvocationOrigin { Protocol = protocol, Target = "test" });
        }

        var outcome = await server.Services.GetRequiredService<App>().InvokeAsync(context, static ctx =>
        {
            ctx.SetOutcome(Result.Success());
            return ValueTask.CompletedTask;
        });
        if (passes)
        {
            Assert.True(outcome.IsSuccess, outcome.Error?.Code);
        }
        else
        {
            Assert.Equal("ACCESS_DENIED_004", outcome.Error?.Code);
        }
    }

    [Fact]
    public async Task ExternalRequests_StillFaceThePolicies_WithoutAnAddressToo()
    {
        var (server, platform, _) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;

        // 缺省的只记录预设：不带 User-Agent 的 HTTP 请求照样命中，而且这个请求没有来源地址（测试宿主没有）
        var (plain, plainBody) = await SendAsync(server, HttpMethod.Get, "/api/auth:me", null, platform, userAgent: null);
        Assert.True(plain == HttpStatusCode.OK, plainBody.ToString());
        var hit = Assert.Single(await HitsAsync(server, platform, "preset.block-scripts"), static h => h.GetProperty("operationId").GetString() == "auth.me");
        Assert.True(string.IsNullOrEmpty(hit.GetProperty("clientIp").GetString()), hit.ToString());
        Assert.False(hit.GetProperty("isBlocked").GetBoolean());

        // 拒绝策略照样拦 HTTP
        await DenyAllButTheTestClientAsync(server, platform);
        var (denied, deniedBody) = await SendAsync(server, HttpMethod.Get, "/api/auth:me", null, platform, userAgent: null);
        Assert.Equal(HttpStatusCode.Forbidden, denied);
        Assert.Equal("ACCESS_DENIED_004", deniedBody.GetProperty("code").GetString());
        var (other, otherBody) = await SendAsync(server, HttpMethod.Get, "/api/auth:me", null, platform, userAgent: "curl/8.0");
        Assert.Equal(HttpStatusCode.Forbidden, other);
        Assert.Equal("ACCESS_DENIED_004", otherBody.GetProperty("code").GetString());
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Get, "/api/auth:me", null, platform)).Status);

        // MCP 走 HTTP 传输：外层 /mcp 是 http，先被拦；放过的 tools/call 是自己的一次调用，协议是 mcp，User-Agent 沿用外层请求
        const string Call = """{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"users_list","arguments":{"page":{"index":1,"size":10}}}}""";
        var (mcpResponse, mcpDenied) = await server.SendAsync(Request(HttpMethod.Post, "/mcp", Call, platform, userAgent: null));
        Assert.Equal(HttpStatusCode.Forbidden, mcpResponse.StatusCode);
        Assert.Equal("ACCESS_DENIED_004", mcpDenied.GetProperty("code").GetString());
        var (_, mcpAllowed) = await server.SendAsync(Request(HttpMethod.Post, "/mcp", Call, platform));
        Assert.False(mcpAllowed.GetProperty("result").TryGetProperty("isError", out var error) && error.GetBoolean(), mcpAllowed.ToString());
    }

    private static string WorkflowIds(string engineId)
        => XiHan.BasicApp.Workflow.Domain.Services.WorkflowIds.ToContract(engineId).ToString("D");
}
