// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using XiHan.BasicApp.Integration.Tests.Infrastructure;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 菜单管理：页面节点来自页面登记（只调展示、改过即定制、种子不覆盖、可恢复），运营建目录与外链，<c>auth.permissions</c> 按种类下发（目录分组、外链、隐藏页面），
/// 删除只针对运营节点，租户主体碰不到；工作台模块可经 <c>BasicApp:Modules:Workbench:Enabled</c> 关掉。
/// </summary>
public sealed class MenusTests : IDisposable
{
    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private static List<string?> Codes(JsonElement items) => items.EnumerateArray().Select(static m => m.GetProperty("code").GetString()).ToList();

    private static JsonElement Node(JsonElement items, string code) => items.EnumerateArray().Single(m => m.GetProperty("code").GetString() == code);

    [Fact]
    public async Task Pages_CanBeCustomizedAndReset_OperatorNodes_ShapeTheMenu()
    {
        await using var server = await BasicAppTestServer.StartAsync(_database.ConnectionString);
        await server.SeedDefaultsAsync();
        await server.RunSeedersAsync();
        var superadmin = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;

        // 整棵树：两侧的页面节点都在（含租户侧的部门、岗位），工作台排在最前
        var (list, listBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/menus", token: superadmin));
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Equal(["workbench.dashboard", "identity.users", "organization.departments", "organization.positions", "identity.online-users", "access.roles", "access.permissions", "access.delegations", "access.field-security", "access.policies", "access.constraint-rules", "access.menus", "tenancy.tenants", "tenancy.editions", "tenancy.subscription", "settings.configs", "settings.dicts", "settings.numbering-rules", "files.library", "files.storage-configs", "files.export-center", "auditing.traces", "auditing.operation-logs", "auditing.login-logs", "auditing.access-logs", "auditing.exception-logs", "auditing.diff-logs", "auditing.permission-change-logs", "auditing.condition-hits", "auditing.open-api-logs"], Codes(listBody.GetProperty("items")));
        var roles = Node(listBody.GetProperty("items"), "access.roles");
        var rolesId = roles.GetProperty("id").GetString()!;
        var usersId = Node(listBody.GetProperty("items"), "identity.users").GetProperty("id").GetString()!;
        Assert.False(roles.GetProperty("isCustomized").GetBoolean());

        // 新建目录：编码存为 custom.<名>、重复冲突；目录不能挂上级；页面不能新建
        var (directory, directoryBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/menus", """{"kind":"Directory","code":"system","title":"System","icon":"lucide:settings","order":150}""", superadmin));
        Assert.Equal(HttpStatusCode.Created, directory.StatusCode);
        Assert.Equal("custom.system", directoryBody.GetProperty("code").GetString());
        var directoryId = directoryBody.GetProperty("id").GetString()!;
        var (duplicate, duplicateBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/menus", """{"kind":"Directory","code":"system","title":"Again"}""", superadmin));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal("ACCESS_CONFLICT_003", duplicateBody.GetProperty("code").GetString());
        var (nested, nestedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/menus", $$"""{"kind":"Directory","code":"inner","title":"Inner","parentId":"{{directoryId}}"}""", superadmin));
        Assert.Equal(HttpStatusCode.BadRequest, nested.StatusCode);
        Assert.Equal("ACCESS_VALIDATION_006", nestedBody.GetProperty("code").GetString());
        var (page, pageBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/menus", """{"kind":"Page","code":"fake","title":"Fake"}""", superadmin));
        Assert.Equal(HttpStatusCode.BadRequest, page.StatusCode);
        Assert.Equal("ACCESS_VALIDATION_008", pageBody.GetProperty("code").GetString());

        // 新建外链：挂在目录下；地址须是 http / https；权限码须在目录里
        var (link, linkBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/menus", $$"""{"kind":"Link","code":"docs","title":"Docs","externalUrl":"https://docs.xihanfun.com","parentId":"{{directoryId}}","order":10}""", superadmin));
        Assert.Equal(HttpStatusCode.Created, link.StatusCode);
        var linkId = linkBody.GetProperty("id").GetString()!;
        var (badUrl, badUrlBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/menus", """{"kind":"Link","code":"evil","title":"Evil","externalUrl":"javascript:alert(1)"}""", superadmin));
        Assert.Equal(HttpStatusCode.BadRequest, badUrl.StatusCode);
        Assert.Equal("ACCESS_VALIDATION_007", badUrlBody.GetProperty("code").GetString());
        var (badPermission, badPermissionBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/menus", """{"kind":"Link","code":"gated","title":"Gated","externalUrl":"https://example.com","permission":"nope.things.read"}""", superadmin));
        Assert.Equal(HttpStatusCode.BadRequest, badPermission.StatusCode);
        Assert.Equal("ACCESS_VALIDATION_001", badPermissionBody.GetProperty("code").GetString());

        // 页面节点只调展示：改名、换图标、挪进目录、开缓存 → 标为已定制；给权限或外链即拒绝
        var (moved, movedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/menus/{rolesId}", $$"""{"title":"Roles & grants","icon":"lucide:shield-check","order":5,"parentId":"{{directoryId}}","isVisible":true,"keepAlive":true}""", superadmin));
        Assert.Equal(HttpStatusCode.OK, moved.StatusCode);
        Assert.True(movedBody.GetProperty("isCustomized").GetBoolean());
        Assert.Equal("access.roles.read", movedBody.GetProperty("permission").GetString());
        var (managed, managedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/menus/{rolesId}", """{"order":5,"permission":"identity.users.read"}""", superadmin));
        Assert.Equal(HttpStatusCode.BadRequest, managed.StatusCode);
        Assert.Equal("ACCESS_VALIDATION_008", managedBody.GetProperty("code").GetString());
        var (hidden, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/menus/{usersId}", """{"order":100,"isVisible":false}""", superadmin));
        Assert.Equal(HttpStatusCode.OK, hidden.StatusCode);

        // auth.permissions：目录成组（子节点按排序）、外链带地址、隐藏页面照常下发并标 hidden（仍可直接访问），页面码不含运营节点
        var (permissions, permissionsBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:permissions", token: superadmin));
        Assert.Equal(HttpStatusCode.OK, permissions.StatusCode);
        var menus = permissionsBody.GetProperty("menus");
        Assert.Equal(["workbench.dashboard", "identity.users", "custom.system", "identity.online-users", "access.permissions", "access.delegations", "access.field-security", "access.policies", "access.constraint-rules", "access.menus", "tenancy.tenants", "tenancy.editions", "settings.configs", "settings.dicts", "settings.numbering-rules", "files.library", "files.storage-configs", "files.export-center", "auditing.traces", "auditing.operation-logs", "auditing.login-logs", "auditing.access-logs", "auditing.exception-logs", "auditing.diff-logs", "auditing.permission-change-logs", "auditing.condition-hits", "auditing.open-api-logs"], Codes(menus));
        Assert.True(Node(menus, "identity.users").GetProperty("hidden").GetBoolean());
        var system = Node(menus, "custom.system");
        Assert.Equal("Directory", system.GetProperty("kind").GetString());
        Assert.Equal("System", system.GetProperty("title").GetString());
        Assert.Equal(["access.roles", "custom.docs"], Codes(system.GetProperty("children")));
        var movedNode = Node(system.GetProperty("children"), "access.roles");
        Assert.Equal("Roles & grants", movedNode.GetProperty("title").GetString());
        Assert.True(movedNode.GetProperty("keepAlive").GetBoolean());
        Assert.Equal("/access/roles", movedNode.GetProperty("route").GetString());
        var docs = Node(system.GetProperty("children"), "custom.docs");
        Assert.Equal("Link", docs.GetProperty("kind").GetString());
        Assert.Equal("https://docs.xihanfun.com", docs.GetProperty("externalUrl").GetString());
        Assert.Equal(string.Empty, docs.GetProperty("route").GetString());
        var pages = permissionsBody.GetProperty("pages").EnumerateArray().Select(static p => p.GetString()).ToList();
        Assert.Contains("identity.users", pages);
        Assert.Contains("access.roles", pages);
        Assert.DoesNotContain("custom.system", pages);

        // 空目录不下发
        var (empty, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/menus", """{"kind":"Directory","code":"empty","title":"Empty"}""", superadmin));
        Assert.Equal(HttpStatusCode.Created, empty.StatusCode);
        var (again, againBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:permissions", token: superadmin));
        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
        Assert.DoesNotContain("custom.empty", Codes(againBody.GetProperty("menus")));

        // 种子再跑：定制过的展示不被覆盖，运营节点不被删
        await server.RunSeedersAsync();
        var (afterSeed, afterSeedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/menus/{rolesId}", token: superadmin));
        Assert.Equal(HttpStatusCode.OK, afterSeed.StatusCode);
        Assert.Equal(directoryId, afterSeedBody.GetProperty("parentId").GetString());
        Assert.Equal("Roles & grants", afterSeedBody.GetProperty("title").GetString());
        Assert.Equal(5, afterSeedBody.GetProperty("order").GetInt32());
        var (linkAfterSeed, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/menus/{linkId}", token: superadmin));
        Assert.Equal(HttpStatusCode.OK, linkAfterSeed.StatusCode);

        // 恢复默认：回到登记的值；运营节点没有默认值
        var (reset, resetBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/menus/{rolesId}:reset", "{}", superadmin));
        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);
        Assert.False(resetBody.GetProperty("isCustomized").GetBoolean());
        Assert.Equal(JsonValueKind.Null, resetBody.GetProperty("parentId").ValueKind);
        Assert.Equal(JsonValueKind.Null, resetBody.GetProperty("title").ValueKind);
        Assert.Equal("lucide:shield", resetBody.GetProperty("icon").GetString());
        Assert.Equal(200, resetBody.GetProperty("order").GetInt32());
        Assert.False(resetBody.GetProperty("keepAlive").GetBoolean());
        var (resetCustom, resetCustomBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/menus/{linkId}:reset", "{}", superadmin));
        Assert.Equal(HttpStatusCode.BadRequest, resetCustom.StatusCode);
        Assert.Equal("ACCESS_VALIDATION_010", resetCustomBody.GetProperty("code").GetString());

        // 删除：页面节点、还有子节点的目录逐项拒绝，不存在 404；先删外链再删目录即可
        var (refused, refusedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Delete, $"/api/menus?ids={directoryId}&ids={rolesId}&ids={Guid.NewGuid()}", token: superadmin));
        Assert.Equal(HttpStatusCode.OK, refused.StatusCode);
        Assert.Empty(refusedBody.GetProperty("succeeded").EnumerateArray());
        Assert.Equal(["ACCESS_CONFLICT_005", "ACCESS_CONFLICT_004", "ACCESS_NOTFOUND_004"], refusedBody.GetProperty("failed").EnumerateArray().Select(static f => f.GetProperty("code").GetString()).ToList());
        var (deleted, deletedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Delete, $"/api/menus?ids={linkId}&ids={directoryId}", token: superadmin));
        Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);
        Assert.Equal(2, deletedBody.GetProperty("succeeded").GetArrayLength());

        // 租户主体碰不到菜单管理（平台侧权限）
        var (tenantSide, tenantSideBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/menus", token: TestTokens.Issue("alice", "acme", "admin")));
        Assert.Equal(HttpStatusCode.Forbidden, tenantSide.StatusCode);
        Assert.Equal("SECURITY_DENIED_004", tenantSideBody.GetProperty("code").GetString());
    }

    [Fact]
    public async Task WorkbenchModule_CanBeSwitchedOff()
    {
        await using var server = await BasicAppTestServer.StartAsync(_database.ConnectionString, settings: [new("BasicApp:Modules:Workbench:Enabled", "false")]);
        await server.SeedDefaultsAsync();
        await server.RunSeedersAsync();
        var superadmin = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;
        var (permissions, permissionsBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:permissions", token: superadmin));
        Assert.Equal(HttpStatusCode.OK, permissions.StatusCode);
        Assert.Equal("identity.users", Codes(permissionsBody.GetProperty("menus"))[0]);
        Assert.DoesNotContain("workbench.dashboard", permissionsBody.GetProperty("pages").EnumerateArray().Select(static p => p.GetString()));

        await Assert.ThrowsAsync<ArgumentException>(() => BasicAppTestServer.StartAsync(_database.ConnectionString, settings: [new("BasicApp:Modules:Workbench:Enabled", "maybe")]));
    }
}
