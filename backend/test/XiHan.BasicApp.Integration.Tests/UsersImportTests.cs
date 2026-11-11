// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.BasicApp.Organization;
using XiHan.Framework.Security.Abstractions.Permissions;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证批量导入用户
/// </summary>
/// <remarks>
/// 每行按新建用户的契约校验、这批里重复与已占用的登录名、部门与角色码逐行核对；有问题的行不写、其余写入并一并入部门与配角色，
/// 共用的初始密码过策略、能登录；缺少入部门或配角色的权限、平台一侧带部门时整批拒绝；约束规则拦下的行记为失败
/// </remarks>
public sealed class UsersImportTests : IDisposable
{
    private const string Password = "Import@2026";

    private static readonly string[] _auditor = ["auditor"];
    private static readonly string[] _cashier = ["cashier"];
    private static readonly string[] _cashierAndAuditor = ["cashier", "auditor"];
    private static readonly string[] _ghost = ["ghost"];

    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private static async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(BasicAppTestServer server, HttpMethod method, string path, object? body, string token)
    {
        var (response, json) = await server.SendAsync(BasicAppTestServer.Request(method, path, body is null ? null : JsonSerializer.Serialize(body), token));
        return (response.StatusCode, json);
    }

    private static Task<(HttpStatusCode Status, JsonElement Body)> ImportAsync(BasicAppTestServer server, string token, object[] rows, string password = Password)
        => SendAsync(server, HttpMethod.Post, "/api/users:import", new { rows, password }, token);

    private static async Task<string> CreateAsync(BasicAppTestServer server, string token, string path, object body)
    {
        var (status, json) = await SendAsync(server, HttpMethod.Post, path, body, token);
        Assert.Equal(HttpStatusCode.Created, status);
        return json.GetProperty("id").GetString()!;
    }

    private static List<int> Succeeded(JsonElement body)
        => [.. body.GetProperty("succeeded").EnumerateArray().Select(static s => s.GetProperty("index").GetInt32())];

    private static List<(int Index, string? Field, string Code)> Failed(JsonElement body)
        => [.. body.GetProperty("failed").EnumerateArray().Select(static f => (f.GetProperty("index").GetInt32(), f.GetProperty("field").GetString(), f.GetProperty("code").GetString()!))];

    private static string IdOf(JsonElement body, int index)
        => body.GetProperty("succeeded").EnumerateArray().Single(s => s.GetProperty("index").GetInt32() == index).GetProperty("id").GetString()!;

    [Fact]
    public async Task Tenant_ImportsValidRows_IntoDepartmentAndRoles_AndReportsEveryOtherRow()
    {
        await using var server = await BasicAppTestServer.StartAsync(_database.ConnectionString);
        await server.SeedDefaultsAsync();
        await server.SeedAsync([], [
            .. OrganizationPermissions.All.Select(static p => new PermissionGrant(GrantSubject.ForRole("admin"), p.Code)),
            new PermissionGrant(GrantSubject.ForRole("admin"), "access.roles.read"),
            new PermissionGrant(GrantSubject.ForRole("admin"), "access.roles.create"),
            new PermissionGrant(GrantSubject.ForRole("admin"), "access.roles.assign"),
        ]);
        var acme = TestTokens.Issue("alice", "acme", "admin");
        var rd = await CreateAsync(server, acme, "/api/departments", new { code = "rd", name = "R&D" });
        await CreateAsync(server, acme, "/api/roles", new { code = "auditor", name = "Auditor" });
        await CreateAsync(server, acme, "/api/users", new { userName = "bob", displayName = "Bob" });

        var (status, body) = await ImportAsync(server, acme, [
            new { userName = "carol", displayName = "Carol", email = "carol@acme.test", gender = "Female", departmentCode = "rd", roleCodes = _auditor },
            new { userName = "bob", displayName = "Bob again" },
            new { userName = "Dave", displayName = "Dave" },
            new { userName = "erin", displayName = "Erin", email = "not-an-email" },
            new { userName = "carol", displayName = "Carol again" },
            new { userName = "frank", displayName = "Frank", departmentCode = "nope" },
            new { userName = "grace", displayName = "Grace", roleCodes = _ghost },
            new { displayName = "Nameless" },
            new { userName = "heidi", displayName = "Heidi" },
        ]);
        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Equal([0, 8], Succeeded(body));
        Assert.Equal(
            [
                (1, "userName", "IDENTITY_CONFLICT_001"),
                (2, "userName", "KERNEL_VALIDATION_003"),
                (3, "email", "KERNEL_VALIDATION_004"),
                (4, "userName", "IDENTITY_CONFLICT_016"),
                (5, "departmentCode", "ORGANIZATION_NOTFOUND_001"),
                (6, "roleCodes", "ACCESS_NOTFOUND_001"),
                (7, "userName", "KERNEL_VALIDATION_003"),
            ],
            Failed(body));

        // 新用户落在部门（主部门）与角色上，共用的初始密码能登录
        var carol = IdOf(body, 0);
        var (_, members) = await SendAsync(server, HttpMethod.Post, "/api/memberships:list", new { filters = new[] { new { field = "departmentId", @operator = "eq", value = rd } } }, acme);
        var membership = Assert.Single(members.GetProperty("items").EnumerateArray());
        Assert.Equal((carol, true), (membership.GetProperty("userId").GetString(), membership.GetProperty("isPrimary").GetBoolean()));
        var (_, roles) = await SendAsync(server, HttpMethod.Get, $"/api/roles:by-user?userId={carol}", null, acme);
        Assert.Equal(["auditor"], roles.GetProperty("items").EnumerateArray().Select(static r => r.GetProperty("code").GetString()).ToList());
        await AuthFlowTests.LoginAsync(server, "carol", Password, "acme");
        await AuthFlowTests.LoginAsync(server, "heidi", Password, "acme");

        // 失败的行没有写入：改好之后再导入照常成功
        var (again, againBody) = await ImportAsync(server, acme, [
            new { userName = "frank", displayName = "Frank", departmentCode = "rd" },
            new { userName = "grace", displayName = "Grace", roleCodes = _auditor },
        ]);
        Assert.Equal(HttpStatusCode.OK, again);
        Assert.Equal([0, 1], Succeeded(againBody));
        Assert.Empty(Failed(againBody));
    }

