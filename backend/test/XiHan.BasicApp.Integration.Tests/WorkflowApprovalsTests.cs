// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts.Users;
using XiHan.BasicApp.Contracts.Workflow;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.BasicApp.Workflow.Domain.Entities;
using XiHan.BasicApp.Workflow.Domain.Services;
using XiHan.BasicApp.Workflow.Handlers;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Messaging.Abstractions.Envelopes;
using XiHan.Framework.Messaging.Abstractions.Handlers;
using XiHan.Framework.Primitives;
using XiHan.Framework.Workflow.Abstractions.Engine;
using XiHan.Framework.Workflow.Abstractions.Events;
using XiHan.Framework.Workflow.Abstractions.Instances;
using XiHan.Framework.Workflow.Abstractions.Serialization;
using XiHan.Framework.Workflow.Abstractions.Storage;
using XiHan.Framework.Workflow.Abstractions.Tasks;
using XiHan.Framework.Workflow.Abstractions;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 待办、审批中心与审批业务（N3 第三批之四）
/// </summary>
/// <remarks>
/// 平台一侧：办理、转办、加签与审批记录，我已审批 / 抄送我的 / 我发起的，详情按参与关系鉴权与审批轨迹，撤回与中止回写，通知与按消息去重，工作台的「我的待办」，审批业务服务；
/// 演示租户：四个演示流程跑到结论（采购另走一遍节点超时）
/// </remarks>
public sealed class WorkflowApprovalsTests : IDisposable
{
    // 书签还在、实例行没了：框架把这条待办当作不在运行，不进列表
    private const int MissingInstanceRows = 0;

