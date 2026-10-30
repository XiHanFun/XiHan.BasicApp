// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using XiHan.BasicApp.Integration.Tests.Infrastructure;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证模仿登录的发起、限制与结束。
/// </summary>
/// <remarks>
/// 候选范围、发起（独立会话、<c>act</c> 声明、目标身份与租户的权限）、模仿态里的限制（不能再模仿、不能切租户）、
/// 结束模仿回到发起人原会话、发起人注销后模仿令牌随之失效、租户主体钉在自己的租户。
/// </remarks>
public sealed class ImpersonationTests : IDisposable
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
    public async Task Candidates_Start_Restrictions_Stop()
    {
        await using var server = await StartAsync();
        var superadminLogin = await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123");
        var superadmin = superadminLogin.GetProperty("accessToken").GetString()!;
        var superadminId = superadminLogin.GetProperty("user").GetProperty("id").GetString()!;

        // 演示租户 demo 的管理员（种子）与一个平台账号 helper
        var (demoDetailList, demoList) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/tenants:list", """{"keyword":"demo"}""", superadmin));
        Assert.Equal(HttpStatusCode.OK, demoDetailList.StatusCode);
        var demoAdminId = demoList.GetProperty("items")[0].GetProperty("adminUserId").GetString()!;
        var (helperCreated, helperBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users", """{"userName":"helper","displayName":"Helper","password":"Helper@12345"}""", superadmin));
        Assert.Equal(HttpStatusCode.Created, helperCreated.StatusCode);
        var helperId = helperBody.GetProperty("id").GetString()!;

        // 候选：平台里有 helper（不含自己）；demo 里有管理员 admin，关键字收窄
        var (platformCandidates, platformBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/impersonation:candidates", token: superadmin));
        Assert.Equal(HttpStatusCode.OK, platformCandidates.StatusCode);
        var platformNames = platformBody.GetProperty("items").EnumerateArray().Select(static c => c.GetProperty("userName").GetString()).ToList();
        Assert.Contains("helper", platformNames);
        Assert.DoesNotContain("superadmin", platformNames);
        var (demoCandidates, demoCandidatesBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/impersonation:candidates?tenantId=demo&keyword=adm", token: superadmin));
        Assert.Equal(HttpStatusCode.OK, demoCandidates.StatusCode);
        Assert.Equal(["admin"], demoCandidatesBody.GetProperty("items").EnumerateArray().Select(static c => c.GetProperty("userName").GetString()).ToList());

        // 目标不在该上下文里（平台账号 helper 不是 demo 的成员）、模仿自己：都拒绝
        var (wrongScope, wrongScopeBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/impersonation:start", $$"""{"targetUserId":"{{helperId}}","tenantId":"demo"}""", superadmin));
        Assert.Equal(HttpStatusCode.Forbidden, wrongScope.StatusCode);
        Assert.Equal("IDENTITY_DENIED_007", wrongScopeBody.GetProperty("code").GetString());
        var (self, selfBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/impersonation:start", $$"""{"targetUserId":"{{superadminId}}"}""", superadmin));
        Assert.Equal(HttpStatusCode.Forbidden, self.StatusCode);
        Assert.Equal("IDENTITY_DENIED_007", selfBody.GetProperty("code").GetString());

        // 发起：以 demo 管理员身份进 demo；响应带发起人；auth.me 同样带；权限是 demo 管理员的（租户侧全部），平台专属操作被拒
        var (started, startedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/impersonation:start", $$"""{"targetUserId":"{{demoAdminId}}","tenantId":"demo","reason":"ticket 42"}""", superadmin));
        Assert.Equal(HttpStatusCode.OK, started.StatusCode);
        Assert.Equal("admin", startedBody.GetProperty("user").GetProperty("userName").GetString());
        Assert.Equal("demo", startedBody.GetProperty("tenantId").GetString());
        Assert.Equal("superadmin", startedBody.GetProperty("impersonatorUserName").GetString());
        Assert.NotEqual(superadminLogin.GetProperty("sessionId").GetString(), startedBody.GetProperty("sessionId").GetString());
        var impersonating = startedBody.GetProperty("accessToken").GetString()!;
        var (me, meBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:me", token: impersonating));
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        Assert.Equal("superadmin", meBody.GetProperty("impersonatorUserName").GetString());
        Assert.Equal("Demo Tenant", meBody.GetProperty("tenantName").GetString());
        var (permissions, permissionsBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:permissions", token: impersonating));
        Assert.Equal(HttpStatusCode.OK, permissions.StatusCode);
        Assert.False(permissionsBody.GetProperty("isPlatform").GetBoolean());
        Assert.Contains("identity.users.create", permissionsBody.GetProperty("permissions").EnumerateArray().Select(static p => p.GetString()));
        var (platformOnly, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/tenants:list", "{}", impersonating));
        Assert.Equal(HttpStatusCode.Forbidden, platformOnly.StatusCode);

        // 模仿态里：不能再模仿、不能切租户
        var (nested, nestedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/impersonation:start", $$"""{"targetUserId":"{{helperId}}","tenantId":"demo"}""", impersonating));
        Assert.Equal(HttpStatusCode.Conflict, nested.StatusCode);
        Assert.Equal("IDENTITY_CONFLICT_003", nestedBody.GetProperty("code").GetString());
        var (noSwitch, noSwitchBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:switch-tenant", "{}", impersonating));
        Assert.Equal(HttpStatusCode.Conflict, noSwitch.StatusCode);
        Assert.Equal("IDENTITY_CONFLICT_003", noSwitchBody.GetProperty("code").GetString());

        // 刷新模仿会话仍带发起人；非模仿态调 stop 是校验错误
        var (refreshed, refreshedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:refresh", $$"""{"refreshToken":"{{startedBody.GetProperty("refreshToken").GetString()}}"}"""));
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        Assert.Equal("superadmin", refreshedBody.GetProperty("impersonatorUserName").GetString());
        var (notActive, notActiveBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/impersonation:stop", "{}", superadmin));
        Assert.Equal(HttpStatusCode.BadRequest, notActive.StatusCode);
        Assert.Equal("IDENTITY_VALIDATION_006", notActiveBody.GetProperty("code").GetString());

        // 结束：回到超管原会话（同一 sid），模仿令牌随即失效
        var refreshedToken = refreshedBody.GetProperty("accessToken").GetString()!;
        var (stopped, stoppedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/impersonation:stop", "{}", refreshedToken));
        Assert.Equal(HttpStatusCode.OK, stopped.StatusCode);
        Assert.Equal("superadmin", stoppedBody.GetProperty("user").GetProperty("userName").GetString());
        Assert.Equal(superadminLogin.GetProperty("sessionId").GetString(), stoppedBody.GetProperty("sessionId").GetString());
        Assert.Equal(JsonValueKind.Null, stoppedBody.GetProperty("impersonatorUserName").ValueKind);
        var (dead, deadBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:me", token: refreshedToken));
        Assert.Equal(HttpStatusCode.Forbidden, dead.StatusCode);
        Assert.Equal("IDENTITY_DENIED_001", deadBody.GetProperty("code").GetString());

        // 租户主体：demo 管理员只能在 demo 里模仿（不填租户即 demo）；指定平台被拒；发起人注销后模仿令牌失效
        var (member, memberBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users", """{"userName":"clerk","displayName":"Clerk","password":"Clerk@123456","homeTenantId":"demo"}""", superadmin));
        Assert.Equal(HttpStatusCode.Created, member.StatusCode);
        var clerkId = memberBody.GetProperty("id").GetString()!;
        var demoAdminLogin = await AuthFlowTests.LoginAsync(server, "admin", "Demo@123456", "demo");
        var demoAdmin = demoAdminLogin.GetProperty("accessToken").GetString()!;
        var (ownCandidates, ownCandidatesBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/impersonation:candidates", token: demoAdmin));
        Assert.Equal(HttpStatusCode.OK, ownCandidates.StatusCode);
        // 超管是 demo 的运维支持成员（演示种子），不算租户内的用户，不在候选里
        Assert.Equal(["clerk"], ownCandidatesBody.GetProperty("items").EnumerateArray().Select(static c => c.GetProperty("userName").GetString()).ToList());
        var (leave, leaveBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/impersonation:start", $$"""{"targetUserId":"{{helperId}}","tenantId":"platform"}""", demoAdmin));
        Assert.Equal(HttpStatusCode.Forbidden, leave.StatusCode);
        Assert.Equal("IDENTITY_DENIED_006", leaveBody.GetProperty("code").GetString());
        var (outsider, outsiderBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/impersonation:start", $$"""{"targetUserId":"{{helperId}}"}""", demoAdmin));
        Assert.Equal(HttpStatusCode.Forbidden, outsider.StatusCode);
        Assert.Equal("IDENTITY_DENIED_007", outsiderBody.GetProperty("code").GetString());
        var (inside, insideBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/impersonation:start", $$"""{"targetUserId":"{{clerkId}}"}""", demoAdmin));
        Assert.Equal(HttpStatusCode.OK, inside.StatusCode);
        Assert.Equal("clerk", insideBody.GetProperty("user").GetProperty("userName").GetString());
        var asClerk = insideBody.GetProperty("accessToken").GetString()!;
        var (clerkMe, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:me", token: asClerk));
        Assert.Equal(HttpStatusCode.OK, clerkMe.StatusCode);
        var (logout, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:logout", "{}", demoAdmin));
        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);
        var (orphan, orphanBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:me", token: asClerk));
        Assert.Equal(HttpStatusCode.Forbidden, orphan.StatusCode);
        Assert.Equal("IDENTITY_DENIED_001", orphanBody.GetProperty("code").GetString());
    }
}
