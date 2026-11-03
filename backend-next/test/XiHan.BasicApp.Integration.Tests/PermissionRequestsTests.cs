// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using XiHan.BasicApp.Integration.Tests.Infrastructure;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 授权申请（N3 第三批之五）
/// </summary>
/// <remarks>
/// 平台一侧：提交即发起缺省的「管理员审批」流程（当前分区第一次时建并发布），审批通过即按申请指派角色或直授权限并生效，驳回、撤回、重新提交、
/// 管理员手工处理与取消流程的中止回写；对象二选一、关键字匹配申请人；模仿会话里对申请的写操作一律拒绝；发起即故障时整单回滚；Workflow 关掉时列表照常、提交即审批未启用（方案 W1）
/// </remarks>
public sealed class PermissionRequestsTests : IDisposable
{
    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Requests_RunThroughApproval_AndTakeEffect()
    {
        var (server, platform, _) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;
        var ids = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var name in new[] { "alice", "bob", "carol" })
        {
            var (created, user) = await SendAsync(server, HttpMethod.Post, "/api/users", $$"""{"userName":"{{name}}","displayName":"{{char.ToUpperInvariant(name[0]) + name[1..]}}","password":"Pass@123456"}""", platform);
            Assert.Equal(HttpStatusCode.Created, created);
            ids[name] = user.GetProperty("id").GetString()!;
        }

        // 申请人：查看、提交、重新提交与撤回；carol 进平台的内建管理员角色，与超管一起是缺省流程的审批人
        var requesters = await RoleAsync(server, platform, "requesters", ["access.permission-requests.read", "access.permission-requests.create", "access.permission-requests.update", "access.permission-requests.withdraw"]);
        await SendAsync(server, HttpMethod.Post, $"/api/roles/{requesters}:assign", Json(new { userIds = new[] { ids["alice"], ids["bob"] } }), platform);
        var auditors = await RoleAsync(server, platform, "auditors", ["identity.users.read"]);
        var (_, roles) = await SendAsync(server, HttpMethod.Post, "/api/roles:list", """{"page":{"index":1,"size":100}}""", platform);
        var admin = roles.GetProperty("items").EnumerateArray().Single(static r => r.GetProperty("code").GetString() == "admin").GetProperty("id").GetString()!;
        await SendAsync(server, HttpMethod.Post, $"/api/roles/{admin}:assign", Json(new { userIds = new[] { ids["carol"] } }), platform);
        var alice = await LoginAsync(server, "alice");
        var bob = await LoginAsync(server, "bob");
        var carol = await LoginAsync(server, "carol");

        // 提交：缺省定义按需建并发布，流程等着两位管理员（不含申请人）
        var (created1, request) = await SendAsync(server, HttpMethod.Post, "/api/permission-requests", Json(new { roleId = auditors, reason = "Need to audit users" }), alice);
        Assert.Equal(HttpStatusCode.Created, created1);
        var requestId = request.GetProperty("id").GetString()!;
        Assert.Equal(("Pending", "auditors", true), (request.GetProperty("status").GetString(), request.GetProperty("roleCode").GetString(), request.GetProperty("approvalEnabled").GetBoolean()));
        Assert.Equal(["Carol", "Super Admin"], request.GetProperty("workflow").GetProperty("pendingAssignees").EnumerateArray().Select(static a => a.GetProperty("displayName").GetString()).Order(StringComparer.Ordinal).ToList());
        Assert.Single(await ListAsync(server, "workflow-definitions", platform, """{"filters":[{"field":"name","operator":"eq","value":"permission-request"}]}"""));
        Assert.Equal((HttpStatusCode.Conflict, "ACCESS_CONFLICT_013"), Code(await SendAsync(server, HttpMethod.Post, "/api/permission-requests", Json(new { roleId = auditors, reason = "Again" }), alice)));

        // 审批详情里是单据组件与与详情同形的数据
        var task = Assert.Single(await ListAsync(server, "tasks", carol));
        var (_, detail) = await SendAsync(server, HttpMethod.Get, $"/api/approvals/{task.GetProperty("instanceId").GetString()}", null, carol);
        Assert.Equal("access/permission-request-approval/index", detail.GetProperty("business").GetProperty("formComponent").GetString());
        Assert.Equal(requestId, JsonDocument.Parse(detail.GetProperty("business").GetProperty("data").GetString()!).RootElement.GetProperty("id").GetString());

