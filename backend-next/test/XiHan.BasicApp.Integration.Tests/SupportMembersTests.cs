// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Access;
using XiHan.BasicApp.Identity;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.BasicApp.Organization;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证运维支持成员
/// </summary>
/// <remarks>
/// 照 4.x 列进租户用户列表，带标记、账号只读、能配角色；不占席位，不进模仿候选、群发收件人和聊天目录
/// </remarks>
public sealed class SupportMembersTests : IDisposable
{
    private const string Password = "Passw0rd!x";

    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private static async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(BasicAppTestServer server, HttpMethod method, string path, string? body, string? token)
    {
        var (response, json) = await server.SendAsync(BasicAppTestServer.Request(method, path, body, token));
        return (response.StatusCode, json);
    }

    /// <summary>
    /// 建一个平台账号，以运维支持身份加入 contoso
    /// </summary>
    private static async Task<string> AddOpsAsync(BasicAppTestServer server, string platform, DateTimeOffset? expiresAt = null)
    {
        var (created, createdBody) = await SendAsync(server, HttpMethod.Post, "/api/users", JsonSerializer.Serialize(new { userName = "ops", displayName = "OPS", password = Password }), platform);
        Assert.True(created == HttpStatusCode.Created, createdBody.ToString());
        var ops = createdBody.GetProperty("id").GetString()!;
        var (added, addedBody) = await SendAsync(server, HttpMethod.Post, "/api/tenant-members", JsonSerializer.Serialize(new { tenantId = "contoso", userId = ops, memberType = "Support", expiresAt }), platform);
        Assert.True(added == HttpStatusCode.Created, addedBody.ToString());
        return ops;
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

    private static async Task<JsonElement> ListAsync(BasicAppTestServer server, string token, string? isSupportMember = null)
    {
        var filters = isSupportMember is null ? [] : new[] { new { field = "isSupportMember", @operator = "eq", value = isSupportMember } };
        var (status, body) = await SendAsync(server, HttpMethod.Post, "/api/users:list", JsonSerializer.Serialize(new { page = new { index = 1, size = 100 }, filters }), token);
        Assert.True(status == HttpStatusCode.OK, body.ToString());
        return body;
    }

    private static List<string?> UserNames(JsonElement page) => page.GetProperty("items").EnumerateArray().Select(static u => u.GetProperty("userName").GetString()).ToList();

    private static string FailureCode(JsonElement batch) => Assert.Single(batch.GetProperty("failed").EnumerateArray()).GetProperty("code").GetString()!;

    private static async Task<(string Access, string Refresh)> SwitchIntoContosoAsync(BasicAppTestServer server)
    {
        var login = (await AuthFlowTests.LoginAsync(server, "ops", Password)).GetProperty("accessToken").GetString()!;
        var (status, body) = await SendAsync(server, HttpMethod.Post, "/api/auth:switch-tenant", """{"tenantId":"contoso"}""", login);
        Assert.True(status == HttpStatusCode.OK, body.ToString());
        return (body.GetProperty("accessToken").GetString()!, body.GetProperty("refreshToken").GetString()!);
    }

    [Fact]
    public async Task ListedWithAMark_AccountStaysReadOnly_AndOnlyRolesCanBeConfigured()
    {
        var (server, platform, contoso) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;
        var ops = await AddOpsAsync(server, platform);
        await AuthFlowTests.LoginAsync(server, "ops", Password);

        // 列表带标记；注册在别处，外部成员的标记也为真（账号只读靠它）；普通用户两个都是假
        var rows = (await ListAsync(server, contoso)).GetProperty("items").EnumerateArray().ToList();
        var row = rows.Single(static u => u.GetProperty("userName").GetString() == "ops");
        Assert.True(row.GetProperty("isSupportMember").GetBoolean());
        Assert.True(row.GetProperty("isExternalMember").GetBoolean());
        var admin = rows.Single(static u => u.GetProperty("userName").GetString() == "admin");
        Assert.False(admin.GetProperty("isSupportMember").GetBoolean());

        // 按 ID 取与详情都带标记，详情不给安全信息（账号不归这个租户管）
        var (found, foundBody) = await SendAsync(server, HttpMethod.Get, $"/api/users/{ops}", null, contoso);
        Assert.Equal((HttpStatusCode.OK, true), (found, foundBody.GetProperty("isSupportMember").GetBoolean()));
        var (detail, detailBody) = await SendAsync(server, HttpMethod.Get, $"/api/users/{ops}:detail", null, contoso);
        Assert.Equal(HttpStatusCode.OK, detail);
        Assert.True(detailBody.GetProperty("user").GetProperty("isSupportMember").GetBoolean());
        Assert.Equal(JsonValueKind.Null, detailBody.GetProperty("security").ValueKind);

        // 最近登录是平台账号的全局信息：列表、按 ID 取与详情都不给租户，平台一侧照常
        foreach (var view in new[] { row, foundBody, detailBody.GetProperty("user") })
        {
            Assert.Equal(JsonValueKind.Null, view.GetProperty("lastLoginAt").ValueKind);
            Assert.Equal(JsonValueKind.Null, view.GetProperty("lastLoginIp").ValueKind);
        }

        var (onPlatform, onPlatformBody) = await SendAsync(server, HttpMethod.Get, $"/api/users/{ops}", null, platform);
        Assert.Equal((HttpStatusCode.OK, JsonValueKind.String), (onPlatform, onPlatformBody.GetProperty("lastLoginAt").ValueKind));

        // 别的选人入口用这个筛选去掉他们；只接受 eq 与布尔值
        Assert.DoesNotContain("ops", UserNames(await ListAsync(server, contoso, "false")));
        Assert.Contains("admin", UserNames(await ListAsync(server, contoso, "false")));
        Assert.Equal(["ops"], UserNames(await ListAsync(server, contoso, "true")));
        var (badValue, badValueBody) = await SendAsync(server, HttpMethod.Post, "/api/users:list", """{"filters":[{"field":"isSupportMember","operator":"eq","value":"maybe"}]}""", contoso);
        Assert.Equal((HttpStatusCode.BadRequest, IdentityErrors.QueryUnsupported), (badValue, badValueBody.GetProperty("code").GetString()));
        var (badOperator, badOperatorBody) = await SendAsync(server, HttpMethod.Post, "/api/users:list", """{"filters":[{"field":"isSupportMember","operator":"ne","value":"true"}]}""", contoso);
        Assert.Equal((HttpStatusCode.BadRequest, IdentityErrors.QueryUnsupported), (badOperator, badOperatorBody.GetProperty("code").GetString()));

        // 平台一侧他就是注册在平台的账号，没有标记；筛选也不报错
        var platformRows = (await ListAsync(server, platform)).GetProperty("items").EnumerateArray().ToList();
        Assert.False(platformRows.Single(static u => u.GetProperty("userName").GetString() == "ops").GetProperty("isSupportMember").GetBoolean());
        Assert.Empty(UserNames(await ListAsync(server, platform, "true")));
        Assert.Contains("ops", UserNames(await ListAsync(server, platform, "false")));

        // 账号只读：资料、密码、登录策略按不存在回答，状态与删除逐项拒绝
        var (updated, updatedBody) = await SendAsync(server, HttpMethod.Put, $"/api/users/{ops}", """{"displayName":"Hacked"}""", contoso);
        Assert.Equal((HttpStatusCode.NotFound, IdentityErrors.UserNotFound), (updated, updatedBody.GetProperty("code").GetString()));
        var (reset, resetBody) = await SendAsync(server, HttpMethod.Post, $"/api/users/{ops}:reset-password", """{"newPassword":"Another1!x"}""", contoso);
        Assert.Equal((HttpStatusCode.NotFound, IdentityErrors.UserNotFound), (reset, resetBody.GetProperty("code").GetString()));
        var (policy, policyBody) = await SendAsync(server, HttpMethod.Post, $"/api/users/{ops}:set-login-policy", """{"allowMultiLogin":false,"maxLoginDevices":0}""", contoso);
        Assert.Equal((HttpStatusCode.NotFound, IdentityErrors.UserNotFound), (policy, policyBody.GetProperty("code").GetString()));
        foreach (var verb in new[] { "lock", "unlock", "disable", "enable", "reset-two-factor" })
        {
            var (status, body) = await SendAsync(server, HttpMethod.Post, $"/api/users:{verb}", $$"""{"ids":["{{ops}}"]}""", contoso);
            Assert.Equal(HttpStatusCode.OK, status);
            Assert.Equal(IdentityErrors.UserNotFound, FailureCode(body));
        }

        var (deleted, deletedBody) = await SendAsync(server, HttpMethod.Delete, $"/api/users?ids={ops}", null, contoso);
        Assert.Equal((HttpStatusCode.OK, IdentityErrors.UserNotFound), (deleted, FailureCode(deletedBody)));
        var (_, seen) = await SendAsync(server, HttpMethod.Get, $"/api/users/{ops}", null, platform);
        Assert.Equal("OPS", seen.GetProperty("displayName").GetString());

        // 只能配角色：部门、部门负责人、数据范围、直授、条件都不认
        var department = (await SendAsync(server, HttpMethod.Post, "/api/departments", """{"code":"hq","name":"HQ"}""", contoso)).Body.GetProperty("id").GetString()!;
        var (joined, joinedBody) = await SendAsync(server, HttpMethod.Post, "/api/memberships", $$"""{"userId":"{{ops}}","departmentId":"{{department}}"}""", contoso);
        Assert.Equal((HttpStatusCode.BadRequest, OrganizationErrors.UserNotInTenant), (joined, joinedBody.GetProperty("code").GetString()));
        var (lead, leadBody) = await SendAsync(server, HttpMethod.Post, "/api/departments", $$"""{"code":"ops","name":"Ops","leaderUserId":"{{ops}}"}""", contoso);
        Assert.Equal((HttpStatusCode.BadRequest, OrganizationErrors.UserNotInTenant), (lead, leadBody.GetProperty("code").GetString()));
        var (scoped, scopedBody) = await SendAsync(server, HttpMethod.Post, "/api/user-data-scopes:set", $$"""{"userId":"{{ops}}","dataScope":"SelfOnly"}""", contoso);
        Assert.Equal((HttpStatusCode.NotFound, AccessErrors.MemberNotFound), (scoped, scopedBody.GetProperty("code").GetString()));
        var (granted, grantedBody) = await SendAsync(server, HttpMethod.Post, "/api/user-grants:grant", $$"""{"userId":"{{ops}}","permissions":["identity.sessions.read"]}""", contoso);
        Assert.Equal((HttpStatusCode.NotFound, AccessErrors.MemberNotFound), (granted, grantedBody.GetProperty("code").GetString()));
    }

    [Fact]
    public async Task RoleWorksAfterSwitchingIn_UntilTheMembershipExpires()
    {
        // 令牌校验走真实时间：时钟从过去起步、拨动后还早于现在，令牌才一直有效
        var clock = new ManualClock(DateTimeOffset.UtcNow.AddHours(-1));
        var (server, platform, contoso) = await SettingsTestHost.StartAsync(_database.ConnectionString, configure: builder => builder.Services.AddSingleton<TimeProvider>(clock));
        await using var running = server;
        var expiresAt = clock.GetUtcNow().AddMinutes(30);
        var ops = await AddOpsAsync(server, platform, expiresAt);
        var roleId = await CreateRoleAsync(server, contoso, "reader", "identity.users.read");

        // 进得去，但没配角色什么都不能做
        var (access, refresh) = await SwitchIntoContosoAsync(server);
        Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(server, HttpMethod.Post, "/api/users:list", "{}", access)).Status);

