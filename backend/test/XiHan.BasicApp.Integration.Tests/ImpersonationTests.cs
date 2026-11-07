// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.BasicApp.Integration.Tests.Infrastructure;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证模仿登录的发起、限制与结束
/// </summary>
/// <remarks>
/// 候选范围、发起（独立会话、<c>act</c> 声明、目标身份与租户的权限）、模仿态里的限制（不能再模仿、不能切租户）、
/// 结束模仿回到发起人原会话、发起人注销后模仿令牌随之失效、租户主体钉在自己的租户、
/// 会话时长的参数与硬期限（刷新续不了期）、给被模仿者的安全通知
/// </remarks>
public sealed class ImpersonationTests : IDisposable
{
    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private async Task<BasicAppTestServer> StartAsync(TimeProvider? clock = null)
    {
        var server = await BasicAppTestServer.StartAsync(_database.ConnectionString, settings: [new("BasicApp:Seed:EnableDemoData", "true")], configure: clock is null ? null : builder => builder.Services.AddSingleton(clock));
        await server.SeedDefaultsAsync();
        await server.RunSeedersAsync();
        return server;
    }

    private static async Task<string> DemoAdminIdAsync(BasicAppTestServer server, string superadmin)
    {
        var (_, list) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/tenants:list", """{"keyword":"demo"}""", superadmin));
        return list.GetProperty("items")[0].GetProperty("adminUserId").GetString()!;
    }

    private static async Task<JsonElement> StartImpersonationAsync(BasicAppTestServer server, string token, string targetId, string? tenantId = null, string? acceptLanguage = null)
    {
        var body = tenantId is null ? $$"""{"targetUserId":"{{targetId}}"}""" : $$"""{"targetUserId":"{{targetId}}","tenantId":"{{tenantId}}"}""";
        var (started, startedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/impersonation:start", body, token, acceptLanguage));
        Assert.Equal(HttpStatusCode.OK, started.StatusCode);
        return startedBody;
    }

    private static async Task StopImpersonationAsync(BasicAppTestServer server, JsonElement started)
    {
        var (stopped, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/impersonation:stop", "{}", started.GetProperty("accessToken").GetString()!));
        Assert.Equal(HttpStatusCode.OK, stopped.StatusCode);
    }

    private static async Task<string> ConfigureAsync(BasicAppTestServer server, string platform, string dataType, string value)
    {
        var (created, createdBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/configs", JsonSerializer.Serialize(new { key = ImpersonationSettings.ConfigKey, name = "Impersonation", dataType, value }), platform));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        return createdBody.GetProperty("id").GetString()!;
    }

    private static async Task<List<JsonElement>> SecurityNoticesAsync(BasicAppTestServer server, string token)
    {
        var (inbox, inboxBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/inbox:list", "{}", token));
        Assert.Equal(HttpStatusCode.OK, inbox.StatusCode);
        return inboxBody.GetProperty("items").EnumerateArray().Where(static n => n.GetProperty("notificationType").GetString() == "Security").ToList();
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
        // 分页：带总数，按登录名排序；页大小 1 时第二页接着第一页
        var platformTotal = platformBody.GetProperty("total").GetInt64();
        Assert.Equal(platformNames.Count, platformTotal);
        var (firstPage, firstPageBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/impersonation:candidates?pageIndex=1&pageSize=1", token: superadmin));
        var (secondPage, secondPageBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/impersonation:candidates?pageIndex=2&pageSize=1", token: superadmin));
        Assert.Equal((HttpStatusCode.OK, HttpStatusCode.OK), (firstPage.StatusCode, secondPage.StatusCode));
        Assert.Equal((platformTotal, platformTotal), (firstPageBody.GetProperty("total").GetInt64(), secondPageBody.GetProperty("total").GetInt64()));
        Assert.Equal(platformNames.Take(2), firstPageBody.GetProperty("items").EnumerateArray().Concat(secondPageBody.GetProperty("items").EnumerateArray()).Select(static c => c.GetProperty("userName").GetString()));
        var (oversized, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/impersonation:candidates?pageSize=101", token: superadmin));
        Assert.Equal(HttpStatusCode.BadRequest, oversized.StatusCode);
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

        // 原会话还挂着一条模仿会话：再发起被拒，要先结束
        var (second, secondBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/impersonation:start", $$"""{"targetUserId":"{{demoAdminId}}","tenantId":"demo","reason":"again"}""", superadmin));
        Assert.Equal((HttpStatusCode.Conflict, "IDENTITY_CONFLICT_015"), (second.StatusCode, secondBody.GetProperty("code").GetString()));
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
        // 超管是 demo 的运维支持成员（演示种子），不算租户内的用户，不在候选里；演示种子另有流程演示用的三个成员
        Assert.Equal(["clerk", "hr", "lead", "manager"], ownCandidatesBody.GetProperty("items").EnumerateArray().Select(static c => c.GetProperty("userName").GetString()).ToList());
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

    [Fact]
    public async Task Session_LastsThirtyMinutesByDefault_RefreshCannotExtendIt_AndItEndsOnTime()
    {
        var clock = new ManualClock(DateTimeOffset.UtcNow);
        await using var server = await StartAsync(clock);
        var superadminLogin = await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123");
        var superadmin = superadminLogin.GetProperty("accessToken").GetString()!;
        var demoAdminId = await DemoAdminIdAsync(server, superadmin);

        // 缺省 30 分钟（不是刷新令牌的 7 天）；访问令牌也不超过它（缺省 120 分钟）
        var started = await StartImpersonationAsync(server, superadmin, demoAdminId, "demo");
        var deadline = clock.GetUtcNow().AddMinutes(30);
        Assert.Equal(deadline, started.GetProperty("refreshExpiresAt").GetDateTimeOffset());
        Assert.Equal(deadline, started.GetProperty("accessExpiresAt").GetDateTimeOffset());

        // 中途刷新：令牌轮换，到期时间不动，新访问令牌同样到期为止
        clock.Set(clock.GetUtcNow().AddMinutes(10));
        var (refreshed, refreshedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:refresh", $$"""{"refreshToken":"{{started.GetProperty("refreshToken").GetString()}}"}"""));
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        Assert.Equal(deadline, refreshedBody.GetProperty("refreshExpiresAt").GetDateTimeOffset());
        Assert.Equal(deadline, refreshedBody.GetProperty("accessExpiresAt").GetDateTimeOffset());
        Assert.NotEqual(started.GetProperty("refreshToken").GetString(), refreshedBody.GetProperty("refreshToken").GetString());

        // 到期：刷新被拒，手里的访问令牌过不了门禁，结束模仿也一样，只能重新发起
        // （令牌校验用真实时间，拨钟之后还算没过期的只有拨钟之前签的那一枚，用它看门禁）
        clock.Set(deadline.AddSeconds(1));
        var (expired, expiredBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:refresh", $$"""{"refreshToken":"{{refreshedBody.GetProperty("refreshToken").GetString()}}"}"""));
        Assert.Equal(HttpStatusCode.Forbidden, expired.StatusCode);
        Assert.Equal("IDENTITY_DENIED_001", expiredBody.GetProperty("code").GetString());
        var impersonating = started.GetProperty("accessToken").GetString()!;
        var (me, meBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:me", token: impersonating));
        Assert.Equal(HttpStatusCode.Forbidden, me.StatusCode);
        Assert.Equal("IDENTITY_DENIED_001", meBody.GetProperty("code").GetString());
        var (stop, stopBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/impersonation:stop", "{}", impersonating));
        Assert.Equal(HttpStatusCode.Forbidden, stop.StatusCode);
        Assert.Equal("IDENTITY_DENIED_001", stopBody.GetProperty("code").GetString());

        // 发起人自己的会话不受影响，照常按 7 天顺延；再发起一次又是 30 分钟
        var (own, ownBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:refresh", $$"""{"refreshToken":"{{superadminLogin.GetProperty("refreshToken").GetString()}}"}"""));
        Assert.Equal(HttpStatusCode.OK, own.StatusCode);
        Assert.Equal(clock.GetUtcNow().AddDays(7), ownBody.GetProperty("refreshExpiresAt").GetDateTimeOffset());
        var again = await StartImpersonationAsync(server, superadmin, demoAdminId, "demo");
        Assert.Equal(clock.GetUtcNow().AddMinutes(30), again.GetProperty("refreshExpiresAt").GetDateTimeOffset());
    }

    [Fact]
    public async Task Settings_SetTheLifetimeAndTheNotice_AndAWrongValueStopsTheStart()
    {
        var clock = new ManualClock(DateTimeOffset.UtcNow);
        await using var server = await StartAsync(clock);
        var superadmin = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;
        var demoAdminId = await DemoAdminIdAsync(server, superadmin);
        var demoAdmin = (await AuthFlowTests.LoginAsync(server, "admin", "Demo@123456", "demo")).GetProperty("accessToken").GetString()!;

        // 设了 5 分钟、不通知：时长照参数，被模仿者收不到；访问令牌与会话一起到期
        var id = await ConfigureAsync(server, superadmin, "Json", """{"sessionMinutes":5,"notifyTarget":false}""");
        var started = await StartImpersonationAsync(server, superadmin, demoAdminId, "demo");
        Assert.Equal(clock.GetUtcNow().AddMinutes(5), started.GetProperty("refreshExpiresAt").GetDateTimeOffset());
        Assert.Equal(clock.GetUtcNow().AddMinutes(5), started.GetProperty("accessExpiresAt").GetDateTimeOffset());
        Assert.Empty(await SecurityNoticesAsync(server, demoAdmin));
        await StopImpersonationAsync(server, started);

        // 只写一项，另一项取缺省：8 小时是上限，比访问令牌（120 分钟）长时访问令牌仍是自己的时长，要靠刷新续
        await UpdateAsync("""{"sessionMinutes":480}""");
        var longest = await StartImpersonationAsync(server, superadmin, demoAdminId, "demo");
        Assert.Equal(clock.GetUtcNow().AddMinutes(480), longest.GetProperty("refreshExpiresAt").GetDateTimeOffset());
        Assert.Equal(clock.GetUtcNow().AddMinutes(120), longest.GetProperty("accessExpiresAt").GetDateTimeOffset());
        Assert.Single(await SecurityNoticesAsync(server, demoAdmin));
        await StopImpersonationAsync(server, longest);

        // 写错：超出范围、类型不对、不是 JSON，都不发起（看不懂的参数不当缺省用）
        foreach (var (dataType, value) in new[] { ("Json", """{"sessionMinutes":481}"""), ("Json", """{"sessionMinutes":0}"""), ("Json", """{"notifyTarget":"yes"}"""), ("Text", "thirty") })
        {
            await UpdateAsync(value, dataType);
            var (rejected, rejectedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/impersonation:start", $$"""{"targetUserId":"{{demoAdminId}}","tenantId":"demo"}""", superadmin));
            Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
            Assert.Equal("IDENTITY_VALIDATION_027", rejectedBody.GetProperty("code").GetString());
        }

        Assert.Single(await SecurityNoticesAsync(server, demoAdmin));

        async Task UpdateAsync(string value, string dataType = "Json")
        {
            var (updated, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/configs/{id}", JsonSerializer.Serialize(new { name = "Impersonation", dataType, value }), superadmin));
            Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        }
    }

    [Fact]
    public async Task Notice_ReachesTheTargetOnTheirSide_InTheirLanguage_WhateverTheirPreferences()
    {
        await using var server = await StartAsync();
        var superadmin = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;
        var demoAdminId = await DemoAdminIdAsync(server, superadmin);

        // 平台主体模仿租户里的人：通知落在租户那一侧。被模仿者关了安全通知与站内信也照样收到（账号通知不看偏好）；没存界面语言按发起调用的区域
        var demoAdmin = (await AuthFlowTests.LoginAsync(server, "admin", "Demo@123456", "demo")).GetProperty("accessToken").GetString()!;
        var (preference, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/profile:update-notification-preference", """{"channelInApp":false,"channelEmail":false,"channelSms":false,"typeAnnouncement":false,"typeTask":false,"typeSecurity":false}""", demoAdmin));
        Assert.Equal(HttpStatusCode.OK, preference.StatusCode);
        var intoTenant = await StartImpersonationAsync(server, superadmin, demoAdminId, "demo", acceptLanguage: "en-US");
        var notice = Assert.Single(await SecurityNoticesAsync(server, demoAdmin));
        Assert.Equal("An administrator signed in as you", notice.GetProperty("title").GetString());
        Assert.Contains("superadmin", notice.GetProperty("content").GetString(), StringComparison.Ordinal);
        Assert.Contains("30 minutes", notice.GetProperty("content").GetString(), StringComparison.Ordinal);
        // 平台一侧的收件箱里没有这一条
        Assert.Empty(await SecurityNoticesAsync(server, superadmin));
        await StopImpersonationAsync(server, intoTenant);

        // 租户主体模仿本租户的人：存了界面语言的按他的语言
        var manager = (await AuthFlowTests.LoginAsync(server, "manager", "Demo@123456", "demo")).GetProperty("accessToken").GetString()!;
        var (saved, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, "/api/user-settings", JsonSerializer.Serialize(new { scene = "Preference", key = "global", value = """{"xihan_locale":"ja-JP"}""", clientId = "tests" }), manager));
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        var (candidates, candidatesBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/impersonation:candidates?keyword=manager", token: demoAdmin));
        Assert.Equal(HttpStatusCode.OK, candidates.StatusCode);
        await StartImpersonationAsync(server, demoAdmin, Assert.Single(candidatesBody.GetProperty("items").EnumerateArray()).GetProperty("id").GetString()!, acceptLanguage: "en-US");
        var localized = Assert.Single(await SecurityNoticesAsync(server, manager));
        Assert.Equal("管理者があなたとしてログインしました", localized.GetProperty("title").GetString());
        Assert.Contains("admin", localized.GetProperty("content").GetString(), StringComparison.Ordinal);

        // 平台主体模仿平台账号：落在平台一侧
        var (helperCreated, helperBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users", """{"userName":"helper","displayName":"Helper","password":"Helper@12345"}""", superadmin));
        Assert.Equal(HttpStatusCode.Created, helperCreated.StatusCode);
        await StartImpersonationAsync(server, superadmin, helperBody.GetProperty("id").GetString()!);
        var helper = (await AuthFlowTests.LoginAsync(server, "helper", "Helper@12345")).GetProperty("accessToken").GetString()!;
        Assert.Single(await SecurityNoticesAsync(server, helper));
    }
}
