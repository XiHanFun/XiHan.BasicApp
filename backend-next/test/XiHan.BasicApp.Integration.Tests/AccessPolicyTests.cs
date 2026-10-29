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
/// 访问策略：平台种子写入六个预置策略；新建一律只记录，改成拒绝即按请求属性拦下（403 并记命中），同层的放行压过拒绝；
/// 会拦下发起修改的这次请求时拒绝保存（防自锁）；只引用认证前属性的平台策略连未登录的调用也拦；试运行逐组逐条件说明。
/// 租户策略只管本租户已登录的调用，平台策略管全部调用，平台拒绝时不看租户层。
/// </summary>
public sealed class AccessPolicyTests : IDisposable
{
    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private static async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(BasicAppTestServer server, HttpMethod method, string path, string? json, string? token)
    {
        var (response, body) = await server.SendAsync(BasicAppTestServer.Request(method, path, json, token));
        return (response.StatusCode, body);
    }

    private static async Task<(HttpStatusCode Status, JsonElement Body)> CreateAsync(BasicAppTestServer server, string token, string code, params object[] conditions)
        => await SendAsync(server, HttpMethod.Post, "/api/policies", JsonSerializer.Serialize(new { code, name = code, conditions }), token);

    private static async Task<string> CreateWithActionAsync(BasicAppTestServer server, string token, string code, string action, params object[] conditions)
    {
        var (created, body) = await CreateAsync(server, token, code, conditions);
        Assert.Equal(HttpStatusCode.Created, created);
        var id = body.GetProperty("id").GetString()!;
        var (switched, switchedBody) = await SendAsync(server, HttpMethod.Post, $"/api/policies/{id}:set-action", $$"""{"action":"{{action}}"}""", token);
        Assert.True(switched == HttpStatusCode.OK, switchedBody.ToString());
        return id;
    }

    private static async Task<List<JsonElement>> HitsAsync(BasicAppTestServer server, string token, string policyCode)
    {
        await server.Services.GetRequiredService<DatabaseAuditTrail>().FlushAsync(CancellationToken.None);
        await server.Services.GetRequiredService<AuditLogWriter>().FlushAsync(CancellationToken.None);
        var (_, hits) = await SendAsync(server, HttpMethod.Post, "/api/condition-hits:list", $$"""{"page":{"index":1,"size":50},"filters":[{"field":"source","operator":"eq","value":"AccessPolicy"},{"field":"policyCode","operator":"eq","value":"{{policyCode}}"}]}""", token);
        return [.. hits.GetProperty("items").EnumerateArray()];
    }

