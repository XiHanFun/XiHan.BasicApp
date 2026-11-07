// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Auditing.Domain.Services;
using XiHan.BasicApp.Identity;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.BasicApp.Runtime.Persistence;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证管理员的用户资料字段、用户列表的列与用户详情
/// </summary>
/// <remarks>
/// 真实姓名、性别、生日、国家与备注管理员能写，备注与本人的个人简介是同一列；列表带角色、主部门、锁定与双因素、最后登录 IP；
/// <c>users.detail</c> 给验证状态、双因素、失败次数、今日统计与当前会话，统计与会话只算当前一侧，外部成员没有账号安全一节，字段安全同样套用
/// </remarks>
public sealed class UserManagementDetailTests : IDisposable
{
    private const string Password = "Passw0rd!x";
    private const string ClientIpHeader = "X-Test-Client-Ip";

    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private static async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(BasicAppTestServer server, HttpMethod method, string path, string? body, string token)
    {
        var (response, json) = await server.SendAsync(BasicAppTestServer.Request(method, path, body, token));
        return (response.StatusCode, json);
    }

    private static async Task FlushLogsAsync(BasicAppTestServer server)
        => await server.Services.GetRequiredService<AuditLogWriter>().FlushAsync(CancellationToken.None);

    [Fact]
    public async Task ProfileFields_AreWrittenByAdmins_AndRemarkIsTheOwnerBio()
    {
        var (server, _, tenant) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;
        var birthday = new DateTimeOffset(1990, 5, 1, 0, 0, 0, TimeSpan.Zero);
        var (created, createdBody) = await SendAsync(server, HttpMethod.Post, "/api/users", JsonSerializer.Serialize(new { userName = "erin", displayName = "Erin", password = Password, realName = " 赵艾琳 ", gender = "Female", birthday, country = "CN", remark = "财务" }), tenant);
        Assert.True(created == HttpStatusCode.Created, createdBody.ToString());
        Assert.Equal(("赵艾琳", "Female", "CN", "财务"), (createdBody.GetProperty("realName").GetString(), createdBody.GetProperty("gender").GetString(), createdBody.GetProperty("country").GetString(), createdBody.GetProperty("remark").GetString()));
        Assert.Equal(birthday, createdBody.GetProperty("birthday").GetDateTimeOffset());
        var id = createdBody.GetProperty("id").GetString()!;

        // 不填性别即未设置
        var (_, plain) = await SendAsync(server, HttpMethod.Post, "/api/users", """{"userName":"finn","displayName":"Finn"}""", tenant);
        Assert.Equal("Unknown", plain.GetProperty("gender").GetString());

        // 修改是整体覆盖：空白当作没填
        var (updated, updatedBody) = await SendAsync(server, HttpMethod.Put, $"/api/users/{id}", """{"displayName":"Erin Z","realName":"赵","gender":"Male","country":"  ","remark":"换了部门"}""", tenant);
        Assert.True(updated == HttpStatusCode.OK, updatedBody.ToString());
        Assert.Equal(("赵", "Male", "换了部门"), (updatedBody.GetProperty("realName").GetString(), updatedBody.GetProperty("gender").GetString(), updatedBody.GetProperty("remark").GetString()));
        Assert.Equal(JsonValueKind.Null, updatedBody.GetProperty("country").ValueKind);
        Assert.Equal(JsonValueKind.Null, updatedBody.GetProperty("birthday").ValueKind);

        // 超长被校验拒绝
        var (tooLong, _) = await SendAsync(server, HttpMethod.Put, $"/api/users/{id}", JsonSerializer.Serialize(new { displayName = "Erin", country = new string('x', 51) }), tenant);
        Assert.Equal(HttpStatusCode.BadRequest, tooLong);

        // 备注与本人的个人简介是同一列
        var own = (await AuthFlowTests.LoginAsync(server, "erin", Password, "contoso")).GetProperty("accessToken").GetString()!;
        var (_, profile) = await SendAsync(server, HttpMethod.Get, "/api/profile", null, own);
        Assert.Equal("换了部门", profile.GetProperty("user").GetProperty("remark").GetString());
        var (_, changed) = await SendAsync(server, HttpMethod.Put, "/api/profile", """{"displayName":"Erin Z","gender":"Male","remark":"本人写的简介"}""", own);
        Assert.Equal("本人写的简介", changed.GetProperty("user").GetProperty("remark").GetString());
        var (_, seen) = await SendAsync(server, HttpMethod.Get, $"/api/users/{id}", null, tenant);
        Assert.Equal("本人写的简介", seen.GetProperty("remark").GetString());
    }

