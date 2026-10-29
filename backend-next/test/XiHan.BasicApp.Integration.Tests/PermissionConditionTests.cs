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
/// 权限条件：挂在授予上满足才授予、挂在拒绝上满足才拒绝，调用时按请求属性判定（<c>auth.permissions</c> 照旧列出），
/// 不满足即 403 并记一条条件命中；只挂在确实存在的绑定上、每个绑定的组数有上限、子句按条件引擎编译；绑定没了连同删除；只记录模式放行但照记命中。
/// </summary>
public sealed class PermissionConditionTests : IDisposable
{
    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private static async Task Flush(BasicAppTestServer server)
    {
        await server.Services.GetRequiredService<DatabaseAuditTrail>().FlushAsync(CancellationToken.None);
        await server.Services.GetRequiredService<AuditLogWriter>().FlushAsync(CancellationToken.None);
    }

    private static async Task<(BasicAppTestServer Server, string Admin, string DanaId, string RoleId, string Dana)> StartAsync(SqliteDatabase database, IEnumerable<KeyValuePair<string, string?>>? settings = null)
    {
        var server = await BasicAppTestServer.StartAsync(database.ConnectionString, settings: settings);
        await server.SeedDefaultsAsync();
        await server.RunSeedersAsync();
        var admin = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;
        var (_, user) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users", """{"userName":"dana","displayName":"Dana","password":"Dana@123456"}""", admin));
        var danaId = user.GetProperty("id").GetString()!;
        var (_, role) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/roles", """{"code":"viewer","name":"Viewer"}""", admin));
        var roleId = role.GetProperty("id").GetString()!;
        await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/roles/{roleId}:grant", """{"permissions":["identity.users.read"]}""", admin));
        await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/roles/{roleId}:assign", $$"""{"userIds":["{{danaId}}"]}""", admin));
        var dana = (await AuthFlowTests.LoginAsync(server, "dana", "Dana@123456")).GetProperty("accessToken").GetString()!;
        return (server, admin, danaId, roleId, dana);
    }

    private static async Task<(HttpStatusCode Status, JsonElement Body)> CreateAsync(BasicAppTestServer server, string admin, object request)
    {
        var (response, body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/permission-conditions", JsonSerializer.Serialize(request), admin));
        return (response.StatusCode, body);
    }

    [Fact]
    public async Task Conditions_GrantWhenMet_DenyWhenMet_AndAreRemovedWithTheirBinding()
    {
        var (server, admin, danaId, roleId, dana) = await StartAsync(_database);
        await using var running = server;
        async Task<(HttpStatusCode Status, JsonElement Body)> ListUsersAsync()
        {
            var (response, body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users:list", "{}", dana));
            return (response.StatusCode, body);
        }

        Assert.Equal(HttpStatusCode.OK, (await ListUsersAsync()).Status);

        // 属性目录：带可用的运算符与认证前能否取得
        var (_, attributes) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/permission-conditions:attributes", token: admin));
        var ip = attributes.GetProperty("items").EnumerateArray().Single(static a => a.GetProperty("name").GetString() == "environment.ip");
        Assert.Equal("IpAddress", ip.GetProperty("kind").GetString());
        Assert.True(ip.GetProperty("isBeforeAuthentication").GetBoolean());
        Assert.Contains("In", ip.GetProperty("operators").EnumerateArray().Select(static o => o.GetString()));

        // 写入的校验：绑定不存在、属性未登记、内置角色
        Assert.Equal("ACCESS_VALIDATION_032", (await CreateAsync(server, admin, new { subjectKind = "Role", subjectId = roleId, permission = "identity.users.update", action = "Grant", attributeName = "subject.userName", @operator = "Equals", value = "dana" })).Body.GetProperty("code").GetString());
        Assert.Equal("SECURITY_VALIDATION_002", (await CreateAsync(server, admin, new { subjectKind = "Role", subjectId = roleId, permission = "identity.users.read", action = "Grant", attributeName = "subject.shoeSize", @operator = "Equals", value = "42" })).Body.GetProperty("code").GetString());
        var (_, rolesBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/roles:list", "{}", admin));
        var builtIn = rolesBody.GetProperty("items").EnumerateArray().First(static r => r.GetProperty("isBuiltIn").GetBoolean()).GetProperty("id").GetString()!;
        Assert.Equal("ACCESS_VALIDATION_033", (await CreateAsync(server, admin, new { subjectKind = "Role", subjectId = builtIn, permission = "identity.users.read", action = "Grant", attributeName = "subject.userName", @operator = "Equals", value = "dana" })).Body.GetProperty("code").GetString());

        // 挂在授予上：满足才授予；不满足即 403，权限码照旧列在 auth.permissions 里
        var (created, condition) = await CreateAsync(server, admin, new { subjectKind = "Role", subjectId = roleId, permission = "identity.users.read", action = "Grant", attributeName = "subject.userName", @operator = "Equals", value = "someone-else" });
        Assert.Equal(HttpStatusCode.Created, created);
        var conditionId = condition.GetProperty("id").GetString()!;
        var (denied, deniedBody) = await ListUsersAsync();
        Assert.Equal(HttpStatusCode.Forbidden, denied);
        Assert.Equal("ACCESS_DENIED_003", deniedBody.GetProperty("code").GetString());
        var (_, permissions) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:permissions", token: dana));
        Assert.Contains("identity.users.read", permissions.GetProperty("permissions").EnumerateArray().Select(static p => p.GetString()));
        var (updated, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/permission-conditions/{conditionId}", """{"attributeName":"subject.userName","operator":"In","value":"dana,erin"}""", admin));
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await ListUsersAsync()).Status);

        // 停用的子句不参与：绑定回到无条件
        await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/permission-conditions/{conditionId}", """{"attributeName":"subject.userName","operator":"Equals","value":"nobody","status":"Disabled"}""", admin));
        Assert.Equal(HttpStatusCode.OK, (await ListUsersAsync()).Status);

        // 挂在拒绝上：满足才拒绝
        await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/user-grants:deny", $$"""{"userId":"{{danaId}}","permissions":["identity.users.read"]}""", admin));
        Assert.Equal(HttpStatusCode.Forbidden, (await ListUsersAsync()).Status);
        var (denyCreated, denyCondition) = await CreateAsync(server, admin, new { subjectKind = "User", subjectId = danaId, permission = "identity.users.read", action = "Deny", attributeName = "environment.client.kind", @operator = "Equals", value = "grpc" });
        Assert.Equal(HttpStatusCode.Created, denyCreated);
        Assert.Equal(HttpStatusCode.OK, (await ListUsersAsync()).Status);
        await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/permission-conditions/{denyCondition.GetProperty("id").GetString()}", """{"attributeName":"environment.client.kind","operator":"Equals","value":"http"}""", admin));
        Assert.Equal(HttpStatusCode.Forbidden, (await ListUsersAsync()).Status);

        // 列表：绑定与子句；撤销即连同删除拒绝上的条件
        var (_, listed) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/permission-conditions?subjectKind=User&subjectId={danaId}", token: admin));
        Assert.Equal("Deny", Assert.Single(listed.GetProperty("bindings").EnumerateArray()).GetProperty("action").GetString());
        Assert.Single(listed.GetProperty("conditions").EnumerateArray());
        await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/user-grants:revoke", $$"""{"userId":"{{danaId}}","permissions":["identity.users.read"]}""", admin));
        var (_, afterRevoke) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/permission-conditions?subjectKind=User&subjectId={danaId}", token: admin));
        Assert.Empty(afterRevoke.GetProperty("conditions").EnumerateArray());

        // 组数上限：每个绑定 5 组
        for (var group = 1; group <= 4; group++)
        {
            Assert.Equal(HttpStatusCode.Created, (await CreateAsync(server, admin, new { subjectKind = "Role", subjectId = roleId, permission = "identity.users.read", action = "Grant", attributeName = "environment.hour", @operator = "GreaterThanOrEquals", value = "0", groupNo = group })).Status);
        }

        Assert.Equal("ACCESS_VALIDATION_034", (await CreateAsync(server, admin, new { subjectKind = "Role", subjectId = roleId, permission = "identity.users.read", action = "Grant", attributeName = "environment.hour", @operator = "GreaterThanOrEquals", value = "0", groupNo = 5 })).Body.GetProperty("code").GetString());

        // 撤销角色的授予即连同删除授予上的条件
        await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/roles/{roleId}:revoke", """{"permissions":["identity.users.read"]}""", admin));
        var (_, roleConditions) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/permission-conditions?subjectKind=Role&subjectId={roleId}", token: admin));
        Assert.Empty(roleConditions.GetProperty("conditions").EnumerateArray());

        // 命中：拒绝的那几次都记下了，来源是权限条件、拦截、时机为授权
        await Flush(server);
        var (_, hits) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/condition-hits:list", """{"page":{"index":1,"size":50}}""", admin));
        var hit = hits.GetProperty("items").EnumerateArray().First();
        Assert.Equal("PermissionCondition", hit.GetProperty("source").GetString());
        Assert.Equal("identity.users.read", hit.GetProperty("policyCode").GetString());
        Assert.True(hit.GetProperty("isBlocked").GetBoolean());
        Assert.Equal("users.list", hit.GetProperty("operationId").GetString());
        var (_, changes) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/permission-change-logs:list", """{"page":{"index":1,"size":200}}""", admin));
        Assert.Contains("ConditionChange", changes.GetProperty("items").EnumerateArray().Select(static c => c.GetProperty("changeType").GetString()));
    }

    [Fact]
    public async Task LogOnlyMode_LetsTheCallThrough_ButRecordsTheHit()
    {
        var (server, admin, _, roleId, dana) = await StartAsync(_database, [new("BasicApp:Access:PermissionConditionMode", "LogOnly")]);
        await using var running = server;
        var (created, _) = await CreateAsync(server, admin, new { subjectKind = "Role", subjectId = roleId, permission = "identity.users.read", action = "Grant", attributeName = "subject.userName", @operator = "Equals", value = "someone-else" });
        Assert.Equal(HttpStatusCode.Created, created);
        var (response, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users:list", "{}", dana));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await Flush(server);
        var (_, hits) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/condition-hits:list", """{"page":{"index":1,"size":50}}""", admin));
        var hit = Assert.Single(hits.GetProperty("items").EnumerateArray());
        Assert.Equal("LogOnly", hit.GetProperty("action").GetString());
        Assert.False(hit.GetProperty("isBlocked").GetBoolean());
    }
}
