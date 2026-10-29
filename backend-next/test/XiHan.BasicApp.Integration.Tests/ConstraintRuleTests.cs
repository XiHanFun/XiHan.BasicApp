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
/// 验证约束规则在指派、授予、继承、委托与会话激活时的判定。
/// </summary>
/// <remarks>
/// 静态职责分离（含继承展开）、基数（角色成员数与用户角色数）、权限互斥与条件在指派、授予、继承与委托时判定，拦截类违规即这一项不做（ACCESS_CONFLICT_012），
/// 警告与只记录类放行并记权限变更日志；动态职责分离在会话激活时判定：登录只激活同组排序最前的，切换角色换激活集，续签按当前规则重新核对。
/// </remarks>
public sealed class ConstraintRuleTests : IDisposable
{
    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private static async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(BasicAppTestServer server, HttpMethod method, string path, string? json, string? token)
    {
        var (response, body) = await server.SendAsync(BasicAppTestServer.Request(method, path, json, token));
        return (response.StatusCode, body);
    }

    private static async Task<(BasicAppTestServer Server, string Admin)> StartAsync(SqliteDatabase database)
    {
        var server = await BasicAppTestServer.StartAsync(database.ConnectionString);
        await server.SeedDefaultsAsync();
        await server.RunSeedersAsync();
        var admin = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;
        return (server, admin);
    }

    private static async Task<string> UserAsync(BasicAppTestServer server, string admin, string userName)
        => (await SendAsync(server, HttpMethod.Post, "/api/users", JsonSerializer.Serialize(new { userName, displayName = userName, password = "Pass@123456" }), admin)).Body.GetProperty("id").GetString()!;

    private static async Task<string> RoleAsync(BasicAppTestServer server, string admin, string code, int order = 0)
        => (await SendAsync(server, HttpMethod.Post, "/api/roles", JsonSerializer.Serialize(new { code, name = code, order }), admin)).Body.GetProperty("id").GetString()!;

    private static async Task<(HttpStatusCode Status, JsonElement Body)> RuleAsync(BasicAppTestServer server, string admin, object rule)
        => await SendAsync(server, HttpMethod.Post, "/api/constraint-rules", JsonSerializer.Serialize(rule), admin);

    private static async Task<JsonElement> AssignAsync(BasicAppTestServer server, string admin, string roleId, params string[] userIds)
        => (await SendAsync(server, HttpMethod.Post, $"/api/roles/{roleId}:assign", JsonSerializer.Serialize(new { userIds }), admin)).Body;

    private static string? FailureOf(JsonElement batch) => batch.GetProperty("failed").EnumerateArray().Select(static f => f.GetProperty("code").GetString()).FirstOrDefault();

