// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts.Workflow;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.BasicApp.Workflow.Seeds;
using XiHan.Framework.Primitives;
using XiHan.Framework.Workflow.Abstractions;
using XiHan.Framework.Workflow.Abstractions.Engine;
using XiHan.Framework.Workflow.Abstractions.Instances;
using XiHan.Framework.Workflow.Abstractions.Tasks;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 工作流的定义、实例与选人（N3 第三批之三）
/// </summary>
/// <remarks>
/// 定义的生命周期（建草稿、改、发布、新版本、停用、归档、删草稿）与发布前的业务校验；平台与租户两个分区互不可见；实例的发起、详情（节点历史与等待）
/// 与控制；选人目录；组织类受理人在演示数据上按 4.x 的规则解析；四个演示流程只首写；模块开关
/// </remarks>
public sealed class WorkflowDefinitionsTests : IDisposable
{
    private const string Approval = """
        {
          "id": "x", "name": "NAME", "displayName": "Approval", "category": "Tests",
          "variables": [{ "name": "amount", "required": false, "defaultValue": 7 }],
          "nodes": [
            { "id": "start", "activityType": "Start" },
            { "id": "approve", "activityType": "UserTask", "properties": { "AssigneeRules": [{ "type": "User", "values": ["ASSIGNEE"] }], "Title": "Approve {{ amount }}" } },
            { "id": "approved", "activityType": "End", "properties": { "Outcome": "approved" } },
            { "id": "rejected", "activityType": "End", "properties": { "Outcome": "OUTCOME" } }
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
    public async Task Definitions_Lifecycle_BusinessGuard_AndPartitions()
    {
        var (server, platform, tenant) = await SettingsTestHost.StartAsync(_database.ConnectionString, configure: builder => builder.Services.AddWorkflowBusiness<TestBusiness>());
        await using var running = server;

        // 建草稿：文档不合法即框架的 WORKFLOW_VALIDATION_001
        Assert.Equal("WORKFLOW_VALIDATION_001", (await SendAsync(server, HttpMethod.Post, "/api/workflow-definitions", Json(new { document = "[1]" }), platform)).Body.GetProperty("code").GetString());
        var (created, draft) = await SendAsync(server, HttpMethod.Post, "/api/workflow-definitions", Json(new { document = Doc("tests-approval", "someone", "rejected") }), platform);
        Assert.Equal(HttpStatusCode.Created, created);
        Assert.Equal(("tests-approval", 1, "Draft", "Tests"), (draft.GetProperty("name").GetString(), draft.GetProperty("version").GetInt32(), draft.GetProperty("status").GetString(), draft.GetProperty("category").GetString()));
        var id = draft.GetProperty("id").GetString()!;

        // 改草稿：显示名改了，名字沿用已存的
        var renamed = Doc("other-name", "someone", "rejected").Replace("\"Approval\"", "\"Renamed\"", StringComparison.Ordinal);
        var (updated, updatedBody) = await SendAsync(server, HttpMethod.Put, $"/api/workflow-definitions/{id}", Json(new { document = renamed }), platform);
        Assert.Equal(HttpStatusCode.OK, updated);
        Assert.Equal(("tests-approval", "Renamed"), (updatedBody.GetProperty("name").GetString(), updatedBody.GetProperty("displayName").GetString()));

        // 发布：结构不合法时问题列在 details.errors
        var (_, broken) = await SendAsync(server, HttpMethod.Post, "/api/workflow-definitions", Json(new { document = Doc("tests-broken", "someone", "rejected").Replace("\"targetNodeId\": \"approve\"", "\"targetNodeId\": \"nowhere\"", StringComparison.Ordinal) }), platform);
        var (invalid, invalidBody) = await SendAsync(server, HttpMethod.Post, $"/api/workflow-definitions/{broken.GetProperty("id").GetString()}:publish", "{}", platform);
        Assert.Equal((HttpStatusCode.BadRequest, "WORKFLOW_VALIDATION_001"), (invalid, invalidBody.GetProperty("code").GetString()));
        var (published, publishedBody) = await SendAsync(server, HttpMethod.Post, $"/api/workflow-definitions/{id}:publish", "{}", platform);
        Assert.Equal((HttpStatusCode.OK, "Published"), (published, publishedBody.GetProperty("status").GetString()));
        Assert.Equal("WORKFLOW_CONFLICT_002", (await SendAsync(server, HttpMethod.Put, $"/api/workflow-definitions/{id}", Json(new { document = renamed }), platform)).Body.GetProperty("code").GetString());

        // 审批业务：名字等于业务码的定义，结论不在业务允许的范围里、有终止节点都不能发布
        var (_, business) = await SendAsync(server, HttpMethod.Post, "/api/workflow-definitions", Json(new { document = Doc(TestBusiness.Code, "someone", "maybe") }), platform);
        Assert.Equal(TestBusiness.NameKey, business.GetProperty("businessNameKey").GetString());
        Assert.Equal("WORKFLOW_VALIDATION_101", (await SendAsync(server, HttpMethod.Post, $"/api/workflow-definitions/{business.GetProperty("id").GetString()}:publish", "{}", platform)).Body.GetProperty("code").GetString());
        var terminate = Doc(TestBusiness.Code, "someone", "rejected").Replace("\"activityType\": \"End\", \"properties\": { \"Outcome\": \"rejected\" }", "\"activityType\": \"Terminate\"", StringComparison.Ordinal);
        var (_, terminating) = await SendAsync(server, HttpMethod.Post, "/api/workflow-definitions", Json(new { document = terminate }), platform);
        Assert.Equal("WORKFLOW_VALIDATION_102", (await SendAsync(server, HttpMethod.Post, $"/api/workflow-definitions/{terminating.GetProperty("id").GetString()}:publish", "{}", platform)).Body.GetProperty("code").GetString());
        var types = (await SendAsync(server, HttpMethod.Get, "/api/workflow-definitions:business-types", null, platform)).Body.GetProperty("items");
        // 各包登记的审批业务按业务码排：Access 的授权申请与测试的业务
        Assert.Equal(["permission-request", TestBusiness.Code], types.EnumerateArray().Select(static t => t.GetProperty("code").GetString()).ToList());
        var type = types.EnumerateArray().Single(static t => t.GetProperty("code").GetString() == TestBusiness.Code);
        Assert.Equal((TestBusiness.Code, "tests/form/index"), (type.GetProperty("code").GetString(), type.GetProperty("formComponent").GetString()));
        var activities = (await SendAsync(server, HttpMethod.Get, "/api/workflow-definitions:activity-types", null, platform)).Body.GetProperty("items").EnumerateArray().Select(static a => a.GetProperty("type").GetString()).ToList();
        Assert.Equal(16, activities.Count);
        Assert.DoesNotContain("Script", activities);
        Assert.DoesNotContain("EventGateway", activities);
        Assert.DoesNotContain("ExternalTask", activities);

        // 新版本、停用、归档、删草稿（只删草稿）；列表按名字、关键字、状态过滤
        var (_, second) = await SendAsync(server, HttpMethod.Post, "/api/workflow-definitions:new-version", Json(new { name = "tests-approval" }), platform);
        Assert.Equal(2, second.GetProperty("version").GetInt32());
        var listed = await ListAsync(server, "workflow-definitions", platform, """{"filters":[{"field":"name","operator":"eq","value":"tests-approval"}]}""");
        Assert.Equal([2, 1], listed.Select(static d => d.GetProperty("version").GetInt32()).ToList());
        Assert.Single(await ListAsync(server, "workflow-definitions", platform, """{"keyword":"renamed","filters":[{"field":"status","operator":"eq","value":"Published"}]}"""));
        Assert.Equal("WORKFLOW_VALIDATION_104", (await SendAsync(server, HttpMethod.Post, "/api/workflow-definitions:list", """{"filters":[{"field":"version","operator":"eq","value":"1"}]}""", platform)).Body.GetProperty("code").GetString());

        // 排序照 4.x 多列、方向照请求，创建时间区间含两端；不支持的列 WORKFLOW_VALIDATION_104，同一列给两次是框架的 WORKFLOW_VALIDATION_002
        var ascending = await ListAsync(server, "workflow-definitions", platform, """{"filters":[{"field":"name","operator":"eq","value":"tests-approval"}],"sorts":[{"field":"version"}]}""");
        Assert.Equal([1, 2], ascending.Select(static d => d.GetProperty("version").GetInt32()).ToList());
        var all = await ListAsync(server, "workflow-definitions", platform);
        Assert.Equal(all.Count, (await ListAsync(server, "workflow-definitions", platform, """{"page":{"index":1,"size":200},"filters":[{"field":"createdAt","operator":"ge","value":"2000-01-01T00:00:00Z"},{"field":"createdAt","operator":"le","value":"2999-01-01T00:00:00Z"}]}""")).Count);
        Assert.Empty(await ListAsync(server, "workflow-definitions", platform, """{"filters":[{"field":"createdAt","operator":"ge","value":"2999-01-01T00:00:00Z"}]}"""));
        Assert.Empty(await ListAsync(server, "workflow-definitions", platform, """{"filters":[{"field":"createdAt","operator":"le","value":"2000-01-01T00:00:00Z"}]}"""));
        var secondary = await ListAsync(server, "workflow-definitions", platform, """{"filters":[{"field":"name","operator":"eq","value":"tests-approval"}],"sorts":[{"field":"name"},{"field":"version"}]}""");
        Assert.Equal([1, 2], secondary.Select(static d => d.GetProperty("version").GetInt32()).ToList());
        Assert.Equal("WORKFLOW_VALIDATION_002", (await SendAsync(server, HttpMethod.Post, "/api/workflow-definitions:list", """{"sorts":[{"field":"version"},{"field":"version","descending":true}]}""", platform)).Body.GetProperty("code").GetString());
        Assert.Equal("WORKFLOW_VALIDATION_104", (await SendAsync(server, HttpMethod.Post, "/api/workflow-definitions:list", """{"sorts":[{"field":"category"}]}""", platform)).Body.GetProperty("code").GetString());
        var deleted = (await SendAsync(server, HttpMethod.Delete, $"/api/workflow-definitions?ids={id}&ids={second.GetProperty("id").GetString()}", null, platform)).Body;
        Assert.Equal([second.GetProperty("id").GetString()], deleted.GetProperty("succeeded").EnumerateArray().Select(static e => e.GetString()).ToList());
        Assert.Equal("WORKFLOW_CONFLICT_002", deleted.GetProperty("failed")[0].GetProperty("code").GetString());
        Assert.Empty((await SendAsync(server, HttpMethod.Post, "/api/workflow-definitions:disable", Json(new { ids = new[] { id } }), platform)).Body.GetProperty("failed").EnumerateArray());
        Assert.Empty((await SendAsync(server, HttpMethod.Post, "/api/workflow-definitions:archive", Json(new { ids = new[] { id } }), platform)).Body.GetProperty("failed").EnumerateArray());
        Assert.Equal("Archived", (await SendAsync(server, HttpMethod.Get, $"/api/workflow-definitions/{id}", null, platform)).Body.GetProperty("status").GetString());

        // 状态按生命周期排（同 4.x）：草稿在前、已归档在后，按名字排正好相反
        var byStatus = await ListAsync(server, "workflow-definitions", platform, """{"page":{"index":1,"size":200},"sorts":[{"field":"status"}]}""");
        Assert.Equal(("Draft", "Archived"), (byStatus[0].GetProperty("status").GetString(), byStatus[^1].GetProperty("status").GetString()));

        // 分区：租户看不到平台的定义，平台看不到租户的
        Assert.Empty(await ListAsync(server, "workflow-definitions", tenant));
        Assert.Equal("WORKFLOW_NOTFOUND_101", (await SendAsync(server, HttpMethod.Get, $"/api/workflow-definitions/{id}", null, tenant)).Body.GetProperty("code").GetString());
        var (_, own) = await SendAsync(server, HttpMethod.Post, "/api/workflow-definitions", Json(new { document = Doc("tests-approval", "someone", "rejected") }), tenant);
        Assert.Equal(1, own.GetProperty("version").GetInt32());
        Assert.DoesNotContain(await ListAsync(server, "workflow-definitions", platform), d => d.GetProperty("id").GetString() == own.GetProperty("id").GetString());
    }

    [Fact]
    public async Task Instances_StartInspectAndControl()
    {
        var (server, platform, _) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;
        var me = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("user").GetProperty("id").GetString()!;
        var (_, draft) = await SendAsync(server, HttpMethod.Post, "/api/workflow-definitions", Json(new { document = Doc("tests-approval", me, "rejected") }), platform);
        await SendAsync(server, HttpMethod.Post, $"/api/workflow-definitions/{draft.GetProperty("id").GetString()}:publish", "{}", platform);

        Assert.Equal("WORKFLOW_VALIDATION_103", (await SendAsync(server, HttpMethod.Post, "/api/workflow-instances:start", Json(new { name = "tests-approval", variables = "[1]" }), platform)).Body.GetProperty("code").GetString());
        var (started, instance) = await SendAsync(server, HttpMethod.Post, "/api/workflow-instances:start", Json(new { name = "tests-approval", displayName = "First", correlationId = "doc-1", variables = """{"amount":42}""" }), platform);
        Assert.Equal(HttpStatusCode.OK, started);
        Assert.Equal(("Running", "First", me, "doc-1"), (instance.GetProperty("status").GetString(), instance.GetProperty("title").GetString(), instance.GetProperty("starterId").GetString(), instance.GetProperty("correlationId").GetString()));
        var id = instance.GetProperty("id").GetString()!;

        // 详情：变量、节点历史、等着用户任务的等待、所用的定义
        var detail = (await SendAsync(server, HttpMethod.Get, $"/api/workflow-instances/{id}", null, platform)).Body;
        Assert.Equal(42, JsonDocument.Parse(detail.GetProperty("variables").GetString()!).RootElement.GetProperty("amount").GetInt32());
        Assert.Contains(detail.GetProperty("nodes").EnumerateArray(), n => n.GetProperty("nodeId").GetString() == "approve");
        var wait = Assert.Single(detail.GetProperty("waits").EnumerateArray());
        Assert.Equal(("approve", "UserTask", me), (wait.GetProperty("nodeId").GetString(), wait.GetProperty("kind").GetString(), wait.GetProperty("key").GetString()));
        Assert.Contains("tests-approval", detail.GetProperty("document").GetString(), StringComparison.Ordinal);

        // 列表：关键字匹配实例标题与关联 ID（照 4.x，按字面，_ 不是通配符），关联 ID 筛选可多个
        Assert.Single(await ListAsync(server, "workflow-instances", platform, """{"keyword":"first"}"""));
        Assert.Single(await ListAsync(server, "workflow-instances", platform, """{"keyword":"doc-1"}"""));
        Assert.Empty(await ListAsync(server, "workflow-instances", platform, """{"keyword":"doc_1"}"""));
        Assert.Single(await ListAsync(server, "workflow-instances", platform, """{"filters":[{"field":"correlationId","operator":"in","value":"doc-1,doc-2"},{"field":"status","operator":"eq","value":"Running"}]}"""));

        // 控制：挂起、恢复、取消；已结束的再取消算成功，不存在的逐项失败
        Assert.Empty((await SendAsync(server, HttpMethod.Post, "/api/workflow-instances:suspend", Json(new { ids = new[] { id }, reason = "hold" }), platform)).Body.GetProperty("failed").EnumerateArray());
        Assert.Equal("Suspended", (await SendAsync(server, HttpMethod.Get, $"/api/workflow-instances/{id}", null, platform)).Body.GetProperty("instance").GetProperty("status").GetString());
        Assert.Empty((await SendAsync(server, HttpMethod.Post, "/api/workflow-instances:resume", Json(new { ids = new[] { id } }), platform)).Body.GetProperty("failed").EnumerateArray());
        var missing = Guid.NewGuid();
        var canceled = (await SendAsync(server, HttpMethod.Post, "/api/workflow-instances:cancel", Json(new { ids = new[] { Guid.Parse(id), missing }, reason = "tests" }), platform)).Body;
        Assert.Equal([id], canceled.GetProperty("succeeded").EnumerateArray().Select(static e => e.GetString()).ToList());
        Assert.Equal("WORKFLOW_NOTFOUND_002", canceled.GetProperty("failed")[0].GetProperty("code").GetString());
        var after = (await SendAsync(server, HttpMethod.Get, $"/api/workflow-instances/{id}", null, platform)).Body;
        Assert.Equal(("Canceled", "tests"), (after.GetProperty("instance").GetProperty("status").GetString(), after.GetProperty("cancellationReason").GetString()));
        Assert.Empty(after.GetProperty("waits").EnumerateArray());
        Assert.Equal("WORKFLOW_NOTFOUND_102", (await SendAsync(server, HttpMethod.Get, $"/api/workflow-instances/{missing}", null, platform)).Body.GetProperty("code").GetString());

        // 排序照 4.x 多列、方向照请求（不给即创建时间降序），状态按生命周期（运行中在已取消之前）；不支持的列 WORKFLOW_VALIDATION_104，同一列给两次是框架的 WORKFLOW_VALIDATION_002
        await SendAsync(server, HttpMethod.Post, "/api/workflow-instances:start", Json(new { name = "tests-approval", displayName = "Second", correlationId = "doc-2" }), platform);
        Assert.Equal(["Second", "First"], Titles(await ListAsync(server, "workflow-instances", platform)));
        Assert.Equal(["First", "Second"], Titles(await ListAsync(server, "workflow-instances", platform, """{"sorts":[{"field":"createdAt"}]}""")));
        Assert.Equal(["First", "Second"], Titles(await ListAsync(server, "workflow-instances", platform, """{"sorts":[{"field":"title"}]}""")));
        Assert.Equal(["Second", "First"], Titles(await ListAsync(server, "workflow-instances", platform, """{"sorts":[{"field":"title","descending":true}]}""")));
        Assert.Equal(["Second", "First"], Titles(await ListAsync(server, "workflow-instances", platform, """{"sorts":[{"field":"status"}]}""")));
        Assert.Equal(["Second", "First"], Titles(await ListAsync(server, "workflow-instances", platform, """{"sorts":[{"field":"definitionName"},{"field":"title","descending":true}]}""")));
        Assert.Equal(["First", "Second"], Titles(await ListAsync(server, "workflow-instances", platform, """{"sorts":[{"field":"definitionName"},{"field":"title"}]}""")));
        Assert.Equal("WORKFLOW_VALIDATION_002", (await SendAsync(server, HttpMethod.Post, "/api/workflow-instances:list", """{"sorts":[{"field":"title"},{"field":"title","descending":true}]}""", platform)).Body.GetProperty("code").GetString());
        Assert.Equal("WORKFLOW_VALIDATION_104", (await SendAsync(server, HttpMethod.Post, "/api/workflow-instances:list", """{"sorts":[{"field":"correlationId"}]}""", platform)).Body.GetProperty("code").GetString());

        // 信号：没有在等的实例时恢复数为 0
        Assert.Equal(0, (await SendAsync(server, HttpMethod.Post, "/api/workflow-instances:signal", Json(new { name = "go" }), platform)).Body.GetProperty("resumed").GetInt32());
    }

    [Fact]
    public async Task DemoData_SeedsFourDefinitionsOnce_AndOrganizationAssigneesFollowTheRules()
    {
        await using var server = await BasicAppTestServer.StartAsync(_database.ConnectionString, settings: [new("BasicApp:Seed:EnableDemoData", "true")]);
        await server.SeedDefaultsAsync();
        await server.RunSeedersAsync();
        await server.RunSeedersAsync();
        var admin = (await AuthFlowTests.LoginAsync(server, "admin", "Demo@123456", "demo")).GetProperty("accessToken").GetString()!;
        var definitions = await ListAsync(server, "workflow-definitions", admin);
        Assert.Equal(WorkflowDemoSeeder.Names.Order(StringComparer.Ordinal), definitions.Select(static d => d.GetProperty("name").GetString()!).Order(StringComparer.Ordinal));
        Assert.All(definitions, static d => Assert.Equal(("Published", 1), (d.GetProperty("status").GetString(), d.GetProperty("version").GetInt32())));

        // 选人：本租户的在用账号、角色、岗位与部门
        var users = (await SendAsync(server, HttpMethod.Get, "/api/workflow-assignees:users?keyword=Demo", null, admin)).Body.GetProperty("items").EnumerateArray().Select(static u => u.GetProperty("userName").GetString()).ToList();
        Assert.Equal(["admin", "hr", "lead", "manager"], users);
        Assert.Equal(2, (await SendAsync(server, HttpMethod.Get, "/api/workflow-assignees:users?keyword=Demo&limit=2", null, admin)).Body.GetProperty("items").GetArrayLength());
        Assert.Equal(HttpStatusCode.BadRequest, (await SendAsync(server, HttpMethod.Get, "/api/workflow-assignees:users?limit=201", null, admin)).Status);
        Assert.Contains((await SendAsync(server, HttpMethod.Get, "/api/workflow-assignees:roles", null, admin)).Body.GetProperty("items").EnumerateArray(), static r => r.GetProperty("code").GetString() == "hr_specialist");
        Assert.Equal(["gm", "manager", "engineer", "specialist"], (await SendAsync(server, HttpMethod.Get, "/api/workflow-assignees:positions", null, admin)).Body.GetProperty("items").EnumerateArray().Select(static p => p.GetProperty("code").GetString()).ToList());
        Assert.Equal(4, (await SendAsync(server, HttpMethod.Get, "/api/workflow-assignees:departments", null, admin)).Body.GetProperty("items").GetArrayLength());

        // 受理人按 4.x 的规则解析：lead 发起报销 → 研发部负责人 manager；请假 → manager、admin 依次；采购 → 会签 admin + manager 与总部负责人 admin；入职 → 人事专员 hr
        var people = await SettingsTestHost.InvokeAsync(server, "demo", async ctx =>
            (await ctx.Services.GetRequiredService<XiHan.BasicApp.Contracts.Users.IUserDirectory>().SearchActiveInSideAsync("demo", null, 50, ctx.CancellationToken)).ToDictionary(static u => u.UserName, static u => u.Id.ToString()));
        async Task<List<string>> FirstTaskAssigneesAsync(string name, string starter, string variables)
            => await SettingsTestHost.InvokeAsync(server, "demo", async ctx =>
            {
                var instance = (await ctx.Services.GetRequiredService<IWorkflowEngine>().StartAsync(new WorkflowStartRequest { Name = new WorkflowName(name), StarterId = people[starter], Variables = System.Text.Json.Nodes.JsonNode.Parse(variables)!.AsObject() }, ctx.CancellationToken)).ValueOrThrow();
                var pending = (await ctx.Services.GetRequiredService<IWorkflowTaskService>().PendingByInstanceAsync(instance.Id, ctx.CancellationToken)).ValueOrThrow();
                return pending.Select(t => people.First(p => p.Value == t.AssigneeId).Key).Distinct().Order(StringComparer.Ordinal).ToList();
            });

        Assert.Equal(["manager"], await FirstTaskAssigneesAsync("demo-expense", "lead", """{"amount":12000}"""));
        Assert.Equal(["manager"], await FirstTaskAssigneesAsync("demo-leave", "lead", """{"days":5}"""));
        Assert.Equal(["admin", "manager"], await FirstTaskAssigneesAsync("demo-procurement", "lead", "{}"));
        Assert.Equal(["hr"], await FirstTaskAssigneesAsync("demo-onboarding", "admin", """{"employeeName":"Newcomer"}"""));

        // 发起人自己是负责人时往上数一级；整条链上都是自己时没有受理人，按策略自动通过（报销走到大额判断后由总经理审批）
        Assert.Equal(["admin"], await FirstTaskAssigneesAsync("demo-expense", "manager", """{"amount":20000}"""));
        Assert.Equal(["admin"], await FirstTaskAssigneesAsync("demo-expense", "admin", """{"amount":20000}"""));
    }

    [Fact]
    public async Task Module_CanBeTurnedOff()
    {
        await using (var off = await BasicAppTestServer.StartAsync(_database.ConnectionString, settings: [new("BasicApp:Modules:Workflow:Enabled", "false")]))
        {
            await off.SeedDefaultsAsync();
            await off.RunSeedersAsync();
            var token = (await AuthFlowTests.LoginAsync(off, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;
            var (_, permissions) = await off.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:permissions", token: token));
            Assert.DoesNotContain(permissions.GetProperty("pages").EnumerateArray(), static p => p.GetString()!.StartsWith("workflow.", StringComparison.Ordinal));
            Assert.DoesNotContain(permissions.GetProperty("permissions").EnumerateArray(), static p => p.GetString()!.StartsWith("workflow.", StringComparison.Ordinal));
            Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(off, HttpMethod.Post, "/api/workflow-definitions:list", "{}", token)).Status);
            var (openApi, _) = await off.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/openapi.json"));
            Assert.Equal(HttpStatusCode.OK, openApi.StatusCode);
        }

        await Assert.ThrowsAsync<ArgumentException>(async () => await BasicAppTestServer.StartAsync(_database.ConnectionString, settings: [new("BasicApp:Modules:Workflow:Enabled", "maybe")]));
        await using var on = await BasicAppTestServer.StartAsync(_database.ConnectionString);
        await on.SeedDefaultsAsync();
        await on.RunSeedersAsync();
        var superadmin = (await AuthFlowTests.LoginAsync(on, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;
        var (_, body) = await on.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:permissions", token: superadmin));
        Assert.Equal(["workflow.approvals", "workflow.definitions", "workflow.instances"], body.GetProperty("pages").EnumerateArray().Select(static p => p.GetString()!).Where(static p => p.StartsWith("workflow.", StringComparison.Ordinal)).Order(StringComparer.Ordinal).ToList());
        var page = MenuTrees.Find(body.GetProperty("menus"), "workflow.definitions");
        Assert.Equal(("/workflow/definitions", "workflow/definitions/index"), (page.GetProperty("route").GetString(), page.GetProperty("component").GetString()));
    }

    [Fact]
    public async Task Diagnostics_LastError_AndStatusNames_FollowTheCallersLocale()
    {
        var (server, platform, _) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;

        // 发布不合法：Problem Details 的字段级 errors 键是节点与连线的路径，值按调用方的语言（框架 ADR-0106）
        var broken = Doc("tests-broken", "someone", "rejected").Replace("\"targetNodeId\": \"approve\"", "\"targetNodeId\": \"nowhere\"", StringComparison.Ordinal);
        var (_, draft) = await SendAsync(server, HttpMethod.Post, "/api/workflow-definitions", Json(new { document = broken }), platform);
        var (invalid, problem) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/workflow-definitions/{draft.GetProperty("id").GetString()}:publish", "{}", platform, "zh-CN"));
        Assert.Equal((HttpStatusCode.BadRequest, "WORKFLOW_VALIDATION_001"), (invalid.StatusCode, problem.GetProperty("code").GetString()));
        Assert.Matches(@"^工作流定义有 \d+ 处问题。$", problem.GetProperty("detail").GetString()!);
        Assert.Equal("转移 't1' 的目标节点 'nowhere' 不存在。", problem.GetProperty("errors").GetProperty("transitions[0].targetNodeId")[0].GetString());

        // 活动类型目录：显示名与分类展示名按调用方的语言，分类标识照旧
        var (_, activities) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/workflow-definitions:activity-types", token: platform, acceptLanguage: "zh-CN"));
        var userTask = activities.GetProperty("items").EnumerateArray().Single(static a => a.GetProperty("type").GetString() == "UserTask");
        Assert.Equal(("用户任务", "Human", "人工"), (userTask.GetProperty("displayName").GetString(), userTask.GetProperty("category").GetString(), userTask.GetProperty("categoryName").GetString()));

        // lastError：出错继续的节点把故障记进变量，详情按调用方的语言给出；节点状态带展示名
        const string continuing = """
            {
              "id": "x", "name": "tests-continue", "displayName": "Continue",
              "nodes": [
                { "id": "start", "activityType": "Start" },
                { "id": "each", "activityType": "ForEach", "continueOnError": true, "properties": { "Items": "3", "DefinitionName": "tests-continue" } },
                { "id": "errored", "activityType": "End", "properties": { "Outcome": "errored" } },
                { "id": "fine", "activityType": "End", "properties": { "Outcome": "fine" } }
              ],
              "transitions": [
                { "id": "t1", "sourceNodeId": "start", "targetNodeId": "each" },
                { "id": "t2", "sourceNodeId": "each", "targetNodeId": "errored", "condition": "outcome == 'error' && lastError.code != null" },
                { "id": "t3", "sourceNodeId": "each", "targetNodeId": "fine", "isDefault": true }
              ]
            }
            """;
        var (_, continueDraft) = await SendAsync(server, HttpMethod.Post, "/api/workflow-definitions", Json(new { document = continuing }), platform);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, $"/api/workflow-definitions/{continueDraft.GetProperty("id").GetString()}:publish", "{}", platform)).Status);
        var (_, instance) = await SendAsync(server, HttpMethod.Post, "/api/workflow-instances:start", Json(new { name = "tests-continue" }), platform);
        Assert.Equal(("Completed", "errored"), (instance.GetProperty("status").GetString(), instance.GetProperty("outcome").GetString()));
        var (_, detail) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/workflow-instances/{instance.GetProperty("id").GetString()}", token: platform, acceptLanguage: "zh-CN"));
        Assert.Equal("活动属性 'Items' 的求值结果必须是数组。", detail.GetProperty("lastError").GetString());
        var start = detail.GetProperty("nodes").EnumerateArray().Single(static n => n.GetProperty("nodeId").GetString() == "start");
        Assert.Equal(("Completed", "已完成"), (start.GetProperty("status").GetString(), start.GetProperty("statusName").GetString()));
        var (_, english) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/workflow-instances/{instance.GetProperty("id").GetString()}", token: platform, acceptLanguage: "en-US"));
        Assert.Equal("Activity property 'Items' must evaluate to an array.", english.GetProperty("lastError").GetString());
    }

    private static string Doc(string name, string assignee, string rejectedOutcome)
        => Approval.Replace("NAME", name, StringComparison.Ordinal).Replace("ASSIGNEE", assignee, StringComparison.Ordinal).Replace("OUTCOME", rejectedOutcome, StringComparison.Ordinal);

    private static string Json<T>(T value) => JsonSerializer.Serialize(value);

    private static async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(BasicAppTestServer server, HttpMethod method, string path, string? body, string token)
    {
        var (response, json) = await server.SendAsync(BasicAppTestServer.Request(method, path, body, token));
        return (response.StatusCode, json);
    }

    private static async Task<List<JsonElement>> ListAsync(BasicAppTestServer server, string resource, string token, string body = """{"page":{"index":1,"size":200}}""")
    {
        var (response, json) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/{resource}:list", body, token));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return json.GetProperty("items").EnumerateArray().ToList();
    }

    private static List<string?> Titles(List<JsonElement> instances) => instances.Select(static i => i.GetProperty("title").GetString()).ToList();

    /// <summary>
    /// 测试用的审批业务
    /// </summary>
    private sealed class TestBusiness : IWorkflowBusinessHandler
    {
        public const string Code = "tests-business";

        public const string NameKey = "tests.business.name";

        public WorkflowBusinessDescriptor Descriptor { get; } = new(Code, NameKey, "tests/form/index", [], [], null);

        public ValueTask<Result> OnTaskCompletingAsync(WorkflowBusinessTask task, InvocationContext context) => ValueTask.FromResult(Result.Success());

        public ValueTask<Result> OnChangingAsync(WorkflowBusinessChange change, InvocationContext context) => ValueTask.FromResult(Result.Success());

        public ValueTask<Result> OnOutcomeAsync(WorkflowBusinessOutcome outcome, InvocationContext context) => ValueTask.FromResult(Result.Success());

        public ValueTask<Result> OnAbortedAsync(WorkflowBusinessAborted aborted, InvocationContext context) => ValueTask.FromResult(Result.Success());

        public ValueTask<Result<string>> GetFormDataAsync(string businessId, InvocationContext context) => ValueTask.FromResult(Result.Success("{}"));
    }
}