        // 配上角色：角色成员带标记，按用户也查得到
        var (assigned, assignedBody) = await SendAsync(server, HttpMethod.Post, $"/api/roles/{roleId}:assign", $$"""{"userIds":["{{ops}}"]}""", contoso);
        Assert.Equal(HttpStatusCode.OK, assigned);
        Assert.Equal([ops], assignedBody.GetProperty("succeeded").EnumerateArray().Select(static e => e.GetString()));
        var (_, members) = await SendAsync(server, HttpMethod.Get, $"/api/roles/{roleId}:members", null, contoso);
        var member = Assert.Single(members.GetProperty("items").EnumerateArray());
        Assert.True(member.GetProperty("isSupportMember").GetBoolean());
        Assert.True(member.GetProperty("isExternalMember").GetBoolean());
        var (_, byUser) = await SendAsync(server, HttpMethod.Get, $"/api/roles:by-user?userId={ops}", null, contoso);
        Assert.Equal(roleId, Assert.Single(byUser.GetProperty("items").EnumerateArray()).GetProperty("id").GetString());

        // 列表按角色筛得到他，改指派有效期也认他
        var (_, filtered) = await SendAsync(server, HttpMethod.Post, "/api/users:list", $$"""{"filters":[{"field":"roleId","operator":"eq","value":"{{roleId}}"}]}""", contoso);
        Assert.Equal(["ops"], UserNames(filtered));
        var (window, windowBody) = await SendAsync(server, HttpMethod.Post, $"/api/roles/{roleId}:update-member", $$"""{"userId":"{{ops}}","expiresAt":"{{clock.GetUtcNow().AddMinutes(25):O}}"}""", contoso);
        Assert.True(window == HttpStatusCode.OK, windowBody.ToString());
        Assert.True(windowBody.GetProperty("isSupportMember").GetBoolean());