    [Fact]
    public async Task StaticRules_GuardAssignmentsGrantsInheritanceAndDelegation()
    {
        var (server, admin) = await StartAsync(_database);
        await using var running = server;
        var dana = await UserAsync(server, admin, "dana");
        var erin = await UserAsync(server, admin, "erin");
        var cashier = await RoleAsync(server, admin, "cashier");
        var auditor = await RoleAsync(server, admin, "auditor");
        var approver = await RoleAsync(server, admin, "approver");
        var viewer = await RoleAsync(server, admin, "viewer");
        var reporter = await RoleAsync(server, admin, "reporter");

        // 写入的校验
        Assert.Equal("ACCESS_VALIDATION_046", (await RuleAsync(server, admin, new { code = "one", name = "one", type = "Ssd", targetType = "Role", items = new[] { new { targetId = cashier } } })).Body.GetProperty("code").GetString());
        Assert.Equal("ACCESS_VALIDATION_045", (await RuleAsync(server, admin, new { code = "bad", name = "bad", type = "MutualExclusion", targetType = "Role", items = new[] { new { targetId = cashier }, new { targetId = auditor } } })).Body.GetProperty("code").GetString());
        Assert.Equal("ACCESS_VALIDATION_048", (await RuleAsync(server, admin, new { code = "ghost", name = "ghost", type = "Ssd", targetType = "Role", items = new[] { new { targetId = cashier }, new { targetId = Guid.NewGuid().ToString() } } })).Body.GetProperty("code").GetString());
        Assert.Equal("ACCESS_VALIDATION_049", (await RuleAsync(server, admin, new { code = "empty", name = "empty", type = "Condition", targetType = "Role", items = new[] { new { targetId = reporter } } })).Body.GetProperty("code").GetString());

        // 静态职责分离：出纳与审计不能同时持有
        var (created, ssd) = await RuleAsync(server, admin, new { code = "cash-audit", name = "Cash vs audit", type = "Ssd", targetType = "Role", priority = 10, items = new[] { new { targetId = cashier }, new { targetId = auditor } } });
        Assert.Equal(HttpStatusCode.Created, created);
        Assert.Equal(2, ssd.GetProperty("items").GetArrayLength());
        Assert.Equal("cashier", ssd.GetProperty("items")[0].GetProperty("targetCode").GetString());
        Assert.Equal("ACCESS_CONFLICT_011", (await RuleAsync(server, admin, new { code = "cash-audit", name = "again", type = "Ssd", targetType = "Role", items = new[] { new { targetId = cashier }, new { targetId = auditor } } })).Body.GetProperty("code").GetString());
        Assert.Null(FailureOf(await AssignAsync(server, admin, cashier, dana)));
        Assert.Equal("ACCESS_CONFLICT_012", FailureOf(await AssignAsync(server, admin, auditor, dana)));
        Assert.Null(FailureOf(await AssignAsync(server, admin, auditor, erin)));

        // 继承展开：审计继承出纳，持有审计的 erin 就同时持有了出纳，整体不做
        var (parented, parentedBody) = await SendAsync(server, HttpMethod.Post, $"/api/roles/{auditor}:set-parents", $$"""{"parentIds":["{{cashier}}"]}""", admin);
        Assert.Equal(HttpStatusCode.Conflict, parented);
        Assert.Equal("ACCESS_CONFLICT_012", parentedBody.GetProperty("code").GetString());

        // 委托角色同样判定：把出纳委托给持有审计的 erin
        var (_, delegated) = await SendAsync(server, HttpMethod.Post, "/api/delegations", $$"""{"delegatorId":"{{dana}}","delegateeId":"{{erin}}","roleId":"{{cashier}}","expiresAt":"{{DateTimeOffset.UtcNow.AddDays(1):O}}"}""", admin);
        Assert.Equal("ACCESS_CONFLICT_012", delegated.GetProperty("code").GetString());

        // 基数：审批人最多一个成员；erin 最多持有一个角色（警告：放行并记日志）
        await RuleAsync(server, admin, new { code = "one-approver", name = "One approver", type = "Cardinality", targetType = "Role", maxAllowed = 1, items = new[] { new { targetId = approver } } });
        await RuleAsync(server, admin, new { code = "erin-roles", name = "Erin roles", type = "Cardinality", targetType = "User", maxAllowed = 1, violationAction = "Warning", items = new[] { new { targetId = erin } } });
        Assert.Null(FailureOf(await AssignAsync(server, admin, approver, dana)));
        Assert.Equal("ACCESS_CONFLICT_012", FailureOf(await AssignAsync(server, admin, approver, erin)));
        Assert.Null(FailureOf(await AssignAsync(server, admin, viewer, erin)));

        // 条件：报表员只能指派给 dana
        await RuleAsync(server, admin, new { code = "reporter-dana", name = "Reporter only Dana", type = "Condition", targetType = "Role", items = new[] { new { targetId = reporter } }, conditions = new[] { new { attributeName = "subject.userName", @operator = "Equals", value = "dana" } } });
        Assert.Equal("ACCESS_CONFLICT_012", FailureOf(await AssignAsync(server, admin, reporter, erin)));
        Assert.Null(FailureOf(await AssignAsync(server, admin, reporter, dana)));

        // 权限互斥：新建与删除用户不能同时直接授予
        await RuleAsync(server, admin, new { code = "create-delete", name = "Create vs delete", type = "MutualExclusion", targetType = "Permission", items = new[] { new { targetId = "identity.users.create" }, new { targetId = "identity.users.delete" } } });
        var (_, granted) = await SendAsync(server, HttpMethod.Post, $"/api/roles/{viewer}:grant", """{"permissions":["identity.users.create","identity.users.delete"]}""", admin);
        Assert.Equal(["identity.users.create"], granted.GetProperty("succeeded").EnumerateArray().Select(static p => p.GetString()));
        Assert.Equal("ACCESS_CONFLICT_012", granted.GetProperty("failed")[0].GetProperty("code").GetString());
        var (_, userGranted) = await SendAsync(server, HttpMethod.Post, "/api/user-grants:grant", $$"""{"userId":"{{dana}}","permissions":["identity.users.delete","identity.users.create"]}""", admin);
        Assert.Equal(["identity.users.delete"], userGranted.GetProperty("succeeded").EnumerateArray().Select(static p => p.GetString()));

        // 停用即不判定；列表按优先级
        var (_, page) = await SendAsync(server, HttpMethod.Post, "/api/constraint-rules:list", """{"page":{"index":1,"size":50}}""", admin);
        Assert.Equal("cash-audit", page.GetProperty("items")[0].GetProperty("code").GetString());
        var ssdId = ssd.GetProperty("id").GetString()!;
        await SendAsync(server, HttpMethod.Post, $"/api/constraint-rules/{ssdId}:set-status", """{"status":"Disabled"}""", admin);
        Assert.Null(FailureOf(await AssignAsync(server, admin, auditor, dana)));

        // 警告类的违规进权限变更日志
        await server.Services.GetRequiredService<DatabaseAuditTrail>().FlushAsync(CancellationToken.None);
        await server.Services.GetRequiredService<AuditLogWriter>().FlushAsync(CancellationToken.None);
        var (_, changes) = await SendAsync(server, HttpMethod.Post, "/api/permission-change-logs:list", """{"page":{"index":1,"size":200}}""", admin);
        Assert.Contains(changes.GetProperty("items").EnumerateArray(), static c => c.GetProperty("changeType").GetString() == "ConstraintViolation" && c.GetProperty("detail").GetString()!.StartsWith("erin-roles", StringComparison.Ordinal));
    }

