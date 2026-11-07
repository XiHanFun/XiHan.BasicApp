// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using XiHan.BasicApp.Access;
using XiHan.BasicApp.Identity;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.BasicApp.Organization;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证外部成员：注册在别处、以普通成员身份加入本租户的账号
/// </summary>
/// <remarks>
/// 租户的用户列表带出他们并标记，能配本租户的角色、部门、数据范围与直授，切进来即拿到这些权限；
/// 账号本身只有注册地能动：资料、密码、状态、双因素、登录策略与删除一律按不存在回答；不是本租户成员的人看不到也配不了
/// </remarks>
public sealed class ExternalMembersTests : IDisposable
{
    private const string Password = "Passw0rd!x";

    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private static async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(BasicAppTestServer server, HttpMethod method, string path, string? body, string token)
    {
        var (response, json) = await server.SendAsync(BasicAppTestServer.Request(method, path, body, token));
        return (response.StatusCode, json);
    }

    /// <summary>
    /// 起两个租户与两个注册在别处的用户
    /// </summary>
    /// <remarks>
    /// contoso 与 fabrikam；alice 与 bob 注册在 fabrikam，alice 加入 contoso 成为外部成员，bob 不是
    /// </remarks>
    private static async Task<Scenario> StartAsync(string connectionString)
    {
        var (server, platform, contoso) = await SettingsTestHost.StartAsync(connectionString);
        var enterprise = await EditionsTests.EditionIdAsync(server, platform, "enterprise");
        var (created, createdBody) = await SendAsync(server, HttpMethod.Post, "/api/tenants", $$"""{"tenantId":"fabrikam","name":"Fabrikam","editionId":"{{enterprise}}"}""", platform);
        Assert.True(created == HttpStatusCode.Created, createdBody.ToString());
        var alice = await CreateAsync(server, platform, "alice", "fabrikam");
        var bob = await CreateAsync(server, platform, "bob", "fabrikam");
        var memberId = await AddMemberAsync(server, platform, "contoso", alice);
        return new Scenario(server, platform, contoso, alice, bob, memberId);
    }

    private static async Task<string> CreateAsync(BasicAppTestServer server, string token, string userName, string? homeTenantId = null)
    {
        var (status, body) = await SendAsync(server, HttpMethod.Post, "/api/users", JsonSerializer.Serialize(new { userName, displayName = userName.ToUpperInvariant(), password = Password, homeTenantId }), token);
        Assert.True(status == HttpStatusCode.Created, body.ToString());
        return body.GetProperty("id").GetString()!;
    }

    private static async Task<string> AddMemberAsync(BasicAppTestServer server, string token, string tenantId, string userId)
    {
        var (status, body) = await SendAsync(server, HttpMethod.Post, "/api/tenant-members", $$"""{"tenantId":"{{tenantId}}","userId":"{{userId}}"}""", token);
        Assert.True(status == HttpStatusCode.Created, body.ToString());
        return body.GetProperty("id").GetString()!;
    }