        // 同意：申请人成了角色成员，重新登录即带上角色的权限
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, $"/api/tasks/{task.GetProperty("id").GetString()}:complete", """{"outcome":"approved"}""", carol)).Status);
        var (_, approved) = await SendAsync(server, HttpMethod.Get, $"/api/permission-requests/{requestId}", null, alice);
        Assert.Equal(("Approved", "Completed"), (approved.GetProperty("status").GetString(), approved.GetProperty("workflow").GetProperty("status").GetString()));
        Assert.Contains("identity.users.read", await PermissionsAsync(server, await LoginAsync(server, "alice")));

        // 直授权限：审批通过即生效，原令牌就看得到
        var (_, direct) = await SendAsync(server, HttpMethod.Post, "/api/permission-requests", Json(new { permissionCode = "access.delegations.read", reason = "Delegation review", expectedExpiresAt = DateTimeOffset.UtcNow.AddDays(7) }), alice);
        Assert.DoesNotContain("access.delegations.read", await PermissionsAsync(server, alice));
        await CompleteAsync(server, carol, direct.GetProperty("id").GetString()!, "approved");
        Assert.Contains("access.delegations.read", await PermissionsAsync(server, alice));

        // 驳回：不授予
        var (_, rejected) = await SendAsync(server, HttpMethod.Post, "/api/permission-requests", Json(new { roleId = auditors, reason = "Want to see users" }), bob);
        await CompleteAsync(server, carol, rejected.GetProperty("id").GetString()!, "rejected");
        Assert.Equal("Rejected", (await SendAsync(server, HttpMethod.Get, $"/api/permission-requests/{rejected.GetProperty("id").GetString()}", null, bob)).Body.GetProperty("status").GetString());
        Assert.DoesNotContain("identity.users.read", await PermissionsAsync(server, await LoginAsync(server, "bob")));

        // 修改后重新提交：旧流程取消、按新内容重新发起
        var (_, pending) = await SendAsync(server, HttpMethod.Post, "/api/permission-requests", Json(new { roleId = auditors, reason = "Second try" }), bob);
        var pendingId = pending.GetProperty("id").GetString()!;
        var firstInstance = pending.GetProperty("workflowInstanceId").GetString()!;
        Assert.Equal((HttpStatusCode.Forbidden, "ACCESS_DENIED_008"), Code(await SendAsync(server, HttpMethod.Put, $"/api/permission-requests/{pendingId}", Json(new { roleId = auditors, reason = "Not mine" }), alice)));
        var (resubmitted, changed) = await SendAsync(server, HttpMethod.Put, $"/api/permission-requests/{pendingId}", Json(new { permissionCode = "access.delegations.read", reason = "Only delegations" }), bob);
        Assert.Equal(HttpStatusCode.OK, resubmitted);
        Assert.NotEqual(firstInstance, changed.GetProperty("workflowInstanceId").GetString());
        Assert.Equal(("Pending", "access.delegations.read"), (changed.GetProperty("status").GetString(), changed.GetProperty("permissionCode").GetString()));
        Assert.Equal("Canceled", (await SendAsync(server, HttpMethod.Get, $"/api/workflow-instances/{firstInstance}", null, platform)).Body.GetProperty("instance").GetProperty("status").GetString());

        // 申请人撤回：流程一起撤回，已完结的不能再撤
        var (_, withdrawn) = await SendAsync(server, HttpMethod.Post, "/api/permission-requests:withdraw", Json(new { ids = new[] { pendingId, requestId } }), bob);
        Assert.Equal([pendingId], withdrawn.GetProperty("succeeded").EnumerateArray().Select(static i => i.GetString()).ToList());
        Assert.Equal("ACCESS_DENIED_008", withdrawn.GetProperty("failed")[0].GetProperty("code").GetString());
        Assert.Equal("Canceled", (await SendAsync(server, HttpMethod.Get, $"/api/workflow-instances/{changed.GetProperty("workflowInstanceId").GetString()}", null, platform)).Body.GetProperty("instance").GetProperty("status").GetString());

        // 管理员手工处理：不能置为已批准、不能处理自己的；驳回别人的同时撤回流程
        var (_, mine) = await SendAsync(server, HttpMethod.Post, "/api/permission-requests", Json(new { roleId = auditors, reason = "Admin wants it" }), platform);
        Assert.Equal((HttpStatusCode.BadRequest, "ACCESS_VALIDATION_054"), Code(await SendAsync(server, HttpMethod.Post, "/api/permission-requests:set-status", Json(new { ids = new[] { mine.GetProperty("id").GetString() }, status = "Approved" }), platform)));
        Assert.Equal("ACCESS_DENIED_009", (await SendAsync(server, HttpMethod.Post, "/api/permission-requests:set-status", Json(new { ids = new[] { mine.GetProperty("id").GetString() }, status = "Rejected" }), platform)).Body.GetProperty("failed")[0].GetProperty("code").GetString());
        var (_, closed) = await SendAsync(server, HttpMethod.Post, "/api/permission-requests:set-status", Json(new { ids = new[] { mine.GetProperty("id").GetString() }, status = "Rejected", remark = "Not needed" }), carol);
        Assert.Single(closed.GetProperty("succeeded").EnumerateArray());
        var (_, after) = await SendAsync(server, HttpMethod.Get, $"/api/permission-requests/{mine.GetProperty("id").GetString()}", null, platform);
        Assert.Equal(("Rejected", "Not needed", "Canceled"), (after.GetProperty("status").GetString(), after.GetProperty("remark").GetString(), after.GetProperty("workflow").GetProperty("status").GetString()));

        // 管理员取消流程：中止回写把待审批的申请置为已撤回
        var (_, aborted) = await SendAsync(server, HttpMethod.Post, "/api/permission-requests", Json(new { roleId = auditors, reason = "Will be aborted" }), bob);
        Assert.Empty((await SendAsync(server, HttpMethod.Post, "/api/workflow-instances:cancel", Json(new { ids = new[] { aborted.GetProperty("workflowInstanceId").GetString() }, reason = "Stop" }), platform)).Body.GetProperty("failed").EnumerateArray());
        var (_, abortedAfter) = await SendAsync(server, HttpMethod.Get, $"/api/permission-requests/{aborted.GetProperty("id").GetString()}", null, bob);
        Assert.Equal(("Withdrawn", "Stop"), (abortedAfter.GetProperty("status").GetString(), abortedAfter.GetProperty("remark").GetString()));

        // 列表：这一侧全部的申请，按状态筛选
        Assert.Equal(2, (await ListAsync(server, "permission-requests", alice, """{"filters":[{"field":"status","operator":"eq","value":"Approved"}]}""")).Count);
    }

    [Fact]
    public async Task TargetIsExclusive_KeywordMatchesRequester_AndImpersonationCannotWrite()
    {
        var (server, platform, _) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;
        var ids = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (name, display) in new[] { ("alice", "Alice Liddell"), ("carol", "Carol") })
        {
            var (created, user) = await SendAsync(server, HttpMethod.Post, "/api/users", Json(new { userName = name, displayName = display, password = "Pass@123456" }), platform);
            Assert.Equal(HttpStatusCode.Created, created);
            ids[name] = user.GetProperty("id").GetString()!;
        }

        var requesters = await RoleAsync(server, platform, "requesters", ["access.permission-requests.read", "access.permission-requests.create", "access.permission-requests.update", "access.permission-requests.withdraw"]);
        await SendAsync(server, HttpMethod.Post, $"/api/roles/{requesters}:assign", Json(new { userIds = new[] { ids["alice"] } }), platform);
        var auditors = await RoleAsync(server, platform, "auditors", ["identity.users.read"]);
        var (_, roles) = await SendAsync(server, HttpMethod.Post, "/api/roles:list", """{"page":{"index":1,"size":100}}""", platform);
        var admin = roles.GetProperty("items").EnumerateArray().Single(static r => r.GetProperty("code").GetString() == "admin").GetProperty("id").GetString()!;
        await SendAsync(server, HttpMethod.Post, $"/api/roles/{admin}:assign", Json(new { userIds = new[] { ids["carol"] } }), platform);
        var alice = await LoginAsync(server, "alice");
        var carol = await LoginAsync(server, "carol");

        // 角色与权限码二选一：都给、都不给，提交与修改重提都校验
        Assert.Equal((HttpStatusCode.BadRequest, "ACCESS_VALIDATION_056"), Code(await SendAsync(server, HttpMethod.Post, "/api/permission-requests", Json(new { roleId = auditors, permissionCode = "access.delegations.read", reason = "Both" }), alice)));
        var (_, request) = await SendAsync(server, HttpMethod.Post, "/api/permission-requests", Json(new { roleId = auditors, reason = "Need to audit users" }), alice);
        var requestId = request.GetProperty("id").GetString()!;
        var instanceId = request.GetProperty("workflowInstanceId").GetString()!;
        Assert.Equal((HttpStatusCode.BadRequest, "ACCESS_VALIDATION_056"), Code(await SendAsync(server, HttpMethod.Put, $"/api/permission-requests/{requestId}", Json(new { roleId = auditors, permissionCode = "access.delegations.read", reason = "Both" }), alice)));
        Assert.Equal((HttpStatusCode.BadRequest, "ACCESS_VALIDATION_051"), Code(await SendAsync(server, HttpMethod.Put, $"/api/permission-requests/{requestId}", Json(new { reason = "Neither" }), alice)));

        // 关键字照 4.x 也匹配申请人：登录名、显示名（经用户目录），以及申请原因与备注
        Assert.Single(await ListAsync(server, "permission-requests", carol, """{"keyword":"alice"}"""));
        Assert.Single(await ListAsync(server, "permission-requests", carol, """{"keyword":"Liddell"}"""));
        Assert.Single(await ListAsync(server, "permission-requests", carol, """{"keyword":"audit users"}"""));
        Assert.Empty(await ListAsync(server, "permission-requests", carol, """{"keyword":"carol"}"""));

        // 模仿会话里对授权申请的写操作一律 409（6.0 新加，4.x 没有）：申请方的提交、修改重提、撤回与审批中心撤回
        var asAlice = await ImpersonateAsync(server, ids["alice"]);
        Assert.Equal((HttpStatusCode.Conflict, "ACCESS_CONFLICT_016"), Code(await SendAsync(server, HttpMethod.Post, "/api/permission-requests", Json(new { permissionCode = "access.delegations.read", reason = "As someone else" }), asAlice)));
        Assert.Equal((HttpStatusCode.Conflict, "ACCESS_CONFLICT_016"), Code(await SendAsync(server, HttpMethod.Put, $"/api/permission-requests/{requestId}", Json(new { roleId = auditors, reason = "Changed" }), asAlice)));
        Assert.Equal((HttpStatusCode.Conflict, "ACCESS_CONFLICT_016"), Code(await SendAsync(server, HttpMethod.Post, "/api/permission-requests:withdraw", Json(new { ids = new[] { requestId } }), asAlice)));
        Assert.Equal((HttpStatusCode.Conflict, "ACCESS_CONFLICT_016"), Code(await SendAsync(server, HttpMethod.Post, $"/api/approvals/{instanceId}:withdraw", "{}", asAlice)));
        Assert.Single(await ListAsync(server, "permission-requests", asAlice));

        // 审批方：同意、驳回、转办、加签与手工处理
        var asCarol = await ImpersonateAsync(server, ids["carol"]);
        var task = Assert.Single(await ListAsync(server, "tasks", asCarol));
        var taskId = task.GetProperty("id").GetString();
        var superadminId = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("user").GetProperty("id").GetString();
        Assert.Equal((HttpStatusCode.Conflict, "ACCESS_CONFLICT_016"), Code(await SendAsync(server, HttpMethod.Post, $"/api/tasks/{taskId}:complete", """{"outcome":"approved"}""", asCarol)));
        Assert.Equal((HttpStatusCode.Conflict, "ACCESS_CONFLICT_016"), Code(await SendAsync(server, HttpMethod.Post, $"/api/tasks/{taskId}:complete", """{"outcome":"rejected"}""", asCarol)));
        Assert.Equal((HttpStatusCode.Conflict, "ACCESS_CONFLICT_016"), Code(await SendAsync(server, HttpMethod.Post, $"/api/tasks/{taskId}:transfer", Json(new { targetUserId = superadminId }), asCarol)));
        Assert.Equal((HttpStatusCode.Conflict, "ACCESS_CONFLICT_016"), Code(await SendAsync(server, HttpMethod.Post, $"/api/tasks/{taskId}:add-assignees", Json(new { userIds = new[] { superadminId } }), asCarol)));
        Assert.Equal((HttpStatusCode.Conflict, "ACCESS_CONFLICT_016"), Code(await SendAsync(server, HttpMethod.Post, "/api/permission-requests:set-status", Json(new { ids = new[] { requestId }, status = "Rejected" }), asCarol)));

        // 管理员控制流程：中止前就拒绝，那一项记失败、流程不动；找不到的实例照常由引擎报
        var missing = Guid.NewGuid().ToString();
        foreach (var (action, body) in new[] { ("cancel", Json(new { ids = new[] { instanceId, missing }, reason = "Stop" })), ("terminate", Json(new { ids = new[] { instanceId }, reason = "Stop" })), ("suspend", Json(new { ids = new[] { instanceId } })) })
        {
            var (_, batch) = await SendAsync(server, HttpMethod.Post, $"/api/workflow-instances:{action}", body, asCarol);
            Assert.Empty(batch.GetProperty("succeeded").EnumerateArray());
            Assert.Equal("ACCESS_CONFLICT_016", batch.GetProperty("failed")[0].GetProperty("code").GetString());
        }

        var (_, still) = await SendAsync(server, HttpMethod.Get, $"/api/permission-requests/{requestId}", null, carol);
        Assert.Equal(("Pending", "Running"), (still.GetProperty("status").GetString(), still.GetProperty("workflow").GetProperty("status").GetString()));

        // 正常会话照常：审批通过即授予
        await CompleteAsync(server, carol, requestId, "approved");
        Assert.Equal("Approved", (await SendAsync(server, HttpMethod.Get, $"/api/permission-requests/{requestId}", null, alice)).Body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task StartFaulted_RollsBack_AndWorkflowOff_ListsWithoutApproval()
    {
        // 平台只有超管一个管理员：超管自己申请时缺省流程找不到审批人（不含申请人），发起即故障，申请随之回滚
        var (server, platform, _) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using (server)
        {
            var target = await RoleAsync(server, platform, "auditors", ["identity.users.read"]);
            var (faulted, faultedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/permission-requests", Json(new { roleId = target, reason = "Nobody approves" }), platform, "zh-CN"));
            Assert.Equal((HttpStatusCode.Conflict, "WORKFLOW_CONFLICT_101"), (faulted.StatusCode, faultedBody.GetProperty("code").GetString()));

            // 中文调用方看到的发起失败原因是中文（框架把引擎的故障原因存成消息键，ADR-0105）
            Assert.Equal("审批流程发起即故障：用户任务 'admin-approve' 没有解析出指派人。", faultedBody.GetProperty("detail").GetString());
            Assert.Empty(await ListAsync(server, "permission-requests", platform));
            Assert.Equal((HttpStatusCode.BadRequest, "ACCESS_VALIDATION_051"), Code(await SendAsync(server, HttpMethod.Post, "/api/permission-requests", Json(new { reason = "Nothing" }), platform)));
            Assert.Equal((HttpStatusCode.BadRequest, "ACCESS_VALIDATION_052"), Code(await SendAsync(server, HttpMethod.Post, "/api/permission-requests", Json(new { permissionCode = "nothing.here.read", reason = "Unknown" }), platform)));
            Assert.Equal((HttpStatusCode.BadRequest, "ACCESS_VALIDATION_053"), Code(await SendAsync(server, HttpMethod.Post, "/api/permission-requests", Json(new { roleId = target, reason = "Past", expectedExpiresAt = DateTimeOffset.UtcNow.AddDays(-1) }), platform)));
        }

        // Workflow 关掉：列表照常、进度为空并标审批未启用，提交即 503
        await using var off = await BasicAppTestServer.StartAsync(_database.ConnectionString, settings: [new("BasicApp:Modules:Workflow:Enabled", "false")]);
        await off.SeedDefaultsAsync();
        var token = (await AuthFlowTests.LoginAsync(off, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;
        var (status, page) = await SendAsync(off, HttpMethod.Post, "/api/permission-requests:list", "{}", token);
        Assert.Equal((HttpStatusCode.OK, 0), (status, page.GetProperty("total").GetInt32()));
        var (_, roleBody) = await SendAsync(off, HttpMethod.Post, "/api/roles", """{"code":"viewers","name":"Viewers"}""", token);
        Assert.Equal((HttpStatusCode.ServiceUnavailable, "ACCESS_UNAVAILABLE_002"), Code(await SendAsync(off, HttpMethod.Post, "/api/permission-requests", Json(new { roleId = roleBody.GetProperty("id").GetString(), reason = "No approval" }), token)));
    }

    private static async Task<string> RoleAsync(BasicAppTestServer server, string token, string code, string[] permissions)
    {
        var (created, role) = await SendAsync(server, HttpMethod.Post, "/api/roles", Json(new { code, name = code }), token);
        Assert.Equal(HttpStatusCode.Created, created);
        var id = role.GetProperty("id").GetString()!;
        await SendAsync(server, HttpMethod.Post, $"/api/roles/{id}:grant", Json(new { permissions }), token);
        return id;
    }

    private static async Task CompleteAsync(BasicAppTestServer server, string approver, string requestId, string outcome)
    {
        var tasks = await ListAsync(server, "tasks", approver);
        var (_, request) = await SendAsync(server, HttpMethod.Get, $"/api/permission-requests/{requestId}", null, approver);
        var instance = request.GetProperty("workflowInstanceId").GetString();
        var task = Assert.Single(tasks, t => t.GetProperty("instanceId").GetString() == instance);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, $"/api/tasks/{task.GetProperty("id").GetString()}:complete", Json(new { outcome }), approver)).Status);
    }

    private static async Task<string> ImpersonateAsync(BasicAppTestServer server, string userId)
    {
        // 每次用超管的一个新会话发起，一个会话同时只挂一条模仿会话
        var superadmin = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;
        var (started, body) = await SendAsync(server, HttpMethod.Post, "/api/impersonation:start", Json(new { targetUserId = userId, reason = "tests" }), superadmin);
        Assert.Equal(HttpStatusCode.OK, started);
        return body.GetProperty("accessToken").GetString()!;
    }

    private static async Task<List<string>> PermissionsAsync(BasicAppTestServer server, string token)
        => (await SendAsync(server, HttpMethod.Get, "/api/auth:permissions", null, token)).Body.GetProperty("permissions").EnumerateArray().Select(static p => p.GetString()!).ToList();

    private static async Task<string> LoginAsync(BasicAppTestServer server, string userName)
        => (await AuthFlowTests.LoginAsync(server, userName, "Pass@123456")).GetProperty("accessToken").GetString()!;

    private static (HttpStatusCode Status, string? Code) Code((HttpStatusCode Status, JsonElement Body) response)
        => (response.Status, response.Body.ValueKind == JsonValueKind.Object && response.Body.TryGetProperty("code", out var code) ? code.GetString() : null);

    private static string Json<T>(T value) => JsonSerializer.Serialize(value);

    private static async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(BasicAppTestServer server, HttpMethod method, string path, string? body, string token)
    {
        var (response, json) = await server.SendAsync(BasicAppTestServer.Request(method, path, body, token));
        return (response.StatusCode, json);
    }

    private static async Task<List<JsonElement>> ListAsync(BasicAppTestServer server, string resource, string token, string body = """{"page":{"index":1,"size":50}}""")
    {
        var (response, json) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/{resource}:list", body, token));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return json.GetProperty("items").EnumerateArray().ToList();
    }
}
