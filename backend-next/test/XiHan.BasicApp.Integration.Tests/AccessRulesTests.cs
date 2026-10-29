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
/// 验证授予规则的拒绝、有效期与用户级数据范围覆盖。
/// </summary>
/// <remarks>
/// 角色 Deny 只拿走本角色的授予、用户 Deny 压过一切、撤销连同规则解除；授予与指派的有效期在判定时按当前时间比较（到点即失效，不必重建缓存、不等令牌刷新）；
/// 用户级数据范围覆盖完全取代角色的数据范围、只在租户侧；这些变更都进权限变更日志。
/// </remarks>
public sealed class AccessRulesTests : IDisposable
{
    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private static async Task<List<JsonElement>> ChangesAsync(BasicAppTestServer server, string token)
    {
        await server.Services.GetRequiredService<DatabaseAuditTrail>().FlushAsync(CancellationToken.None);
        await server.Services.GetRequiredService<AuditLogWriter>().FlushAsync(CancellationToken.None);
        var (response, json) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/permission-change-logs:list", """{"page":{"index":1,"size":200}}""", token));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return json.GetProperty("items").EnumerateArray().ToList();
    }

    [Fact]
    public async Task Denies_Windows_AndAssignmentExpiry_NarrowTheGrantsAtOnce()
    {
        var clock = new ManualClock(DateTimeOffset.UtcNow);
        await using var server = await BasicAppTestServer.StartAsync(_database.ConnectionString, configure: builder => builder.Services.AddSingleton<TimeProvider>(clock));
        await server.SeedDefaultsAsync();
        await server.RunSeedersAsync();
        var admin = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;
        var (createdUser, createdUserBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users", """{"userName":"dana","displayName":"Dana","password":"Dana@123456"}""", admin));
        Assert.Equal(HttpStatusCode.Created, createdUser.StatusCode);
        var danaId = createdUserBody.GetProperty("id").GetString()!;
        var (_, roleBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/roles", """{"code":"viewer","name":"Viewer"}""", admin));
        var roleId = roleBody.GetProperty("id").GetString()!;
        await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/roles/{roleId}:grant", """{"permissions":["identity.users.read"]}""", admin));
        var (assigned, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/roles/{roleId}:assign", $$"""{"userIds":["{{danaId}}"]}""", admin));
        Assert.Equal(HttpStatusCode.OK, assigned.StatusCode);
        var dana = (await AuthFlowTests.LoginAsync(server, "dana", "Dana@123456")).GetProperty("accessToken").GetString()!;
        async Task<HttpStatusCode> ListUsersAsync() => (await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users:list", "{}", dana))).Response.StatusCode;
        Assert.Equal(HttpStatusCode.OK, await ListUsersAsync());

        // 角色 Deny：撤掉同码授予、写拒绝，立即生效；详情列出规则；撤销即解除拒绝（算成功），但也就没有授予了
        var (denied, deniedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/roles/{roleId}:deny", """{"permissions":["identity.users.read"],"reason":"audit freeze"}""", admin));
        Assert.Equal(HttpStatusCode.OK, denied.StatusCode);
        Assert.Equal(["identity.users.read"], deniedBody.GetProperty("succeeded").EnumerateArray().Select(static p => p.GetString()));
        Assert.Equal(HttpStatusCode.Forbidden, await ListUsersAsync());
        var (_, detailBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/roles/{roleId}", token: admin));
        Assert.Empty(detailBody.GetProperty("permissions").EnumerateArray());
        var rule = Assert.Single(detailBody.GetProperty("rules").EnumerateArray());
        Assert.Equal("Deny", rule.GetProperty("action").GetString());
        Assert.Equal("audit freeze", rule.GetProperty("reason").GetString());
        var (revoked, revokedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/roles/{roleId}:revoke", """{"permissions":["identity.users.read"]}""", admin));
        Assert.Equal(HttpStatusCode.OK, revoked.StatusCode);
        Assert.Equal(1, revokedBody.GetProperty("succeeded").GetArrayLength());
        Assert.Equal(HttpStatusCode.Forbidden, await ListUsersAsync());
        await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/roles/{roleId}:grant", """{"permissions":["identity.users.read"]}""", admin));
        Assert.Equal(HttpStatusCode.OK, await ListUsersAsync());

        // 用户 Deny 压过角色授予；撤销后恢复
        var (userDenied, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/user-grants:deny", $$"""{"userId":"{{danaId}}","permissions":["identity.users.read"]}""", admin));
        Assert.Equal(HttpStatusCode.OK, userDenied.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, await ListUsersAsync());
        var (_, grantsBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/user-grants?userId={danaId}", token: admin));
        Assert.Equal("Deny", Assert.Single(grantsBody.GetProperty("grants").EnumerateArray()).GetProperty("action").GetString());
        await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/user-grants:revoke", $$"""{"userId":"{{danaId}}","permissions":["identity.users.read"]}""", admin));
        Assert.Equal(HttpStatusCode.OK, await ListUsersAsync());

        // 带有效期的直接授予：到期即失效（判定按当前时间，不重建缓存）；到期不晚于生效即拒绝
        var (badWindow, badWindowBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/user-grants:grant", $$"""{"userId":"{{danaId}}","permissions":["identity.users.update"],"effectiveAt":"{{clock.GetUtcNow().AddHours(2):O}}","expiresAt":"{{clock.GetUtcNow().AddHours(1):O}}"}""", admin));
        Assert.Equal(HttpStatusCode.BadRequest, badWindow.StatusCode);
        Assert.Equal("ACCESS_VALIDATION_011", badWindowBody.GetProperty("code").GetString());
        var (windowed, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/user-grants:grant", $$"""{"userId":"{{danaId}}","permissions":["identity.users.update"],"expiresAt":"{{clock.GetUtcNow().AddHours(1):O}}"}""", admin));
        Assert.Equal(HttpStatusCode.OK, windowed.StatusCode);
        async Task<bool> HoldsAsync(string permission)
        {
            var (_, body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:permissions", token: dana));
            return body.GetProperty("permissions").EnumerateArray().Any(p => p.GetString() == permission);
        }

        Assert.True(await HoldsAsync("identity.users.update"));

        // 指派到期：令牌里的角色声明还在，判定立即把它拿掉
        var (window, windowBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/roles/{roleId}:update-member", $$"""{"userId":"{{danaId}}","expiresAt":"{{clock.GetUtcNow().AddMinutes(30):O}}"}""", admin));
        Assert.Equal(HttpStatusCode.OK, window.StatusCode);
        Assert.Equal(JsonValueKind.String, windowBody.GetProperty("expiresAt").ValueKind);
        Assert.Equal(HttpStatusCode.OK, await ListUsersAsync());
        clock.Set(clock.GetUtcNow().AddHours(2));
        Assert.Equal(HttpStatusCode.Forbidden, await ListUsersAsync());
        Assert.False(await HoldsAsync("identity.users.update"));
        var (_, permissionsBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:permissions", token: dana));
        Assert.Empty(permissionsBody.GetProperty("roles").EnumerateArray());

        var changes = (await ChangesAsync(server, admin)).Select(static c => c.GetProperty("changeType").GetString()).ToList();
        Assert.Contains("RoleDeny", changes);
        Assert.Contains("UserDeny", changes);
        Assert.Contains("UserRevoke", changes);
        Assert.Contains("UserGrant", changes);
        Assert.Contains("RoleAssignmentWindow", changes);
    }

    [Fact]
    public async Task UserDataScopeOverride_ReplacesTheRoleScopes_OnTheTenantSideOnly()
    {
        var (server, platform, tenant) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;
        var (created, createdBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users", """{"userName":"erin","displayName":"Erin","password":"Erin@123456"}""", tenant));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var erinId = createdBody.GetProperty("id").GetString()!;
        var (granted, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/user-grants:grant", $$"""{"userId":"{{erinId}}","permissions":["identity.users.read"]}""", tenant));
        Assert.Equal(HttpStatusCode.OK, granted.StatusCode);
        var erin = (await AuthFlowTests.LoginAsync(server, "erin", "Erin@123456", "contoso")).GetProperty("accessToken").GetString()!;
        async Task<List<string?>> VisibleAsync()
        {
            var (_, body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users:list", "{}", erin));
            return body.GetProperty("items").EnumerateArray().Select(static u => u.GetProperty("userName").GetString()).Order(StringComparer.Ordinal).ToList();
        }

        // 没有角色也没有覆盖：不受限；覆盖为只看自己即只剩自己；清除后恢复
        Assert.Equal(["admin", "erin"], await VisibleAsync());
        var (set, setBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/user-data-scopes:set", $$"""{"userId":"{{erinId}}","dataScope":"SelfOnly"}""", tenant));
        Assert.Equal(HttpStatusCode.OK, set.StatusCode);
        Assert.Equal("SelfOnly", setBody.GetProperty("dataScope").GetString());
        Assert.Equal(["erin"], await VisibleAsync());
        var (_, readBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/user-data-scopes?userId={erinId}", token: tenant));
        Assert.Equal("SelfOnly", readBody.GetProperty("dataScope").GetString());
        var (cleared, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/user-data-scopes:set", $$"""{"userId":"{{erinId}}"}""", tenant));
        Assert.Equal(HttpStatusCode.OK, cleared.StatusCode);
        Assert.Equal(["admin", "erin"], await VisibleAsync());

        // 自定义范围要列出部门；平台侧不设数据范围
        var (empty, emptyBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/user-data-scopes:set", $$"""{"userId":"{{erinId}}","dataScope":"Custom"}""", tenant));
        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);
        Assert.Equal("ACCESS_VALIDATION_013", emptyBody.GetProperty("code").GetString());
        var (platformSide, platformSideBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/user-data-scopes?userId={erinId}", token: platform));
        Assert.NotEqual(HttpStatusCode.OK, platformSide.StatusCode);
        Assert.NotEqual("ACCESS_VALIDATION_013", platformSideBody.GetProperty("code").GetString());

        var changes = (await ChangesAsync(server, tenant)).Select(static c => c.GetProperty("changeType").GetString()).ToList();
        Assert.Equal(2, changes.Count(static c => c == "UserDataScope"));
    }
}
