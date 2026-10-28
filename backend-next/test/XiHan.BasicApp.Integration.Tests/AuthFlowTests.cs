// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using XiHan.BasicApp.Integration.Tests.Infrastructure;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 身份链路：密码登录与锁定、访问令牌与会话门禁、刷新令牌轮换、注销、当前主体、租户账号登录与本人改密。
/// </summary>
public sealed class AuthFlowTests : IDisposable
{
    private const string SuperAdminLogin = """{"userName":"superadmin","password":"SuperAdmin@123"}""";

    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private async Task<BasicAppTestServer> StartAsync()
    {
        // 锁定阈值压到 3 次，用例短一点
        var server = await BasicAppTestServer.StartAsync(_database.ConnectionString, settings: [new("BasicApp:Auth:MaxFailedAccessAttempts", "3")]);
        await server.SeedDefaultsAsync();
        await server.RunSeedersAsync();
        return server;
    }

    [Fact]
    public async Task Login_IssuesTokens_AndMe_ReturnsThePrincipal()
    {
        await using var server = await StartAsync();
        var (response, signedIn) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:login", SuperAdminLogin));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // 没开双因素：直接拿到令牌，没有第二步
        Assert.Equal(JsonValueKind.Null, signedIn.GetProperty("twoFactor").ValueKind);
        var body = signedIn.GetProperty("token");
        var access = body.GetProperty("accessToken").GetString()!;
        Assert.True(body.GetProperty("refreshToken").GetString()!.Length >= 32);
        Assert.Equal(JsonValueKind.Null, body.GetProperty("tenantId").ValueKind);
        Assert.False(body.GetProperty("mustChangePassword").GetBoolean());
        Assert.Equal("superadmin", body.GetProperty("user").GetProperty("userName").GetString());
        Assert.Equal("platform", body.GetProperty("user").GetProperty("homeTenantId").GetString());
        Assert.True(body.GetProperty("accessExpiresAt").GetDateTimeOffset() > DateTimeOffset.UtcNow);
        var sessionId = body.GetProperty("sessionId").GetString();

        var (me, meBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:me", token: access));
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        Assert.Equal("superadmin", meBody.GetProperty("user").GetProperty("userName").GetString());
        Assert.Equal(sessionId, meBody.GetProperty("sessionId").GetString());
        Assert.Equal(JsonValueKind.Null, meBody.GetProperty("tenantId").ValueKind);
        Assert.Equal(JsonValueKind.String, meBody.GetProperty("user").GetProperty("lastLoginAt").ValueKind);

        // 超级管理员经种子写入的全局授予 * 调管理操作
        var (list, listBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users:list", "{}", access));
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Equal(1, listBody.GetProperty("total").GetInt64());

        // 自助操作只要求已认证：匿名是未认证
        var (anonymous, anonymousBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:me"));
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.Equal("SECURITY_DENIED_001", anonymousBody.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Login_RejectsWrongPassword_AndLocksOutAfterRepeatedFailures()
    {
        await using var server = await StartAsync();
        const string wrong = """{"userName":"superadmin","password":"nope-12345"}""";
        for (var attempt = 1; attempt <= 2; attempt++)
        {
            var (denied, deniedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:login", wrong, acceptLanguage: "zh-CN"));
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
            Assert.Equal("IDENTITY_DENIED_003", deniedBody.GetProperty("code").GetString());
            Assert.Equal("用户名或密码不正确。", deniedBody.GetProperty("detail").GetString());
        }

        // 第 3 次失败触发锁定：失败计数在独立作用域提交，不随失败的调用回滚
        var (locked, lockedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:login", wrong, acceptLanguage: "en-US"));
        Assert.Equal(HttpStatusCode.Forbidden, locked.StatusCode);
        Assert.Equal("IDENTITY_DENIED_004", lockedBody.GetProperty("code").GetString());
        Assert.StartsWith("Too many failed sign-in attempts", lockedBody.GetProperty("detail").GetString(), StringComparison.Ordinal);

        // 锁定期内正确密码也拒绝
        var (stillLocked, stillLockedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:login", SuperAdminLogin));
        Assert.Equal(HttpStatusCode.Forbidden, stillLocked.StatusCode);
        Assert.Equal("IDENTITY_DENIED_004", stillLockedBody.GetProperty("code").GetString());

        // 不存在的用户与错密码同码，不泄露账号是否存在
        var (unknown, unknownBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:login", """{"userName":"nobody","password":"nope-12345"}"""));
        Assert.Equal(HttpStatusCode.Forbidden, unknown.StatusCode);
        Assert.Equal("IDENTITY_DENIED_003", unknownBody.GetProperty("code").GetString());

        // 请求形状在 Validate 阶段按 Schema 拒绝：缺密码、租户 ID 不合文法
        var (invalid, invalidBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:login", """{"userName":"superadmin","tenant":"bad tenant!"}"""));
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal("KERNEL_VALIDATION_003", invalidBody.GetProperty("code").GetString());
        Assert.True(invalidBody.GetProperty("errors").TryGetProperty("password", out _));
        Assert.True(invalidBody.GetProperty("errors").TryGetProperty("tenant", out _));
    }

    [Fact]
    public async Task Refresh_RotatesTheToken_AndLogout_RevokesTheSession()
    {
        await using var server = await StartAsync();
        var first = await LoginAsync(server, "superadmin", "SuperAdmin@123");
        var refresh = first.GetProperty("refreshToken").GetString()!;

        var (refreshed, refreshedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:refresh", $$"""{"refreshToken":"{{refresh}}"}"""));
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        Assert.Equal(first.GetProperty("sessionId").GetString(), refreshedBody.GetProperty("sessionId").GetString());
        Assert.NotEqual(refresh, refreshedBody.GetProperty("refreshToken").GetString());

        // 旧刷新令牌已轮换失效
        var (stale, staleBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:refresh", $$"""{"refreshToken":"{{refresh}}"}"""));
        Assert.Equal(HttpStatusCode.Forbidden, stale.StatusCode);
        Assert.Equal("IDENTITY_DENIED_001", staleBody.GetProperty("code").GetString());

        var access = refreshedBody.GetProperty("accessToken").GetString()!;
        var (logout, logoutBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:logout", "{}", access));
        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);
        Assert.Equal(first.GetProperty("sessionId").GetString(), logoutBody.GetProperty("sessionId").GetString());

        // 会话已吊销：访问令牌在 Authenticate 阶段被会话门禁拒绝，刷新令牌也不再可用
        var (gone, goneBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:me", token: access));
        Assert.Equal(HttpStatusCode.Forbidden, gone.StatusCode);
        Assert.Equal("IDENTITY_DENIED_001", goneBody.GetProperty("code").GetString());
        var (noRefresh, noRefreshBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:refresh", $$"""{"refreshToken":"{{refreshedBody.GetProperty("refreshToken").GetString()}}"}"""));
        Assert.Equal(HttpStatusCode.Forbidden, noRefresh.StatusCode);
        Assert.Equal("IDENTITY_DENIED_001", noRefreshBody.GetProperty("code").GetString());

        // 不带会话的令牌（外部签发）照常认证，但不能注销
        var (noSession, noSessionBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:logout", "{}", TestTokens.Issue("root", null, "admin")));
        Assert.Equal(HttpStatusCode.BadRequest, noSession.StatusCode);
        Assert.Equal("IDENTITY_VALIDATION_005", noSessionBody.GetProperty("code").GetString());
    }

    [Fact]
    public async Task TenantUser_LogsInWithTenant_AndChangesOwnPassword()
    {
        await using var server = await StartAsync();
        var admin = (await LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;
        var (created, createdBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users", """{"userName":"alice","displayName":"Alice","password":"Alice@12345","homeTenantId":"acme"}""", admin));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal("acme", createdBody.GetProperty("homeTenantId").GetString());

        // 租户账号登录须带租户；不带即按平台账号找，找不到
        var (platformMiss, platformMissBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:login", """{"userName":"alice","password":"Alice@12345"}"""));
        Assert.Equal(HttpStatusCode.Forbidden, platformMiss.StatusCode);
        Assert.Equal("IDENTITY_DENIED_003", platformMissBody.GetProperty("code").GetString());

        var login = await LoginAsync(server, "alice", "Alice@12345", "acme");
        Assert.Equal("acme", login.GetProperty("tenantId").GetString());
        var access = login.GetProperty("accessToken").GetString()!;

        // 租户声明进入主体：me 带租户；没有授予的管理操作被拒绝
        var (me, meBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:me", token: access));
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        Assert.Equal("acme", meBody.GetProperty("tenantId").GetString());
        var (forbidden, forbiddenBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users:list", "{}", access));
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal("SECURITY_DENIED_004", forbiddenBody.GetProperty("code").GetString());

        // 改密：当前密码错 → 拒绝；不含数字 → 策略；与当前相同 → 历史；成功后旧密码不能登录、当前会话保留
        var (wrongCurrent, wrongCurrentBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/profile:change-password", """{"currentPassword":"nope-12345","newPassword":"Alice@67890"}""", access));
        Assert.Equal(HttpStatusCode.Forbidden, wrongCurrent.StatusCode);
        Assert.Equal("IDENTITY_DENIED_003", wrongCurrentBody.GetProperty("code").GetString());
        var (weak, weakBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/profile:change-password", """{"currentPassword":"Alice@12345","newPassword":"onlyletters"}""", access, "zh-CN"));
        Assert.Equal(HttpStatusCode.BadRequest, weak.StatusCode);
        Assert.Equal("IDENTITY_VALIDATION_003", weakBody.GetProperty("code").GetString());
        Assert.Equal("密码须同时包含字母与数字。", weakBody.GetProperty("detail").GetString());
        var (reused, reusedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/profile:change-password", """{"currentPassword":"Alice@12345","newPassword":"Alice@12345"}""", access));
        Assert.Equal(HttpStatusCode.BadRequest, reused.StatusCode);
        Assert.Equal("IDENTITY_VALIDATION_004", reusedBody.GetProperty("code").GetString());
        var (changed, changedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/profile:change-password", """{"currentPassword":"Alice@12345","newPassword":"Alice@67890"}""", access));
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        Assert.Equal(createdBody.GetProperty("id").GetString(), changedBody.GetProperty("userId").GetString());
        var (stillValid, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:me", token: access));
        Assert.Equal(HttpStatusCode.OK, stillValid.StatusCode);
        var (oldPassword, oldPasswordBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:login", """{"userName":"alice","password":"Alice@12345","tenant":"acme"}"""));
        Assert.Equal(HttpStatusCode.Forbidden, oldPassword.StatusCode);
        Assert.Equal("IDENTITY_DENIED_003", oldPasswordBody.GetProperty("code").GetString());
        var relogin = await LoginAsync(server, "alice", "Alice@67890", "acme");
        Assert.Equal("acme", relogin.GetProperty("tenantId").GetString());
    }

    internal static async Task<JsonElement> LoginAsync(BasicAppTestServer server, string userName, string password, string? tenant = null)
    {
        var json = tenant is null
            ? $$"""{"userName":"{{userName}}","password":"{{password}}"}"""
            : $$"""{"userName":"{{userName}}","password":"{{password}}","tenant":"{{tenant}}"}""";
        var (response, body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:login", json));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return body.GetProperty("token");
    }
}