        // 试算认他：持有角色的人都在批量试算里
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Get, $"/api/permission-conditions:trial?userId={ops}&permission=identity.users.read", null, contoso)).Status);
        var (_, batch) = await SendAsync(server, HttpMethod.Get, $"/api/permission-conditions:batch-trial?subjectKind=Role&subjectId={roleId}&permission=identity.users.read", null, contoso);
        Assert.Equal(1, batch.GetProperty("totalUsers").GetInt32());
        var (trial, trialBody) = await SendAsync(server, HttpMethod.Post, "/api/policies:trial", $$"""{"action":"Deny","userId":"{{ops}}","ip":"10.1.2.3","conditions":[{"attributeName":"environment.ip","operator":"In","value":"10.0.0.0/8"}]}""", contoso);
        Assert.True(trial == HttpStatusCode.OK, trialBody.ToString());
        Assert.Equal("ops", trialBody.GetProperty("userName").GetString());

        // 续签后按角色能用，角色外的还是不行
        var (refreshed, refreshedBody) = await SendAsync(server, HttpMethod.Post, "/api/auth:refresh", $$"""{"refreshToken":"{{refresh}}"}""", null);
        Assert.True(refreshed == HttpStatusCode.OK, refreshedBody.ToString());
        access = refreshedBody.GetProperty("accessToken").GetString()!;
        refresh = refreshedBody.GetProperty("refreshToken").GetString()!;
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, "/api/users:list", "{}", access)).Status);
        Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(server, HttpMethod.Post, "/api/roles:list", "{}", access)).Status);

        // 到期：切不进去，留在里面的会话续签被拒，列表与角色成员都看不到，也配不上
        clock.Set(expiresAt.AddSeconds(1));
        var platformLogin = (await AuthFlowTests.LoginAsync(server, "ops", Password)).GetProperty("accessToken").GetString()!;
        var (denied, deniedBody) = await SendAsync(server, HttpMethod.Post, "/api/auth:switch-tenant", """{"tenantId":"contoso"}""", platformLogin);
        Assert.Equal((HttpStatusCode.Forbidden, "IDENTITY_DENIED_006"), (denied, deniedBody.GetProperty("code").GetString()));
        var (stale, staleBody) = await SendAsync(server, HttpMethod.Post, "/api/auth:refresh", $$"""{"refreshToken":"{{refresh}}"}""", null);
        Assert.Equal((HttpStatusCode.Forbidden, "IDENTITY_DENIED_006"), (stale, staleBody.GetProperty("code").GetString()));
        Assert.DoesNotContain("ops", UserNames(await ListAsync(server, contoso)));
        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(server, HttpMethod.Get, $"/api/users/{ops}", null, contoso)).Status);
        var (_, membersAfter) = await SendAsync(server, HttpMethod.Get, $"/api/roles/{roleId}:members", null, contoso);
        Assert.Equal(0, membersAfter.GetProperty("total").GetInt64());
        var otherRole = await CreateRoleAsync(server, contoso, "auditor");
        var (_, assignedLate) = await SendAsync(server, HttpMethod.Post, $"/api/roles/{otherRole}:assign", $$"""{"userIds":["{{ops}}"]}""", contoso);
        Assert.Equal(AccessErrors.MemberNotFound, FailureCode(assignedLate));
    }

    [Fact]
    public async Task RemovingTheMembership_EndsTheRolesToo()
    {
        var (server, platform, contoso) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;
        var ops = await AddOpsAsync(server, platform);
        var roleId = await CreateRoleAsync(server, contoso, "reader", "identity.users.read");
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, $"/api/roles/{roleId}:assign", $$"""{"userIds":["{{ops}}"]}""", contoso)).Status);

        // 再加回来不带旧角色（与外部成员同）
        var (_, listed) = await SendAsync(server, HttpMethod.Get, "/api/tenant-members?tenantId=contoso", null, platform);
        var memberId = listed.GetProperty("items").EnumerateArray().Single(m => m.GetProperty("userId").GetString() == ops).GetProperty("id").GetString()!;
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Delete, $"/api/tenant-members?ids={memberId}", null, platform)).Status);
        Assert.DoesNotContain("ops", UserNames(await ListAsync(server, contoso)));
        var (added, addedBody) = await SendAsync(server, HttpMethod.Post, "/api/tenant-members", $$"""{"tenantId":"contoso","userId":"{{ops}}","memberType":"Support"}""", platform);
        Assert.True(added == HttpStatusCode.Created, addedBody.ToString());
        var (_, byUser) = await SendAsync(server, HttpMethod.Get, $"/api/roles:by-user?userId={ops}", null, contoso);
        Assert.Empty(byUser.GetProperty("items").EnumerateArray());
    }

    [Fact]
    public async Task TakesNoSeat_AndStaysOutOfCandidatesRecipientsAndTheChatDirectory()
    {
        var (server, platform, contoso) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;
        async Task<long> SeatsAsync() => (await SendAsync(server, HttpMethod.Get, "/api/subscription", null, contoso)).Body.GetProperty("usedSeats").GetInt64();
        var seats = await SeatsAsync();
        var ops = await AddOpsAsync(server, platform);
        var roleId = await CreateRoleAsync(server, contoso, "reader", "identity.users.read");
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, $"/api/roles/{roleId}:assign", $$"""{"userIds":["{{ops}}"]}""", contoso)).Status);

        // 配了角色也不占席位
        Assert.Equal(seats, await SeatsAsync());

        // 模仿：候选里没有，直接发起也被拒
        var (_, candidates) = await SendAsync(server, HttpMethod.Get, "/api/impersonation:candidates?tenantId=contoso", null, platform);
        Assert.DoesNotContain("ops", candidates.GetProperty("items").EnumerateArray().Select(static c => c.GetProperty("userName").GetString()));
        var (impersonated, impersonatedBody) = await SendAsync(server, HttpMethod.Post, "/api/impersonation:start", $$"""{"targetUserId":"{{ops}}","tenantId":"contoso"}""", platform);
        Assert.Equal((HttpStatusCode.Forbidden, IdentityErrors.ImpersonationTargetUnavailable), (impersonated, impersonatedBody.GetProperty("code").GetString()));

        // 群发：全部用户只有管理员一个；指定他收不到；按角色的候选经一侧筛选，配了这个角色也不算
        async Task<(HttpStatusCode Status, JsonElement Body)> PublishAsync(object draft)
        {
            var (created, createdBody) = await SendAsync(server, HttpMethod.Post, "/api/notifications", JsonSerializer.Serialize(draft), contoso);
            Assert.True(created == HttpStatusCode.Created, createdBody.ToString());
            return await SendAsync(server, HttpMethod.Post, $"/api/notifications/{createdBody.GetProperty("id").GetString()}:publish", "{}", contoso);
        }

        Assert.Equal(1, (await PublishAsync(new { title = "All" })).Body.GetProperty("recipientCount").GetInt32());
        Assert.Equal("NOTIFICATION_VALIDATION_004", (await PublishAsync(new { title = "Ops", targetType = "User", targetIds = new[] { ops } })).Body.GetProperty("code").GetString());
        Assert.Equal("NOTIFICATION_VALIDATION_004", (await PublishAsync(new { title = "Readers", targetType = "Role", targetIds = new[] { roleId } })).Body.GetProperty("code").GetString());

        // 聊天：选人目录里没有，单聊也发不起来
        var (options, optionsBody) = await SendAsync(server, HttpMethod.Get, "/api/chat-conversations:user-options", null, contoso);
        Assert.True(options == HttpStatusCode.OK, optionsBody.ToString());
        Assert.DoesNotContain("ops", optionsBody.GetProperty("items").EnumerateArray().Select(static u => u.GetProperty("userName").GetString()));
        var (single, singleBody) = await SendAsync(server, HttpMethod.Post, "/api/chat-conversations:open-single", $$"""{"peerUserId":"{{ops}}"}""", contoso);
        Assert.Equal((HttpStatusCode.BadRequest, "CHAT_VALIDATION_021"), (single, singleBody.GetProperty("code").GetString()));
    }
}
