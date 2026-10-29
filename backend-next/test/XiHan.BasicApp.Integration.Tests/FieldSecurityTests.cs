// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Auditing.Domain.Services;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.Framework.Auditing.Database.Trail;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证字段安全规则的命中、打码与写入保护。
/// </summary>
/// <remarks>
/// 方案 5.9，语义沿用 4.x。
/// 规则按角色、用户、部门（含下级部门）命中，只在所属一侧生效；用户目录在投影时打码，按受保护字段筛选排序被拒、关键字不再覆盖它们；
/// 修改时交回脱敏值或空值即保留原值，改只读字段被拒，只写字段可填新值；多条规则取最严；写规则的校验与冲突；删角色连同删规则；变更进权限变更日志。
/// </remarks>
public sealed class FieldSecurityTests : IDisposable
{
    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private static async Task<JsonElement> UserAsync(BasicAppTestServer server, string token, string id)
    {
        var (response, body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/users/{id}", token: token));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return body;
    }

    private static async Task<string> RuleAsync(BasicAppTestServer server, string token, string json)
    {
        var (response, body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/field-security", json, token));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return body.GetProperty("id").GetString()!;
    }

    [Fact]
    public async Task Rules_MaskTheDirectory_GuardQueriesAndWrites_ForTheirTargets()
    {
        var (server, platform, admin) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;

        // 被看的人与看的人：viewer 角色能读改建用户、数据范围全部
        var (createdErin, erinBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users", """{"userName":"erin","displayName":"Erin","email":"erin@contoso.com","phoneNumber":"13800001111"}""", admin));
        Assert.Equal(HttpStatusCode.Created, createdErin.StatusCode);
        var erinId = erinBody.GetProperty("id").GetString()!;
        var (_, finnBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users", """{"userName":"finn","displayName":"Finn","password":"Finn@123456"}""", admin));
        var finnId = finnBody.GetProperty("id").GetString()!;
        var (_, roleBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/roles", """{"code":"viewer","name":"Viewer","dataScope":"All"}""", admin));
        var roleId = roleBody.GetProperty("id").GetString()!;
        await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/roles/{roleId}:grant", """{"permissions":["identity.users.read","identity.users.update","identity.users.create"]}""", admin));
        await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/roles/{roleId}:assign", $$"""{"userIds":["{{finnId}}"]}""", admin));
        var finn = (await AuthFlowTests.LoginAsync(server, "finn", "Finn@123456", "contoso")).GetProperty("accessToken").GetString()!;

        // 实体目录
        var (_, entities) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/field-security:entities", token: admin));
        var users = Assert.Single(entities.GetProperty("items").EnumerateArray(), static e => e.GetProperty("code").GetString() == "identity.users");
        Assert.Equal(["userName", "displayName", "email", "phoneNumber", "lastLoginAt"], users.GetProperty("fields").EnumerateArray().Select(static f => f.GetProperty("name").GetString()));

        // 角色规则：邮箱部分脱敏、只读
        await RuleAsync(server, admin, $$"""{"targetKind":"Role","targetId":"{{roleId}}","entity":"identity.users","field":"email","strategy":"PartialMask","keepHead":2,"keepTail":4}""");
        var seen = await UserAsync(server, finn, erinId);
        Assert.Equal("er**********.com", seen.GetProperty("email").GetString());
        Assert.Equal("13800001111", seen.GetProperty("phoneNumber").GetString());
        Assert.Equal("erin@contoso.com", (await UserAsync(server, admin, erinId)).GetProperty("email").GetString());
        var (_, listed) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users:list", """{"keyword":"erin"}""", finn));
        Assert.Equal("er**********.com", Assert.Single(listed.GetProperty("items").EnumerateArray()).GetProperty("email").GetString());

        // 不能按受保护字段筛选
        var (filtered, filteredBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users:list", """{"filters":[{"field":"email","operator":"contains","value":"contoso"}]}""", finn));
        Assert.Equal(HttpStatusCode.Forbidden, filtered.StatusCode);
        Assert.Equal("IDENTITY_DENIED_011", filteredBody.GetProperty("code").GetString());

        // 交回脱敏值视为没改（其他字段照改）；改只读字段被拒；建用户时填只读字段被拒
        var (kept, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/users/{erinId}", """{"displayName":"Erin Z","email":"er**********.com","phoneNumber":"13800001111"}""", finn));
        Assert.Equal(HttpStatusCode.OK, kept.StatusCode);
        var afterKeep = await UserAsync(server, admin, erinId);
        Assert.Equal("erin@contoso.com", afterKeep.GetProperty("email").GetString());
        Assert.Equal("Erin Z", afterKeep.GetProperty("displayName").GetString());
        var (changed, changedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/users/{erinId}", """{"displayName":"Erin Z","email":"other@contoso.com"}""", finn));
        Assert.Equal(HttpStatusCode.Forbidden, changed.StatusCode);
        Assert.Equal("IDENTITY_DENIED_012", changedBody.GetProperty("code").GetString());
        var (createdWithEmail, createdWithEmailBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users", """{"userName":"gina","displayName":"Gina","email":"gina@contoso.com"}""", finn));
        Assert.Equal(HttpStatusCode.Forbidden, createdWithEmail.StatusCode);
        Assert.Equal("IDENTITY_DENIED_012", createdWithEmailBody.GetProperty("code").GetString());

        // 用户规则：手机号隐藏、可改写（只写）；邮箱再加一条全部星号，合并取最严
        await RuleAsync(server, admin, $$"""{"targetKind":"User","targetId":"{{finnId}}","entity":"identity.users","field":"phoneNumber","strategy":"Hidden","isEditable":true}""");
        await RuleAsync(server, admin, $$"""{"targetKind":"User","targetId":"{{finnId}}","entity":"identity.users","field":"email","strategy":"FullMask"}""");
        seen = await UserAsync(server, finn, erinId);
        Assert.Equal(JsonValueKind.Null, seen.GetProperty("phoneNumber").ValueKind);
        Assert.Equal(new string('*', "erin@contoso.com".Length), seen.GetProperty("email").GetString());
        var (blankPhone, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/users/{erinId}", """{"displayName":"Erin Z"}""", finn));
        Assert.Equal(HttpStatusCode.OK, blankPhone.StatusCode);
        Assert.Equal("13800001111", (await UserAsync(server, admin, erinId)).GetProperty("phoneNumber").GetString());
        var (newPhone, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/users/{erinId}", """{"displayName":"Erin Z","phoneNumber":"13900002222"}""", finn));
        Assert.Equal(HttpStatusCode.OK, newPhone.StatusCode);
        Assert.Equal("13900002222", (await UserAsync(server, admin, erinId)).GetProperty("phoneNumber").GetString());

        // 部门规则：挂在上级部门，下级部门的成员同样命中；登录名与显示名都受保护时关键字搜不到
        async Task<string> DepartmentAsync(string code, string? parent) => (await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/departments", parent is null ? $$"""{"code":"{{code}}","name":"{{code}}"}""" : $$"""{"code":"{{code}}","name":"{{code}}","parentId":"{{parent}}"}""", admin))).Body.GetProperty("id").GetString()!;
        var hq = await DepartmentAsync("hq", null);
        var rd = await DepartmentAsync("rd", hq);
        var (joined, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/memberships", $$"""{"userId":"{{finnId}}","departmentId":"{{rd}}"}""", admin));
        Assert.Equal(HttpStatusCode.Created, joined.StatusCode);
        await RuleAsync(server, admin, $$"""{"targetKind":"Department","targetId":"{{hq}}","entity":"identity.users","field":"userName","strategy":"Hash"}""");
        await RuleAsync(server, admin, $$"""{"targetKind":"Department","targetId":"{{hq}}","entity":"identity.users","field":"displayName","strategy":"Redact","replacement":"[hidden]"}""");
        seen = await UserAsync(server, finn, erinId);
        Assert.Equal(16, seen.GetProperty("userName").GetString()!.Length);
        Assert.Equal("[hidden]", seen.GetProperty("displayName").GetString());
        var (_, keywordBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users:list", """{"keyword":"erin"}""", finn));
        Assert.Empty(keywordBody.GetProperty("items").EnumerateArray());
        var (sorted, sortedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users:list", """{"sorts":[{"field":"userName"}]}""", finn));
        Assert.Equal(HttpStatusCode.Forbidden, sorted.StatusCode);
        Assert.Equal("IDENTITY_DENIED_011", sortedBody.GetProperty("code").GetString());

        // 停用即不再生效；规则只管所在一侧：平台超管看不到租户规则，也不受它影响
        var (_, page) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/field-security:list", """{"filters":[{"field":"field","operator":"eq","value":"displayName"}]}""", admin));
        var redact = Assert.Single(page.GetProperty("items").EnumerateArray());
        Assert.Equal("hq", redact.GetProperty("targetName").GetString());
        var (disabled, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/field-security/{redact.GetProperty("id").GetString()}", $$"""{"targetKind":"Department","targetId":"{{hq}}","entity":"identity.users","field":"displayName","strategy":"Redact","replacement":"[hidden]","status":"Disabled"}""", admin));
        Assert.Equal(HttpStatusCode.OK, disabled.StatusCode);
        Assert.Equal("Erin Z", (await UserAsync(server, finn, erinId)).GetProperty("displayName").GetString());
        var (_, platformPage) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/field-security:list", "{}", platform));
        Assert.Empty(platformPage.GetProperty("items").EnumerateArray());
        Assert.Equal("erin@contoso.com", (await UserAsync(server, platform, erinId)).GetProperty("email").GetString());

        // 删角色连同删以它为目标的规则
        var (_, before) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/field-security:list", """{"filters":[{"field":"targetKind","operator":"eq","value":"Role"}]}""", admin));
        Assert.Single(before.GetProperty("items").EnumerateArray());
        var (deleted, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Delete, $"/api/roles?ids={roleId}", token: admin));
        Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);
        var (_, after) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/field-security:list", """{"filters":[{"field":"targetKind","operator":"eq","value":"Role"}]}""", admin));
        Assert.Empty(after.GetProperty("items").EnumerateArray());

        await server.Services.GetRequiredService<DatabaseAuditTrail>().FlushAsync(CancellationToken.None);
        await server.Services.GetRequiredService<AuditLogWriter>().FlushAsync(CancellationToken.None);
        var (_, changes) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/permission-change-logs:list", """{"page":{"index":1,"size":200}}""", admin));
        Assert.Equal(6, changes.GetProperty("items").EnumerateArray().Count(static c => c.GetProperty("changeType").GetString() == "FieldSecurity"));
    }

    [Fact]
    public async Task Writes_RejectRulesThatDoNotApply()
    {
        var (server, platform, admin) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;
        var (_, roleBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/roles", """{"code":"clerk","name":"Clerk"}""", admin));
        var roleId = roleBody.GetProperty("id").GetString()!;
        async Task<string?> RejectAsync(string json, string token, HttpStatusCode status)
        {
            var (response, body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/field-security", json, token));
            Assert.Equal(status, response.StatusCode);
            return body.GetProperty("code").GetString();
        }

        string Rule(string field, string strategy, string extra = "") => $$"""{"targetKind":"Role","targetId":"{{roleId}}","entity":"identity.users","field":"{{field}}","strategy":"{{strategy}}"{{extra}}}""";
        Assert.Equal("ACCESS_VALIDATION_014", await RejectAsync($$"""{"targetKind":"Role","targetId":"{{roleId}}","entity":"identity.nothing","field":"email","strategy":"Hidden"}""", admin, HttpStatusCode.BadRequest));
        Assert.Equal("ACCESS_VALIDATION_015", await RejectAsync(Rule("password", "Hidden"), admin, HttpStatusCode.BadRequest));
        Assert.Equal("ACCESS_VALIDATION_016", await RejectAsync(Rule("lastLoginAt", "FullMask"), admin, HttpStatusCode.BadRequest));
        Assert.Equal("ACCESS_VALIDATION_016", await RejectAsync(Rule("displayName", "Hidden"), admin, HttpStatusCode.BadRequest));
        Assert.Equal("ACCESS_VALIDATION_017", await RejectAsync(Rule("email", "None", ""","isEditable":true"""), admin, HttpStatusCode.BadRequest));
        Assert.Equal("ACCESS_VALIDATION_018", await RejectAsync(Rule("email", "PartialMask", ""","keepHead":0,"keepTail":0"""), admin, HttpStatusCode.BadRequest));
        Assert.Equal("ACCESS_VALIDATION_019", await RejectAsync(Rule("email", "Redact"), admin, HttpStatusCode.BadRequest));
        Assert.Equal("ACCESS_NOTFOUND_001", await RejectAsync($$"""{"targetKind":"Role","targetId":"{{Guid.NewGuid()}}","entity":"identity.users","field":"email","strategy":"Hidden"}""", admin, HttpStatusCode.NotFound));
        Assert.Equal("ACCESS_VALIDATION_020", await RejectAsync($$"""{"targetKind":"Department","targetId":"{{Guid.NewGuid()}}","entity":"identity.users","field":"email","strategy":"Hidden"}""", platform, HttpStatusCode.BadRequest));

        // 明文只读是合法规则；同一目标同一字段只有一条
        await RuleAsync(server, admin, Rule("email", "None"));
        Assert.Equal("ACCESS_CONFLICT_006", await RejectAsync(Rule("email", "FullMask"), admin, HttpStatusCode.Conflict));
    }

    [Fact]
    public void Merge_TakesTheStrictestReading_AndTheSmallestKeep()
    {
        var merged = FieldRules.Merge(
        [
            new FieldRule("email", FieldMaskStrategy.PartialMask, 3, 4, null, IsEditable: true),
            new FieldRule("email", FieldMaskStrategy.PartialMask, 1, 6, null, IsEditable: false),
            new FieldRule("phoneNumber", FieldMaskStrategy.None, null, null, null, IsEditable: false),
            new FieldRule("phoneNumber", FieldMaskStrategy.Hash, null, null, null, IsEditable: true),
        ]);
        Assert.Equal("a************.com", merged.Mask("email", "alice@contoso.com"));
        Assert.Equal(FieldWrite.Denied, merged.OnUpdate("email", "alice@contoso.com", "bob@contoso.com"));
        Assert.Equal(FieldWrite.Keep, merged.OnUpdate("email", "alice@contoso.com", merged.Mask("email", "alice@contoso.com")));
        Assert.Equal(16, merged.Mask("phoneNumber", "13800001111")!.Length);
        Assert.Equal(FieldWrite.Denied, merged.OnUpdate("phoneNumber", "13800001111", "13900002222"));
        Assert.Equal("ab***", new FieldRule("x", FieldMaskStrategy.PartialMask, 2, 0, null, false).Mask("abcde"));
        Assert.Equal("****", new FieldRule("x", FieldMaskStrategy.PartialMask, 3, 3, null, false).Mask("abcd"));
    }
}