    [Fact]
    public async Task DynamicSeparation_ActivatesOnePerGroup_SwitchesRoles_AndRechecksOnRefresh()
    {
        var (server, admin) = await StartAsync(_database);
        await using var running = server;
        var dana = await UserAsync(server, admin, "dana");
        var frank = await UserAsync(server, admin, "frank");
        var maker = await RoleAsync(server, admin, "maker", order: 1);
        var checker = await RoleAsync(server, admin, "checker", order: 2);
        var other = await RoleAsync(server, admin, "other");
        await SendAsync(server, HttpMethod.Post, $"/api/roles/{checker}:grant", """{"permissions":["identity.users.read"]}""", admin);
        await AssignAsync(server, admin, maker, dana, frank);
        await AssignAsync(server, admin, checker, dana, frank);

        // frank 先登录：还没有 DSD 规则，两个都激活
        var frankToken = await AuthFlowTests.LoginAsync(server, "frank", "Pass@123456");
        await RuleAsync(server, admin, new { code = "maker-checker", name = "Maker vs checker", type = "Dsd", targetType = "Role", items = new[] { new { targetId = maker }, new { targetId = checker } } });

        // 登录：同组只激活排序最前的（maker），checker 的权限不在
        var token = (await AuthFlowTests.LoginAsync(server, "dana", "Pass@123456")).GetProperty("accessToken").GetString()!;
        async Task<Dictionary<string, bool>> ActiveAsync(string accessToken)
            => (await SendAsync(server, HttpMethod.Get, "/api/auth:session-roles", null, accessToken)).Body.GetProperty("items").EnumerateArray().ToDictionary(static i => i.GetProperty("code").GetString()!, static i => i.GetProperty("isActive").GetBoolean());
        Assert.Equal(new Dictionary<string, bool> { ["maker"] = true, ["checker"] = false }, await ActiveAsync(token));
        Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(server, HttpMethod.Post, "/api/users:list", "{}", token)).Status);

        // 切换角色：换成 checker，换签令牌；两个一起违反 DSD；不持有的不能激活
        var (switched, switchedBody) = await SendAsync(server, HttpMethod.Post, "/api/auth:activate-roles", $$"""{"roleIds":["{{checker}}"]}""", token);
        Assert.Equal(HttpStatusCode.OK, switched);
        token = switchedBody.GetProperty("accessToken").GetString()!;
        Assert.Equal(new Dictionary<string, bool> { ["maker"] = false, ["checker"] = true }, await ActiveAsync(token));
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, "/api/users:list", "{}", token)).Status);
        Assert.Equal("ACCESS_CONFLICT_012", (await SendAsync(server, HttpMethod.Post, "/api/auth:activate-roles", $$"""{"roleIds":["{{maker}}","{{checker}}"]}""", token)).Body.GetProperty("code").GetString());
        Assert.Equal("ACCESS_VALIDATION_050", (await SendAsync(server, HttpMethod.Post, "/api/auth:activate-roles", $$"""{"roleIds":["{{other}}"]}""", token)).Body.GetProperty("code").GetString());

        // 续签按当前规则重新核对：frank 的两个角色只留排序最前的，另一个记停用（原因是规则编码）
        var (_, refreshed) = await SendAsync(server, HttpMethod.Post, "/api/auth:refresh", $$"""{"refreshToken":"{{frankToken.GetProperty("refreshToken").GetString()}}"}""", null);
        Assert.Equal(new Dictionary<string, bool> { ["maker"] = true, ["checker"] = false }, await ActiveAsync(refreshed.GetProperty("accessToken").GetString()!));
        var (_, rows) = await SendAsync(server, HttpMethod.Post, "/api/session-roles:list", $$"""{"page":{"index":1,"size":50},"filters":[{"field":"userId","operator":"eq","value":"{{frank}}"},{"field":"status","operator":"eq","value":"Inactive"}]}""", admin);
        var inactive = Assert.Single(rows.GetProperty("items").EnumerateArray());
        Assert.Equal(("checker", "dsd:maker-checker"), (inactive.GetProperty("roleCode").GetString(), inactive.GetProperty("reason").GetString()));
        Assert.Equal(inactive.GetProperty("id").GetString(), (await SendAsync(server, HttpMethod.Get, $"/api/session-roles/{inactive.GetProperty("id").GetString()}", null, admin)).Body.GetProperty("id").GetString());

        // 切换角色与续签停用都进登录日志
        await server.Services.GetRequiredService<DatabaseAuditTrail>().FlushAsync(CancellationToken.None);
        await server.Services.GetRequiredService<AuditLogWriter>().FlushAsync(CancellationToken.None);
        var (_, logs) = await SendAsync(server, HttpMethod.Post, "/api/login-logs:list", """{"page":{"index":1,"size":100}}""", admin);
        var activations = logs.GetProperty("items").EnumerateArray().Where(static l => l.GetProperty("event").GetString() == "RoleActivation").ToList();
        Assert.Contains(activations, static l => l.GetProperty("userName").GetString() == "dana");
        Assert.Contains(activations, static l => l.GetProperty("detail").GetString()!.StartsWith("dsd:maker-checker", StringComparison.Ordinal));
    }
}
