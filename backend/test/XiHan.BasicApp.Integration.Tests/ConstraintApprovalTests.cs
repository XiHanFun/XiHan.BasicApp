// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Auditing.Domain.Services;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.Framework.Auditing.Database.Trail;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证约束规则的「需审批」
/// </summary>
/// <remarks>
/// 管理员给用户指派角色、直授权限触犯需审批类规则时替该用户发起授权申请（已转审批），审批通过才生效并在权限变更日志里记下批准的申请；
/// 拒绝类照旧拦下；没有审批载体的变更（给角色授权限）与导入按不能审批处理；审批未启用时说明是哪条规则要求审批；动态职责分离不能选需审批
/// </remarks>
public sealed class ConstraintApprovalTests : IDisposable
{
    private const string Password = "Pass@123456";

    private static readonly string[] _read = ["identity.users.read"];
    private static readonly string[] _delete = ["identity.users.delete"];
    private static readonly string[] _readAndDelete = ["identity.users.read", "identity.users.delete"];
    private static readonly string[] _cashierAndAuditor = ["cashier", "auditor"];

    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private static string Json<T>(T value) => JsonSerializer.Serialize(value);

    private static async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(BasicAppTestServer server, HttpMethod method, string path, object? body, string token)
    {
        var (response, json) = await server.SendAsync(BasicAppTestServer.Request(method, path, body is null ? null : Json(body), token));
        return (response.StatusCode, json);
    }

    private static async Task<string> CreateAsync(BasicAppTestServer server, string token, string path, object body)
    {
        var (status, json) = await SendAsync(server, HttpMethod.Post, path, body, token);
        Assert.Equal(HttpStatusCode.Created, status);
        return json.GetProperty("id").GetString()!;
    }

    private static Task<string> UserAsync(BasicAppTestServer server, string token, string userName)
        => CreateAsync(server, token, "/api/users", new { userName, displayName = char.ToUpperInvariant(userName[0]) + userName[1..], password = Password });

    private static Task<string> RoleAsync(BasicAppTestServer server, string token, string code)
        => CreateAsync(server, token, "/api/roles", new { code, name = code });

    private static Task<string> RuleAsync(BasicAppTestServer server, string token, string code, string type, string targetType, string action, params string[] targets)
        => CreateAsync(server, token, "/api/constraint-rules", new { code, name = code, type, targetType, violationAction = action, items = targets.Select(static t => new { targetId = t }).ToArray() });

    private static async Task<JsonElement> AssignAsync(BasicAppTestServer server, string token, string roleId, params string[] userIds)
    {
        var (status, body) = await SendAsync(server, HttpMethod.Post, $"/api/roles/{roleId}:assign", new { userIds }, token);
        Assert.Equal(HttpStatusCode.OK, status);
        return body;
    }

    private static List<string> Failures(JsonElement batch)
        => [.. batch.GetProperty("failed").EnumerateArray().Select(static f => f.GetProperty("code").GetString()!)];

    private static async Task<List<string>> RolesOfAsync(BasicAppTestServer server, string token, string userId)
        => [.. (await SendAsync(server, HttpMethod.Get, $"/api/roles:by-user?userId={userId}", null, token)).Body.GetProperty("items").EnumerateArray().Select(static r => r.GetProperty("code").GetString()!)];

    private static async Task<JsonElement> PendingRequestAsync(BasicAppTestServer server, string token, string userId)
    {
        var (_, list) = await SendAsync(server, HttpMethod.Post, "/api/permission-requests:list", new { page = new { index = 1, size = 50 }, filters = new[] { new { field = "status", @operator = "eq", value = "Pending" } } }, token);
        return Assert.Single(list.GetProperty("items").EnumerateArray(), r => r.GetProperty("requestUserId").GetString() == userId);
    }

