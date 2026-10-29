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
/// 角色继承与限时委托：上级的授予传给下级角色的持有人、上级的拒绝作用于整条链、停用的上级截断继承，写入防环与防冗余、仍被继承的角色不能删；
/// 委托人此刻持有才能委托、委托人失去即委托失效、未到生效时间的到点自动生效、到期与撤销立即失效；这些变更都进权限变更日志。
/// </summary>
public sealed class InheritanceDelegationTests : IDisposable
{
    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private static async Task<(BasicAppTestServer Server, ManualClock Clock, string Admin)> StartAsync(SqliteDatabase database)
    {
        var clock = new ManualClock(DateTimeOffset.UtcNow);
        var server = await BasicAppTestServer.StartAsync(database.ConnectionString, configure: builder => builder.Services.AddSingleton<TimeProvider>(clock));
        await server.SeedDefaultsAsync();
        await server.RunSeedersAsync();
        var admin = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;
        return (server, clock, admin);
    }

    private static async Task<string> CreateUserAsync(BasicAppTestServer server, string admin, string userName)
    {
        var (created, body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users", $$"""{"userName":"{{userName}}","displayName":"{{userName}}","password":"Pass@123456"}""", admin));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        return body.GetProperty("id").GetString()!;
    }

    private static async Task<string> CreateRoleAsync(BasicAppTestServer server, string admin, string code, params string[] permissions)
    {
        var (_, body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/roles", $$"""{"code":"{{code}}","name":"{{code}}"}""", admin));
        var roleId = body.GetProperty("id").GetString()!;
        if (permissions.Length > 0)
        {
            var (granted, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/roles/{roleId}:grant", JsonSerializer.Serialize(new { permissions }), admin));
            Assert.Equal(HttpStatusCode.OK, granted.StatusCode);
        }

        return roleId;
    }

    private static async Task<List<string?>> ChangeTypesAsync(BasicAppTestServer server, string token)
    {
        await server.Services.GetRequiredService<DatabaseAuditTrail>().FlushAsync(CancellationToken.None);
        await server.Services.GetRequiredService<AuditLogWriter>().FlushAsync(CancellationToken.None);
        var (_, json) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/permission-change-logs:list", """{"page":{"index":1,"size":200}}""", token));
        return json.GetProperty("items").EnumerateArray().Select(static c => c.GetProperty("changeType").GetString()).ToList();
    }

    private static async Task<bool> HoldsAsync(BasicAppTestServer server, string token, string permission)
    {
        var (_, body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:permissions", token: token));
        return body.GetProperty("permissions").EnumerateArray().Any(p => p.GetString() == permission);
    }

    [Fact]
    public async Task Inheritance_PassesParentGrantsAndDenies_AlongActiveParents()
    {
        var (server, _, admin) = await StartAsync(_database);
        await using var running = server;
        var danaId = await CreateUserAsync(server, admin, "dana");
        var baseRole = await CreateRoleAsync(server, admin, "base", "identity.users.read");
        var lead = await CreateRoleAsync(server, admin, "lead", "identity.users.update");
        var child = await CreateRoleAsync(server, admin, "child");
        var (assigned, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/roles/{child}:assign", $$"""{"userIds":["{{danaId}}"]}""", admin));
        Assert.Equal(HttpStatusCode.OK, assigned.StatusCode);
        var dana = (await AuthFlowTests.LoginAsync(server, "dana", "Pass@123456")).GetProperty("accessToken").GetString()!;
        async Task<HttpStatusCode> ListUsersAsync() => (await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users:list", "{}", dana))).Response.StatusCode;
        Assert.Equal(HttpStatusCode.Forbidden, await ListUsersAsync());

        // child ← lead ← base：持有 child 即拿到两级上级的授予，令牌不必刷新
        var (leadParents, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/roles/{lead}:set-parents", $$"""{"parentIds":["{{baseRole}}"]}""", admin));
        Assert.Equal(HttpStatusCode.OK, leadParents.StatusCode);
        var (childParents, childBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/roles/{child}:set-parents", $$"""{"parentIds":["{{lead}}"]}""", admin));
        Assert.Equal(HttpStatusCode.OK, childParents.StatusCode);
        Assert.Equal(["lead", "base"], childBody.GetProperty("ancestors").EnumerateArray().Select(static a => a.GetProperty("code").GetString()));
        Assert.Equal(["base", "lead", "child"], childBody.GetProperty("ancestors")[1].GetProperty("path").EnumerateArray().Select(static p => p.GetString()));
        Assert.Equal(["identity.users.update", "identity.users.read"], childBody.GetProperty("inherited").EnumerateArray().Select(static i => i.GetProperty("permission").GetString()));
        Assert.Equal(HttpStatusCode.OK, await ListUsersAsync());
        Assert.True(await HoldsAsync(server, dana, "identity.users.update"));
        var (_, baseHierarchy) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/roles/{baseRole}:hierarchy", token: admin));
        Assert.Equal(["lead", "child"], baseHierarchy.GetProperty("descendants").EnumerateArray().Select(static d => d.GetProperty("code").GetString()));

        // 写入规则：不能继承自己、不能成环、直接上级之间不能互为祖先、内置角色不参与
        async Task<string?> SetParentsAsync(string roleId, params string[] parentIds)
        {
            var (response, body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/roles/{roleId}:set-parents", JsonSerializer.Serialize(new { parentIds }), admin));
            return response.StatusCode == HttpStatusCode.OK ? null : body.GetProperty("code").GetString();
        }

        Assert.Equal("ACCESS_VALIDATION_022", await SetParentsAsync(child, child));
        Assert.Equal("ACCESS_VALIDATION_025", await SetParentsAsync(baseRole, child));
        Assert.Equal("ACCESS_VALIDATION_026", await SetParentsAsync(child, lead, baseRole));
        var (_, rolesBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/roles:list", "{}", admin));
        var builtIn = rolesBody.GetProperty("items").EnumerateArray().First(static r => r.GetProperty("isBuiltIn").GetBoolean()).GetProperty("id").GetString()!;
        Assert.Equal("ACCESS_VALIDATION_023", await SetParentsAsync(child, builtIn));

        // 上级的拒绝作用于整条链：base 拒绝 update，lead 授予的 update 也拿不到
        var (denied, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/roles/{baseRole}:deny", """{"permissions":["identity.users.update"]}""", admin));
        Assert.Equal(HttpStatusCode.OK, denied.StatusCode);
        Assert.False(await HoldsAsync(server, dana, "identity.users.update"));
        await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/roles/{baseRole}:revoke", """{"permissions":["identity.users.update"]}""", admin));
        Assert.True(await HoldsAsync(server, dana, "identity.users.update"));

        // 停用的上级截断继承：停用 lead，base 也传不到 child
        var (disabled, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/roles/{lead}", """{"name":"lead","status":"Disabled"}""", admin));
        Assert.Equal(HttpStatusCode.OK, disabled.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, await ListUsersAsync());
        var (_, truncated) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/roles/{child}:hierarchy", token: admin));
        Assert.All(truncated.GetProperty("ancestors").EnumerateArray(), static a => Assert.False(a.GetProperty("isEffective").GetBoolean()));
        Assert.Empty(truncated.GetProperty("inherited").EnumerateArray());
        var other = await CreateRoleAsync(server, admin, "other");
        Assert.Equal("ACCESS_VALIDATION_024", await SetParentsAsync(other, lead));
        await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/roles/{lead}", """{"name":"lead","status":"Active"}""", admin));
        Assert.Equal(HttpStatusCode.OK, await ListUsersAsync());

        // 仍被继承的角色不能删；下级可以删，连同它的边
        var (blocked, blockedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Delete, $"/api/roles?ids={baseRole}", token: admin));
        Assert.Equal(HttpStatusCode.OK, blocked.StatusCode);
        Assert.Equal("ACCESS_CONFLICT_008", blockedBody.GetProperty("failed")[0].GetProperty("code").GetString());
        var (removed, removedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Delete, $"/api/roles?ids={child}", token: admin));
        Assert.Equal(HttpStatusCode.OK, removed.StatusCode);
        Assert.Equal(1, removedBody.GetProperty("succeeded").GetArrayLength());
        var (_, leadHierarchy) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/roles/{lead}:hierarchy", token: admin));
        Assert.Empty(leadHierarchy.GetProperty("descendants").EnumerateArray());

        // 清空上级即解除继承
        Assert.Null(await SetParentsAsync(lead));
        var (unblocked, unblockedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Delete, $"/api/roles?ids={baseRole}", token: admin));
        Assert.Equal(HttpStatusCode.OK, unblocked.StatusCode);
        Assert.Equal(1, unblockedBody.GetProperty("succeeded").GetArrayLength());

        Assert.Contains("RoleInheritance", await ChangeTypesAsync(server, admin));
    }

    [Fact]
    public async Task Delegations_LendWhatTheDelegatorHolds_ForTheirWindow()
    {
        var (server, clock, admin) = await StartAsync(_database);
        await using var running = server;
        var aliceId = await CreateUserAsync(server, admin, "alice");
        var bobId = await CreateUserAsync(server, admin, "bob");
        var approver = await CreateRoleAsync(server, admin, "approver", "identity.users.read");
        var (assigned, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/roles/{approver}:assign", $$"""{"userIds":["{{aliceId}}"]}""", admin));
        Assert.Equal(HttpStatusCode.OK, assigned.StatusCode);
        var bob = (await AuthFlowTests.LoginAsync(server, "bob", "Pass@123456")).GetProperty("accessToken").GetString()!;
        async Task<HttpStatusCode> ListUsersAsync() => (await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users:list", "{}", bob))).Response.StatusCode;
        Assert.Equal(HttpStatusCode.Forbidden, await ListUsersAsync());

        async Task<(HttpStatusCode Status, JsonElement Body)> CreateAsync(object request)
        {
            var (response, body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/delegations", JsonSerializer.Serialize(request), admin));
            return (response.StatusCode, body);
        }

        var hourLater = clock.GetUtcNow().AddHours(1);

        // 新建的校验：同一人、目标不是恰好一个、到期已过、委托人没有
        Assert.Equal("ACCESS_VALIDATION_027", (await CreateAsync(new { delegatorId = aliceId, delegateeId = aliceId, permission = "identity.users.read", expiresAt = hourLater })).Body.GetProperty("code").GetString());
        Assert.Equal("ACCESS_VALIDATION_028", (await CreateAsync(new { delegatorId = aliceId, delegateeId = bobId, expiresAt = hourLater })).Body.GetProperty("code").GetString());
        Assert.Equal("ACCESS_VALIDATION_029", (await CreateAsync(new { delegatorId = aliceId, delegateeId = bobId, permission = "identity.users.read", expiresAt = clock.GetUtcNow().AddMinutes(-1) })).Body.GetProperty("code").GetString());
        Assert.Equal("ACCESS_VALIDATION_030", (await CreateAsync(new { delegatorId = aliceId, delegateeId = bobId, permission = "identity.users.delete", expiresAt = hourLater })).Body.GetProperty("code").GetString());

        // 委托权限码：立即生效；重复的未到期委托冲突
        var (created, delegation) = await CreateAsync(new { delegatorId = aliceId, delegateeId = bobId, permission = "identity.users.read", expiresAt = hourLater, reason = "vacation" });
        Assert.Equal(HttpStatusCode.Created, created);
        Assert.Equal("Active", delegation.GetProperty("status").GetString());
        Assert.Equal("alice (@alice)", delegation.GetProperty("delegatorName").GetString());
        Assert.Equal(HttpStatusCode.OK, await ListUsersAsync());
        Assert.Equal("ACCESS_CONFLICT_007", (await CreateAsync(new { delegatorId = aliceId, delegateeId = bobId, permission = "identity.users.read", expiresAt = hourLater })).Body.GetProperty("code").GetString());

        // 委托人失去即委托失效，恢复即恢复
        await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/roles/{approver}:unassign", $$"""{"userIds":["{{aliceId}}"]}""", admin));
        Assert.Equal(HttpStatusCode.Forbidden, await ListUsersAsync());
        await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/roles/{approver}:assign", $$"""{"userIds":["{{aliceId}}"]}""", admin));
        Assert.Equal(HttpStatusCode.OK, await ListUsersAsync());

        // 撤销立即失效；再撤销即冲突
        var delegationId = delegation.GetProperty("id").GetString()!;
        var (revoked, revokedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/delegations/{delegationId}:revoke", "{}", admin));
        Assert.Equal(HttpStatusCode.OK, revoked.StatusCode);
        Assert.Equal("Revoked", revokedBody.GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.Forbidden, await ListUsersAsync());
        var (again, againBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/delegations/{delegationId}:revoke", "{}", admin));
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal("ACCESS_CONFLICT_009", againBody.GetProperty("code").GetString());

        // 委托角色、晚些生效：到点自动生效，按角色的链拿到授予；到期即失效
        var (pending, pendingBody) = await CreateAsync(new { delegatorId = aliceId, delegateeId = bobId, roleId = approver, effectiveAt = clock.GetUtcNow().AddHours(1), expiresAt = clock.GetUtcNow().AddHours(2) });
        Assert.Equal(HttpStatusCode.Created, pending);
        Assert.Equal("Pending", pendingBody.GetProperty("status").GetString());
        Assert.Equal("approver", pendingBody.GetProperty("roleName").GetString());
        Assert.Equal(HttpStatusCode.Forbidden, await ListUsersAsync());
        clock.Set(clock.GetUtcNow().AddMinutes(90));
        Assert.Equal(HttpStatusCode.OK, await ListUsersAsync());
        async Task<List<string?>> StatusesAsync(string filter)
        {
            var (_, body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/delegations:list", filter, admin));
            return body.GetProperty("items").EnumerateArray().Select(static d => d.GetProperty("status").GetString()).ToList();
        }

        Assert.Equal(["Active"], await StatusesAsync("""{"filters":[{"field":"status","operator":"eq","value":"Active"}]}"""));
        clock.Set(clock.GetUtcNow().AddHours(1));
        Assert.Equal(HttpStatusCode.Forbidden, await ListUsersAsync());
        Assert.Equal(["Expired", "Revoked"], (await StatusesAsync("""{"filters":[{"field":"status","operator":"in","value":"Expired,Revoked"}]}""")).Order(StringComparer.Ordinal));

        // 删除记录
        var (deleted, deletedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Delete, $"/api/delegations?ids={delegationId}", token: admin));
        Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);
        Assert.Equal(1, deletedBody.GetProperty("succeeded").GetArrayLength());
        Assert.Single(await StatusesAsync("{}"));

        Assert.Contains("Delegation", await ChangeTypesAsync(server, admin));
    }
}
