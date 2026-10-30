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
/// 验证租户所有权转移与运维支持成员。
/// </summary>
/// <remarks>
/// 所有者即租户的管理员（<c>adminUserId</c>）：转移只给注册地用户或生效中的普通成员（启用、未锁定），新所有者拿到管理员角色、原所有者保留，记权限变更日志；
/// 所有者不能被删除、锁定或移出成员。运维支持成员只能是平台账号、不占席位、不进模仿候选，到期即不能进入。
/// </remarks>
public sealed class TenantOwnershipTests : IDisposable
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

    private static async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(BasicAppTestServer server, HttpMethod method, string path, string? body, string token)
    {
        var (response, json) = await server.SendAsync(BasicAppTestServer.Request(method, path, body, token));
        return (response.StatusCode, json);
    }

    private static async Task<string> CreateUserAsync(BasicAppTestServer server, string token, string userName, string? homeTenantId)
    {
        var home = homeTenantId is null ? string.Empty : $",\"homeTenantId\":\"{homeTenantId}\"";
        var (status, body) = await SendAsync(server, HttpMethod.Post, "/api/users", $$"""{"userName":"{{userName}}","displayName":"{{userName}}","password":"Passw0rd@123"{{home}}}""", token);
        Assert.Equal(HttpStatusCode.Created, status);
        return body.GetProperty("id").GetString()!;
    }

    private static string FirstFailure(JsonElement batch) => batch.GetProperty("failed")[0].GetProperty("code").GetString()!;

    [Fact]
    public async Task Ownership_Transfers_AndOwnersAreProtected_SupportMembersStayOutside()
    {
        await using var server = await StartAsync();
        var superadmin = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;

        // contoso：admin 是所有者；bob 注册地 contoso；carol 注册地 demo、以普通成员进 contoso；ops 是平台账号
        var (created, createdBody) = await SendAsync(server, HttpMethod.Post, "/api/tenants", """{"tenantId":"contoso","name":"Contoso"}""", superadmin);
        Assert.Equal(HttpStatusCode.Created, created);
        var contosoId = createdBody.GetProperty("id").GetString()!;
        var (missing, missingBody) = await SendAsync(server, HttpMethod.Post, $"/api/tenants/{contosoId}:transfer-owner", $$"""{"userId":"{{Guid.NewGuid()}}"}""", superadmin);
        Assert.Equal((HttpStatusCode.Conflict, "TENANCY_CONFLICT_010"), (missing, missingBody.GetProperty("code").GetString()));
        var (initialized, initializedBody) = await SendAsync(server, HttpMethod.Post, $"/api/tenants/{contosoId}:initialize-admin", """{"userName":"admin","password":"Admin@123456"}""", superadmin);
        Assert.Equal(HttpStatusCode.OK, initialized);
        var adminId = initializedBody.GetProperty("id").GetString()!;
        var bobId = await CreateUserAsync(server, superadmin, "bob", "contoso");
        var carolId = await CreateUserAsync(server, superadmin, "carol", "demo");
        var opsId = await CreateUserAsync(server, superadmin, "ops", null);

        // 成员类型与账号要对上；到期须晚于生效
        var (supportCarol, supportCarolBody) = await SendAsync(server, HttpMethod.Post, "/api/tenant-members", $$"""{"tenantId":"contoso","userId":"{{carolId}}","memberType":"Support"}""", superadmin);
        Assert.Equal((HttpStatusCode.BadRequest, "TENANCY_VALIDATION_006"), (supportCarol, supportCarolBody.GetProperty("code").GetString()));
        var (badWindow, badWindowBody) = await SendAsync(server, HttpMethod.Post, "/api/tenant-members", $$"""{"tenantId":"contoso","userId":"{{opsId}}","memberType":"Support","effectiveAt":"2030-01-02T00:00:00Z","expiresAt":"2030-01-01T00:00:00Z"}""", superadmin);
        Assert.Equal((HttpStatusCode.BadRequest, "TENANCY_VALIDATION_007"), (badWindow, badWindowBody.GetProperty("code").GetString()));
        var (carolJoined, carolJoinedBody) = await SendAsync(server, HttpMethod.Post, "/api/tenant-members", $$"""{"tenantId":"contoso","userId":"{{carolId}}"}""", superadmin);
        Assert.Equal(HttpStatusCode.Created, carolJoined);
        var carolMemberId = carolJoinedBody.GetProperty("id").GetString()!;
        var seatsBefore = (await SendAsync(server, HttpMethod.Get, $"/api/tenants/{contosoId}", null, superadmin)).Body.GetProperty("quota").GetProperty("usedSeats").GetInt64();

        // 运维支持不占席位、能切换进去，但不进模仿候选
        var expires = DateTimeOffset.UtcNow.AddDays(1).ToString("O");
        var (opsJoined, opsJoinedBody) = await SendAsync(server, HttpMethod.Post, "/api/tenant-members", $$"""{"tenantId":"contoso","userId":"{{opsId}}","memberType":"Support","expiresAt":"{{expires}}"}""", superadmin);
        Assert.Equal(HttpStatusCode.Created, opsJoined);
        Assert.Equal(JsonValueKind.String, opsJoinedBody.GetProperty("expiresAt").ValueKind);
        Assert.Equal(seatsBefore, (await SendAsync(server, HttpMethod.Get, $"/api/tenants/{contosoId}", null, superadmin)).Body.GetProperty("quota").GetProperty("usedSeats").GetInt64());
        var ops = (await AuthFlowTests.LoginAsync(server, "ops", "Passw0rd@123")).GetProperty("accessToken").GetString()!;
        var (opsSwitch, opsSwitchBody) = await SendAsync(server, HttpMethod.Post, "/api/auth:switch-tenant", """{"tenantId":"contoso"}""", ops);
        Assert.Equal(HttpStatusCode.OK, opsSwitch);
        var opsRefresh = opsSwitchBody.GetProperty("refreshToken").GetString()!;
        var (candidates, candidatesBody) = await SendAsync(server, HttpMethod.Get, "/api/impersonation:candidates?tenantId=contoso", null, superadmin);
        Assert.Equal(HttpStatusCode.OK, candidates);
        var names = candidatesBody.GetProperty("items").EnumerateArray().Select(static c => c.GetProperty("userName").GetString()).ToList();
        Assert.Contains("carol", names);
        Assert.DoesNotContain("ops", names);

        // 已过期的成员关系不能进入，也不出现在「我的租户」
        var lateId = await CreateUserAsync(server, superadmin, "late", null);
        var past = DateTimeOffset.UtcNow.AddMinutes(-1).ToString("O");
        Assert.Equal(HttpStatusCode.Created, (await SendAsync(server, HttpMethod.Post, "/api/tenant-members", $$"""{"tenantId":"contoso","userId":"{{lateId}}","memberType":"Support","expiresAt":"{{past}}"}""", superadmin)).Status);
        var late = (await AuthFlowTests.LoginAsync(server, "late", "Passw0rd@123")).GetProperty("accessToken").GetString()!;
        var (lateSwitch, lateSwitchBody) = await SendAsync(server, HttpMethod.Post, "/api/auth:switch-tenant", """{"tenantId":"contoso"}""", late);
        Assert.Equal((HttpStatusCode.Forbidden, "IDENTITY_DENIED_006"), (lateSwitch, lateSwitchBody.GetProperty("code").GetString()));
        Assert.Empty((await SendAsync(server, HttpMethod.Get, "/api/tenants:mine", null, late)).Body.GetProperty("tenants").EnumerateArray());

        // 转移：运维支持与现任所有者都不行；锁定的注册地用户不行
        var (toSupport, toSupportBody) = await SendAsync(server, HttpMethod.Post, $"/api/tenants/{contosoId}:transfer-owner", $$"""{"userId":"{{opsId}}"}""", superadmin);
        Assert.Equal((HttpStatusCode.BadRequest, "TENANCY_VALIDATION_005"), (toSupport, toSupportBody.GetProperty("code").GetString()));
        var (toSelf, toSelfBody) = await SendAsync(server, HttpMethod.Post, $"/api/tenants/{contosoId}:transfer-owner", $$"""{"userId":"{{adminId}}"}""", superadmin);
        Assert.Equal((HttpStatusCode.Conflict, "TENANCY_CONFLICT_011"), (toSelf, toSelfBody.GetProperty("code").GetString()));
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, "/api/users:lock", $$"""{"ids":["{{bobId}}"]}""", superadmin)).Status);
        var (toLocked, toLockedBody) = await SendAsync(server, HttpMethod.Post, $"/api/tenants/{contosoId}:transfer-owner", $$"""{"userId":"{{bobId}}"}""", superadmin);
        Assert.Equal((HttpStatusCode.BadRequest, "TENANCY_VALIDATION_005"), (toLocked, toLockedBody.GetProperty("code").GetString()));
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, "/api/users:unlock", $$"""{"ids":["{{bobId}}"]}""", superadmin)).Status);

        // 转给 bob：所有者改写、bob 拿到管理员权限、admin 保留；记一条所有权转移的权限变更日志
        var (transferred, transferredBody) = await SendAsync(server, HttpMethod.Post, $"/api/tenants/{contosoId}:transfer-owner", $$"""{"userId":"{{bobId}}"}""", superadmin);
        Assert.Equal(HttpStatusCode.OK, transferred);
        Assert.Equal(bobId, transferredBody.GetProperty("adminUserId").GetString());
        var bob = (await AuthFlowTests.LoginAsync(server, "bob", "Passw0rd@123", "contoso")).GetProperty("accessToken").GetString()!;
        Assert.NotEmpty((await SendAsync(server, HttpMethod.Get, "/api/auth:permissions", null, bob)).Body.GetProperty("permissions").EnumerateArray());
        var admin = (await AuthFlowTests.LoginAsync(server, "admin", "Admin@123456", "contoso")).GetProperty("accessToken").GetString()!;
        Assert.NotEmpty((await SendAsync(server, HttpMethod.Get, "/api/auth:permissions", null, admin)).Body.GetProperty("permissions").EnumerateArray());
        Assert.True((await SendAsync(server, HttpMethod.Get, "/api/tenants:mine", null, bob)).Body.GetProperty("tenants")[0].GetProperty("isOwner").GetBoolean());
        await server.Services.GetRequiredService<DatabaseAuditTrail>().FlushAsync(CancellationToken.None);
        await server.Services.GetRequiredService<AuditLogWriter>().FlushAsync(CancellationToken.None);
        var changes = (await SendAsync(server, HttpMethod.Post, "/api/permission-change-logs:list", """{"page":{"index":1,"size":200}}""", superadmin)).Body.GetProperty("items").EnumerateArray().ToList();
        var change = Assert.Single(changes, c => c.GetProperty("changeType").GetString() == "TenantOwner");
        Assert.Equal(("contoso", bobId), (change.GetProperty("targetTenantId").GetString(), change.GetProperty("targetUserId").GetString()));
        Assert.Contains(adminId, change.GetProperty("detail").GetString(), StringComparison.Ordinal);

        // 所有者不能删除、锁定；转给普通成员 carol 之后，她的成员行不能移出
        var (deleteOwner, deleteOwnerBody) = await SendAsync(server, HttpMethod.Delete, $"/api/users?ids={bobId}", null, superadmin);
        Assert.Equal((HttpStatusCode.OK, "IDENTITY_CONFLICT_011"), (deleteOwner, FirstFailure(deleteOwnerBody)));
        var (lockOwner, lockOwnerBody) = await SendAsync(server, HttpMethod.Post, "/api/users:lock", $$"""{"ids":["{{bobId}}"]}""", superadmin);
        Assert.Equal((HttpStatusCode.OK, "IDENTITY_CONFLICT_011"), (lockOwner, FirstFailure(lockOwnerBody)));
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, $"/api/tenants/{contosoId}:transfer-owner", $$"""{"userId":"{{carolId}}"}""", superadmin)).Status);
        var (removeOwner, removeOwnerBody) = await SendAsync(server, HttpMethod.Delete, $"/api/tenant-members?ids={carolMemberId}", null, superadmin);
        Assert.Equal((HttpStatusCode.OK, "TENANCY_CONFLICT_012"), (removeOwner, FirstFailure(removeOwnerBody)));
        // 运维支持移出之后，留在 contoso 里的会话不能再续签
        var opsMemberId = opsJoinedBody.GetProperty("id").GetString()!;
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Delete, $"/api/tenant-members?ids={opsMemberId}", null, superadmin)).Status);
        var (staleOps, staleOpsBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:refresh", $$"""{"refreshToken":"{{opsRefresh}}"}"""));
        Assert.Equal((HttpStatusCode.Forbidden, "IDENTITY_DENIED_006"), (staleOps.StatusCode, staleOpsBody.GetProperty("code").GetString()));

        // 卸任的 bob 又能删了
        var (deleteFormer, deleteFormerBody) = await SendAsync(server, HttpMethod.Delete, $"/api/users?ids={bobId}", null, superadmin);
        Assert.Equal(HttpStatusCode.OK, deleteFormer);
        Assert.Empty(deleteFormerBody.GetProperty("failed").EnumerateArray());
    }
}