    private static async Task ApproveAsync(BasicAppTestServer server, string approver, JsonElement request)
    {
        var instance = request.GetProperty("workflowInstanceId").GetString();
        var (_, tasks) = await SendAsync(server, HttpMethod.Post, "/api/tasks:list", new { page = new { index = 1, size = 50 } }, approver);
        var task = Assert.Single(tasks.GetProperty("items").EnumerateArray(), t => t.GetProperty("instanceId").GetString() == instance);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, $"/api/tasks/{task.GetProperty("id").GetString()}:complete", new { outcome = "approved" }, approver)).Status);
    }

    private static async Task<List<string>> PermissionsAsync(BasicAppTestServer server, string userName)
    {
        var token = (await AuthFlowTests.LoginAsync(server, userName, Password)).GetProperty("accessToken").GetString()!;
        return [.. (await SendAsync(server, HttpMethod.Get, "/api/auth:permissions", null, token)).Body.GetProperty("permissions").EnumerateArray().Select(static p => p.GetString()!)];
    }

    [Fact]
    public async Task RequireApproval_TurnsDirectChangesIntoRequests_ThatTakeEffectOnApproval()
    {
        var (server, platform, _) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;
        var alice = await UserAsync(server, platform, "alice");
        var carol = await UserAsync(server, platform, "carol");
        var (_, roles) = await SendAsync(server, HttpMethod.Post, "/api/roles:list", new { page = new { index = 1, size = 100 } }, platform);
        var admin = roles.GetProperty("items").EnumerateArray().Single(static r => r.GetProperty("code").GetString() == "admin").GetProperty("id").GetString()!;
        await AssignAsync(server, platform, admin, carol);
        var approver = (await AuthFlowTests.LoginAsync(server, "carol", Password)).GetProperty("accessToken").GetString()!;

        var cashier = await RoleAsync(server, platform, "cashier");
        var auditor = await RoleAsync(server, platform, "auditor");
        var treasurer = await RoleAsync(server, platform, "treasurer");
        Assert.Empty(Failures(await AssignAsync(server, platform, cashier, alice)));
        await RuleAsync(server, platform, "cash-audit", "Ssd", "Role", "RequireApproval", cashier, auditor);
        await RuleAsync(server, platform, "cash-treasury", "Ssd", "Role", "Deny", cashier, treasurer);

        // 拒绝类照旧拦下，不发起申请；需审批类不指派、替 alice 发起授权申请，再指派一次即已有待审批的同一申请
        Assert.Equal(["ACCESS_CONFLICT_012"], Failures(await AssignAsync(server, platform, treasurer, alice)));
        var routed = await AssignAsync(server, platform, auditor, alice);
        Assert.Equal(["ACCESS_CONFLICT_019"], Failures(routed));
        Assert.Empty(routed.GetProperty("succeeded").EnumerateArray());
        Assert.DoesNotContain("auditor", await RolesOfAsync(server, platform, alice));
        var request = await PendingRequestAsync(server, platform, alice);
        Assert.Equal("auditor", request.GetProperty("roleCode").GetString());
        Assert.Contains("cash-audit", request.GetProperty("reason").GetString(), StringComparison.Ordinal);
        Assert.Equal(["ACCESS_CONFLICT_013"], Failures(await AssignAsync(server, platform, auditor, alice)));

        // 发起人（超管）不在缺省流程的审批人里；carol 同意之后 alice 才成为成员，日志里记下批准它的申请
        var (_, superTasks) = await SendAsync(server, HttpMethod.Post, "/api/tasks:list", new { page = new { index = 1, size = 50 } }, platform);
        Assert.DoesNotContain(superTasks.GetProperty("items").EnumerateArray(), t => t.GetProperty("instanceId").GetString() == request.GetProperty("workflowInstanceId").GetString());
        await ApproveAsync(server, approver, request);
        Assert.Contains("auditor", await RolesOfAsync(server, platform, alice));
        await server.Services.GetRequiredService<DatabaseAuditTrail>().FlushAsync(CancellationToken.None);
        await server.Services.GetRequiredService<AuditLogWriter>().FlushAsync(CancellationToken.None);
        var (_, changes) = await SendAsync(server, HttpMethod.Post, "/api/permission-change-logs:list", new { page = new { index = 1, size = 200 } }, platform);
        Assert.Contains(changes.GetProperty("items").EnumerateArray(), c => c.GetProperty("changeType").GetString() == "ConstraintViolation"
            && c.GetProperty("detail").GetString()!.StartsWith("cash-audit Ssd RequireApproval", StringComparison.Ordinal)
            && c.GetProperty("detail").GetString()!.EndsWith($"request:{request.GetProperty("id").GetString()}", StringComparison.Ordinal));

        // 直授同理；给角色授权限没有审批的载体，按不能审批拦下
        var (granted, grantedBody) = await SendAsync(server, HttpMethod.Post, "/api/user-grants:grant", new { userId = alice, permissions = _read }, platform);
        Assert.Equal((HttpStatusCode.OK, 0), (granted, grantedBody.GetProperty("failed").GetArrayLength()));
        await RuleAsync(server, platform, "read-delete", "MutualExclusion", "Permission", "RequireApproval", "identity.users.read", "identity.users.delete");
        var (_, direct) = await SendAsync(server, HttpMethod.Post, "/api/user-grants:grant", new { userId = alice, permissions = _delete }, platform);
        Assert.Equal(["ACCESS_CONFLICT_019"], Failures(direct));
        Assert.DoesNotContain("identity.users.delete", await PermissionsAsync(server, "alice"));
        var permissionRequest = await PendingRequestAsync(server, platform, alice);
        Assert.Equal("identity.users.delete", permissionRequest.GetProperty("permissionCode").GetString());
        await ApproveAsync(server, approver, permissionRequest);
        Assert.Contains("identity.users.delete", await PermissionsAsync(server, "alice"));
        var (_, roleGrant) = await SendAsync(server, HttpMethod.Post, $"/api/roles/{treasurer}:grant", new { permissions = _readAndDelete }, platform);
        Assert.Equal(["ACCESS_CONFLICT_018"], Failures(roleGrant));

        // 导入时不转申请，提示导入后单独指派
        var (_, imported) = await SendAsync(server, HttpMethod.Post, "/api/users:import", new { password = Password, rows = new[] { new { userName = "dave", displayName = "Dave", roleCodes = _cashierAndAuditor } } }, platform);
        Assert.Equal(("roleCodes", "ACCESS_CONFLICT_018"), (imported.GetProperty("failed")[0].GetProperty("field").GetString(), imported.GetProperty("failed")[0].GetProperty("code").GetString()));
    }

    [Fact]
    public async Task DynamicSeparation_CannotRequireApproval_AndApprovalOff_SaysWhichRuleNeedsIt()
    {
        await using var server = await BasicAppTestServer.StartAsync(_database.ConnectionString, settings: [new("BasicApp:Modules:Workflow:Enabled", "false")]);
        await server.SeedDefaultsAsync();
        await server.RunSeedersAsync();
        var platform = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;
        var alice = await UserAsync(server, platform, "alice");
        var maker = await RoleAsync(server, platform, "maker");
        var checker = await RoleAsync(server, platform, "checker");

        var (dynamic, dynamicBody) = await SendAsync(server, HttpMethod.Post, "/api/constraint-rules", new { code = "maker-checker-session", name = "Maker checker", type = "Dsd", targetType = "Role", violationAction = "RequireApproval", items = new[] { new { targetId = maker }, new { targetId = checker } } }, platform);
        Assert.Equal((HttpStatusCode.BadRequest, "ACCESS_VALIDATION_057"), (dynamic, dynamicBody.GetProperty("code").GetString()));

        await RuleAsync(server, platform, "maker-checker", "Ssd", "Role", "RequireApproval", maker, checker);
        Assert.Empty(Failures(await AssignAsync(server, platform, maker, alice)));
        var (status, body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/roles/{checker}:assign", Json(new { userIds = new[] { alice } }), platform, acceptLanguage: "en-US"));
        Assert.Equal(HttpStatusCode.OK, status.StatusCode);
        var failure = Assert.Single(body.GetProperty("failed").EnumerateArray());
        Assert.Equal("ACCESS_UNAVAILABLE_002", failure.GetProperty("code").GetString());
        Assert.Equal("Constraint rule \"maker-checker\" (maker-checker) requires approval, but approval is not enabled; the change was not made.", failure.GetProperty("message").GetString());
    }
}