    private const string Approval = """
        {
          "id": "x", "name": "tests-business", "displayName": "Approval", "category": "Tests",
          "nodes": [
            { "id": "start", "activityType": "Start" },
            { "id": "approve", "activityType": "UserTask", "displayName": "Manager review", "properties": { "AssigneeRules": [{ "type": "User", "values": ["ASSIGNEE"] }], "CcUserIds": ["CC"], "Title": "Review request" } },
            { "id": "approved", "activityType": "End", "properties": { "Outcome": "approved" } },
            { "id": "rejected", "activityType": "End", "properties": { "Outcome": "rejected" } }
          ],
          "transitions": [
            { "id": "t1", "sourceNodeId": "start", "targetNodeId": "approve" },
            { "id": "t2", "sourceNodeId": "approve", "targetNodeId": "approved", "condition": "outcome == 'approved'" },
            { "id": "t3", "sourceNodeId": "approve", "targetNodeId": "rejected", "isDefault": true }
          ]
        }
        """;

    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task TaskList_ReadsInstancesOfTheCallersPartitionOnly()
    {
        var (server, platform, tenant) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;

        // 平台与租户各起一个流程，受理人是发起人自己
        async Task<(string Id, string TaskId)> StartAsync(string token, string assignee)
        {
            var document = Approval.Replace("ASSIGNEE", assignee, StringComparison.Ordinal).Replace("CC", assignee, StringComparison.Ordinal);
            var (drafted, draft) = await SendAsync(server, HttpMethod.Post, "/api/workflow-definitions", Json(new { document }), token);
            Assert.True(drafted == HttpStatusCode.Created, draft.ToString());
            Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, $"/api/workflow-definitions/{draft.GetProperty("id").GetString()}:publish", "{}", token)).Status);
            var (started, instance) = await SendAsync(server, HttpMethod.Post, "/api/workflow-instances:start", Json(new { name = "tests-business", displayName = "Partitioned" }), token);
            Assert.True(started == HttpStatusCode.OK, instance.ToString());
            return (instance.GetProperty("id").GetString()!, Assert.Single(await ListAsync(server, "tasks", token)).GetProperty("id").GetString()!);
        }

        async Task<string> MeAsync(string token) => (await SendAsync(server, HttpMethod.Get, "/api/auth:me", null, token)).Body.GetProperty("user").GetProperty("id").GetString()!;
        var (platformInstance, _) = await StartAsync(platform, await MeAsync(platform));
        var (tenantInstance, _) = await StartAsync(tenant, await MeAsync(tenant));

        // 节点名来自实例的定义：取得到实例才有显示名
        foreach (var (token, instance) in new[] { (platform, platformInstance), (tenant, tenantInstance) })
        {
            var todo = Assert.Single(await ListAsync(server, "tasks", token));
            Assert.Equal((instance, "Manager review"), (todo.GetProperty("instanceId").GetString(), todo.GetProperty("nodeName").GetString()));
        }

        // 别的分区的实例取不到
        var engineIds = new[] { platformInstance, tenantInstance }.Select(static id => WorkflowIds.ToEngine(Guid.Parse(id))).ToList();
        var seen = await SettingsTestHost.InvokeAsync(server, "contoso", async ctx =>
            (await ctx.Services.GetRequiredService<IWorkflowInstanceStore>().FindManyAsync(ctx.GetSecurity().Tenant?.Id, engineIds, ctx.CancellationToken)).ValueOrThrow().Select(static i => i.Id).ToList());
        Assert.Equal([engineIds[1]], seen);

        // 实例行缺失时列表还在：节点名退回节点 ID
        await using (var connection = new Microsoft.Data.Sqlite.SqliteConnection(_database.ConnectionString))
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM workflow_instance_record WHERE id = $id";
            command.Parameters.AddWithValue("$id", engineIds[1]);
            Assert.Equal(1, await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken));
        }

        var (listed, afterDelete) = await SendAsync(server, HttpMethod.Post, "/api/tasks:list", """{"page":{"index":1,"size":50}}""", tenant);
        Assert.True(listed == HttpStatusCode.OK, afterDelete.ToString());
        Assert.Equal(MissingInstanceRows, afterDelete.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task Tasks_ApprovalCenter_AndBusinessCallbacks_FollowParticipation()
    {
        var log = new BusinessLog();
        var (server, platform, _) = await SettingsTestHost.StartAsync(_database.ConnectionString, configure: builder =>
        {
            builder.Services.AddSingleton(log);
            builder.Services.AddWorkflowBusiness<RecordingBusiness>();
            builder.Services.AddWorkflowBusiness<DefaultedBusiness>();
        });
        await using var running = server;
        var ids = new Dictionary<string, string>(StringComparer.Ordinal);
        var tokens = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var name in new[] { "alice", "bob", "carol", "dave" })
        {
            var (created, user) = await SendAsync(server, HttpMethod.Post, "/api/users", $$"""{"userName":"{{name}}","displayName":"{{char.ToUpperInvariant(name[0]) + name[1..]}}","password":"Pass@123456"}""", platform);
            Assert.Equal(HttpStatusCode.Created, created);
            ids[name] = user.GetProperty("id").GetString()!;
            tokens[name] = (await AuthFlowTests.LoginAsync(server, name, "Pass@123456")).GetProperty("accessToken").GetString()!;
        }

        var (_, draft) = await SendAsync(server, HttpMethod.Post, "/api/workflow-definitions", Json(new { document = Approval.Replace("ASSIGNEE", ids["alice"], StringComparison.Ordinal).Replace("CC", ids["carol"], StringComparison.Ordinal) }), platform);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, $"/api/workflow-definitions/{draft.GetProperty("id").GetString()}:publish", "{}", platform)).Status);
        async Task<string> StartAsync(string correlation)
        {
            var (started, instance) = await SendAsync(server, HttpMethod.Post, "/api/workflow-instances:start", Json(new { name = "tests-business", displayName = $"Purchase {correlation}", correlationId = correlation }), platform);
            Assert.Equal(HttpStatusCode.OK, started);
            return instance.GetProperty("id").GetString()!;
        }

        // 提醒按收件人偏好里的界面语言出文案：alice 存了日文，carol 没存，按这次调用的区域（没带即缺省英文）
        var (savedLocale, _) = await SendAsync(server, HttpMethod.Put, "/api/user-settings", Json(new { scene = "Preference", key = "global", value = """{"xihan_locale":"ja-JP","theme":"dark"}""", clientId = "tests" }), tokens["alice"]);
        Assert.Equal(HttpStatusCode.OK, savedLocale);
        var first = await StartAsync("doc-1");

        // 我的待办：受理人锁定为调用方，带节点、发起人与审批业务；关键字也匹配关联 ID
        var todo = Assert.Single(await ListAsync(server, "tasks", tokens["alice"]));
        Assert.Equal((first, "Manager review", "Review request", "超级管理员", RecordingBusiness.NameKey), (todo.GetProperty("instanceId").GetString(), todo.GetProperty("nodeName").GetString(), todo.GetProperty("title").GetString(), todo.GetProperty("starterName").GetString(), todo.GetProperty("businessNameKey").GetString()));
        Assert.Single(await ListAsync(server, "tasks", tokens["alice"], """{"keyword":"doc-1"}"""));
        Assert.Empty(await ListAsync(server, "tasks", tokens["bob"]));
        var taskId = todo.GetProperty("id").GetString()!;

        // 抄送：处理器写抄送记录、给被抄送人发站内信；受理人收到待办
        await EventuallyAsync(async () => (await ListAsync(server, "approvals", tokens["carol"], suffix: "list-cc")).Count == 1);
        await EventuallyAsync(async () => (await InboxAsync(server, tokens["alice"])).Any(static i => i.GetProperty("link").GetString()!.StartsWith("/workflow/approvals?tab=todo", StringComparison.Ordinal)));
        await EventuallyAsync(async () => (await InboxAsync(server, tokens["carol"])).Any(static i => i.GetProperty("link").GetString() == "/workflow/approvals?tab=cc"));
        Assert.Equal("承認待ち：Review request", (await InboxAsync(server, tokens["alice"])).Single(static i => i.GetProperty("link").GetString()!.StartsWith("/workflow/approvals?tab=todo", StringComparison.Ordinal)).GetProperty("title").GetString());
        Assert.StartsWith("CC: ", (await InboxAsync(server, tokens["carol"])).Single(static i => i.GetProperty("link").GetString() == "/workflow/approvals?tab=cc").GetProperty("title").GetString(), StringComparison.Ordinal);

        // 详情按参与关系：受理人、被抄送人、发起人能看，别人 403
        Assert.Equal((HttpStatusCode.Forbidden, "WORKFLOW_DENIED_101"), Code(await SendAsync(server, HttpMethod.Get, $"/api/approvals/{first}", null, tokens["bob"])));
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Get, $"/api/approvals/{first}", null, tokens["carol"])).Status);
        var (_, detail) = await SendAsync(server, HttpMethod.Get, $"/api/approvals/{first}", null, tokens["alice"]);
        Assert.Equal((taskId, "Manager review"), (detail.GetProperty("task").GetProperty("taskId").GetString(), detail.GetProperty("task").GetProperty("nodeName").GetString()));
        Assert.Equal((RecordingBusiness.Code, "tests/form/index", "doc-1", """{"id":"doc-1"}"""), (detail.GetProperty("business").GetProperty("code").GetString(), detail.GetProperty("business").GetProperty("formComponent").GetString(), detail.GetProperty("business").GetProperty("businessId").GetString(), detail.GetProperty("business").GetProperty("data").GetString()));
        Assert.False(detail.GetProperty("canWithdraw").GetBoolean());

        // 办理他人的待办、转给停用的人都不行
        Assert.Equal((HttpStatusCode.NotFound, "WORKFLOW_NOTFOUND_103"), Code(await SendAsync(server, HttpMethod.Post, $"/api/tasks/{taskId}:complete", """{"outcome":"approved"}""", tokens["bob"])));
        await SendAsync(server, HttpMethod.Post, "/api/users:disable", Json(new { ids = new[] { ids["dave"] } }), platform);
        Assert.Equal((HttpStatusCode.BadRequest, "WORKFLOW_VALIDATION_106"), Code(await SendAsync(server, HttpMethod.Post, $"/api/tasks/{taskId}:transfer", Json(new { targetUserId = ids["dave"] }), tokens["alice"])));

        // 转办：待办换人，转办人有一条转办记录，新受理人收到通知
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, $"/api/tasks/{taskId}:transfer", Json(new { targetUserId = ids["bob"], comment = "please take it" }), tokens["alice"])).Status);
        Assert.Empty(await ListAsync(server, "tasks", tokens["alice"]));
        var transferred = Assert.Single(await ListAsync(server, "tasks", tokens["bob"]));
        var done = Assert.Single(await ListAsync(server, "approvals", tokens["alice"], suffix: "list-done"));
        Assert.Equal(("Transferred", "please take it", "Running"), (done.GetProperty("action").GetString(), done.GetProperty("comment").GetString(), done.GetProperty("instanceStatus").GetString()));
        await EventuallyAsync(async () => (await InboxAsync(server, tokens["bob"])).Any(static i => i.GetProperty("content").GetString()!.Contains("please take it", StringComparison.Ordinal)));

        // 业务的办理前校验拒绝时待办保留；通过后结论回写到业务、实例完成
        var bobTask = transferred.GetProperty("id").GetString()!;
        Assert.Equal((HttpStatusCode.BadRequest, "TESTS_VALIDATION_001"), Code(await SendAsync(server, HttpMethod.Post, $"/api/tasks/{bobTask}:complete", """{"outcome":"rejected","comment":"veto"}""", tokens["bob"])));
        Assert.Single(await ListAsync(server, "tasks", tokens["bob"]));
        var (completed, result) = await SendAsync(server, HttpMethod.Post, $"/api/tasks/{bobTask}:complete", """{"outcome":"approved","comment":"fine"}""", tokens["bob"]);
        Assert.Equal((HttpStatusCode.OK, "Completed"), (completed, result.GetProperty("instanceStatus").GetString()));
        Assert.Contains($"completing:doc-1:approved:{ids["bob"]}", log.Calls);
        Assert.Contains("outcome:doc-1:approved", log.Calls);

        // 审批轨迹：发起、抄送、转办、办理、结束
        var (_, finished) = await SendAsync(server, HttpMethod.Get, $"/api/approvals/{first}", null, platform);
        Assert.Equal(["Started", "CarbonCopied", "Transferred", "Completed", "Finished"], finished.GetProperty("timeline").EnumerateArray().Select(static t => t.GetProperty("kind").GetString()).ToList());
        Assert.Equal(["Bob"], finished.GetProperty("timeline")[2].GetProperty("targetUserNames").EnumerateArray().Select(static n => n.GetString()).ToList());
        Assert.Equal(JsonValueKind.Null, finished.GetProperty("task").ValueKind);

        // 我发起的：只有顶层实例，关键字匹配关联 ID；已结束的不能撤回
        var started = Assert.Single(await ListAsync(server, "approvals", platform, """{"keyword":"doc-1"}""", suffix: "list-started"));
        Assert.Equal(("Completed", "approved", RecordingBusiness.NameKey), (started.GetProperty("status").GetString(), started.GetProperty("outcome").GetString(), started.GetProperty("businessNameKey").GetString()));
        Assert.Equal((HttpStatusCode.Conflict, "WORKFLOW_CONFLICT_102"), Code(await SendAsync(server, HttpMethod.Post, $"/api/approvals/{first}:withdraw", "{}", platform)));

        // 撤回：只有发起人；撤回后业务收到中止回写
        var second = await StartAsync("doc-2");
        Assert.Equal((HttpStatusCode.Forbidden, "WORKFLOW_DENIED_102"), Code(await SendAsync(server, HttpMethod.Post, $"/api/approvals/{second}:withdraw", "{}", tokens["alice"])));
        var (withdrawn, after) = await SendAsync(server, HttpMethod.Post, $"/api/approvals/{second}:withdraw", "{}", platform);
        Assert.Equal((HttpStatusCode.OK, "Canceled"), (withdrawn, after.GetProperty("status").GetString()));
        Assert.Contains("aborted:doc-2:Canceled", log.Calls);

        // 管理员终止同样回写；工作台「我的待办」是待办数与最近几条
        var third = await StartAsync("doc-3");
        await EventuallyAsync(async () => (await WidgetAsync(server, tokens["alice"])).GetProperty("value").GetInt64() == 1);
        Assert.StartsWith($"/workflow/approvals?tab=todo&instanceId={third}&taskId=", (await WidgetAsync(server, tokens["alice"])).GetProperty("items")[0].GetProperty("link").GetString(), StringComparison.Ordinal);
        Assert.Empty((await SendAsync(server, HttpMethod.Post, "/api/workflow-instances:terminate", Json(new { ids = new[] { third }, reason = "stop" }), platform)).Body.GetProperty("failed").EnumerateArray());
        Assert.Contains("aborted:doc-3:Terminated", log.Calls);
        Assert.Equal(0, (await WidgetAsync(server, tokens["alice"])).GetProperty("value").GetInt64());

        // 审批业务服务：缺省定义按需建并发布，一张单据同时只有一个进行中的实例，进度按单据取最近一次，业务自己撤回不回调中止
        await SettingsTestHost.InvokeAsync(server, null, async ctx =>
        {
            var service = ctx.Services.GetRequiredService<IWorkflowBusinessService>();
            var defaulted = (await service.StartAsync(new WorkflowBusinessStartRequest(DefaultedBusiness.Code, "auto-1", "Auto", null), ctx)).ValueOrThrow();
            Assert.Equal((WorkflowBusinessStatus.Completed, "approved"), (defaulted.Status, defaulted.Outcome));
            Assert.Equal("WORKFLOW_VALIDATION_107", (await service.StartAsync(new WorkflowBusinessStartRequest("tests-nothing", "x", "X", null), ctx)).Error!.Code);

            Assert.Equal(WorkflowBusinessStatus.Running, (await service.StartAsync(new WorkflowBusinessStartRequest(RecordingBusiness.Code, "svc-1", "Service 1", null), ctx)).ValueOrThrow().Status);
            Assert.Equal("WORKFLOW_CONFLICT_005", (await service.StartAsync(new WorkflowBusinessStartRequest(RecordingBusiness.Code, "svc-1", "Service 1 again", null), ctx)).Error!.Code);
            var progress = (await service.ProgressAsync(RecordingBusiness.Code, ["svc-1", "doc-2", "missing"], ctx)).ValueOrThrow();
            Assert.Equal(["doc-2", "svc-1"], progress.Keys.Order(StringComparer.Ordinal));
            Assert.Equal((WorkflowBusinessStatus.Running, "Alice"), (progress["svc-1"].Status, Assert.Single(progress["svc-1"].PendingAssignees).DisplayName));
            Assert.Equal(WorkflowBusinessStatus.Canceled, progress["doc-2"].Status);
            Assert.True((await service.WithdrawAsync(RecordingBusiness.Code, "svc-1", "not needed", ctx)).ValueOrThrow());
            Assert.False((await service.WithdrawAsync(RecordingBusiness.Code, "svc-1", "again", ctx)).ValueOrThrow());
            return true;
        });
        Assert.DoesNotContain(log.Calls, static c => c.StartsWith("aborted:svc-1", StringComparison.Ordinal));
        Assert.Contains("outcome:auto-1:approved", log.Calls);
        Assert.Single(await ListAsync(server, "workflow-definitions", platform, """{"filters":[{"field":"name","operator":"eq","value":"tests-defaulted"}]}"""));
    }

    [Fact]
    public async Task Notifications_AreDeliveredOncePerMessage()
    {
        var (server, platform, _) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;
        var me = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("user").GetProperty("id").GetString()!;
        var (_, draft) = await SendAsync(server, HttpMethod.Post, "/api/workflow-definitions", Json(new { document = Approval.Replace("tests-business", "tests-plain", StringComparison.Ordinal).Replace("ASSIGNEE", me, StringComparison.Ordinal).Replace("CC", me, StringComparison.Ordinal) }), platform);
        await SendAsync(server, HttpMethod.Post, $"/api/workflow-definitions/{draft.GetProperty("id").GetString()}:publish", "{}", platform);
        await SendAsync(server, HttpMethod.Post, "/api/workflow-instances:start", Json(new { name = "tests-plain", correlationId = "dup-1" }), platform);
        await EventuallyAsync(async () => (await ListAsync(server, "approvals", platform, suffix: "list-cc")).Count == 1);

        // 同一条消息再投递一次：回执在，什么也不做（不再发站内信、不再写抄送记录）
        var receipts = await SettingsTestHost.InvokeAsync(server, null, async ctx => await ctx.Services.GetRequiredService<SideRows>().Records<SysWorkflowMessageReceipt>("platform").ToListAsync(ctx.CancellationToken));
        Assert.Equal(receipts.Count, receipts.Select(static r => r.MessageId).Distinct(StringComparer.Ordinal).Count());
        var created = receipts.First(static r => r.Topic == WorkflowTopics.TaskCreated);
        var task = Assert.Single(await ListAsync(server, "tasks", platform));
        var inbox = (await InboxAsync(server, platform)).Count;
        Assert.True(inbox >= 2);
        var replayed = await SettingsTestHost.InvokeAsync(server, null, async ctx =>
        {
            var data = WorkflowJson.SerializeEvent(new WorkflowEventData
            {
                InstanceId = WorkflowIds.ToEngine(Guid.Parse(task.GetProperty("instanceId").GetString()!)),
                DefinitionId = "unused",
                DefinitionName = "tests-plain",
                Title = "replayed",
                NodeId = "approve",
                TaskId = WorkflowIds.ToEngine(Guid.Parse(task.GetProperty("id").GetString()!)),
                AssigneeId = me,
                CcUserIds = [me],
            });
            var envelope = new MessageEnvelope { Id = created.MessageId, Source = WorkflowTopics.Source, Type = WorkflowTopics.TaskCreated, Data = data };
            var handler = ActivatorUtilities.CreateInstance<TaskCreatedHandler>(ctx.Services);
            return await handler.HandleAsync(new MessageDelivery { Topic = WorkflowTopics.TaskCreated, Envelope = envelope, Attempt = 2, Mode = SubscriptionMode.Competing }, ctx, ctx.CancellationToken);
        });
        Assert.True(replayed.IsSuccess);
        Assert.Equal(inbox, (await InboxAsync(server, platform)).Count);
        Assert.Single(await ListAsync(server, "approvals", platform, suffix: "list-cc"));
    }

    [Fact]
    public async Task DemoFlows_RunToTheirConclusions()
    {
        var clock = new ManualClock(DateTimeOffset.UtcNow);
        await using var server = await BasicAppTestServer.StartAsync(_database.ConnectionString, settings: [new("BasicApp:Seed:EnableDemoData", "true")], configure: builder => builder.Services.AddSingleton<TimeProvider>(clock));
        await server.SeedDefaultsAsync();
        await server.RunSeedersAsync();
        var people = await SettingsTestHost.InvokeAsync(server, "demo", async ctx =>
            (await ctx.Services.GetRequiredService<IUserDirectory>().SearchActiveInSideAsync("demo", null, 50, ctx.CancellationToken)).ToDictionary(static u => u.UserName, static u => WorkflowIds.ToEngineUser(u.Id)));

        async Task<string> StartAsync(string name, string starter, string variables)
            => await SettingsTestHost.InvokeAsync(server, "demo", async ctx => (await ctx.Services.GetRequiredService<IWorkflowEngine>().StartAsync(new WorkflowStartRequest { Name = new WorkflowName(name), StarterId = people[starter], Variables = System.Text.Json.Nodes.JsonNode.Parse(variables)!.AsObject() }, ctx.CancellationToken)).ValueOrThrow().Id);

        async Task<WorkflowInstance> InstanceAsync(string id)
            => await SettingsTestHost.InvokeAsync(server, "demo", async ctx => (await ctx.Services.GetRequiredService<IWorkflowEngine>().FindInstanceAsync(id, ctx.CancellationToken)).ValueOrThrow().ValueOrThrow());

        // 每一轮办掉一条待办（按给的结论，null 即不办）后重新取待办（驳回会撤掉并行分支上的待办）；没有可办的时钟往前拨、轮询一次到期的等待
        async Task<List<string>> DriveAsync(string id, Func<UserTask, string?> decide, bool advance = true)
        {
            var handled = new List<string>();
            for (var round = 0; round < 100; round++)
            {
                if ((await InstanceAsync(id)).IsFinal)
                {
                    return handled;
                }

                var next = await SettingsTestHost.InvokeAsync(server, "demo", async ctx =>
                {
                    var tasks = ctx.Services.GetRequiredService<IWorkflowTaskService>();
                    foreach (var (name, user) in people)
                    {
                        foreach (var task in (await tasks.QueryAsync(new UserTaskQuery { AssigneeId = user, Take = UserTaskQuery.MaxTake }, ctx.CancellationToken)).ValueOrThrow().Items)
                        {
                            if (decide(task) is { } decision)
                            {
                                return (Name: name, User: user, Task: task, Decision: decision);
                            }
                        }
                    }

                    return default;
                });
                if (next.Task is not null)
                {
                    await SettingsTestHost.InvokeAsync(server, "demo", async ctx =>
                        (await ctx.Services.GetRequiredService<IWorkflowTaskService>().CompleteAsync([new UserTaskCompletion(next.Task.Id, next.User) { Outcome = next.Decision }], ctx.CancellationToken))[0].ValueOrThrow());
                    handled.Add($"{next.Task.NodeId}:{next.Name}");
                    continue;
                }

                if (!advance)
                {
                    return handled;
                }

                clock.Set(clock.GetUtcNow().AddMinutes(3));
                await server.Services.GetRequiredService<DueBookmarkPoller>().PollOnceAsync(100, CancellationToken.None);
            }

            Assert.Fail($"Workflow instance {id} did not finish.");
            return handled;
        }

        static string? Approve(UserTask task) => UserTaskOutcomes.Approved;

        // 报销 12000：直属主管 manager，超过 10000 再由总经理 admin
        var expense = await StartAsync("demo-expense", "lead", """{"amount":12000}""");
        Assert.Equal(["leader-approve:manager", "gm-approve:admin"], await DriveAsync(expense, Approve));
        Assert.Equal((WorkflowInstanceStatus.Completed, "approved"), ((await InstanceAsync(expense)).Status, (await InstanceAsync(expense)).Outcome));

        // 请假 5 天：两级主管依次（manager、admin），超过 3 天再由人事部 hr；抄送人事专员
        var leave = await StartAsync("demo-leave", "lead", """{"days":5}""");
        Assert.Equal(["leader-chain:manager", "leader-chain:admin", "hr-approve:hr"], await DriveAsync(leave, Approve));
        Assert.Equal("approved", (await InstanceAsync(leave)).Outcome);

        // 采购驳回：会签里有人驳回即结束
        var rejected = await StartAsync("demo-procurement", "lead", "{}");
        await DriveAsync(rejected, static task => task.NodeId == "countersign" ? UserTaskOutcomes.Rejected : UserTaskOutcomes.Approved);
        Assert.Equal("rejected", (await InstanceAsync(rejected)).Outcome);

        // 采购同意：admin 与 manager 会签、总部负责人 admin 审核，汇聚后总经理 admin
        var procurement = await StartAsync("demo-procurement", "lead", "{}");
        Assert.Equal(["countersign:admin", "countersign:manager", "gm-approve:admin", "hq-review:admin"], (await DriveAsync(procurement, Approve)).Order(StringComparer.Ordinal));
        Assert.Equal("approved", (await InstanceAsync(procurement)).Outcome);

        // 采购超时：会签与总部负责人都同意、总经理一直不办，120 秒后以超时关闭
        var timedOut = await StartAsync("demo-procurement", "lead", "{}");
        await DriveAsync(timedOut, static task => task.NodeId == "gm-approve" ? null : UserTaskOutcomes.Approved, advance: false);
        clock.Set(clock.GetUtcNow().AddMinutes(3));
        await server.Services.GetRequiredService<DueBookmarkPoller>().PollOnceAsync(100, CancellationToken.None);
        Assert.Equal((WorkflowInstanceStatus.Completed, "timeout"), ((await InstanceAsync(timedOut)).Status, (await InstanceAsync(timedOut)).Outcome));

        // 入职：人事准备（hr）、导师确认（lead）、等 60 秒、逐件起采购子流程、差旅报销子流程，最后同意
        var onboarding = await StartAsync("demo-onboarding", "admin", """{"employeeName":"Newcomer"}""");
        var steps = await DriveAsync(onboarding, Approve);
        Assert.Equal(["hr-prepare:hr", "mentor-confirm:lead"], steps.Take(2).ToList());
        Assert.Contains("gm-approve:admin", steps);
        Assert.Equal((WorkflowInstanceStatus.Completed, "approved"), ((await InstanceAsync(onboarding)).Status, (await InstanceAsync(onboarding)).Outcome));
    }

    private static (HttpStatusCode Status, string? Code) Code((HttpStatusCode Status, JsonElement Body) response)
        => (response.Status, response.Body.ValueKind == JsonValueKind.Object && response.Body.TryGetProperty("code", out var code) ? code.GetString() : null);

    private static string Json<T>(T value) => JsonSerializer.Serialize(value);

    private static async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(BasicAppTestServer server, HttpMethod method, string path, string? body, string token)
    {
        var (response, json) = await server.SendAsync(BasicAppTestServer.Request(method, path, body, token));
        return (response.StatusCode, json);
    }

    [Fact]
    public async Task Faults_AreLocalized_ForTheCallerAndTheRecipient()
    {
        var (server, platform, _) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;

        // 受理角色没有成员：发起即故障，框架把原因存成消息键与参数（ADR-0105），读的时候按读取方的区域本地化
        var faulting = Approval
            .Replace("\"type\": \"User\", \"values\": [\"ASSIGNEE\"]", "\"type\": \"Role\", \"values\": [\"nobody\"]", StringComparison.Ordinal)
            .Replace("\"CcUserIds\": [\"CC\"], ", string.Empty, StringComparison.Ordinal)
            .Replace("tests-business", "tests-faulting", StringComparison.Ordinal);
        var (_, draft) = await SendAsync(server, HttpMethod.Post, "/api/workflow-definitions", Json(new { document = faulting }), platform);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, $"/api/workflow-definitions/{draft.GetProperty("id").GetString()}:publish", "{}", platform)).Status);

        // 发起人在偏好里存了日文：故障通知按收件人的语言
        var (saved, _) = await SendAsync(server, HttpMethod.Put, "/api/user-settings", Json(new { scene = "Preference", key = "global", value = """{"xihan_locale":"ja-JP"}""", clientId = "tests" }), platform);
        Assert.Equal(HttpStatusCode.OK, saved);

        // 中文调用方看到中文的原因，英文读取方看到英文
        var (started, instance) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/workflow-instances:start", Json(new { name = "tests-faulting" }), platform, "zh-CN"));
        Assert.Equal(HttpStatusCode.OK, started.StatusCode);
        Assert.Equal(("Faulted", "用户任务 'approve' 没有解析出指派人。"), (instance.GetProperty("status").GetString(), instance.GetProperty("faultMessage").GetString()));
        var (_, english) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/workflow-instances/{instance.GetProperty("id").GetString()}", token: platform, acceptLanguage: "en-US"));
        Assert.Equal("User task 'approve' resolved no assignee.", english.GetProperty("instance").GetProperty("faultMessage").GetString());

        await EventuallyAsync(async () => (await InboxAsync(server, platform)).Any(static i => i.GetProperty("link").GetString() == "/workflow/approvals?tab=started"));
        var notice = (await InboxAsync(server, platform)).Single(static i => i.GetProperty("link").GetString() == "/workflow/approvals?tab=started");
        Assert.StartsWith("ワークフロー障害：", notice.GetProperty("title").GetString(), StringComparison.Ordinal);
        Assert.EndsWith("：ユーザータスク 'approve' の担当者を解決できませんでした。", notice.GetProperty("content").GetString(), StringComparison.Ordinal);

        // 发起人能撤回故障的实例：实例取消，审批业务收到中止回写，单据不会一直停在审批中
        var faultedId = instance.GetProperty("id").GetString()!;
        var (withdrawn, after) = await SendAsync(server, HttpMethod.Post, $"/api/approvals/{faultedId}:withdraw", "{}", platform);
        Assert.Equal((HttpStatusCode.OK, "Canceled"), (withdrawn, after.GetProperty("status").GetString()));
    }

    private static async Task<List<JsonElement>> ListAsync(BasicAppTestServer server, string resource, string token, string body = """{"page":{"index":1,"size":50}}""", string suffix = "list")
    {
        var (response, json) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/{resource}:{suffix}", body, token));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return json.GetProperty("items").EnumerateArray().ToList();
    }

    private static Task<List<JsonElement>> InboxAsync(BasicAppTestServer server, string token) => ListAsync(server, "inbox", token);

    private static async Task<JsonElement> WidgetAsync(BasicAppTestServer server, string token)
        => (await SendAsync(server, HttpMethod.Get, "/api/workbench:widget-data?key=workflow.todo", null, token)).Body;

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

    /// <summary>
    /// 审批业务回调的记录
    /// </summary>
    private sealed class BusinessLog
    {
        private readonly ConcurrentQueue<string> _calls = new();

        public IReadOnlyCollection<string> Calls => _calls;

        public void Add(string call) => _calls.Enqueue(call);
    }

    /// <summary>
    /// 记下各回调的审批业务
    /// </summary>
    /// <remarks>
    /// 驳回时办理前校验拒绝（测试待办保留）
    /// </remarks>
    private sealed class RecordingBusiness(BusinessLog log) : IWorkflowBusinessHandler
    {
        public const string Code = "tests-business";

        public const string NameKey = "tests.business.name";

        public WorkflowBusinessDescriptor Descriptor { get; } = new(Code, NameKey, "tests/form/index", [], [], null);

        public ValueTask<Result> OnTaskCompletingAsync(WorkflowBusinessTask task, InvocationContext context)
        {
            log.Add($"completing:{task.BusinessId}:{task.Outcome}:{task.ActorId}");
            return ValueTask.FromResult(task.Outcome == "rejected" ? Result.Failure(Error.Validation("TESTS_VALIDATION_001", "tests.veto")) : Result.Success());
        }

        public ValueTask<Result> OnChangingAsync(WorkflowBusinessChange change, InvocationContext context) => ValueTask.FromResult(Result.Success());

        public ValueTask<Result> OnOutcomeAsync(WorkflowBusinessOutcome outcome, InvocationContext context)
        {
            log.Add($"outcome:{outcome.BusinessId}:{outcome.Outcome}");
            return ValueTask.FromResult(Result.Success());
        }

        public ValueTask<Result> OnAbortedAsync(WorkflowBusinessAborted aborted, InvocationContext context)
        {
            log.Add($"aborted:{aborted.BusinessId}:{aborted.Status}");
            return ValueTask.FromResult(Result.Success());
        }

        public ValueTask<Result<string>> GetFormDataAsync(string businessId, InvocationContext context)
            => ValueTask.FromResult(Result.Success($$"""{"id":"{{businessId}}"}"""));
    }

    /// <summary>
    /// 带缺省定义的审批业务：找不到人即自动同意，发起即走完
    /// </summary>
    private sealed class DefaultedBusiness(BusinessLog log) : IWorkflowBusinessHandler
    {
        public const string Code = "tests-defaulted";

        public WorkflowBusinessDescriptor Descriptor { get; } = new(Code, "tests.defaulted.name", "tests/form/index", [], [], """
            {
              "name": "tests-defaulted", "displayName": "Defaulted",
              "nodes": [
                { "id": "start", "activityType": "Start" },
                { "id": "approve", "activityType": "UserTask", "properties": { "AssigneeRules": [{ "type": "Role", "values": ["nobody"] }], "EmptyAssigneeStrategy": "AutoApprove" } },
                { "id": "approved", "activityType": "End", "properties": { "Outcome": "approved" } },
                { "id": "rejected", "activityType": "End", "properties": { "Outcome": "rejected" } }
              ],
              "transitions": [
                { "id": "t1", "sourceNodeId": "start", "targetNodeId": "approve" },
                { "id": "t2", "sourceNodeId": "approve", "targetNodeId": "approved", "condition": "outcome == 'approved'" },
                { "id": "t3", "sourceNodeId": "approve", "targetNodeId": "rejected", "isDefault": true }
              ]
            }
            """);

        public ValueTask<Result> OnTaskCompletingAsync(WorkflowBusinessTask task, InvocationContext context) => ValueTask.FromResult(Result.Success());

        public ValueTask<Result> OnChangingAsync(WorkflowBusinessChange change, InvocationContext context) => ValueTask.FromResult(Result.Success());

        public ValueTask<Result> OnOutcomeAsync(WorkflowBusinessOutcome outcome, InvocationContext context)
        {
            log.Add($"outcome:{outcome.BusinessId}:{outcome.Outcome}");
            return ValueTask.FromResult(Result.Success());
        }

        public ValueTask<Result> OnAbortedAsync(WorkflowBusinessAborted aborted, InvocationContext context) => ValueTask.FromResult(Result.Success());

        public ValueTask<Result<string>> GetFormDataAsync(string businessId, InvocationContext context) => ValueTask.FromResult(Result.Success("{}"));
    }
}
