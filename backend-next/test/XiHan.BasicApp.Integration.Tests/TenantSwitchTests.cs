// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using XiHan.BasicApp.Integration.Tests.Infrastructure;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 租户成员与切换租户：平台侧管理成员（加、重复、列、移除）；<c>tenants.mine</c> 列出注册地与成员租户；
/// <c>auth.switch-tenant</c> 只放行注册地 / 成员 / 平台账号回平台，切换后同一会话换上下文、旧刷新令牌失效、权限按新租户重算。
/// </summary>
public sealed class TenantSwitchTests : IDisposable
{
    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private async Task<BasicAppTestServer> StartAsync()
    {
        var server = await BasicAppTestServer.StartAsync(_database.ConnectionString, settings: [new("BasicApp:Seed:EnableDemoData", "true")]);
        await server.SeedDefaultsAsync();
        await server.RunSeedersAsync();
        return server;
    }

    [Fact]
    public async Task Members_Mine_And_SwitchTenant()
    {
        await using var server = await StartAsync();
        var superadminLogin = await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123");
        var superadmin = superadminLogin.GetProperty("accessToken").GetString()!;
        var superadminId = superadminLogin.GetProperty("user").GetProperty("id").GetString()!;
        Assert.Equal(JsonValueKind.Null, superadminLogin.GetProperty("tenantName").ValueKind);

        // 开通租户并初始化管理员；演示种子已把超管加为 demo 的成员
        var (created, createdBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/tenants", """{"tenantId":"contoso","name":"Contoso"}""", superadmin));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var contosoId = createdBody.GetProperty("id").GetString()!;
        var (initialized, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/tenants/{contosoId}:initialize-admin", """{"userName":"admin","password":"Admin@123456"}""", superadmin));
        Assert.Equal(HttpStatusCode.OK, initialized.StatusCode);

        // 平台账号 operator：注册地平台、没有成员关系
        var (operatorCreated, operatorBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users", """{"userName":"operator","displayName":"Operator","password":"Operator@123"}""", superadmin));
        Assert.Equal(HttpStatusCode.Created, operatorCreated.StatusCode);
        var operatorId = operatorBody.GetProperty("id").GetString()!;

        // 我的租户：超管能进平台；成员租户里只有演示种子给的 demo
        var (mine, mineBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/tenants:mine", token: superadmin));
        Assert.Equal(HttpStatusCode.OK, mine.StatusCode);
        Assert.True(mineBody.GetProperty("canAccessPlatform").GetBoolean());
        Assert.True(mineBody.GetProperty("platformIsCurrent").GetBoolean());
        Assert.Equal(["demo"], mineBody.GetProperty("tenants").EnumerateArray().Select(static t => t.GetProperty("tenantId").GetString()).ToList());
        Assert.Equal("Owner", mineBody.GetProperty("tenants")[0].GetProperty("memberType").GetString());
        // 加入时间：成员是成为成员的时间
        Assert.True(mineBody.GetProperty("tenants")[0].GetProperty("joinedAt").GetDateTimeOffset() > DateTimeOffset.UnixEpoch);

        // 加成员：operator 进 contoso；重复与注册地用户都是冲突；租户 / 用户不存在各自 404
        var (added, addedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/tenant-members", $$"""{"tenantId":"contoso","userId":"{{operatorId}}"}""", superadmin));
        Assert.Equal(HttpStatusCode.Created, added.StatusCode);
        Assert.Equal("operator", addedBody.GetProperty("userName").GetString());
        Assert.Equal("Member", addedBody.GetProperty("memberType").GetString());
        var memberId = addedBody.GetProperty("id").GetString()!;
        var (duplicate, duplicateBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/tenant-members", $$"""{"tenantId":"contoso","userId":"{{operatorId}}"}""", superadmin));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal("TENANCY_CONFLICT_005", duplicateBody.GetProperty("code").GetString());
        var (noTenant, noTenantBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/tenant-members", $$"""{"tenantId":"nope","userId":"{{operatorId}}"}""", superadmin));
        Assert.Equal(HttpStatusCode.NotFound, noTenant.StatusCode);
        Assert.Equal("TENANCY_NOTFOUND_001", noTenantBody.GetProperty("code").GetString());
        var (noUser, noUserBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/tenant-members", $$"""{"tenantId":"contoso","userId":"{{Guid.NewGuid()}}"}""", superadmin));
        Assert.Equal(HttpStatusCode.NotFound, noUser.StatusCode);
        Assert.Equal("TENANCY_NOTFOUND_003", noUserBody.GetProperty("code").GetString());

        var (listed, listedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/tenant-members?tenantId=contoso", token: superadmin));
        Assert.Equal(HttpStatusCode.OK, listed.StatusCode);
        Assert.Equal(["operator"], listedBody.GetProperty("items").EnumerateArray().Select(static m => m.GetProperty("userName").GetString()).ToList());

        // 租户主体碰不到成员管理（平台侧权限）
        var contosoAdmin = (await AuthFlowTests.LoginAsync(server, "admin", "Admin@123456", "contoso")).GetProperty("accessToken").GetString()!;
        var (tenantSide, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/tenant-members?tenantId=contoso", token: contosoAdmin));
        Assert.Equal(HttpStatusCode.Forbidden, tenantSide.StatusCode);

        // operator 登录后能进 contoso（成员），不能进 demo（既非注册地也非成员），回平台可以（平台账号）
        var operatorLogin = await AuthFlowTests.LoginAsync(server, "operator", "Operator@123");
        var operatorToken = operatorLogin.GetProperty("accessToken").GetString()!;
        var operatorRefresh = operatorLogin.GetProperty("refreshToken").GetString()!;
        var (operatorMine, operatorMineBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/tenants:mine", token: operatorToken));
        Assert.Equal(HttpStatusCode.OK, operatorMine.StatusCode);
        Assert.Equal(["contoso"], operatorMineBody.GetProperty("tenants").EnumerateArray().Select(static t => t.GetProperty("tenantId").GetString()).ToList());
        Assert.False(operatorMineBody.GetProperty("tenants")[0].GetProperty("isHome").GetBoolean());

        var (deniedSwitch, deniedSwitchBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:switch-tenant", """{"tenantId":"demo"}""", operatorToken));
        Assert.Equal(HttpStatusCode.Forbidden, deniedSwitch.StatusCode);
        Assert.Equal("IDENTITY_DENIED_006", deniedSwitchBody.GetProperty("code").GetString());

        var (switched, switchedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:switch-tenant", """{"tenantId":"contoso"}""", operatorToken));
        Assert.Equal(HttpStatusCode.OK, switched.StatusCode);
        Assert.Equal("contoso", switchedBody.GetProperty("tenantId").GetString());
        Assert.Equal("Contoso", switchedBody.GetProperty("tenantName").GetString());
        Assert.Equal(operatorLogin.GetProperty("sessionId").GetString(), switchedBody.GetProperty("sessionId").GetString());
        var inContoso = switchedBody.GetProperty("accessToken").GetString()!;

        // 旧刷新令牌随切换轮换失效；新令牌在 contoso 里工作：auth.me 带租户名，权限是租户侧（成员没有角色即没有权限码）
        var (staleRefresh, staleRefreshBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:refresh", $$"""{"refreshToken":"{{operatorRefresh}}"}"""));
        Assert.Equal(HttpStatusCode.Forbidden, staleRefresh.StatusCode);
        Assert.Equal("IDENTITY_DENIED_001", staleRefreshBody.GetProperty("code").GetString());
        var (me, meBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:me", token: inContoso));
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        Assert.Equal("contoso", meBody.GetProperty("tenantId").GetString());
        Assert.Equal("Contoso", meBody.GetProperty("tenantName").GetString());
        Assert.Equal(JsonValueKind.Null, meBody.GetProperty("impersonatorUserName").ValueKind);
        var (permissions, permissionsBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:permissions", token: inContoso));
        Assert.Equal(HttpStatusCode.OK, permissions.StatusCode);
        Assert.False(permissionsBody.GetProperty("isPlatform").GetBoolean());
        Assert.Empty(permissionsBody.GetProperty("permissions").EnumerateArray());
        var (mineInside, mineInsideBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/tenants:mine", token: inContoso));
        Assert.Equal(HttpStatusCode.OK, mineInside.StatusCode);
        Assert.False(mineInsideBody.GetProperty("platformIsCurrent").GetBoolean());
        Assert.True(mineInsideBody.GetProperty("tenants")[0].GetProperty("isCurrent").GetBoolean());

        // 回平台：平台账号可以；租户管理员（注册地 contoso）回平台被拒，切到自己的注册地允许
        var (back, backBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:switch-tenant", "{}", inContoso));
        Assert.Equal(HttpStatusCode.OK, back.StatusCode);
        Assert.Equal(JsonValueKind.Null, backBody.GetProperty("tenantId").ValueKind);
        var (adminToPlatform, adminToPlatformBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:switch-tenant", "{}", contosoAdmin));
        Assert.Equal(HttpStatusCode.Forbidden, adminToPlatform.StatusCode);
        Assert.Equal("IDENTITY_DENIED_006", adminToPlatformBody.GetProperty("code").GetString());
        var (adminHome, adminHomeBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:switch-tenant", """{"tenantId":"contoso"}""", contosoAdmin));
        Assert.Equal(HttpStatusCode.OK, adminHome.StatusCode);
        Assert.Equal("contoso", adminHomeBody.GetProperty("tenantId").GetString());
        // 注册地租户的加入时间是账号建立的时间
        var (_, adminMine) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/tenants:mine", token: contosoAdmin));
        var home = Assert.Single(adminMine.GetProperty("tenants").EnumerateArray());
        Assert.True(home.GetProperty("isHome").GetBoolean());
        Assert.True(home.GetProperty("joinedAt").GetDateTimeOffset() > DateTimeOffset.UnixEpoch);

        // 移除成员后不能再进；成员行不存在逐项报错
        var (removed, removedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Delete, $"/api/tenant-members?ids={memberId}&ids={Guid.NewGuid()}", token: superadmin));
        Assert.Equal(HttpStatusCode.OK, removed.StatusCode);
        Assert.Equal([memberId], removedBody.GetProperty("succeeded").EnumerateArray().Select(static e => e.GetString()).ToList());
        Assert.Equal("TENANCY_NOTFOUND_002", removedBody.GetProperty("failed")[0].GetProperty("code").GetString());
        var backToken = backBody.GetProperty("accessToken").GetString()!;
        var (noLonger, noLongerBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:switch-tenant", """{"tenantId":"contoso"}""", backToken));
        Assert.Equal(HttpStatusCode.Forbidden, noLonger.StatusCode);
        Assert.Equal("IDENTITY_DENIED_006", noLongerBody.GetProperty("code").GetString());

        // 停用的租户进不去
        var (suspended, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/tenants:suspend", $$"""{"ids":["{{contosoId}}"]}""", superadmin));
        Assert.Equal(HttpStatusCode.OK, suspended.StatusCode);
        var (unavailable, unavailableBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:switch-tenant", """{"tenantId":"contoso"}""", superadmin));
        Assert.Equal(HttpStatusCode.Forbidden, unavailable.StatusCode);
        Assert.Equal("IDENTITY_DENIED_005", unavailableBody.GetProperty("code").GetString());
        _ = superadminId;
    }
}