    private static async Task<string> CreateRoleAsync(BasicAppTestServer server, string token, string code, params string[] permissions)
    {
        var (status, body) = await SendAsync(server, HttpMethod.Post, "/api/roles", JsonSerializer.Serialize(new { code, name = code }), token);
        Assert.True(status == HttpStatusCode.Created, body.ToString());
        var id = body.GetProperty("id").GetString()!;
        if (permissions.Length > 0)
        {
            Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, $"/api/roles/{id}:grant", JsonSerializer.Serialize(new { permissions }), token)).Status);
        }

        return id;
    }

    private static async Task<JsonElement> ListAsync(BasicAppTestServer server, string token, string? keyword = null, string? homeTenantId = null)
    {
        var filters = homeTenantId is null ? [] : new[] { new { field = "homeTenantId", @operator = "eq", value = homeTenantId } };
        var (status, body) = await SendAsync(server, HttpMethod.Post, "/api/users:list", JsonSerializer.Serialize(new { page = new { index = 1, size = 100 }, filters, keyword }), token);
        Assert.True(status == HttpStatusCode.OK, body.ToString());
        return body;
    }

    private static List<string?> UserNames(JsonElement page) => page.GetProperty("items").EnumerateArray().Select(static u => u.GetProperty("userName").GetString()).ToList();

    private static async Task<string> SwitchedTokenAsync(BasicAppTestServer server, string userName, string homeTenantId, string tenantId)
    {
        var login = await AuthFlowTests.LoginAsync(server, userName, Password, homeTenantId);
        var (status, body) = await SendAsync(server, HttpMethod.Post, "/api/auth:switch-tenant", JsonSerializer.Serialize(new { tenantId }), login.GetProperty("accessToken").GetString()!);
        Assert.True(status == HttpStatusCode.OK, body.ToString());
        return body.GetProperty("accessToken").GetString()!;
    }

    private static string FailureCode(JsonElement batch) => Assert.Single(batch.GetProperty("failed").EnumerateArray()).GetProperty("code").GetString()!;

    [Fact]
    public async Task TenantList_CarriesExternalMembers_AndNobodyElseFromOtherTenants()
    {
        var scene = await StartAsync(_database.ConnectionString);
        await using var running = scene.Server;

        var listed = await ListAsync(scene.Server, scene.Contoso);
        Assert.Contains("alice", UserNames(listed));
        Assert.DoesNotContain("bob", UserNames(listed));
        var alice = listed.GetProperty("items").EnumerateArray().Single(static u => u.GetProperty("userName").GetString() == "alice");
        Assert.True(alice.GetProperty("isExternalMember").GetBoolean());
        Assert.Equal("fabrikam", alice.GetProperty("homeTenantId").GetString());
        var admin = listed.GetProperty("items").EnumerateArray().Single(static u => u.GetProperty("userName").GetString() == "admin");
        Assert.False(admin.GetProperty("isExternalMember").GetBoolean());

        // 关键字与注册地筛选都只在本租户的用户与成员里挑
        Assert.Empty(UserNames(await ListAsync(scene.Server, scene.Contoso, keyword: "bob")));
        Assert.Equal(["alice"], UserNames(await ListAsync(scene.Server, scene.Contoso, homeTenantId: "fabrikam")));

        // 按 ID 取：成员取得到并带标记，别的租户的人按不存在回答
        var (found, foundBody) = await SendAsync(scene.Server, HttpMethod.Get, $"/api/users/{scene.Alice}", null, scene.Contoso);
        Assert.Equal(HttpStatusCode.OK, found);
        Assert.True(foundBody.GetProperty("isExternalMember").GetBoolean());
        var (hidden, hiddenBody) = await SendAsync(scene.Server, HttpMethod.Get, $"/api/users/{scene.Bob}", null, scene.Contoso);
        Assert.Equal((HttpStatusCode.NotFound, IdentityErrors.UserNotFound), (hidden, hiddenBody.GetProperty("code").GetString()));

        // 平台看全部用户，都不算外部成员
        var platform = await ListAsync(scene.Server, scene.Platform, homeTenantId: "fabrikam");
        Assert.All(platform.GetProperty("items").EnumerateArray(), static u => Assert.False(u.GetProperty("isExternalMember").GetBoolean()));
    }

    [Fact]
    public async Task ExternalMember_GetsRolesDepartmentsDataScopeAndGrants_AndUsesThemAfterSwitching()
    {
        var scene = await StartAsync(_database.ConnectionString);
        await using var running = scene.Server;
        var roleId = await CreateRoleAsync(scene.Server, scene.Contoso, "reader", "identity.users.read");

        // 角色：成员能配，不是成员的逐项失败；成员列表带标记
        var (assigned, assignedBody) = await SendAsync(scene.Server, HttpMethod.Post, $"/api/roles/{roleId}:assign", $$"""{"userIds":["{{scene.Alice}}","{{scene.Bob}}"]}""", scene.Contoso);
        Assert.Equal(HttpStatusCode.OK, assigned);
        Assert.Equal([scene.Alice], assignedBody.GetProperty("succeeded").EnumerateArray().Select(static e => e.GetString()));
        Assert.Equal(AccessErrors.MemberNotFound, FailureCode(assignedBody));
        var (_, members) = await SendAsync(scene.Server, HttpMethod.Get, $"/api/roles/{roleId}:members", null, scene.Contoso);
        Assert.Equal(1, members.GetProperty("total").GetInt64());
        Assert.True(Assert.Single(members.GetProperty("items").EnumerateArray()).GetProperty("isExternalMember").GetBoolean());

        // 部门：成员能进，不是成员的被拒；负责人同样
        var (_, department) = await SendAsync(scene.Server, HttpMethod.Post, "/api/departments", """{"code":"rd","name":"R&D"}""", scene.Contoso);
        var departmentId = department.GetProperty("id").GetString()!;
        var (joined, joinedBody) = await SendAsync(scene.Server, HttpMethod.Post, "/api/memberships", $$"""{"userId":"{{scene.Alice}}","departmentId":"{{departmentId}}"}""", scene.Contoso);
        Assert.True(joined == HttpStatusCode.Created, joinedBody.ToString());
        Assert.Equal("ALICE", joinedBody.GetProperty("displayName").GetString());
        var (rejected, rejectedBody) = await SendAsync(scene.Server, HttpMethod.Post, "/api/memberships", $$"""{"userId":"{{scene.Bob}}","departmentId":"{{departmentId}}"}""", scene.Contoso);
        Assert.Equal((HttpStatusCode.BadRequest, OrganizationErrors.UserNotInTenant), (rejected, rejectedBody.GetProperty("code").GetString()));

        // 数据范围与直授：成员能配，不是成员的不存在
        var (scoped, _) = await SendAsync(scene.Server, HttpMethod.Post, "/api/user-data-scopes:set", $$"""{"userId":"{{scene.Alice}}","dataScope":"SelfOnly"}""", scene.Contoso);
        Assert.Equal(HttpStatusCode.OK, scoped);
        var (_, scopeBody) = await SendAsync(scene.Server, HttpMethod.Get, $"/api/user-data-scopes?userId={scene.Alice}", null, scene.Contoso);
        Assert.Equal("SelfOnly", scopeBody.GetProperty("dataScope").GetString());
        var (scopedBob, scopedBobBody) = await SendAsync(scene.Server, HttpMethod.Post, "/api/user-data-scopes:set", $$"""{"userId":"{{scene.Bob}}","dataScope":"SelfOnly"}""", scene.Contoso);
        Assert.Equal((HttpStatusCode.NotFound, AccessErrors.MemberNotFound), (scopedBob, scopedBobBody.GetProperty("code").GetString()));
        var (granted, _) = await SendAsync(scene.Server, HttpMethod.Post, "/api/user-grants:grant", $$"""{"userId":"{{scene.Alice}}","permissions":["identity.sessions.read"]}""", scene.Contoso);
        Assert.Equal(HttpStatusCode.OK, granted);
        var (grantedBob, _) = await SendAsync(scene.Server, HttpMethod.Post, "/api/user-grants:grant", $$"""{"userId":"{{scene.Bob}}","permissions":["identity.sessions.read"]}""", scene.Contoso);
        Assert.Equal(HttpStatusCode.NotFound, grantedBob);

        // 列表的角色与部门列也带上
        var alice = (await ListAsync(scene.Server, scene.Contoso, keyword: "alice")).GetProperty("items")[0];
        Assert.Equal(["reader"], alice.GetProperty("roleNames").EnumerateArray().Select(static r => r.GetString()));
        Assert.Equal("R&D", alice.GetProperty("departmentName").GetString());

        // 切进 contoso 即拿到这些权限，用得上（数据范围收窄到自己）
        var inside = await SwitchedTokenAsync(scene.Server, "alice", "fabrikam", "contoso");
        var (permissions, permissionsBody) = await SendAsync(scene.Server, HttpMethod.Get, "/api/auth:permissions", null, inside);
        Assert.Equal(HttpStatusCode.OK, permissions);
        var codes = permissionsBody.GetProperty("permissions").EnumerateArray().Select(static p => p.GetString()).ToList();
        Assert.Contains("identity.users.read", codes);
        Assert.Contains("identity.sessions.read", codes);
        Assert.Equal(["alice"], UserNames(await ListAsync(scene.Server, inside)));

        // 回到自己的注册地，这些角色都不算数
        var home = (await AuthFlowTests.LoginAsync(scene.Server, "alice", Password, "fabrikam")).GetProperty("accessToken").GetString()!;
        var (_, homePermissions) = await SendAsync(scene.Server, HttpMethod.Get, "/api/auth:permissions", null, home);
        Assert.Empty(homePermissions.GetProperty("permissions").EnumerateArray());
    }

    [Fact]
    public async Task ExternalMember_AccountStaysWithTheHomeTenant()
    {
        var scene = await StartAsync(_database.ConnectionString);
        await using var running = scene.Server;
        var alice = scene.Alice;

        // 资料、登录策略、重置密码：按不存在回答，什么都没动
        var (updated, updatedBody) = await SendAsync(scene.Server, HttpMethod.Put, $"/api/users/{alice}", """{"displayName":"Hacked","email":"hacked@contoso.com"}""", scene.Contoso);
        Assert.Equal((HttpStatusCode.NotFound, IdentityErrors.UserNotFound), (updated, updatedBody.GetProperty("code").GetString()));
        var (reset, resetBody) = await SendAsync(scene.Server, HttpMethod.Post, $"/api/users/{alice}:reset-password", """{"newPassword":"Another1!x"}""", scene.Contoso);
        Assert.Equal((HttpStatusCode.NotFound, IdentityErrors.UserNotFound), (reset, resetBody.GetProperty("code").GetString()));
        var (policy, policyBody) = await SendAsync(scene.Server, HttpMethod.Post, $"/api/users/{alice}:set-login-policy", """{"allowMultiLogin":false,"maxLoginDevices":0}""", scene.Contoso);
        Assert.Equal((HttpStatusCode.NotFound, IdentityErrors.UserNotFound), (policy, policyBody.GetProperty("code").GetString()));
        var (readPolicy, _) = await SendAsync(scene.Server, HttpMethod.Get, $"/api/users/{alice}:login-policy", null, scene.Contoso);
        Assert.Equal(HttpStatusCode.NotFound, readPolicy);

        // 状态与删除：逐项按不存在拒绝
        foreach (var verb in new[] { "lock", "unlock", "disable", "enable", "reset-two-factor" })
        {
            var (status, body) = await SendAsync(scene.Server, HttpMethod.Post, $"/api/users:{verb}", $$"""{"ids":["{{alice}}"]}""", scene.Contoso);
            Assert.Equal(HttpStatusCode.OK, status);
            Assert.Equal(IdentityErrors.UserNotFound, FailureCode(body));
        }

        var (deleted, deletedBody) = await SendAsync(scene.Server, HttpMethod.Delete, $"/api/users?ids={alice}", null, scene.Contoso);
        Assert.Equal((HttpStatusCode.OK, IdentityErrors.UserNotFound), (deleted, FailureCode(deletedBody)));

        // 账号原样：注册地与平台看到的还是原来的，原密码照常能登录
        var (_, seen) = await SendAsync(scene.Server, HttpMethod.Get, $"/api/users/{alice}", null, scene.Platform);
        Assert.Equal(("ALICE", "Active"), (seen.GetProperty("displayName").GetString(), seen.GetProperty("status").GetString()));
        Assert.Equal(JsonValueKind.Null, seen.GetProperty("email").ValueKind);
        Assert.Equal("alice", (await AuthFlowTests.LoginAsync(scene.Server, "alice", Password, "fabrikam")).GetProperty("user").GetProperty("userName").GetString());

        // 平台照常能改（注册地的管理员同样）
        var (changed, _) = await SendAsync(scene.Server, HttpMethod.Put, $"/api/users/{alice}", """{"displayName":"Alice L"}""", scene.Platform);
        Assert.Equal(HttpStatusCode.OK, changed);
    }

    [Fact]
    public async Task OtherPickers_AcceptExternalMembers_ButNotStrangers()
    {
        var scene = await StartAsync(_database.ConnectionString);
        await using var running = scene.Server;
        var dana = await CreateAsync(scene.Server, scene.Contoso, "dana");
        var roleId = await CreateRoleAsync(scene.Server, scene.Contoso, "reader", "identity.users.read");
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(scene.Server, HttpMethod.Post, $"/api/roles/{roleId}:assign", $$"""{"userIds":["{{dana}}"]}""", scene.Contoso)).Status);

        // 委托：被委托人可以是外部成员，不能是别的租户的人
        var expires = DateTimeOffset.UtcNow.AddDays(1).ToString("O");
        var (delegated, delegatedBody) = await SendAsync(scene.Server, HttpMethod.Post, "/api/delegations", $$"""{"delegatorId":"{{dana}}","delegateeId":"{{scene.Alice}}","roleId":"{{roleId}}","expiresAt":"{{expires}}"}""", scene.Contoso);
        Assert.True(delegated == HttpStatusCode.Created, delegatedBody.ToString());
        var (delegatedBob, delegatedBobBody) = await SendAsync(scene.Server, HttpMethod.Post, "/api/delegations", $$"""{"delegatorId":"{{dana}}","delegateeId":"{{scene.Bob}}","roleId":"{{roleId}}","expiresAt":"{{expires}}"}""", scene.Contoso);
        Assert.Equal((HttpStatusCode.NotFound, AccessErrors.MemberNotFound), (delegatedBob, delegatedBobBody.GetProperty("code").GetString()));

        // 字段安全的目标
        var (rule, ruleBody) = await SendAsync(scene.Server, HttpMethod.Post, "/api/field-security", $$"""{"targetKind":"User","targetId":"{{scene.Alice}}","entity":"identity.users","field":"email","strategy":"Hidden"}""", scene.Contoso);
        Assert.True(rule == HttpStatusCode.Created, ruleBody.ToString());
        var (ruleBob, ruleBobBody) = await SendAsync(scene.Server, HttpMethod.Post, "/api/field-security", $$"""{"targetKind":"User","targetId":"{{scene.Bob}}","entity":"identity.users","field":"email","strategy":"Hidden"}""", scene.Contoso);
        Assert.Equal((HttpStatusCode.NotFound, AccessErrors.MemberNotFound), (ruleBob, ruleBobBody.GetProperty("code").GetString()));

        // 部门负责人
        var (lead, leadBody) = await SendAsync(scene.Server, HttpMethod.Post, "/api/departments", $$"""{"code":"ops","name":"Ops","leaderUserId":"{{scene.Alice}}"}""", scene.Contoso);
        Assert.True(lead == HttpStatusCode.Created, leadBody.ToString());
        var (leadBob, leadBobBody) = await SendAsync(scene.Server, HttpMethod.Post, "/api/departments", $$"""{"code":"ops2","name":"Ops2","leaderUserId":"{{scene.Bob}}"}""", scene.Contoso);
        Assert.Equal((HttpStatusCode.BadRequest, OrganizationErrors.UserNotInTenant), (leadBob, leadBobBody.GetProperty("code").GetString()));
    }

    [Fact]
    public async Task LeavingTheTenant_EndsVisibilityAndRoles_WhileAMembershipOutsideItsWindowHidesThem()
    {
        var scene = await StartAsync(_database.ConnectionString);
        await using var running = scene.Server;
        var roleId = await CreateRoleAsync(scene.Server, scene.Contoso, "reader", "identity.users.read");
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(scene.Server, HttpMethod.Post, $"/api/roles/{roleId}:assign", $$"""{"userIds":["{{scene.Alice}}"]}""", scene.Contoso)).Status);

        // 被移出：列表、按 ID 取与角色成员都看不到，再配角色也配不上
        var (removed, removedBody) = await SendAsync(scene.Server, HttpMethod.Delete, $"/api/tenant-members?ids={scene.MemberId}", null, scene.Platform);
        Assert.Equal(HttpStatusCode.OK, removed);
        Assert.Equal([scene.MemberId], removedBody.GetProperty("succeeded").EnumerateArray().Select(static e => e.GetString()));
        Assert.DoesNotContain("alice", UserNames(await ListAsync(scene.Server, scene.Contoso)));
        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(scene.Server, HttpMethod.Get, $"/api/users/{scene.Alice}", null, scene.Contoso)).Status);
        var (_, members) = await SendAsync(scene.Server, HttpMethod.Get, $"/api/roles/{roleId}:members", null, scene.Contoso);
        Assert.Equal(0, members.GetProperty("total").GetInt64());
        var (_, assigned) = await SendAsync(scene.Server, HttpMethod.Post, $"/api/roles/{roleId}:assign", $$"""{"userIds":["{{scene.Alice}}"]}""", scene.Contoso);
        Assert.Equal(AccessErrors.MemberNotFound, FailureCode(assigned));

        // 再加回来不带旧角色（成员身份撤销即解除角色）
        await AddMemberAsync(scene.Server, scene.Platform, "contoso", scene.Alice);
        var (_, again) = await SendAsync(scene.Server, HttpMethod.Get, $"/api/roles/{roleId}:members", null, scene.Contoso);
        Assert.Equal(0, again.GetProperty("total").GetInt64());
        var (_, byUser) = await SendAsync(scene.Server, HttpMethod.Get, $"/api/roles:by-user?userId={scene.Alice}", null, scene.Contoso);
        Assert.Empty(byUser.GetProperty("items").EnumerateArray());

        // 还没生效的成员关系不算：看不到也配不了
        var carol = await CreateAsync(scene.Server, scene.Platform, "carol", "fabrikam");
        var later = DateTimeOffset.UtcNow.AddDays(1).ToString("O");
        var (pending, pendingBody) = await SendAsync(scene.Server, HttpMethod.Post, "/api/tenant-members", $$"""{"tenantId":"contoso","userId":"{{carol}}","effectiveAt":"{{later}}"}""", scene.Platform);
        Assert.True(pending == HttpStatusCode.Created, pendingBody.ToString());
        Assert.DoesNotContain("carol", UserNames(await ListAsync(scene.Server, scene.Contoso)));
        var (_, assignedLater) = await SendAsync(scene.Server, HttpMethod.Post, $"/api/roles/{roleId}:assign", $$"""{"userIds":["{{carol}}"]}""", scene.Contoso);
        Assert.Equal(AccessErrors.MemberNotFound, FailureCode(assignedLater));
    }

    /// <summary>
    /// 一个用例的起手
    /// </summary>
    /// <param name="Server">宿主</param>
    /// <param name="Platform">超管的令牌</param>
    /// <param name="Contoso">contoso 管理员的令牌</param>
    /// <param name="Alice">注册在 fabrikam、加入 contoso 的用户</param>
    /// <param name="Bob">注册在 fabrikam、不是 contoso 成员的用户</param>
    /// <param name="MemberId">alice 的成员行</param>
    private sealed record Scenario(BasicAppTestServer Server, string Platform, string Contoso, string Alice, string Bob, string MemberId);
}