    [Fact]
    public async Task Policies_LogOnlyUntilSwitched_DenyBlocks_AllowWinsInLayer_AndCannotLockOutTheEditor()
    {
        var server = await BasicAppTestServer.StartAsync(_database.ConnectionString);
        await using var running = server;
        await server.SeedDefaultsAsync();
        await server.RunSeedersAsync();
        await server.RunSeedersAsync();
        var admin = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;

        // 预置策略：平台侧六条（再跑一次种子不重复），按求值顺序；拒绝类停用、只记录类启用，时机按引用的属性推出
        var (_, seeded) = await SendAsync(server, HttpMethod.Post, "/api/policies:list", """{"page":{"index":1,"size":50}}""", admin);
        var presets = seeded.GetProperty("items").EnumerateArray().ToList();
        Assert.Equal(["preset.ip-blacklist", "preset.admin-office-network", "preset.sensitive-ops-network", "preset.off-hours", "preset.overseas", "preset.block-scripts"], presets.Select(static p => p.GetProperty("code").GetString()));
        Assert.Equal(("Deny", "Disabled", "BeforeAuthentication"), (presets[0].GetProperty("action").GetString(), presets[0].GetProperty("status").GetString(), presets[0].GetProperty("phase").GetString()));
        Assert.Equal(("LogOnly", "Active", "AfterAuthentication"), (presets[5].GetProperty("action").GetString(), presets[5].GetProperty("status").GetString(), presets[5].GetProperty("phase").GetString()));
        Assert.Equal(14, presets[5].GetProperty("conditions").GetArrayLength());
        Assert.Equal((false, true), (presets[0].GetProperty("isEffective").GetBoolean(), presets[5].GetProperty("isEffective").GetBoolean()));
        var (_, templates) = await SendAsync(server, HttpMethod.Get, "/api/policies:templates", null, admin);
        Assert.Equal(6, templates.GetProperty("items").GetArrayLength());
        var (_, attributes) = await SendAsync(server, HttpMethod.Get, "/api/policies:attributes", null, admin);
        Assert.True(attributes.GetProperty("items").EnumerateArray().Single(static a => a.GetProperty("name").GetString() == "environment.ip").GetProperty("isBeforeAuthentication").GetBoolean());

        var (_, user) = await SendAsync(server, HttpMethod.Post, "/api/users", """{"userName":"dana","displayName":"Dana","password":"Dana@123456"}""", admin);
        var danaId = user.GetProperty("id").GetString()!;
        var dana = (await AuthFlowTests.LoginAsync(server, "dana", "Dana@123456")).GetProperty("accessToken").GetString()!;
        async Task<(HttpStatusCode Status, JsonElement Body)> DanaAsync() => await SendAsync(server, HttpMethod.Get, "/api/auth:permissions", null, dana);

        // 新建一律只记录；写入的校验
        var danaClause = new { attributeName = "subject.userName", @operator = "Equals", value = "dana" };
        var (created, policy) = await CreateAsync(server, admin, "block-dana", danaClause);
        Assert.Equal(HttpStatusCode.Created, created);
        Assert.Equal(("LogOnly", "Active"), (policy.GetProperty("action").GetString(), policy.GetProperty("status").GetString()));
        var policyId = policy.GetProperty("id").GetString()!;
        Assert.Equal(HttpStatusCode.OK, (await DanaAsync()).Status);
        Assert.Equal("ACCESS_VALIDATION_037", (await CreateAsync(server, admin, "has space", danaClause)).Body.GetProperty("code").GetString());
        Assert.Equal("ACCESS_CONFLICT_010", (await CreateAsync(server, admin, "block-dana", danaClause)).Body.GetProperty("code").GetString());
        Assert.Equal("SECURITY_VALIDATION_002", (await CreateAsync(server, admin, "shoe", new { attributeName = "subject.shoeSize", @operator = "Equals", value = "42" })).Body.GetProperty("code").GetString());
        var (_, window) = await SendAsync(server, HttpMethod.Post, "/api/policies", """{"code":"window","name":"window","effectiveAt":"2030-01-02T00:00:00Z","expiresAt":"2030-01-01T00:00:00Z","conditions":[{"attributeName":"subject.userName","operator":"Equals","value":"dana"}]}""", admin);
        Assert.Equal("ACCESS_VALIDATION_040", window.GetProperty("code").GetString());

        // 改成拒绝：dana 的调用被拦下，管理员照常
        var (denied, _) = await SendAsync(server, HttpMethod.Post, $"/api/policies/{policyId}:set-action", """{"action":"Deny"}""", admin);
        Assert.Equal(HttpStatusCode.OK, denied);
        var (blocked, blockedBody) = await DanaAsync();
        Assert.Equal(HttpStatusCode.Forbidden, blocked);
        Assert.Equal("ACCESS_DENIED_004", blockedBody.GetProperty("code").GetString());

        // 同层的放行压过拒绝；停用放行即恢复拦截
        var allowId = await CreateWithActionAsync(server, admin, "allow-dana", "Allow", danaClause);
        Assert.Equal(HttpStatusCode.OK, (await DanaAsync()).Status);
        await SendAsync(server, HttpMethod.Post, $"/api/policies/{allowId}:set-status", """{"status":"Disabled"}""", admin);
        Assert.Equal(HttpStatusCode.Forbidden, (await DanaAsync()).Status);

        // 防自锁：会拦下发起修改的这次请求即拒绝保存，策略保持只记录
        var (_, lockMe) = await CreateAsync(server, admin, "lock-me", new { attributeName = "subject.userName", @operator = "Equals", value = "superadmin" });
        var lockMeId = lockMe.GetProperty("id").GetString()!;
        var (refused, refusedBody) = await SendAsync(server, HttpMethod.Post, $"/api/policies/{lockMeId}:set-action", """{"action":"Deny"}""", admin);
        Assert.Equal(HttpStatusCode.BadRequest, refused);
        Assert.Equal("ACCESS_VALIDATION_041", refusedBody.GetProperty("code").GetString());
        Assert.Equal("LogOnly", (await SendAsync(server, HttpMethod.Get, $"/api/policies/{lockMeId}", null, admin)).Body.GetProperty("action").GetString());

        // 只引用认证前属性的平台策略：未登录的登录请求按 User-Agent 拦下
        await CreateWithActionAsync(server, admin, "block-agent", "Deny", new { attributeName = "environment.client.userAgent", @operator = "Contains", value = "blocked-agent" });
        var login = BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:login", """{"userName":"dana","password":"Dana@123456"}""");
        login.Headers.UserAgent.ParseAdd("blocked-agent/1.0");
        var (loginResponse, loginBody) = await server.SendAsync(login);
        Assert.Equal(HttpStatusCode.Forbidden, loginResponse.StatusCode);
        Assert.Equal("ACCESS_DENIED_004", loginBody.GetProperty("code").GetString());

        // 试运行：草稿条件按给定的地址求值；已有策略按给定的用户；地址写错
        var (_, draft) = await SendAsync(server, HttpMethod.Post, "/api/policies:trial", """{"action":"Deny","ip":"10.1.2.3","conditions":[{"attributeName":"environment.ip","operator":"In","value":"10.0.0.0/8"}]}""", admin);
        Assert.Equal(("Matched", true, "BeforeAuthentication"), (draft.GetProperty("outcome").GetString(), draft.GetProperty("wouldBlock").GetBoolean(), draft.GetProperty("phase").GetString()));
        Assert.Equal("Matched", draft.GetProperty("groups")[0].GetProperty("clauses")[0].GetProperty("outcome").GetString());
        var (_, saved) = await SendAsync(server, HttpMethod.Post, "/api/policies:trial", $$"""{"policyId":"{{policyId}}","userId":"{{danaId}}"}""", admin);
        Assert.Equal(("Matched", true, "dana"), (saved.GetProperty("outcome").GetString(), saved.GetProperty("wouldBlock").GetBoolean(), saved.GetProperty("userName").GetString()));
        var (_, anonymous) = await SendAsync(server, HttpMethod.Post, "/api/policies:trial", $$"""{"policyId":"{{policyId}}"}""", admin);
        Assert.NotEqual("Matched", anonymous.GetProperty("outcome").GetString());
        Assert.Equal("ACCESS_VALIDATION_036", (await SendAsync(server, HttpMethod.Post, "/api/policies:trial", $$"""{"policyId":"{{policyId}}","ip":"not-an-ip"}""", admin)).Body.GetProperty("code").GetString());

        // 命中：拦下的记为拦截，被同层放行压过的照记不拦，时机按引用的属性推出；只记录的照记不拦
        var danaHits = await HitsAsync(server, admin, "block-dana");
        Assert.Contains(danaHits, static h => h.GetProperty("action").GetString() == "Deny" && h.GetProperty("isBlocked").GetBoolean());
        Assert.Contains(danaHits, static h => h.GetProperty("action").GetString() == "Deny" && !h.GetProperty("isBlocked").GetBoolean());
        Assert.All(danaHits, static h => Assert.Equal("AfterAuthentication", h.GetProperty("phase").GetString()));
        var agentHit = Assert.Single(await HitsAsync(server, admin, "block-agent"));
        Assert.Equal(("BeforeAuthentication", true), (agentHit.GetProperty("phase").GetString(), agentHit.GetProperty("isBlocked").GetBoolean()));
        Assert.Contains(await HitsAsync(server, admin, "preset.block-scripts"), static h => h.GetProperty("action").GetString() == "LogOnly" && !h.GetProperty("isBlocked").GetBoolean());

        // 删除即不再拦
        var (_, deleted) = await SendAsync(server, HttpMethod.Delete, $"/api/policies?ids={policyId}", null, admin);
        Assert.Single(deleted.GetProperty("succeeded").EnumerateArray());
        Assert.Equal(HttpStatusCode.OK, (await DanaAsync()).Status);
    }

    [Fact]
    public async Task TenantPolicies_GovernOnlyTheirTenant_AndPlatformDenialSkipsTheTenantLayer()
    {
        var (server, platform, tenant) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;

        // 租户策略只管本租户的调用
        await CreateWithActionAsync(server, tenant, "no-roles", "Deny", new { attributeName = "resource.path", @operator = "Contains", value = "/api/roles" });
        var (tenantRoles, tenantRolesBody) = await SendAsync(server, HttpMethod.Post, "/api/roles:list", "{}", tenant);
        Assert.Equal(HttpStatusCode.Forbidden, tenantRoles);
        Assert.Equal("ACCESS_DENIED_004", tenantRolesBody.GetProperty("code").GetString());
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, "/api/roles:list", "{}", platform)).Status);

        // 平台策略管租户的调用；平台拦下时不看租户层，租户层的放行压不过
        await CreateWithActionAsync(server, platform, "no-contoso-users", "Deny",
            new { attributeName = "subject.tenantCode", @operator = "Equals", value = "contoso" },
            new { attributeName = "resource.path", @operator = "Contains", value = "/api/users" });
        await CreateWithActionAsync(server, tenant, "allow-users", "Allow", new { attributeName = "resource.path", @operator = "Contains", value = "/api/users" });
        Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(server, HttpMethod.Post, "/api/users:list", "{}", tenant)).Status);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, "/api/users:list", "{}", platform)).Status);

        // 列表只列当前一侧
        var (_, listed) = await SendAsync(server, HttpMethod.Post, "/api/policies:list", """{"page":{"index":1,"size":50}}""", tenant);
        Assert.Equal(["allow-users", "no-roles"], listed.GetProperty("items").EnumerateArray().Select(static p => p.GetProperty("code").GetString()));
    }
}
