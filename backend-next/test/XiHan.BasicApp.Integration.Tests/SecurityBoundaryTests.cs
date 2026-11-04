// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using Scriban;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Runtime.Seeding;
using XiHan.BasicApp.Runtime.Templates;
using XiHan.BasicApp.Integration.Tests.Infrastructure;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证作用侧、租户上下文、模仿与委托的边界
/// </summary>
/// <remarks>
/// 主体都是真实账号的令牌：持 <c>*</c> 的管理员才能说明拦下来的不是「没有授予」
/// </remarks>
public sealed class SecurityBoundaryTests : IDisposable
{
    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private async Task<(BasicAppTestServer Server, string SuperAdmin)> StartAsync()
    {
        var server = await BasicAppTestServer.StartAsync(_database.ConnectionString);
        await server.SeedDefaultsAsync();
        await server.RunSeedersAsync();
        return (server, (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!);
    }

    /// <summary>
    /// 开一个不绑套餐的租户（不受套餐白名单收窄）并取它管理员的令牌
    /// </summary>
    private static async Task<string> UnrestrictedTenantAdminAsync(BasicAppTestServer server, string superadmin, string tenantId)
    {
        var (created, body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/tenants", $$"""{"tenantId":"{{tenantId}}","name":"{{tenantId}}"}""", superadmin));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = body.GetProperty("id").GetString()!;
        var (initialized, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/tenants/{id}:initialize-admin", """{"userName":"admin","password":"Admin@123456"}""", superadmin));
        Assert.Equal(HttpStatusCode.OK, initialized.StatusCode);
        var (unbound, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/tenants/{id}:change-edition", "{}", superadmin));
        Assert.Equal(HttpStatusCode.OK, unbound.StatusCode);
        return (await AuthFlowTests.LoginAsync(server, "admin", "Admin@123456", tenantId)).GetProperty("accessToken").GetString()!;
    }

    private static async Task<string> CreateUserAsync(BasicAppTestServer server, string token, string userName)
    {
        var (created, body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users", $$"""{"userName":"{{userName}}","displayName":"{{userName}}","password":"Pass@123456"}""", token));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        return body.GetProperty("id").GetString()!;
    }

    private static async Task<string> GrantRoleAsync(BasicAppTestServer server, string token, string userId, params string[] permissions)
    {
        var code = $"r{Guid.NewGuid():N}"[..12];
        var (created, role) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/roles", $$"""{"code":"{{code}}","name":"{{code}}","dataScope":"All"}""", token));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var roleId = role.GetProperty("id").GetString()!;
        var (granted, grantedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/roles/{roleId}:grant", JsonSerializer.Serialize(new { permissions }), token));
        Assert.True(granted.StatusCode == HttpStatusCode.OK, grantedBody.ToString());
        var (assigned, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/roles/{roleId}:assign", $$"""{"userIds":["{{userId}}"]}""", token));
        Assert.Equal(HttpStatusCode.OK, assigned.StatusCode);
        return roleId;
    }

    private static async Task<string> TokenAsync(BasicAppTestServer server, string userName, string? tenant = null)
        => (await AuthFlowTests.LoginAsync(server, userName, "Pass@123456", tenant)).GetProperty("accessToken").GetString()!;

    [Theory]
    [InlineData("POST", "/api/ai-providers:list")]
    [InlineData("POST", "/api/ai-prompts:list")]
    [InlineData("POST", "/api/codegen-tables:list")]
    [InlineData("POST", "/api/oauth-apps:list")]
    [InlineData("POST", "/api/telegram-bots:list")]
    [InlineData("GET", "/api/login-bans")]
    public async Task UnrestrictedTenantAdmin_CannotCallPlatformOperations(string method, string path)
    {
        var (server, superadmin) = await StartAsync();
        await using var running = server;
        var tenantAdmin = await UnrestrictedTenantAdminAsync(server, superadmin, "contoso");

        var (response, body) = await server.SendAsync(BasicAppTestServer.Request(new HttpMethod(method), path, method == "POST" ? "{}" : null, tenantAdmin));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("SECURITY_DENIED_004", body.GetProperty("code").GetString());

        // 平台一侧持 * 的超级管理员照常能调
        var (own, ownBody) = await server.SendAsync(BasicAppTestServer.Request(new HttpMethod(method), path, method == "POST" ? "{}" : null, superadmin));
        Assert.True(own.StatusCode == HttpStatusCode.OK, ownBody.ToString());
    }

    [Fact]
    public async Task PlatformToken_CannotEnterATenantWithABaggageHeader()
    {
        var (server, superadmin) = await StartAsync();
        await using var running = server;
        var tenantAdmin = await UnrestrictedTenantAdminAsync(server, superadmin, "contoso");
        await CreateUserAsync(server, tenantAdmin, "alice");
        await CreateUserAsync(server, superadmin, "operator");
        var platformAccount = await TokenAsync(server, "operator");

        HttpRequestMessage Assignees(string token, string? baggage)
        {
            var request = BasicAppTestServer.Request(HttpMethod.Get, "/api/workflow-assignees:users?limit=200", token: token);
            if (baggage is not null)
            {
                request.Headers.Add("baggage", baggage);
            }

            return request;
        }

        // 没有任何权限的平台账号，也试不进去
        var (denied, deniedBody) = await server.SendAsync(Assignees(platformAccount, "xihan.tenant=contoso"));
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.Equal("IDENTITY_DENIED_006", deniedBody.GetProperty("code").GetString());

        // 超级管理员也一样：进租户走 auth.switch-tenant
        var (superDenied, _) = await server.SendAsync(Assignees(superadmin, "xihan.tenant=contoso"));
        Assert.Equal(HttpStatusCode.Forbidden, superDenied.StatusCode);

        // 不带头照常，别的 Baggage 键不受影响；租户令牌的声明优先，带别的租户也还在自己这一侧
        var (plain, plainBody) = await server.SendAsync(Assignees(platformAccount, null));
        Assert.True(plain.StatusCode == HttpStatusCode.OK, plainBody.ToString());
        Assert.DoesNotContain(plainBody.GetProperty("items").EnumerateArray(), static u => u.GetProperty("userName").GetString() == "alice");
        var (locale, _) = await server.SendAsync(Assignees(platformAccount, "xihan.locale=en-US"));
        Assert.Equal(HttpStatusCode.OK, locale.StatusCode);
        var (own, ownBody) = await server.SendAsync(Assignees(tenantAdmin, "xihan.tenant=other"));
        Assert.True(own.StatusCode == HttpStatusCode.OK, ownBody.ToString());
        Assert.Contains(ownBody.GetProperty("items").EnumerateArray(), static u => u.GetProperty("userName").GetString() == "alice");
    }

    [Fact]
    public async Task Impersonation_StopsAtTheActorsOwnPrivileges()
    {
        var (server, superadmin) = await StartAsync();
        await using var running = server;
        var superadminId = (await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:me", token: superadmin))).Body.GetProperty("user").GetProperty("id").GetString()!;
        var operatorId = await CreateUserAsync(server, superadmin, "operator");
        var peerId = await CreateUserAsync(server, superadmin, "peer");
        var plainId = await CreateUserAsync(server, superadmin, "plain");
        await GrantRoleAsync(server, superadmin, operatorId, "identity.impersonation.start", "identity.users.read");
        await GrantRoleAsync(server, superadmin, peerId, "identity.users.read", "identity.users.reset-password");
        await GrantRoleAsync(server, superadmin, plainId, "identity.users.read");
        var operatorToken = await TokenAsync(server, "operator");

        async Task<(HttpStatusCode Status, JsonElement Body)> ImpersonateAsync(string target, string token)
        {
            var (response, body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/impersonation:start", $$"""{"targetUserId":"{{target}}"}""", token));
            return (response.StatusCode, body);
        }

        // 候选里没有超级管理员；模仿超级管理员、权限超出自己的账号都拒绝，回答相同
        var (candidates, candidatesBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/impersonation:candidates", token: operatorToken));
        Assert.Equal(HttpStatusCode.OK, candidates.StatusCode);
        Assert.DoesNotContain(candidatesBody.GetProperty("items").EnumerateArray(), static c => c.GetProperty("userName").GetString() == "superadmin");
        foreach (var target in new[] { superadminId, peerId })
        {
            var (status, body) = await ImpersonateAsync(target, operatorToken);
            Assert.Equal(HttpStatusCode.Forbidden, status);
            Assert.Equal("IDENTITY_DENIED_007", body.GetProperty("code").GetString());
        }

        // 权限在自己手里的账号可以；超级管理员不比较
        Assert.Equal(HttpStatusCode.OK, (await ImpersonateAsync(plainId, operatorToken)).Status);
        var (asPeer, asPeerBody) = await ImpersonateAsync(peerId, superadmin);
        Assert.Equal(HttpStatusCode.OK, asPeer);

        // 模仿态：目标自己持有的接管类权限也用不了，权限列表里也不再有；能读的照常
        var impersonating = asPeerBody.GetProperty("accessToken").GetString()!;
        var (reset, resetBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/users/{plainId}:reset-password", """{"newPassword":"Pass@654321"}""", impersonating));
        Assert.Equal(HttpStatusCode.Forbidden, reset.StatusCode);
        Assert.Equal("SECURITY_DENIED_004", resetBody.GetProperty("code").GetString());
        var (read, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users:list", "{}", impersonating));
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        var (effective, effectiveBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:permissions", token: impersonating));
        Assert.Equal(HttpStatusCode.OK, effective.StatusCode);
        var codes = effectiveBody.GetProperty("permissions").EnumerateArray().Select(static p => p.GetString()).ToList();
        Assert.Contains("identity.users.read", codes);
        Assert.DoesNotContain("identity.users.reset-password", codes);

        // 同一个权限码，不在模仿态时能用
        var peer = await TokenAsync(server, "peer");
        var (direct, directBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/users/{plainId}:reset-password", """{"newPassword":"Pass@654321"}""", peer));
        Assert.True(direct.StatusCode == HttpStatusCode.OK, directBody.ToString());
    }

    [Fact]
    public async Task TenantAdmin_CannotImpersonateAnotherAllPowerfulAccount()
    {
        var (server, superadmin) = await StartAsync();
        await using var running = server;
        var tenantAdmin = await UnrestrictedTenantAdminAsync(server, superadmin, "contoso");
        var deputyId = await CreateUserAsync(server, tenantAdmin, "deputy");
        var memberId = await CreateUserAsync(server, tenantAdmin, "member");
        var deputyRole = await GrantRoleAsync(server, tenantAdmin, deputyId, "identity.impersonation.start");
        Assert.NotEmpty(deputyRole);

        var (asMember, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/impersonation:start", $$"""{"targetUserId":"{{memberId}}"}""", tenantAdmin));
        Assert.Equal(HttpStatusCode.OK, asMember.StatusCode);

        // 持模仿权限的成员模仿不了持 * 的管理员
        var deputy = await TokenAsync(server, "deputy", "contoso");
        var adminId = (await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:me", token: tenantAdmin))).Body.GetProperty("user").GetProperty("id").GetString()!;
        var (denied, deniedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/impersonation:start", $$"""{"targetUserId":"{{adminId}}"}""", deputy));
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.Equal("IDENTITY_DENIED_007", deniedBody.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Impersonation_SeesOnlyThisSide_OfAnAccountThatBelongsToSeveral()
    {
        var (server, superadmin) = await StartAsync();
        await using var running = server;
        var contoso = await UnrestrictedTenantAdminAsync(server, superadmin, "contoso");
        await UnrestrictedTenantAdminAsync(server, superadmin, "fabrikam");

        // 注册在 contoso、又是 fabrikam 的成员；在 fabrikam 登录过
        var memberId = await CreateUserAsync(server, contoso, "member");
        var (joined, joinedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/tenant-members", $$"""{"tenantId":"fabrikam","userId":"{{memberId}}","memberType":"Member"}""", superadmin));
        Assert.True(joined.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created, joinedBody.ToString());
        var elsewhere = await AuthFlowTests.LoginAsync(server, "member", "Pass@123456", "contoso");
        var (switched, switchedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:switch-tenant", """{"tenantId":"fabrikam"}""", elsewhere.GetProperty("accessToken").GetString()));
        Assert.True(switched.StatusCode == HttpStatusCode.OK, switchedBody.ToString());
        var sessionElsewhere = switchedBody.GetProperty("sessionId").GetString()!;

        // contoso 的管理员模仿他：只看到 contoso 这一侧的会话，fabrikam 的吊销不了
        var (started, startedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/impersonation:start", $$"""{"targetUserId":"{{memberId}}"}""", contoso));
        Assert.True(started.StatusCode == HttpStatusCode.OK, startedBody.ToString());
        var impersonating = startedBody.GetProperty("accessToken").GetString()!;
        var (sessions, sessionsBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/profile:sessions", token: impersonating));
        Assert.True(sessions.StatusCode == HttpStatusCode.OK, sessionsBody.ToString());
        Assert.DoesNotContain(sessionsBody.GetProperty("items").EnumerateArray(), s => s.GetProperty("id").GetString() == sessionElsewhere);
        var (revoke, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/profile:revoke-session", $$"""{"sessionId":"{{sessionElsewhere}}"}""", impersonating));
        Assert.NotEqual(HttpStatusCode.OK, revoke.StatusCode);

        // 本人看得到全部
        var (own, ownBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/profile:sessions", token: switchedBody.GetProperty("accessToken").GetString()));
        Assert.True(own.StatusCode == HttpStatusCode.OK, ownBody.ToString());
        Assert.Contains(ownBody.GetProperty("items").EnumerateArray(), s => s.GetProperty("id").GetString() == sessionElsewhere);
    }

    [Fact]
    public async Task ContactDetails_AreAsGoodAsAPasswordReset()
    {
        var (server, superadmin) = await StartAsync();
        await using var running = server;
        var tenantAdmin = await UnrestrictedTenantAdminAsync(server, superadmin, "contoso");
        var editorId = await CreateUserAsync(server, tenantAdmin, "editor");
        var targetId = await CreateUserAsync(server, tenantAdmin, "target");
        var adminId = (await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:me", token: tenantAdmin))).Body.GetProperty("user").GetProperty("id").GetString()!;
        await GrantRoleAsync(server, tenantAdmin, editorId, "identity.users.update", "identity.users.read");
        var editor = await TokenAsync(server, "editor", "contoso");

        // 只有修改权限：改名可以，改别人的邮箱不行（改了就能走找回密码），改自己的可以
        var (renamed, renamedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/users/{targetId}", """{"displayName":"Renamed"}""", editor));
        Assert.True(renamed.StatusCode == HttpStatusCode.OK, renamedBody.ToString());
        var (email, emailBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/users/{targetId}", """{"displayName":"Renamed","email":"mine@example.com"}""", editor));
        Assert.Equal(HttpStatusCode.Forbidden, email.StatusCode);
        Assert.Equal("IDENTITY_DENIED_021", emailBody.GetProperty("code").GetString());
        var (own, ownBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/users/{editorId}", """{"displayName":"Editor","email":"editor@example.com"}""", editor));
        Assert.True(own.StatusCode == HttpStatusCode.OK, ownBody.ToString());

        // 持 * 的管理员可以改普通账号的；系统账号的联系方式谁也改不了，也不能重置它的双因素
        var (byAdmin, byAdminBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/users/{targetId}", """{"displayName":"Renamed","email":"target@example.com"}""", tenantAdmin));
        Assert.True(byAdmin.StatusCode == HttpStatusCode.OK, byAdminBody.ToString());
        var superadminId = (await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:me", token: superadmin))).Body.GetProperty("user").GetProperty("id").GetString()!;
        var (systemTenant, systemTenantBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/users/{adminId}", """{"displayName":"Admin","email":"mine@example.com"}""", superadmin));
        Assert.Equal(HttpStatusCode.Conflict, systemTenant.StatusCode);
        Assert.Equal("IDENTITY_CONFLICT_014", systemTenantBody.GetProperty("code").GetString());
        var (twoFactor, twoFactorBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users:reset-two-factor", $$"""{"ids":["{{adminId}}"]}""", superadmin));
        Assert.True(twoFactor.StatusCode == HttpStatusCode.OK, twoFactorBody.ToString());
        Assert.Equal("IDENTITY_CONFLICT_014", twoFactorBody.GetProperty("failed")[0].GetProperty("code").GetString());
        Assert.NotEmpty(superadminId);
    }

    [Fact]
    public async Task Seed_RefusesToWriteAPublicSuperAdminPassword()
    {
        await using var server = await BasicAppTestServer.StartAsync(_database.ConnectionString, settings: [new("BasicApp:Seed:SuperAdminPassword", "")]);
        var seeded = await server.Services.GetRequiredService<SeedRunner>().RunAsync(["identity.platform"], TestContext.Current.CancellationToken);
        Assert.True(seeded.IsFailure);
        Assert.Equal("IDENTITY_VALIDATION_026", seeded.Error!.Code);
    }

    [Theory]
    [InlineData("{{ a%%.a }}")]
    [InlineData("{{ 1%% + 1 }}")]
    [InlineData("{{ a%% | string.upcase }}")]
    public void Templates_TooDeepToEvaluate_AreCaughtWithoutWalkingTheStack(string shape)
    {
        var at = shape.IndexOf("%%", StringComparison.Ordinal);
        var head = shape[..at];
        var unit = shape[(at + 2)..shape.LastIndexOf(" }}", StringComparison.Ordinal)];
        unit = unit.Length == 0 ? ".a" : unit;
        // 有的形状解析器自己就报错，成员链能解析通过，要靠深度上限
        var deep = Template.Parse(head + string.Concat(Enumerable.Repeat(unit, 20000)) + " }}");
        Assert.True(deep.HasErrors || TemplateLimits.IsTooDeep(deep));
    }

    [Fact]
    public async Task Templates_AtTheDepthLimit_RenderOnAThreadPoolStack()
    {
        // 上限之内的模板真的渲染得动：线程池线程的栈和请求里一样
        var self = new Scriban.Runtime.ScriptObject { ["b"] = "ok" };
        self["a"] = self;
        var model = new Scriban.Runtime.ScriptObject { ["a"] = self };
        var chain = Template.Parse("{{ a" + string.Concat(Enumerable.Repeat(".a", TemplateLimits.MaxDepth - 12)) + ".b }}");
        Assert.False(chain.HasErrors);
        Assert.False(TemplateLimits.IsTooDeep(chain));
        Assert.Equal("ok", await Task.Run(() => chain.Render(model), TestContext.Current.CancellationToken));
        var nested = Template.Parse(string.Concat(Enumerable.Repeat("{{ if true }}", 40)) + "ok" + string.Concat(Enumerable.Repeat("{{ end }}", 40)));
        Assert.False(TemplateLimits.IsTooDeep(nested));
        Assert.Equal("ok", await Task.Run(() => nested.Render(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CodeGeneration_CannotReadTheApplicationsOwnTables_OrInjectThroughATableName()
    {
        var (server, superadmin) = await StartAsync();
        await using var running = server;

        // 导入只读表结构，生成代码用得上；零代码运行时不读应用自己的表
        var (imported, importedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/codegen-tables:import", """{"tableNames":["sys_user_security"]}""", superadmin));
        Assert.True(imported.StatusCode == HttpStatusCode.OK, importedBody.ToString());
        var (_, list) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/codegen-tables:list", """{"page":{"index":1,"size":10},"keyword":"sys_user_security"}""", superadmin));
        var table = list.GetProperty("items")[0];
        var id = table.GetProperty("id").GetString()!;
        var (read, readBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/codegen-runtime:list", JsonSerializer.Serialize(new { id, page = new { index = 1, size = 10 } }), superadmin));
        Assert.Equal(HttpStatusCode.BadRequest, read.StatusCode);
        Assert.Equal("CODEGENERATION_VALIDATION_015", readBody.GetProperty("code").GetString());

        // 表名不能改：运行时拿它拼查询，改成子查询就是任意 SQL
        var (got, full) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/codegen-tables/{id}", token: superadmin));
        Assert.Equal(HttpStatusCode.OK, got.StatusCode);
        var saved = full.GetProperty("table");
        string[] fields =
        [
            "concurrencyStamp", "tableName", "className", "templateType", "deliveryType", "scope", "target", "databaseType", "status", "tableComment",
            "namespace", "moduleName", "businessName", "functionName", "author", "enabledActions", "primaryKeyColumn", "treeParentColumn", "treeNameColumn",
            "masterTableId", "masterForeignKey", "dataSourceId", "options", "remark",
        ];
        var request = fields.ToDictionary(static f => f, f => (object?)saved.GetProperty(f).Clone());
        request["tableName"] = "(select password_hash as id from sys_user_security) t";
        var (renamed, renamedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/codegen-tables/{id}", JsonSerializer.Serialize(request), superadmin));
        Assert.Equal(HttpStatusCode.BadRequest, renamed.StatusCode);
        Assert.Equal("CODEGENERATION_VALIDATION_011", renamedBody.GetProperty("code").GetString());
    }

    [Fact]
    public async Task CodeGeneration_DataSources_FollowTheOutboundPolicy()
    {
        await using var server = await BasicAppTestServer.StartAsync(_database.ConnectionString, settings: [new("XiHan:Security:Outbound:AllowLoopback", "false"), new("BasicApp:CodeGeneration:AllowSqliteDataSource", "false")]);
        await server.SeedDefaultsAsync();
        await server.RunSeedersAsync();
        var superadmin = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;

        async Task<(HttpStatusCode Status, JsonElement Body)> TestAsync(object source)
        {
            var (created, body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/codegen-datasources", JsonSerializer.Serialize(source), superadmin));
            Assert.True(created.StatusCode == HttpStatusCode.Created, body.ToString());
            var (tested, testedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/codegen-datasources/{body.GetProperty("id").GetString()}:test-connection", "{}", superadmin));
            return (tested.StatusCode, testedBody);
        }

        // 内网地址、附加参数里改写的主机、连接串里的主机都拦；SQLite 的「地址」是服务器上的文件路径，缺省不开放
        foreach (var source in new object[]
        {
            new { sourceName = "inner", databaseType = "PostgreSql", host = "127.0.0.1", port = 5432, databaseName = "d", userName = "u" },
            new { sourceName = "rewritten", databaseType = "PostgreSql", host = "93.184.216.34", port = 5432, databaseName = "d", userName = "u", extraParams = ";Host=127.0.0.1" },
            new { sourceName = "raw", databaseType = "PostgreSql", host = "93.184.216.34", port = 5432, databaseName = "d", userName = "u", connectionString = "Host=10.0.0.5;Database=d;Username=u" },
        })
        {
            var (status, body) = await TestAsync(source);
            Assert.Equal(HttpStatusCode.BadRequest, status);
            Assert.Equal("CODEGENERATION_VALIDATION_012", body.GetProperty("code").GetString());
        }

        var (sqlite, sqliteBody) = await TestAsync(new { sourceName = "file", databaseType = "Sqlite", host = "localhost", port = 0, databaseName = "secret.db", userName = "none" });
        Assert.Equal(HttpStatusCode.BadRequest, sqlite);
        Assert.Equal("CODEGENERATION_VALIDATION_013", sqliteBody.GetProperty("code").GetString());

        // 改了地址要重新填口令：留空的旧口令不能发给假数据库
        var (created, source2) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/codegen-datasources", """{"sourceName":"erp","databaseType":"PostgreSql","host":"db.example.com","port":5432,"databaseName":"d","userName":"u","password":"p@ss"}""", superadmin));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var (moved, movedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/codegen-datasources/{source2.GetProperty("id").GetString()}", JsonSerializer.Serialize(new { concurrencyStamp = source2.GetProperty("concurrencyStamp").GetString(), sourceName = "erp", databaseType = "PostgreSql", host = "attacker.example.com", port = 5432, databaseName = "d", userName = "u" }), superadmin));
        Assert.Equal(HttpStatusCode.BadRequest, moved.StatusCode);
        Assert.Equal("CODEGENERATION_VALIDATION_014", movedBody.GetProperty("code").GetString());
    }

    [Fact]
    public async Task DelegatingForSomeoneElse_RequiresHoldingItYourself()
    {
        var (server, superadmin) = await StartAsync();
        await using var running = server;
        var holderId = await CreateUserAsync(server, superadmin, "holder");
        var ownerId = await CreateUserAsync(server, superadmin, "owner");
        var bystanderId = await CreateUserAsync(server, superadmin, "bystander");
        await GrantRoleAsync(server, superadmin, holderId, "access.delegations.create");
        await GrantRoleAsync(server, superadmin, ownerId, "access.roles.grant");
        var holder = await TokenAsync(server, "holder");
        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);

        // 只有委托权限的人，不能把所有者的权限委托给自己，也不能转给别人
        foreach (var delegatee in new[] { holderId, bystanderId })
        {
            var (denied, deniedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/delegations", JsonSerializer.Serialize(new { delegatorId = ownerId, delegateeId = delegatee, permission = "access.roles.grant", expiresAt }), holder));
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
            Assert.Equal("ACCESS_DENIED_012", deniedBody.GetProperty("code").GetString());
        }

        // 委托自己持有的可以；持 * 的管理员可以代他人委托
        var (own, ownBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/delegations", JsonSerializer.Serialize(new { delegatorId = holderId, delegateeId = bystanderId, permission = "access.delegations.create", expiresAt }), holder));
        Assert.True(own.StatusCode == HttpStatusCode.Created, ownBody.ToString());
        var (byAdmin, byAdminBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/delegations", JsonSerializer.Serialize(new { delegatorId = ownerId, delegateeId = bystanderId, permission = "access.roles.grant", expiresAt }), superadmin));
        Assert.True(byAdmin.StatusCode == HttpStatusCode.Created, byAdminBody.ToString());
    }
}
