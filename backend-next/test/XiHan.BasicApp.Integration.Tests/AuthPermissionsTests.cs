// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using XiHan.BasicApp.Integration.Tests.Infrastructure;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// <c>auth.permissions</c>：通配展开、按作用侧裁剪、角色摘要、按权限裁剪的菜单树（菜单种子来自各包页面登记、可重复执行）；<c>permissions.list</c> 的目录。
/// </summary>
public sealed class AuthPermissionsTests : IDisposable
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
    public async Task SuperAdmin_SeesTheWholeCatalog_AndEveryMenu()
    {
        await using var server = await StartAsync();
        // 种子可重复执行：菜单对齐后不重复
        await server.RunSeedersAsync();
        var admin = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;

        var (response, body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:permissions", token: admin));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(body.GetProperty("isPlatform").GetBoolean());
        var permissions = body.GetProperty("permissions").EnumerateArray().Select(static p => p.GetString()).ToList();
        Assert.Contains("identity.users.create", permissions);
        Assert.Contains("access.roles.grant", permissions);
        Assert.DoesNotContain("*", permissions);
        Assert.Equal(permissions.Order(StringComparer.Ordinal), permissions);
        Assert.Equal(["admin"], body.GetProperty("roles").EnumerateArray().Select(static r => r.GetProperty("code").GetString()).ToList());

        var menus = body.GetProperty("menus").EnumerateArray().ToList();
        // 工作台（Tier 3 模块，排序 0、登录即可见）是第一页：登录后的落点
        Assert.Equal(["workbench.dashboard", "identity.users", "access.roles", "access.permissions", "access.menus", "tenancy.tenants", "tenancy.editions", "settings.configs", "settings.dicts", "settings.numbering-rules", "auditing.traces", "auditing.operation-logs", "auditing.login-logs", "auditing.access-logs", "auditing.exception-logs", "auditing.diff-logs", "auditing.permission-change-logs", "auditing.condition-hits", "auditing.open-api-logs"], menus.Select(static m => m.GetProperty("code").GetString()).ToList());
        Assert.Equal("Page", menus[0].GetProperty("kind").GetString());
        Assert.Equal(JsonValueKind.Null, menus[0].GetProperty("permission").ValueKind);
        Assert.Equal("identity/users/index", menus[1].GetProperty("component").GetString());
        Assert.Equal("/access/roles", menus[2].GetProperty("route").GetString());
        Assert.Equal("access.roles.read", menus[2].GetProperty("permission").GetString());
        Assert.Equal(["workbench.dashboard", "identity.users", "access.roles", "access.permissions", "access.menus", "tenancy.tenants", "tenancy.editions", "settings.configs", "settings.dicts", "settings.numbering-rules", "auditing.traces", "auditing.operation-logs", "auditing.login-logs", "auditing.access-logs", "auditing.exception-logs", "auditing.diff-logs", "auditing.permission-change-logs", "auditing.condition-hits", "auditing.open-api-logs"], body.GetProperty("pages").EnumerateArray().Select(static p => p.GetString()).ToList());

        var (catalog, catalogBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/permissions", token: admin));
        Assert.Equal(HttpStatusCode.OK, catalog.StatusCode);
        var roleRead = catalogBody.GetProperty("items").EnumerateArray().Single(static i => i.GetProperty("code").GetString() == "access.roles.read");
        Assert.Equal("access", roleRead.GetProperty("package").GetString());
        Assert.Equal("roles", roleRead.GetProperty("resource").GetString());
        Assert.Equal("Both", roleRead.GetProperty("scope").GetString());
    }

    [Fact]
    public async Task RoleMember_SeesOnlyGrantedPermissions_AndPrunedMenus()
    {
        await using var server = await StartAsync();
        var admin = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;
        var (role, roleBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/roles", """{"code":"viewer","name":"Viewer"}""", admin));
        Assert.Equal(HttpStatusCode.Created, role.StatusCode);
        var roleId = roleBody.GetProperty("id").GetString()!;
        await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/roles/{roleId}:grant", """{"permissions":["identity.users.read"]}""", admin));
        var (bob, bobBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users", """{"userName":"bob","displayName":"Bob","password":"Bob@123456"}""", admin));
        Assert.Equal(HttpStatusCode.Created, bob.StatusCode);
        await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/roles/{roleId}:assign", $$"""{"userIds":["{{bobBody.GetProperty("id").GetString()}}"]}""", admin));

        var token = (await AuthFlowTests.LoginAsync(server, "bob", "Bob@123456")).GetProperty("accessToken").GetString()!;
        var (response, body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:permissions", token: token));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(["identity.users.read"], body.GetProperty("permissions").EnumerateArray().Select(static p => p.GetString()).ToList());
        Assert.Equal(["viewer"], body.GetProperty("roles").EnumerateArray().Select(static r => r.GetProperty("code").GetString()).ToList());
        Assert.Equal(["workbench.dashboard", "identity.users"], body.GetProperty("menus").EnumerateArray().Select(static m => m.GetProperty("code").GetString()).ToList());
        Assert.Equal(["workbench.dashboard", "identity.users"], body.GetProperty("pages").EnumerateArray().Select(static p => p.GetString()).ToList());

        // 没有 access.permissions.read 的主体看不到权限目录
        var (catalog, catalogBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/permissions", token: token));
        Assert.Equal(HttpStatusCode.Forbidden, catalog.StatusCode);
        Assert.Equal("SECURITY_DENIED_004", catalogBody.GetProperty("code").GetString());
    }

    [Fact]
    public async Task ExternalToken_IsResolvedByItsRoleClaims_WithinItsTenant()
    {
        await using var server = await StartAsync();
        // 测试令牌的 role 声明是字符串角色名，与种子的全局授予（identity.users.read / create）对接
        var (response, body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:permissions", token: TestTokens.Issue("alice", "acme", "admin")));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(body.GetProperty("isPlatform").GetBoolean());
        Assert.Equal(["identity.users.create", "identity.users.read"], body.GetProperty("permissions").EnumerateArray().Select(static p => p.GetString()).ToList());
        Assert.Equal(0, body.GetProperty("roles").GetArrayLength());
        Assert.Equal(["workbench.dashboard", "identity.users"], body.GetProperty("menus").EnumerateArray().Select(static m => m.GetProperty("code").GetString()).ToList());

        var (anonymous, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:permissions"));
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
    }
}