    [Fact]
    public async Task List_CarriesRolesDepartmentSecurityFlagsAndLastLoginIp()
    {
        // 测试服务器没有远端地址：用例按请求头给出来源地址
        var (server, platform, tenant) = await SettingsTestHost.StartAsync(_database.ConnectionString, configureApp: static app => app.Use(async (http, next) =>
        {
            if (http.Request.Headers.TryGetValue(ClientIpHeader, out var ip))
            {
                http.Connection.RemoteIpAddress = IPAddress.Parse(ip.ToString());
            }

            await next();
        }));
        await using var running = server;
        var (_, erin) = await SendAsync(server, HttpMethod.Post, "/api/users", $$"""{"userName":"erin","displayName":"Erin","password":"{{Password}}","gender":"Female","country":"CN"}""", tenant);
        var erinId = erin.GetProperty("id").GetString()!;
        var (_, finn) = await SendAsync(server, HttpMethod.Post, "/api/users", """{"userName":"finn","displayName":"Finn"}""", tenant);
        var finnId = finn.GetProperty("id").GetString()!;

        // 两个角色，一个停用的不算；主部门优先于最早加入的
        var (_, reader) = await SendAsync(server, HttpMethod.Post, "/api/roles", """{"code":"reader","name":"Reader","order":1}""", tenant);
        var (_, auditor) = await SendAsync(server, HttpMethod.Post, "/api/roles", """{"code":"auditor","name":"Auditor","order":2}""", tenant);
        var (_, retired) = await SendAsync(server, HttpMethod.Post, "/api/roles", """{"code":"retired","name":"Retired","order":0}""", tenant);
        foreach (var role in new[] { reader, auditor, retired })
        {
            Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, $"/api/roles/{role.GetProperty("id").GetString()}:assign", $$"""{"userIds":["{{erinId}}"]}""", tenant)).Status);
        }

        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Put, $"/api/roles/{retired.GetProperty("id").GetString()}", """{"name":"Retired","order":0,"status":"Disabled"}""", tenant)).Status);
        var (_, hq) = await SendAsync(server, HttpMethod.Post, "/api/departments", """{"code":"hq","name":"HQ"}""", tenant);
        var (_, rd) = await SendAsync(server, HttpMethod.Post, "/api/departments", """{"code":"rd","name":"R&D"}""", tenant);
        await SendAsync(server, HttpMethod.Post, "/api/memberships", $$"""{"userId":"{{erinId}}","departmentId":"{{hq.GetProperty("id").GetString()}}"}""", tenant);
        await SendAsync(server, HttpMethod.Post, "/api/memberships", $$"""{"userId":"{{erinId}}","departmentId":"{{rd.GetProperty("id").GetString()}}","isPrimary":true}""", tenant);
        await SendAsync(server, HttpMethod.Post, "/api/users:lock", $$"""{"ids":["{{finnId}}"]}""", tenant);
        var login = BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:login", JsonSerializer.Serialize(new { userName = "erin", password = Password, tenant = "contoso" }));
        login.Headers.Add(ClientIpHeader, "203.0.113.7");
        Assert.Equal(HttpStatusCode.OK, (await server.SendAsync(login)).Response.StatusCode);

        var (status, page) = await SendAsync(server, HttpMethod.Post, "/api/users:list", """{"page":{"index":1,"size":50}}""", tenant);
        Assert.True(status == HttpStatusCode.OK, page.ToString());
        var rows = page.GetProperty("items").EnumerateArray().ToDictionary(static u => u.GetProperty("userName").GetString()!);
        var row = rows["erin"];
        Assert.Equal(["Reader", "Auditor"], row.GetProperty("roleNames").EnumerateArray().Select(static r => r.GetString()));
        Assert.Equal("R&D", row.GetProperty("departmentName").GetString());
        Assert.Equal(("Female", "CN"), (row.GetProperty("gender").GetString(), row.GetProperty("country").GetString()));
        Assert.False(row.GetProperty("isLocked").GetBoolean());
        Assert.False(row.GetProperty("twoFactorEnabled").GetBoolean());
        Assert.Equal(JsonValueKind.String, row.GetProperty("lastLoginAt").ValueKind);
        Assert.Equal("203.0.113.7", row.GetProperty("lastLoginIp").GetString());
        Assert.True(rows["finn"].GetProperty("isLocked").GetBoolean());
        Assert.Empty(rows["finn"].GetProperty("roleNames").EnumerateArray());
        Assert.Equal(JsonValueKind.Null, rows["finn"].GetProperty("departmentName").ValueKind);

        // 平台一侧没有部门；租户的角色不算平台的
        var (_, platformPage) = await SendAsync(server, HttpMethod.Post, "/api/users:list", """{"page":{"index":1,"size":50},"filters":[{"field":"homeTenantId","operator":"eq","value":"contoso"}]}""", platform);
        var platformRow = platformPage.GetProperty("items").EnumerateArray().Single(static u => u.GetProperty("userName").GetString() == "erin");
        Assert.Empty(platformRow.GetProperty("roleNames").EnumerateArray());
        Assert.Equal(JsonValueKind.Null, platformRow.GetProperty("departmentName").ValueKind);
    }

    [Fact]
    public async Task Detail_GivesSecurityActivityAndCurrentSession_ScopedToTheSide()
    {
        var (server, platform, tenant) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;
        var (_, erin) = await SendAsync(server, HttpMethod.Post, "/api/users", $$"""{"userName":"erin","displayName":"Erin","password":"{{Password}}"}""", tenant);
        var erinId = erin.GetProperty("id").GetString()!;

        // 没登录过：没有统计，也没有会话
        var (quiet, quietBody) = await SendAsync(server, HttpMethod.Get, $"/api/users/{erinId}:detail?timeZone=Asia/Shanghai", null, tenant);
        Assert.True(quiet == HttpStatusCode.OK, quietBody.ToString());
        Assert.Equal(0, quietBody.GetProperty("today").GetProperty("logins").GetInt32());
        Assert.Equal(JsonValueKind.Null, quietBody.GetProperty("currentSession").ValueKind);
        Assert.Equal("erin", quietBody.GetProperty("user").GetProperty("userName").GetString());
        Assert.False(quietBody.GetProperty("user").GetProperty("isExternalMember").GetBoolean());

        // 错一次密码、登录两次（第二次之后失败计数清零）、发几个请求
        var (wrong, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:login", JsonSerializer.Serialize(new { userName = "erin", password = "wrong-password", tenant = "contoso" })));
        Assert.Equal(HttpStatusCode.Forbidden, wrong.StatusCode);
        var (_, failed) = await SendAsync(server, HttpMethod.Get, $"/api/users/{erinId}:detail", null, tenant);
        Assert.Equal(1, failed.GetProperty("security").GetProperty("failedAccessCount").GetInt32());
        var first = (await AuthFlowTests.LoginAsync(server, "erin", Password, "contoso")).GetProperty("accessToken").GetString()!;
        await AuthFlowTests.LoginAsync(server, "erin", Password, "contoso");
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Get, "/api/profile", null, first)).Status);
        await FlushLogsAsync(server);

        var (ok, detail) = await SendAsync(server, HttpMethod.Get, $"/api/users/{erinId}:detail?timeZone=Asia/Shanghai", null, tenant);
        Assert.True(ok == HttpStatusCode.OK, detail.ToString());
        var security = detail.GetProperty("security");
        Assert.Equal((false, false, false, false, 0), (security.GetProperty("emailVerified").GetBoolean(), security.GetProperty("phoneVerified").GetBoolean(), security.GetProperty("twoFactorEnabled").GetBoolean(), security.GetProperty("isLocked").GetBoolean(), security.GetProperty("failedAccessCount").GetInt32()));
        Assert.Empty(security.GetProperty("twoFactorMethods").EnumerateArray());
        Assert.Equal(2, detail.GetProperty("today").GetProperty("logins").GetInt32());
        Assert.True(detail.GetProperty("today").GetProperty("accesses").GetInt32() > 0);
        Assert.True(detail.GetProperty("today").GetProperty("onlineSeconds").GetInt64() >= 0);
        var session = detail.GetProperty("currentSession");
        Assert.Equal(JsonValueKind.String, session.GetProperty("loginAt").ValueKind);
        Assert.Equal(JsonValueKind.String, session.GetProperty("lastActiveAt").ValueKind);

        // 锁定即标记；时区不认得是 400；不存在是 404
        await SendAsync(server, HttpMethod.Post, "/api/users:lock", $$"""{"ids":["{{erinId}}"]}""", tenant);
        var (_, locked) = await SendAsync(server, HttpMethod.Get, $"/api/users/{erinId}:detail", null, tenant);
        Assert.True(locked.GetProperty("security").GetProperty("isLocked").GetBoolean());
        var (badZone, badZoneBody) = await SendAsync(server, HttpMethod.Get, $"/api/users/{erinId}:detail?timeZone=Mars/Olympus", null, tenant);
        Assert.Equal((HttpStatusCode.BadRequest, IdentityErrors.TimeZoneInvalid), (badZone, badZoneBody.GetProperty("code").GetString()));
        var (missing, _) = await SendAsync(server, HttpMethod.Get, $"/api/users/{Guid.NewGuid()}:detail", null, tenant);
        Assert.Equal(HttpStatusCode.NotFound, missing);

        // 平台看租户的用户：账号安全在，日志与会话都在租户那一侧
        var (_, fromPlatform) = await SendAsync(server, HttpMethod.Get, $"/api/users/{erinId}:detail", null, platform);
        Assert.True(fromPlatform.GetProperty("security").GetProperty("isLocked").GetBoolean());
        Assert.Equal(JsonValueKind.Null, fromPlatform.GetProperty("today").ValueKind);
        Assert.Equal(JsonValueKind.Null, fromPlatform.GetProperty("currentSession").ValueKind);
    }

    [Fact]
    public async Task Detail_OfAnExternalMember_LeavesOutTheAccountAndOtherTenants()
    {
        var (server, platform, contoso) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;
        var enterprise = await EditionsTests.EditionIdAsync(server, platform, "enterprise");
        await SendAsync(server, HttpMethod.Post, "/api/tenants", $$"""{"tenantId":"fabrikam","name":"Fabrikam","editionId":"{{enterprise}}"}""", platform);
        var (_, alice) = await SendAsync(server, HttpMethod.Post, "/api/users", $$"""{"userName":"alice","displayName":"Alice","password":"{{Password}}","homeTenantId":"fabrikam"}""", platform);
        var aliceId = alice.GetProperty("id").GetString()!;
        var (_, bob) = await SendAsync(server, HttpMethod.Post, "/api/users", $$"""{"userName":"bob","displayName":"Bob","password":"{{Password}}","homeTenantId":"fabrikam"}""", platform);
        await SendAsync(server, HttpMethod.Post, "/api/tenant-members", $$"""{"tenantId":"contoso","userId":"{{aliceId}}"}""", platform);

        // alice 在注册地登录，在 contoso 这边没有活动也没有会话
        var login = await AuthFlowTests.LoginAsync(server, "alice", Password, "fabrikam");
        await FlushLogsAsync(server);
        var (ok, detail) = await SendAsync(server, HttpMethod.Get, $"/api/users/{aliceId}:detail", null, contoso);
        Assert.True(ok == HttpStatusCode.OK, detail.ToString());
        Assert.True(detail.GetProperty("user").GetProperty("isExternalMember").GetBoolean());
        Assert.Equal(JsonValueKind.Null, detail.GetProperty("security").ValueKind);
        Assert.Equal(0, detail.GetProperty("today").GetProperty("logins").GetInt32());
        Assert.Equal(0L, detail.GetProperty("today").GetProperty("onlineSeconds").GetInt64());
        Assert.Equal(JsonValueKind.Null, detail.GetProperty("currentSession").ValueKind);

        // 切进来就看得到这边的会话
        var (switched, _) = await SendAsync(server, HttpMethod.Post, "/api/auth:switch-tenant", """{"tenantId":"contoso"}""", login.GetProperty("accessToken").GetString()!);
        Assert.Equal(HttpStatusCode.OK, switched);
        var (_, inside) = await SendAsync(server, HttpMethod.Get, $"/api/users/{aliceId}:detail", null, contoso);
        Assert.Equal(JsonValueKind.Object, inside.GetProperty("currentSession").ValueKind);

        // 不是本租户成员的人按不存在回答
        var (hidden, hiddenBody) = await SendAsync(server, HttpMethod.Get, $"/api/users/{bob.GetProperty("id").GetString()}:detail", null, contoso);
        Assert.Equal((HttpStatusCode.NotFound, IdentityErrors.UserNotFound), (hidden, hiddenBody.GetProperty("code").GetString()));
    }

    [Fact]
    public async Task ListAndDetail_RunOnPostgres()
    {
        // 真库：时间列的投影、月表的计数与跨包批量查询（没设 XIHAN_TEST_POSTGRES 即跳过）
        await using var database = await PostgresDatabase.CreateAsync(PostgresDatabase.RequireServer(), "xihan_basicapp_user_detail");
        var (server, _, tenant) = await SettingsTestHost.StartAsync(database.ConnectionString, DatabaseProviders.PostgreSql);
        await using var running = server;
        var birthday = new DateTimeOffset(1990, 5, 1, 0, 0, 0, TimeSpan.Zero);
        var (_, erin) = await SendAsync(server, HttpMethod.Post, "/api/users", JsonSerializer.Serialize(new { userName = "erin", displayName = "Erin", password = Password, birthday, gender = "Female" }), tenant);
        var erinId = erin.GetProperty("id").GetString()!;
        var (_, role) = await SendAsync(server, HttpMethod.Post, "/api/roles", """{"code":"reader","name":"Reader"}""", tenant);
        await SendAsync(server, HttpMethod.Post, $"/api/roles/{role.GetProperty("id").GetString()}:assign", $$"""{"userIds":["{{erinId}}"]}""", tenant);
        var (_, department) = await SendAsync(server, HttpMethod.Post, "/api/departments", """{"code":"hq","name":"HQ"}""", tenant);
        await SendAsync(server, HttpMethod.Post, "/api/memberships", $$"""{"userId":"{{erinId}}","departmentId":"{{department.GetProperty("id").GetString()}}"}""", tenant);
        await AuthFlowTests.LoginAsync(server, "erin", Password, "contoso");
        await FlushLogsAsync(server);

        var (listed, page) = await SendAsync(server, HttpMethod.Post, "/api/users:list", """{"keyword":"erin"}""", tenant);
        Assert.True(listed == HttpStatusCode.OK, page.ToString());
        var row = Assert.Single(page.GetProperty("items").EnumerateArray());
        Assert.Equal(["Reader"], row.GetProperty("roleNames").EnumerateArray().Select(static r => r.GetString()));
        Assert.Equal("HQ", row.GetProperty("departmentName").GetString());
        Assert.Equal(birthday, row.GetProperty("birthday").GetDateTimeOffset());

        var (ok, detail) = await SendAsync(server, HttpMethod.Get, $"/api/users/{erinId}:detail?timeZone=Asia/Shanghai", null, tenant);
        Assert.True(ok == HttpStatusCode.OK, detail.ToString());
        Assert.Equal(1, detail.GetProperty("today").GetProperty("logins").GetInt32());
        Assert.Equal(JsonValueKind.Object, detail.GetProperty("currentSession").ValueKind);
    }

    [Fact]
    public async Task Detail_NeedsTheReadPermission_RespectsDataScope_AndMasksProtectedFields()
    {
        var (server, _, tenant) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;
        var (_, erin) = await SendAsync(server, HttpMethod.Post, "/api/users", $$"""{"userName":"erin","displayName":"Erin","email":"erin@contoso.com","password":"{{Password}}"}""", tenant);
        var erinId = erin.GetProperty("id").GetString()!;
        var (_, finn) = await SendAsync(server, HttpMethod.Post, "/api/users", $$"""{"userName":"finn","displayName":"Finn","password":"{{Password}}"}""", tenant);
        var finnId = finn.GetProperty("id").GetString()!;

        // 没有读用户的权限
        var bare = (await AuthFlowTests.LoginAsync(server, "finn", Password, "contoso")).GetProperty("accessToken").GetString()!;
        var (denied, _) = await SendAsync(server, HttpMethod.Get, $"/api/users/{erinId}:detail", null, bare);
        Assert.Equal(HttpStatusCode.Forbidden, denied);

        // 只有读用户的权限就够；字段安全同样套到详情的资料上
        var (_, role) = await SendAsync(server, HttpMethod.Post, "/api/roles", """{"code":"reader","name":"Reader","dataScope":"All"}""", tenant);
        var roleId = role.GetProperty("id").GetString()!;
        await SendAsync(server, HttpMethod.Post, $"/api/roles/{roleId}:grant", """{"permissions":["identity.users.read"]}""", tenant);
        await SendAsync(server, HttpMethod.Post, $"/api/roles/{roleId}:assign", $$"""{"userIds":["{{finnId}}"]}""", tenant);
        var (ruled, ruledBody) = await SendAsync(server, HttpMethod.Post, "/api/field-security", $$"""{"targetKind":"Role","targetId":"{{roleId}}","entity":"identity.users","field":"email","strategy":"FullMask"}""", tenant);
        Assert.True(ruled == HttpStatusCode.Created, ruledBody.ToString());
        var reader = (await AuthFlowTests.LoginAsync(server, "finn", Password, "contoso")).GetProperty("accessToken").GetString()!;
        var (allowed, detail) = await SendAsync(server, HttpMethod.Get, $"/api/users/{erinId}:detail", null, reader);
        Assert.True(allowed == HttpStatusCode.OK, detail.ToString());
        Assert.Equal(new string('*', "erin@contoso.com".Length), detail.GetProperty("user").GetProperty("email").GetString());

        // 数据范围收窄后，范围外的人按不存在回答
        var (scoped, _) = await SendAsync(server, HttpMethod.Post, "/api/user-data-scopes:set", $$"""{"userId":"{{finnId}}","dataScope":"SelfOnly"}""", tenant);
        Assert.Equal(HttpStatusCode.OK, scoped);
        var (outOfScope, _) = await SendAsync(server, HttpMethod.Get, $"/api/users/{erinId}:detail", null, reader);
        Assert.Equal(HttpStatusCode.NotFound, outOfScope);
        var (self, _) = await SendAsync(server, HttpMethod.Get, $"/api/users/{finnId}:detail", null, reader);
        Assert.Equal(HttpStatusCode.OK, self);
    }
}
