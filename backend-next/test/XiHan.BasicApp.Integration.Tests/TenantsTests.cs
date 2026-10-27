// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using XiHan.BasicApp.Integration.Tests.Infrastructure;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 租户：开通写目录、管理员初始化（租户侧全部权限）、租户管理员在本租户内工作、平台专属操作对租户主体拒绝、停用 / 恢复影响登录与调用、删除受用户数约束。
/// </summary>
public sealed class TenantsTests : IDisposable
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
    public async Task Provision_InitializeAdmin_SuspendAndResume_Delete()
    {
        await using var server = await StartAsync();
        var admin = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;

        // 开通：业务实体 + 目录（启用）；重复 / 保留 ID 冲突；文法在 Validate 阶段拒绝
        var (created, createdBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/tenants", """{"tenantId":"contoso","name":"Contoso","order":10}""", admin));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal("contoso", createdBody.GetProperty("tenantId").GetString());
        Assert.Equal("Active", createdBody.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, createdBody.GetProperty("adminUserId").ValueKind);
        var id = createdBody.GetProperty("id").GetString()!;
        var (duplicate, duplicateBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/tenants", """{"tenantId":"contoso","name":"Again"}""", admin));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal("TENANCY_CONFLICT_001", duplicateBody.GetProperty("code").GetString());
        var (reserved, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/tenants", """{"tenantId":"platform","name":"Nope"}""", admin));
        Assert.Equal(HttpStatusCode.Conflict, reserved.StatusCode);
        var (invalid, invalidBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/tenants", """{"tenantId":"bad id!","name":"Nope"}""", admin));
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.True(invalidBody.GetProperty("errors").TryGetProperty("tenantId", out _));

        // 租户主体没有平台侧权限：按契约在 Authorize 阶段拒绝
        var (tenantSide, tenantSideBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/tenants", """{"tenantId":"other","name":"Other"}""", TestTokens.Issue("alice", "acme", "admin")));
        Assert.Equal(HttpStatusCode.Forbidden, tenantSide.StatusCode);
        Assert.Equal("SECURITY_DENIED_004", tenantSideBody.GetProperty("code").GetString());

        // 初始化管理员：建用户、首次须改密、租户范围的 * 授予；只能一次
        var (initialized, initializedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/tenants/{id}:initialize-admin", """{"userName":"admin","password":"Admin@123456","displayName":"Contoso Admin"}""", admin));
        Assert.Equal(HttpStatusCode.OK, initialized.StatusCode);
        Assert.Equal("admin", initializedBody.GetProperty("userName").GetString());
        Assert.Equal("contoso", initializedBody.GetProperty("homeTenantId").GetString());
        var (again, againBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/tenants/{id}:initialize-admin", """{"userName":"admin2","password":"Admin@123456"}""", admin));
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal("TENANCY_CONFLICT_002", againBody.GetProperty("code").GetString());
        var (detail, detailBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/tenants/{id}", token: admin));
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        Assert.Equal("admin", detailBody.GetProperty("adminUserName").GetString());
        Assert.Equal(1, detailBody.GetProperty("userCount").GetInt64());

        // 租户管理员登录：带租户、须改密；有效权限是租户侧目录全部，菜单没有平台专属页
        var login = await AuthFlowTests.LoginAsync(server, "admin", "Admin@123456", "contoso");
        Assert.True(login.GetProperty("mustChangePassword").GetBoolean());
        Assert.Equal("contoso", login.GetProperty("tenantId").GetString());
        var tenantAdmin = login.GetProperty("accessToken").GetString()!;
        var (permissions, permissionsBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:permissions", "{}", tenantAdmin));
        Assert.Equal(HttpStatusCode.OK, permissions.StatusCode);
        Assert.False(permissionsBody.GetProperty("isPlatform").GetBoolean());
        var codes = permissionsBody.GetProperty("permissions").EnumerateArray().Select(static p => p.GetString()).ToList();
        Assert.Contains("identity.users.create", codes);
        Assert.Contains("access.roles.grant", codes);
        Assert.DoesNotContain("tenancy.tenants.read", codes);
        Assert.Equal(["identity.users", "organization.departments", "organization.positions", "access.roles"], permissionsBody.GetProperty("menus").EnumerateArray().Select(static m => m.GetProperty("code").GetString()).ToList());

        // 在本租户内工作：只看到本租户的用户；平台专属操作即使持有 * 也被操作本身拒绝
        var (list, listBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users:list", "{}", tenantAdmin));
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Equal(1, listBody.GetProperty("total").GetInt64());
        var (platformOnly, platformOnlyBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/tenants:list", "{}", tenantAdmin));
        Assert.Equal(HttpStatusCode.Forbidden, platformOnly.StatusCode);
        Assert.Equal("TENANCY_DENIED_001", platformOnlyBody.GetProperty("code").GetString());

        // 停用：目录禁用 → 带租户的调用被框架拒绝、登录被拒；恢复后恢复
        var (suspended, suspendedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/tenants:suspend", $$"""{"ids":["{{id}}","{{Guid.NewGuid()}}"]}""", admin));
        Assert.Equal(HttpStatusCode.OK, suspended.StatusCode);
        Assert.Equal([id], suspendedBody.GetProperty("succeeded").EnumerateArray().Select(static e => e.GetString()).ToList());
        Assert.Equal("TENANCY_NOTFOUND_001", suspendedBody.GetProperty("failed")[0].GetProperty("code").GetString());
        var (blocked, blockedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users:list", "{}", tenantAdmin));
        Assert.Equal(HttpStatusCode.Forbidden, blocked.StatusCode);
        Assert.Equal("MULTITENANCY_DENIED_001", blockedBody.GetProperty("code").GetString());
        var (deniedLogin, deniedLoginBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:login", """{"userName":"admin","password":"Admin@123456","tenant":"contoso"}""", acceptLanguage: "zh-CN"));
        Assert.Equal(HttpStatusCode.Forbidden, deniedLogin.StatusCode);
        Assert.Equal("IDENTITY_DENIED_005", deniedLoginBody.GetProperty("code").GetString());
        Assert.Equal("租户 'contoso' 不存在或已停用。", deniedLoginBody.GetProperty("detail").GetString());
        var (deniedRefresh, deniedRefreshBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:refresh", $$"""{"refreshToken":"{{login.GetProperty("refreshToken").GetString()}}"}"""));
        Assert.Equal(HttpStatusCode.Forbidden, deniedRefresh.StatusCode);
        Assert.Equal("IDENTITY_DENIED_005", deniedRefreshBody.GetProperty("code").GetString());
        var (resumed, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/tenants:resume", $$"""{"ids":["{{id}}"]}""", admin));
        Assert.Equal(HttpStatusCode.OK, resumed.StatusCode);
        var relogin = await AuthFlowTests.LoginAsync(server, "admin", "Admin@123456", "contoso");
        Assert.Equal("contoso", relogin.GetProperty("tenantId").GetString());

        // 修改：名称同步到目录（登录响应里租户仍可用即可）；列表与关键字
        var (updated, updatedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/tenants/{id}", """{"name":"Contoso Ltd","order":5}""", admin));
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal("Contoso Ltd", updatedBody.GetProperty("name").GetString());
        var (listed, listedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/tenants:list", """{"keyword":"contoso"}""", admin));
        Assert.Equal(HttpStatusCode.OK, listed.StatusCode);
        Assert.Equal(1, listedBody.GetProperty("total").GetInt64());

        // 删除：还有用户的拒绝；空租户删除后从目录移除、不能再登录
        var (deleteBusy, deleteBusyBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Delete, $"/api/tenants?ids={id}", token: admin));
        Assert.Equal(HttpStatusCode.OK, deleteBusy.StatusCode);
        Assert.Equal("TENANCY_CONFLICT_003", deleteBusyBody.GetProperty("failed")[0].GetProperty("code").GetString());
        var (empty, emptyBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/tenants", """{"tenantId":"empty","name":"Empty"}""", admin));
        Assert.Equal(HttpStatusCode.Created, empty.StatusCode);
        var (deleted, deletedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Delete, $"/api/tenants?ids={emptyBody.GetProperty("id").GetString()}", token: admin));
        Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);
        Assert.Equal(1, deletedBody.GetProperty("succeeded").GetArrayLength());
        var (gone, goneBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:login", """{"userName":"nobody","password":"Nobody@123","tenant":"empty"}"""));
        Assert.Equal(HttpStatusCode.Forbidden, gone.StatusCode);
        Assert.Equal("IDENTITY_DENIED_005", goneBody.GetProperty("code").GetString());
    }

    [Fact]
    public async Task DemoTenant_IsSeededOnlyWhenDemoDataIsEnabled()
    {
        await using var server = await BasicAppTestServer.StartAsync(_database.ConnectionString, settings: [new("BasicApp:Seed:EnableDemoData", "true")]);
        await server.RunSeedersAsync();
        await server.RunSeedersAsync();
        var login = await AuthFlowTests.LoginAsync(server, "admin", "Demo@123456", "demo");
        Assert.Equal("demo", login.GetProperty("tenantId").GetString());
        Assert.True(login.GetProperty("mustChangePassword").GetBoolean());

        var admin = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;
        var (listed, listedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/tenants:list", "{}", admin));
        Assert.Equal(HttpStatusCode.OK, listed.StatusCode);
        Assert.Equal(["demo"], listedBody.GetProperty("items").EnumerateArray().Select(static t => t.GetProperty("tenantId").GetString()).ToList());
    }
}