    [Fact]
    public async Task DepartmentAndRoleColumns_NeedTheirOwnPermissions_OrTheWholeBatchIsDenied()
    {
        await using var server = await BasicAppTestServer.StartAsync(_database.ConnectionString);
        await server.SeedDefaultsAsync();
        await server.SeedAsync([], [.. OrganizationPermissions.All.Select(static p => new PermissionGrant(GrantSubject.ForRole("admin"), p.Code))]);
        var admin = TestTokens.Issue("alice", "acme", "admin");
        await CreateAsync(server, admin, "/api/departments", new { code = "rd", name = "R&D" });
        await server.SeedAsync([], [new PermissionGrant(GrantSubject.ForRole("creator"), "identity.users.create")]);
        var creator = TestTokens.Issue("ivan", "acme", "creator");

        var (department, departmentBody) = await ImportAsync(server, creator, [new { userName = "judy", displayName = "Judy" }, new { userName = "kim", displayName = "Kim", departmentCode = "rd" }]);
        Assert.Equal((HttpStatusCode.Forbidden, "SECURITY_DENIED_004"), (department, departmentBody.GetProperty("code").GetString()));
        var (role, roleBody) = await ImportAsync(server, admin, [new { userName = "judy", displayName = "Judy", roleCodes = _auditor }]);
        Assert.Equal((HttpStatusCode.Forbidden, "SECURITY_DENIED_004"), (role, roleBody.GetProperty("code").GetString()));

        // 整批拒绝时一行也没写：不带这两列就能导入同一批人
        var (plain, plainBody) = await ImportAsync(server, creator, [new { userName = "judy", displayName = "Judy" }, new { userName = "kim", displayName = "Kim" }]);
        Assert.Equal(HttpStatusCode.OK, plain);
        Assert.Equal([0, 1], Succeeded(plainBody));
    }

    [Fact]
    public async Task Platform_ChecksThePasswordPolicy_ConstraintRules_AndRejectsDepartments()
    {
        await using var server = await BasicAppTestServer.StartAsync(_database.ConnectionString);
        await server.SeedDefaultsAsync();
        await server.RunSeedersAsync();
        var root = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;
        var cashier = await CreateAsync(server, root, "/api/roles", new { code = "cashier", name = "Cashier" });
        var auditor = await CreateAsync(server, root, "/api/roles", new { code = "auditor", name = "Auditor" });
        await CreateAsync(server, root, "/api/constraint-rules", new { code = "cash-audit", name = "Cash vs audit", type = "Ssd", targetType = "Role", items = new[] { new { targetId = cashier }, new { targetId = auditor } } });

        var (weak, weakBody) = await ImportAsync(server, root, [new { userName = "mallory", displayName = "Mallory" }], password: "abcdefgh");
        Assert.Equal((HttpStatusCode.BadRequest, "IDENTITY_VALIDATION_003"), (weak, weakBody.GetProperty("code").GetString()));
        var (department, departmentBody) = await ImportAsync(server, root, [new { userName = "mallory", displayName = "Mallory", departmentCode = "rd" }]);
        Assert.Equal((HttpStatusCode.Forbidden, "ORGANIZATION_DENIED_001"), (department, departmentBody.GetProperty("code").GetString()));

        var (status, body) = await ImportAsync(server, root, [
            new { userName = "niaj", displayName = "Niaj", roleCodes = _cashierAndAuditor },
            new { userName = "olivia", displayName = "Olivia", roleCodes = _cashier },
        ]);
        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Equal([1], Succeeded(body));
        Assert.Equal([(0, "roleCodes", "ACCESS_CONFLICT_012")], Failed(body));
        var (_, roles) = await SendAsync(server, HttpMethod.Get, $"/api/roles:by-user?userId={IdOf(body, 1)}", null, root);
        Assert.Equal(["cashier"], roles.GetProperty("items").EnumerateArray().Select(static r => r.GetProperty("code").GetString()).ToList());
        await AuthFlowTests.LoginAsync(server, "olivia", Password);
    }
}
