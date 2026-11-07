// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using XiHan.BasicApp.Identity;
using XiHan.BasicApp.Integration.Tests.Infrastructure;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证账号的登录策略、系统内置账号的保护与本人关闭账号
/// </summary>
/// <remarks>
/// 登录策略在登录时执行：不允许多端只留新会话，限了设备数吊销最早的。系统内置账号（超管与租户开通时的管理员）不能被停用、删除、锁定或重置密码，本人也不能改用户名或关闭账号
/// 关闭账号要当前密码，模仿会话不能做，租户所有者要先转移；停用后登录给 <c>IDENTITY_DENIED_018</c>，注销后账号不存在
/// </remarks>
public sealed class UserAccountPolicyTests : IDisposable
{
    private const string Password = "Passw0rd!x";

    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private static async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(BasicAppTestServer server, HttpMethod method, string path, string? body, string token)
    {
        var (response, json) = await server.SendAsync(BasicAppTestServer.Request(method, path, body, token));
        return (response.StatusCode, json);
    }

    private static async Task<string> CreateUserAsync(BasicAppTestServer server, string token, string userName)
    {
        var (status, body) = await SendAsync(server, HttpMethod.Post, "/api/users", JsonSerializer.Serialize(new { userName, displayName = userName, password = Password }), token);
        Assert.True(status == HttpStatusCode.Created, body.ToString());
        return body.GetProperty("id").GetString()!;
    }

