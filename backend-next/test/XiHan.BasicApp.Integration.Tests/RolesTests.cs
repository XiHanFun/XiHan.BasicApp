// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.Framework.Security.Abstractions.Permissions;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 角色：创建 / 唯一码 / 授予与撤销（目录校验、逐项结果）/ 指派与解除（角色进令牌、刷新后生效）/ 成员 / 删除（内建不可删）；租户侧角色只在本租户可见。
/// </summary>
public sealed class RolesTests : IDisposable
{
    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private async Task<BasicAppTestServer> StartAsync()
    {
        var server = await BasicAppTestServer.StartAsync(_database.ConnectionString);
        await server.SeedDefaultsAsync();
        await server.RunSeedersAsync();
        return server;
    }

    [Fact]
    public async Task PlatformRole_GrantsFlowThroughTheToken_AndRevocationTakesEffect()
    {
        await using var server = await StartAsync();
        var adminLogin = await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123");
        var admin = adminLogin.GetProperty("accessToken").GetString()!;

        // 超级管理员的令牌带内建 admin 角色（种子指派）
        var (me, meBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:me", token: admin));
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        Assert.Equal(1, meBody.GetProperty("roles").GetArrayLength());
        Assert.True(Guid.TryParse(meBody.GetProperty("roles")[0].GetString(), out _));

        var (created, createdBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/roles", """{"code":"auditor","name":"Auditor","order":10}""", admin));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal("platform", createdBody.GetProperty("ownerTenantId").GetString());
        Assert.False(createdBody.GetProperty("isBuiltIn").GetBoolean());
        var roleId = createdBody.GetProperty("id").GetString()!;

        var (duplicate, duplicateBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/roles", """{"code":"auditor","name":"Again"}""", admin));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal("ACCESS_CONFLICT_001", duplicateBody.GetProperty("code").GetString());

        // 授予：目录里的通过，不在目录的逐项失败；重复授予算成功
        var (granted, grantedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/roles/{roleId}:grant", """{"permissions":["identity.users.read","nope.things.read"]}""", admin));
        Assert.Equal(HttpStatusCode.OK, granted.StatusCode);
        Assert.Equal(["identity.users.read"], grantedBody.GetProperty("succeeded").EnumerateArray().Select(static e => e.GetString()).ToList());
        Assert.Equal("ACCESS_VALIDATION_001", grantedBody.GetProperty("failed")[0].GetProperty("code").GetString());
        var (again, againBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/roles/{roleId}:grant", """{"permissions":["identity.users.read"]}""", admin));
        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
        Assert.Equal(1, againBody.GetProperty("succeeded").GetArrayLength());

        var (detail, detailBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/roles/{roleId}", token: admin));
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        Assert.Equal(["identity.users.read"], detailBody.GetProperty("permissions").EnumerateArray().Select(static e => e.GetString()).ToList());
        Assert.Equal(0, detailBody.GetProperty("memberCount").GetInt64());

        // 指派：平台用户 bob 进角色；不存在的用户逐项 NOTFOUND
        var (bobCreated, bobBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users", """{"userName":"bob","displayName":"Bob","password":"Bob@123456"}""", admin));
        Assert.Equal(HttpStatusCode.Created, bobCreated.StatusCode);
        var bobId = bobBody.GetProperty("id").GetString()!;
        var (assigned, assignedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/roles/{roleId}:assign", $$"""{"userIds":["{{bobId}}","{{Guid.NewGuid()}}"]}""", admin));
        Assert.Equal(HttpStatusCode.OK, assigned.StatusCode);
        Assert.Equal([bobId], assignedBody.GetProperty("succeeded").EnumerateArray().Select(static e => e.GetString()).ToList());
        Assert.Equal("ACCESS_NOTFOUND_002", assignedBody.GetProperty("failed")[0].GetProperty("code").GetString());

        var (members, membersBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/roles/{roleId}:members", token: admin));
        Assert.Equal(HttpStatusCode.OK, members.StatusCode);
        Assert.Equal(1, membersBody.GetProperty("total").GetInt64());
        Assert.Equal("bob", membersBody.GetProperty("items")[0].GetProperty("userName").GetString());

        // bob 登录：令牌带角色 ID，经角色授予能列用户、不能建用户
        var bobLogin = await AuthFlowTests.LoginAsync(server, "bob", "Bob@123456");
        var bob = bobLogin.GetProperty("accessToken").GetString()!;
        var (bobMe, bobMeBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:me", token: bob));
        Assert.Equal(roleId, bobMeBody.GetProperty("roles")[0].GetString());
        var (list, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users:list", "{}", bob));
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var (forbidden, forbiddenBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users", """{"userName":"carl","displayName":"Carl"}""", bob));
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal("SECURITY_DENIED_004", forbiddenBody.GetProperty("code").GetString());

        // 撤销后立即生效（框架授予缓存随撤销失效）
        var (revoked, revokedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/roles/{roleId}:revoke", """{"permissions":["identity.users.read"]}""", admin));
        Assert.Equal(HttpStatusCode.OK, revoked.StatusCode);
        Assert.Equal(1, revokedBody.GetProperty("succeeded").GetArrayLength());
        var (denied, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users:list", "{}", bob));
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);

        // 解除指派立即生效：当前令牌虽仍带角色声明，权限过滤器按指派求交把它拿掉；刷新后声明里也没有了
        await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/roles/{roleId}:grant", """{"permissions":["identity.users.read"]}""", admin));
        var (regranted, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users:list", "{}", bob));
        Assert.Equal(HttpStatusCode.OK, regranted.StatusCode);
        var (unassigned, unassignedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/roles/{roleId}:unassign", $$"""{"userIds":["{{bobId}}"]}""", admin));
        Assert.Equal(HttpStatusCode.OK, unassigned.StatusCode);
        Assert.Equal(1, unassignedBody.GetProperty("succeeded").GetArrayLength());
        var (droppedAtOnce, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users:list", "{}", bob));
        Assert.Equal(HttpStatusCode.Forbidden, droppedAtOnce.StatusCode);
        var (refreshed, refreshedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:refresh", $$"""{"refreshToken":"{{bobLogin.GetProperty("refreshToken").GetString()}}"}"""));
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        var (afterRefresh, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users:list", "{}", refreshedBody.GetProperty("accessToken").GetString()));
        Assert.Equal(HttpStatusCode.Forbidden, afterRefresh.StatusCode);

        // 删除：自建角色可删，内建 admin 不可删
        var adminRoleId = meBody.GetProperty("roles")[0].GetString();
        var (deleted, deletedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Delete, $"/api/roles?ids={roleId}&ids={adminRoleId}", token: admin));
        Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);
        Assert.Equal([roleId], deletedBody.GetProperty("succeeded").EnumerateArray().Select(static e => e.GetString()).ToList());
        Assert.Equal("ACCESS_CONFLICT_002", deletedBody.GetProperty("failed")[0].GetProperty("code").GetString());

        var (roles, rolesBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/roles:list", """{"sorts":[{"field":"code"}]}""", admin));
        Assert.Equal(HttpStatusCode.OK, roles.StatusCode);
        Assert.Equal(["admin"], rolesBody.GetProperty("items").EnumerateArray().Select(static r => r.GetProperty("code").GetString()).ToList());
        Assert.True(rolesBody.GetProperty("items")[0].GetProperty("isBuiltIn").GetBoolean());
    }

    [Fact]
    public async Task TenantRoles_AreScopedToTheirTenant()
    {
        await using var server = await StartAsync();
        await server.SeedAsync([], [
            new PermissionGrant(GrantSubject.ForRole("admin"), "access.roles.create", "acme"),
            new PermissionGrant(GrantSubject.ForRole("admin"), "access.roles.read", "acme"),
            new PermissionGrant(GrantSubject.ForRole("admin"), "access.roles.grant", "acme"),
            new PermissionGrant(GrantSubject.ForRole("admin"), "access.roles.assign", "acme"),
            new PermissionGrant(GrantSubject.ForRole("admin"), "access.roles.read", "globex"),
        ]);
        var acme = TestTokens.Issue("alice", "acme", "admin");
        var globex = TestTokens.Issue("carol", "globex", "admin");

        var (created, createdBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/roles", """{"code":"editor","name":"Editor"}""", acme));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal("acme", createdBody.GetProperty("ownerTenantId").GetString());
        var roleId = createdBody.GetProperty("id").GetString()!;

        var (granted, grantedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/roles/{roleId}:grant", """{"permissions":["identity.users.read"]}""", acme));
        Assert.Equal(HttpStatusCode.OK, granted.StatusCode);
        Assert.Equal(1, grantedBody.GetProperty("succeeded").GetArrayLength());

        // 别的租户看不到：列表为空、按 ID 取是 404；平台主体也看不到租户角色
        var (otherList, otherListBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/roles:list", "{}", globex));
        Assert.Equal(HttpStatusCode.OK, otherList.StatusCode);
        Assert.Equal(0, otherListBody.GetProperty("total").GetInt64());
        var (otherGet, otherGetBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/roles/{roleId}", token: globex));
        Assert.Equal(HttpStatusCode.NotFound, otherGet.StatusCode);
        Assert.Equal("ACCESS_NOTFOUND_001", otherGetBody.GetProperty("code").GetString());
        var (ownList, ownListBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/roles:list", """{"keyword":"edit"}""", acme));
        Assert.Equal(1, ownListBody.GetProperty("total").GetInt64());
        Assert.Equal("editor", ownListBody.GetProperty("items")[0].GetProperty("code").GetString());

        // 成员只能是本租户的用户：globex 的用户指派进 acme 的角色逐项 NOTFOUND，acme 的用户成功
        var (dave, daveBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users", """{"userName":"dave","displayName":"Dave"}""", globex));
        Assert.Equal(HttpStatusCode.Created, dave.StatusCode);
        var (erin, erinBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users", """{"userName":"erin","displayName":"Erin","password":"Erin@123456"}""", acme));
        Assert.Equal(HttpStatusCode.Created, erin.StatusCode);
        var (assigned, assignedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/roles/{roleId}:assign", $$"""{"userIds":["{{daveBody.GetProperty("id").GetString()}}","{{erinBody.GetProperty("id").GetString()}}"]}""", acme));
        Assert.Equal(HttpStatusCode.OK, assigned.StatusCode);
        Assert.Equal(1, assignedBody.GetProperty("succeeded").GetArrayLength());
        Assert.Equal("ACCESS_NOTFOUND_002", assignedBody.GetProperty("failed")[0].GetProperty("code").GetString());

        // erin 以租户登录：令牌带租户角色，能在本租户列用户
        var erinLogin = await AuthFlowTests.LoginAsync(server, "erin", "Erin@123456", "acme");
        var erinToken = erinLogin.GetProperty("accessToken").GetString()!;
        var (erinMe, erinMeBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:me", token: erinToken));
        Assert.Equal(HttpStatusCode.OK, erinMe.StatusCode);
        Assert.Equal([roleId], erinMeBody.GetProperty("roles").EnumerateArray().Select(static r => r.GetString()).ToList());
        var (list, listBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users:list", "{}", erinToken));
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Equal(JsonValueKind.Number, listBody.GetProperty("total").ValueKind);
        // 租户角色的授予是租户行：同名角色 ID 不会跨租户命中，erin 也没有创建权限
        var (forbidden, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users", """{"userName":"frank","displayName":"Frank"}""", erinToken));
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }
}
