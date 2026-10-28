// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.Framework.Security.Abstractions.Permissions;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 用户管理操作：修改、批量锁定 / 解锁（逐项结果）、重置密码、批量删除；不能对自己操作；租户主体只能管本租户。
/// </summary>
public sealed class UsersManagementTests : IDisposable
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
    public async Task Update_Lock_Unlock_ResetPassword_Delete_AsPlatformAdmin()
    {
        await using var server = await StartAsync();
        var adminLogin = await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123");
        var admin = adminLogin.GetProperty("accessToken").GetString()!;
        var adminId = adminLogin.GetProperty("user").GetProperty("id").GetString()!;
        var (created, createdBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users", """{"userName":"bob","displayName":"Bob","password":"Bob@123456","homeTenantId":"acme"}""", admin));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var bobId = createdBody.GetProperty("id").GetString()!;
        var bob = (await AuthFlowTests.LoginAsync(server, "bob", "Bob@123456", "acme")).GetProperty("accessToken").GetString()!;

        // update：PUT /users/{id}
        var (updated, updatedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/users/{bobId}", """{"displayName":"Bob W","email":"bob@example.com","phoneNumber":"+86 13800000000"}""", admin));
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal("Bob W", updatedBody.GetProperty("displayName").GetString());
        Assert.Equal("+86 13800000000", updatedBody.GetProperty("phoneNumber").GetString());
        var (missing, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/users/{Guid.NewGuid()}", """{"displayName":"Nobody"}""", admin));
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);

        // lock：逐项结果——bob 成功，自己是 CONFLICT_002，不存在的是 NOTFOUND_001
        var unknownId = Guid.NewGuid();
        var (locked, lockedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users:lock", $$"""{"ids":["{{bobId}}","{{adminId}}","{{unknownId}}"]}""", admin));
        Assert.Equal(HttpStatusCode.OK, locked.StatusCode);
        Assert.Equal([bobId], lockedBody.GetProperty("succeeded").EnumerateArray().Select(static e => e.GetString()).ToList());
        var failed = lockedBody.GetProperty("failed");
        Assert.Equal(2, failed.GetArrayLength());
        Assert.Equal(adminId, failed[0].GetProperty("id").GetString());
        Assert.Equal("IDENTITY_CONFLICT_002", failed[0].GetProperty("code").GetString());
        Assert.Equal("IDENTITY_NOTFOUND_001", failed[1].GetProperty("code").GetString());

        // 锁定即吊销会话、拒绝登录
        var (revoked, revokedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:me", token: bob));
        Assert.Equal(HttpStatusCode.Forbidden, revoked.StatusCode);
        Assert.Equal("IDENTITY_DENIED_001", revokedBody.GetProperty("code").GetString());
        var (lockedLogin, lockedLoginBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:login", """{"userName":"bob","password":"Bob@123456","tenant":"acme"}""", acceptLanguage: "en-US"));
        Assert.Equal(HttpStatusCode.Forbidden, lockedLogin.StatusCode);
        Assert.Equal("IDENTITY_DENIED_004", lockedLoginBody.GetProperty("code").GetString());
        Assert.Equal("The account is not available (status: Locked).", lockedLoginBody.GetProperty("detail").GetString());

        // unlock：已经是 Active 的也算成功（幂等）
        for (var round = 0; round < 2; round++)
        {
            var (unlocked, unlockedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users:unlock", $$"""{"ids":["{{bobId}}"]}""", admin));
            Assert.Equal(HttpStatusCode.OK, unlocked.StatusCode);
            Assert.Equal([bobId], unlockedBody.GetProperty("succeeded").EnumerateArray().Select(static e => e.GetString()).ToList());
            Assert.Equal(0, unlockedBody.GetProperty("failed").GetArrayLength());
        }

        bob = (await AuthFlowTests.LoginAsync(server, "bob", "Bob@123456", "acme")).GetProperty("accessToken").GetString()!;

        // reset-password：POST /users/{id}:reset-password；吊销会话，下次登录须改密
        var (reset, resetBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/users/{bobId}:reset-password", """{"newPassword":"Bob@999999"}""", admin));
        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);
        Assert.True(resetBody.GetProperty("sessionsRevoked").GetBoolean());
        var (afterReset, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:me", token: bob));
        Assert.Equal(HttpStatusCode.Forbidden, afterReset.StatusCode);
        var (oldPassword, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:login", """{"userName":"bob","password":"Bob@123456","tenant":"acme"}"""));
        Assert.Equal(HttpStatusCode.Forbidden, oldPassword.StatusCode);
        var relogin = await AuthFlowTests.LoginAsync(server, "bob", "Bob@999999", "acme");
        Assert.True(relogin.GetProperty("mustChangePassword").GetBoolean());

        // 集合入参、1 到 100 个：空集合在 Validate 阶段拒绝
        var (empty, emptyBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users:lock", """{"ids":[]}""", admin));
        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);
        Assert.Equal("KERNEL_VALIDATION_003", emptyBody.GetProperty("code").GetString());

        // delete：请求只有标量集合，走 DELETE /users?ids=...；不能删自己；删后按 ID 取是 404、不能再登录
        var (deleted, deletedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Delete, $"/api/users?ids={bobId}&ids={adminId}", token: admin));
        Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);
        Assert.Equal([bobId], deletedBody.GetProperty("succeeded").EnumerateArray().Select(static e => e.GetString()).ToList());
        Assert.Equal("IDENTITY_CONFLICT_002", deletedBody.GetProperty("failed")[0].GetProperty("code").GetString());
        var (gone, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/users/{bobId}", token: admin));
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
        var (deletedLogin, deletedLoginBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:login", """{"userName":"bob","password":"Bob@999999","tenant":"acme"}"""));
        Assert.Equal(HttpStatusCode.Forbidden, deletedLogin.StatusCode);
        Assert.Equal("IDENTITY_DENIED_003", deletedLoginBody.GetProperty("code").GetString());
    }

    [Fact]
    public async Task TenantAdmin_OnlyManagesItsOwnTenant()
    {
        await using var server = await StartAsync();
        await server.SeedAsync([], [new PermissionGrant(GrantSubject.ForRole("admin"), "identity.users.update"), new PermissionGrant(GrantSubject.ForRole("admin"), "identity.users.lock")]);
        var acme = TestTokens.Issue("alice", "acme", "admin");
        var globex = TestTokens.Issue("carol", "globex", "admin");
        var (created, createdBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users", """{"userName":"dave","displayName":"Dave"}""", globex));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var daveId = createdBody.GetProperty("id").GetString()!;

        // 别的租户的用户：修改是 404，批量操作逐项 NOTFOUND
        var (foreign, foreignBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/users/{daveId}", """{"displayName":"Dave X"}""", acme));
        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        Assert.Equal("IDENTITY_NOTFOUND_001", foreignBody.GetProperty("code").GetString());
        var (foreignLock, foreignLockBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users:lock", $$"""{"ids":["{{daveId}}"]}""", acme));
        Assert.Equal(HttpStatusCode.OK, foreignLock.StatusCode);
        Assert.Equal(0, foreignLockBody.GetProperty("succeeded").GetArrayLength());
        Assert.Equal("IDENTITY_NOTFOUND_001", foreignLockBody.GetProperty("failed")[0].GetProperty("code").GetString());

        // 本租户正常
        var (own, ownBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/users/{daveId}", """{"displayName":"Dave X"}""", globex));
        Assert.Equal(HttpStatusCode.OK, own.StatusCode);
        Assert.Equal("Dave X", ownBody.GetProperty("displayName").GetString());

        // 没有授予的动作仍由后端拒绝：viewer 不能修改
        var (viewer, viewerBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/users/{daveId}", """{"displayName":"Dave Y"}""", TestTokens.Issue("eve", "globex", "viewer")));
        Assert.Equal(HttpStatusCode.Forbidden, viewer.StatusCode);
        Assert.Equal("SECURITY_DENIED_004", viewerBody.GetProperty("code").GetString());
    }
}