    private static async Task<(HttpStatusCode Status, JsonElement Body)> LoginAsync(BasicAppTestServer server, string userName, string password, string? tenantId = null)
    {
        var (response, body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:login", JsonSerializer.Serialize(new { userName, password, tenantId })));
        return (response.StatusCode, body);
    }

    // 访问令牌还在有效期内，会话被吊销之后调用被会话门禁拒绝（403）
    private static async Task<HttpStatusCode> ProbeAsync(BasicAppTestServer server, string token)
        => (await SendAsync(server, HttpMethod.Get, "/api/profile", null, token)).Status;

    private static string FailureCode(JsonElement batch) => Assert.Single(batch.GetProperty("failed").EnumerateArray()).GetProperty("code").GetString()!;

    [Fact]
    public async Task LoginPolicy_IsEnforcedAtSignIn()
    {
        var (server, platform, _) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;
        var id = await CreateUserAsync(server, platform, "kit");

        // 缺省：允许多端、不限设备
        var (read, readBody) = await SendAsync(server, HttpMethod.Get, $"/api/users/{id}:login-policy", null, platform);
        Assert.Equal(HttpStatusCode.OK, read);
        Assert.Equal((true, 0), (readBody.GetProperty("allowMultiLogin").GetBoolean(), readBody.GetProperty("maxLoginDevices").GetInt32()));
        var first = (await LoginAsync(server, "kit", Password)).Body.GetProperty("token").GetProperty("accessToken").GetString()!;
        var second = (await LoginAsync(server, "kit", Password)).Body.GetProperty("token").GetProperty("accessToken").GetString()!;
        Assert.Equal((HttpStatusCode.OK, HttpStatusCode.OK), (await ProbeAsync(server, first), await ProbeAsync(server, second)));

        // 超出范围被校验拒绝
        var (invalid, _) = await SendAsync(server, HttpMethod.Post, $"/api/users/{id}:set-login-policy", """{"allowMultiLogin":true,"maxLoginDevices":100}""", platform);
        Assert.Equal(HttpStatusCode.BadRequest, invalid);

        // 限两台：第三次登录吊销最早的那一个
        var (limited, limitedBody) = await SendAsync(server, HttpMethod.Post, $"/api/users/{id}:set-login-policy", """{"allowMultiLogin":true,"maxLoginDevices":2}""", platform);
        Assert.Equal((HttpStatusCode.OK, 2), (limited, limitedBody.GetProperty("maxLoginDevices").GetInt32()));
        var third = (await LoginAsync(server, "kit", Password)).Body.GetProperty("token").GetProperty("accessToken").GetString()!;
        Assert.Equal(HttpStatusCode.Forbidden, await ProbeAsync(server, first));
        Assert.Equal((HttpStatusCode.OK, HttpStatusCode.OK), (await ProbeAsync(server, second), await ProbeAsync(server, third)));

        // 不允许多端：只留新会话
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, $"/api/users/{id}:set-login-policy", """{"allowMultiLogin":false,"maxLoginDevices":0}""", platform)).Status);
        var fourth = (await LoginAsync(server, "kit", Password)).Body.GetProperty("token").GetProperty("accessToken").GetString()!;
        Assert.Equal((HttpStatusCode.Forbidden, HttpStatusCode.Forbidden), (await ProbeAsync(server, second), await ProbeAsync(server, third)));
        Assert.Equal(HttpStatusCode.OK, await ProbeAsync(server, fourth));
    }

    [Fact]
    public async Task SystemAccounts_CannotBeDisabledDeletedLockedOrReset()
    {
        var (server, platform, tenant) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;
        var ops = await CreateUserAsync(server, platform, "ops");
        var superadmin = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("user");
        Assert.True(superadmin.GetProperty("isSystemAccount").GetBoolean());
        var superadminId = superadmin.GetProperty("id").GetString()!;
        var (_, grantBody) = await SendAsync(server, HttpMethod.Get, "/api/users/" + ops, null, platform);
        Assert.False(grantBody.GetProperty("isSystemAccount").GetBoolean());

        // 租户开通时的管理员同样是系统内置账号；操作者是平台超管，排除「不能对自己操作」
        var (_, admins) = await SendAsync(server, HttpMethod.Post, "/api/users:list", """{"page":{"index":1,"size":50},"filters":[{"field":"homeTenantId","operator":"eq","value":"contoso"}]}""", platform);
        var admin = admins.GetProperty("items").EnumerateArray().Single(static u => u.GetProperty("userName").GetString() == "admin");
        Assert.True(admin.GetProperty("isSystemAccount").GetBoolean());
        var adminId = admin.GetProperty("id").GetString()!;
        foreach (var verb in new[] { "disable", "lock" })
        {
            var (status, body) = await SendAsync(server, HttpMethod.Post, $"/api/users:{verb}", $$"""{"ids":["{{adminId}}"]}""", platform);
            Assert.Equal(HttpStatusCode.OK, status);
            Assert.Equal(IdentityErrors.SystemAccount, FailureCode(body));
        }

        var (deleted, deletedBody) = await SendAsync(server, HttpMethod.Delete, $"/api/users?ids={adminId}", null, platform);
        Assert.Equal((HttpStatusCode.OK, IdentityErrors.SystemAccount), (deleted, FailureCode(deletedBody)));
        var (reset, resetBody) = await SendAsync(server, HttpMethod.Post, $"/api/users/{adminId}:reset-password", """{"newPassword":"Another1!x"}""", platform);
        Assert.Equal((HttpStatusCode.Conflict, IdentityErrors.SystemAccount), (reset, resetBody.GetProperty("code").GetString()));

        // 本人也不能改用户名或关闭账号
        var (renamed, renamedBody) = await SendAsync(server, HttpMethod.Post, "/api/profile:change-username", """{"userName":"root2","password":"SuperAdmin@123"}""", platform);
        Assert.Equal((HttpStatusCode.Conflict, IdentityErrors.SystemAccount), (renamed, renamedBody.GetProperty("code").GetString()));
        var (closed, closedBody) = await SendAsync(server, HttpMethod.Post, "/api/profile:deactivate-account", """{"password":"SuperAdmin@123"}""", platform);
        Assert.Equal((HttpStatusCode.Conflict, IdentityErrors.SystemAccount), (closed, closedBody.GetProperty("code").GetString()));
        Assert.Equal(HttpStatusCode.OK, await ProbeAsync(server, platform));
        Assert.Equal(HttpStatusCode.OK, await ProbeAsync(server, tenant));

        // 普通账号不受影响
        var (locked, lockedBody) = await SendAsync(server, HttpMethod.Post, "/api/users:lock", $$"""{"ids":["{{ops}}"]}""", platform);
        Assert.Equal((HttpStatusCode.OK, ops), (locked, Assert.Single(lockedBody.GetProperty("succeeded").EnumerateArray()).GetString()));
        Assert.NotEqual(superadminId, ops);
    }

    [Fact]
    public async Task ClosingOwnAccount_NeedsThePassword_AndTakesEffect()
    {
        var (server, platform, _) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;
        await CreateUserAsync(server, platform, "dora");
        await CreateUserAsync(server, platform, "emil");
        var dora = (await LoginAsync(server, "dora", Password)).Body.GetProperty("token").GetProperty("accessToken").GetString()!;
        var emil = (await LoginAsync(server, "emil", Password)).Body.GetProperty("token").GetProperty("accessToken").GetString()!;

        // 密码不对：不动
        var (wrong, wrongBody) = await SendAsync(server, HttpMethod.Post, "/api/profile:deactivate-account", """{"password":"not-the-password"}""", dora);
        Assert.Equal(IdentityErrors.InvalidCredentials, wrongBody.GetProperty("code").GetString());
        Assert.NotEqual(HttpStatusCode.OK, wrong);
        Assert.Equal(HttpStatusCode.OK, await ProbeAsync(server, dora));

        // 停用：会话下线，登录被拒，管理员可以重新启用
        var (deactivated, deactivatedBody) = await SendAsync(server, HttpMethod.Post, "/api/profile:deactivate-account", JsonSerializer.Serialize(new { password = Password }), dora);
        Assert.True(deactivated == HttpStatusCode.OK, deactivatedBody.ToString());
        var doraId = deactivatedBody.GetProperty("userId").GetString()!;
        Assert.Equal(HttpStatusCode.Forbidden, await ProbeAsync(server, dora));
        var (blocked, blockedBody) = await LoginAsync(server, "dora", Password);
        Assert.Equal(IdentityErrors.AccountDisabled, blockedBody.GetProperty("code").GetString());
        Assert.NotEqual(HttpStatusCode.OK, blocked);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, "/api/users:enable", $$"""{"ids":["{{doraId}}"]}""", platform)).Status);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(server, "dora", Password)).Status);

        // 注销：账号不存在了
        var (deleted, deletedBody) = await SendAsync(server, HttpMethod.Post, "/api/profile:delete-account", JsonSerializer.Serialize(new { password = Password }), emil);
        Assert.True(deleted == HttpStatusCode.OK, deletedBody.ToString());
        Assert.Equal(HttpStatusCode.Forbidden, await ProbeAsync(server, emil));
        var (gone, goneBody) = await LoginAsync(server, "emil", Password);
        Assert.Equal(IdentityErrors.InvalidCredentials, goneBody.GetProperty("code").GetString());
        Assert.NotEqual(HttpStatusCode.OK, gone);
    }

    [Fact]
    public async Task FailedSignIn_ShowsInTheProfile()
    {
        var (server, platform, _) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;
        await CreateUserAsync(server, platform, "finn");
        var token = (await LoginAsync(server, "finn", Password)).Body.GetProperty("token").GetProperty("accessToken").GetString()!;
        Assert.Equal(JsonValueKind.Null, (await SendAsync(server, HttpMethod.Get, "/api/profile", null, token)).Body.GetProperty("lastFailedLoginAt").ValueKind);
        await LoginAsync(server, "finn", "not-the-password");
        Assert.Equal(JsonValueKind.String, (await SendAsync(server, HttpMethod.Get, "/api/profile", null, token)).Body.GetProperty("lastFailedLoginAt").ValueKind);
    }
}
