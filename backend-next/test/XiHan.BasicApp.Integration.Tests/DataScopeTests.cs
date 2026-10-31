// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using XiHan.BasicApp.Integration.Tests.Infrastructure;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证数据范围与角色化租户管理员。
/// </summary>
/// <remarks>
/// 开通的管理员经内建租户角色 <c>admin</c> 拿到 <c>*</c>（角色进令牌、数据范围全部）；
/// 普通角色按本部门 / 本部门及子部门 / 自定义 / 只看自己缩小用户列表、单条读取与批量操作，归属列表随之缩小；平台角色只能全部。
/// 文件库与通知管理的列表按创建人套用同一判定。
/// </remarks>
public sealed class DataScopeTests : IDisposable
{
    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task TenantAdminIsRoleBased_AndRoleDataScopesNarrowWhatUsersCanSee()
    {
        await using var server = await BasicAppTestServer.StartAsync(_database.ConnectionString);
        await server.SeedDefaultsAsync();
        await server.RunSeedersAsync();
        var superadmin = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;

        // 开通租户（企业版：白名单是租户侧全部）并初始化管理员：管理员的权限来自内建租户角色 admin（* / 数据范围全部），不再是用户级授予
        var enterpriseId = await EditionsTests.EditionIdAsync(server, superadmin, "enterprise");
        var (created, createdBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/tenants", $$"""{"tenantId":"contoso","name":"Contoso","editionId":"{{enterpriseId}}"}""", superadmin));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var tenantId = createdBody.GetProperty("id").GetString()!;
        var (initialized, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/tenants/{tenantId}:initialize-admin", """{"userName":"admin","password":"Admin@123456"}""", superadmin));
        Assert.Equal(HttpStatusCode.OK, initialized.StatusCode);
        var admin = (await AuthFlowTests.LoginAsync(server, "admin", "Admin@123456", "contoso")).GetProperty("accessToken").GetString()!;
        var (permissions, permissionsBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:permissions", token: admin));
        Assert.Equal(HttpStatusCode.OK, permissions.StatusCode);
        Assert.Equal(["admin"], permissionsBody.GetProperty("roles").EnumerateArray().Select(static r => r.GetProperty("code").GetString()).ToList());
        var (roleList, roleListBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/roles:list", "{}", admin));
        Assert.Equal(HttpStatusCode.OK, roleList.StatusCode);
        var adminRole = Assert.Single(roleListBody.GetProperty("items").EnumerateArray());
        Assert.True(adminRole.GetProperty("isBuiltIn").GetBoolean());
        Assert.Equal("All", adminRole.GetProperty("dataScope").GetString());

        // 组织：hq → rd；mkt 独立；用户 dave（hq）、bob（rd）、carol（mkt）、erin（无部门）
        async Task<string> CreateUser(string name) => (await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users", $$"""{"userName":"{{name}}","displayName":"{{name}}","password":"User@123456"}""", admin))).Body.GetProperty("id").GetString()!;
        async Task<string> CreateDepartment(string code, string? parent) => (await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/departments", parent is null ? $$"""{"code":"{{code}}","name":"{{code}}"}""" : $$"""{"code":"{{code}}","name":"{{code}}","parentId":"{{parent}}"}""", admin))).Body.GetProperty("id").GetString()!;
        var dave = await CreateUser("dave");
        var bob = await CreateUser("bob");
        var carol = await CreateUser("carol");
        var erin = await CreateUser("erin");
        var hq = await CreateDepartment("hq", null);
        var rd = await CreateDepartment("rd", hq);
        var mkt = await CreateDepartment("mkt", null);
        foreach (var (user, department) in new[] { (dave, hq), (bob, rd), (carol, mkt) })
        {
            var (joined, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/memberships", $$"""{"userId":"{{user}}","departmentId":"{{department}}"}""", admin));
            Assert.Equal(HttpStatusCode.Created, joined.StatusCode);
        }

        // 角色 lead：读用户、读归属、改用户；新建租户角色缺省只看自己
        var (roleCreated, roleBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/roles", """{"code":"lead","name":"Lead"}""", admin));
        Assert.Equal(HttpStatusCode.Created, roleCreated.StatusCode);
        Assert.Equal("SelfOnly", roleBody.GetProperty("dataScope").GetString());
        var lead = roleBody.GetProperty("id").GetString()!;
        var (granted, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/roles/{lead}:grant", """{"permissions":["identity.users.read","identity.users.update","organization.memberships.read"]}""", admin));
        Assert.Equal(HttpStatusCode.OK, granted.StatusCode);
        var (assigned, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/roles/{lead}:assign", $$"""{"userIds":["{{dave}}"]}""", admin));
        Assert.Equal(HttpStatusCode.OK, assigned.StatusCode);
        var (byUser, byUserBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/roles:by-user?userId={dave}", token: admin));
        Assert.Equal(HttpStatusCode.OK, byUser.StatusCode);
        Assert.Equal(["lead"], byUserBody.GetProperty("items").EnumerateArray().Select(static r => r.GetProperty("code").GetString()).ToList());

        async Task SetScope(string scope, string customDepartments = "[]")
        {
            var (updated, updatedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/roles/{lead}", $$"""{"name":"Lead","dataScope":"{{scope}}","customDepartments":{{customDepartments}}}""", admin));
            Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
            Assert.Equal(scope, updatedBody.GetProperty("dataScope").GetString());
        }

        async Task<List<string?>> VisibleUsers(string token)
        {
            var (list, listBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users:list", """{"sorts":[{"field":"userName"}]}""", token));
            Assert.Equal(HttpStatusCode.OK, list.StatusCode);
            return listBody.GetProperty("items").EnumerateArray().Select(static u => u.GetProperty("userName").GetString()).ToList();
        }

        // 只看自己
        var daveToken = (await AuthFlowTests.LoginAsync(server, "dave", "User@123456", "contoso")).GetProperty("accessToken").GetString()!;
        Assert.Equal(["dave"], await VisibleUsers(daveToken));
        var (hiddenGet, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/users/{bob}", token: daveToken));
        Assert.Equal(HttpStatusCode.NotFound, hiddenGet.StatusCode);
        var (hiddenUpdate, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/users/{bob}", """{"displayName":"Nope"}""", daveToken));
        Assert.Equal(HttpStatusCode.NotFound, hiddenUpdate.StatusCode);

        // 本部门：dave 在 hq，只多看到 hq 的人（没有别人）；本部门及子部门：加上 rd 的 bob；归属列表同样缩小
        await SetScope("DepartmentOnly");
        Assert.Equal(["dave"], await VisibleUsers(daveToken));
        await SetScope("DepartmentAndChildren");
        Assert.Equal(["bob", "dave"], await VisibleUsers(daveToken));
        var (memberships, membershipsBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/memberships:list", "{}", daveToken));
        Assert.Equal(HttpStatusCode.OK, memberships.StatusCode);
        Assert.Equal(["bob", "dave"], membershipsBody.GetProperty("items").EnumerateArray().Select(static m => m.GetProperty("userName").GetString()).OrderBy(static n => n, StringComparer.Ordinal).ToList());
        var (visibleGet, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/users/{bob}", token: daveToken));
        Assert.Equal(HttpStatusCode.OK, visibleGet.StatusCode);

        // 自定义：只 mkt（不含子部门）→ dave 自己 + carol；至少一个部门；部门必须在本租户
        await SetScope("Custom", $$"""[{"departmentId":"{{mkt}}"}]""");
        Assert.Equal(["carol", "dave"], await VisibleUsers(daveToken));
        var (detail, detailBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/roles/{lead}", token: admin));
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        Assert.Equal(mkt, detailBody.GetProperty("customDepartments")[0].GetProperty("departmentId").GetString());
        var (emptyCustom, emptyCustomBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/roles/{lead}", """{"name":"Lead","dataScope":"Custom","customDepartments":[]}""", admin));
        Assert.Equal(HttpStatusCode.BadRequest, emptyCustom.StatusCode);
        Assert.Equal("ACCESS_VALIDATION_005", emptyCustomBody.GetProperty("code").GetString());
        var (foreignCustom, foreignCustomBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/roles/{lead}", $$"""{"name":"Lead","dataScope":"Custom","customDepartments":[{"departmentId":"{{Guid.NewGuid()}}"}]}""", admin));
        Assert.Equal(HttpStatusCode.NotFound, foreignCustom.StatusCode);
        Assert.Equal("ACCESS_NOTFOUND_003", foreignCustomBody.GetProperty("code").GetString());

        // 全部：看到所有人（含没有部门的 erin 与管理员）
        await SetScope("All");
        Assert.Equal(["admin", "bob", "carol", "dave", "erin"], await VisibleUsers(daveToken));
        Assert.NotNull(erin);

        // 平台角色只能全部
        var (platformRole, platformRoleBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/roles", """{"code":"auditor","name":"Auditor","dataScope":"SelfOnly"}""", superadmin));
        Assert.Equal(HttpStatusCode.BadRequest, platformRole.StatusCode);
        Assert.Equal("ACCESS_VALIDATION_004", platformRoleBody.GetProperty("code").GetString());
        var (platformOk, platformOkBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/roles", """{"code":"auditor","name":"Auditor"}""", superadmin));
        Assert.Equal(HttpStatusCode.Created, platformOk.StatusCode);
        Assert.Equal("All", platformOkBody.GetProperty("dataScope").GetString());
        Assert.Equal(JsonValueKind.Array, permissionsBody.GetProperty("menus").ValueKind);
    }
    [Fact]
    public async Task FileAndNotificationLists_FollowTheCreatorsDataScope()
    {
        await using var server = await BasicAppTestServer.StartAsync(_database.ConnectionString);
        await server.SeedDefaultsAsync();
        await server.RunSeedersAsync();
        var superadmin = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;
        var enterpriseId = await EditionsTests.EditionIdAsync(server, superadmin, "enterprise");
        var (created, createdBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/tenants", $$"""{"tenantId":"contoso","name":"Contoso","editionId":"{{enterpriseId}}"}""", superadmin));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var (initialized, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/tenants/{createdBody.GetProperty("id").GetString()}:initialize-admin", """{"userName":"admin","password":"Admin@123456"}""", superadmin));
        Assert.Equal(HttpStatusCode.OK, initialized.StatusCode);
        var admin = (await AuthFlowTests.LoginAsync(server, "admin", "Admin@123456", "contoso")).GetProperty("accessToken").GetString()!;

        // dave 在 hq、bob 在 rd（hq 的下级）；角色 lead 能读、能建文件与通知，缺省只看自己
        async Task<string> CreateUser(string name) => (await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users", $$"""{"userName":"{{name}}","displayName":"{{name}}","password":"User@123456"}""", admin))).Body.GetProperty("id").GetString()!;
        async Task<string> CreateDepartment(string code, string? parent) => (await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/departments", parent is null ? $$"""{"code":"{{code}}","name":"{{code}}"}""" : $$"""{"code":"{{code}}","name":"{{code}}","parentId":"{{parent}}"}""", admin))).Body.GetProperty("id").GetString()!;
        var dave = await CreateUser("dave");
        var bob = await CreateUser("bob");
        var hq = await CreateDepartment("hq", null);
        var rd = await CreateDepartment("rd", hq);
        foreach (var (user, department) in new[] { (dave, hq), (bob, rd) })
        {
            var (joined, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/memberships", $$"""{"userId":"{{user}}","departmentId":"{{department}}"}""", admin));
            Assert.Equal(HttpStatusCode.Created, joined.StatusCode);
        }

        var (roleCreated, roleBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/roles", """{"code":"lead","name":"Lead"}""", admin));
        Assert.Equal(HttpStatusCode.Created, roleCreated.StatusCode);
        var lead = roleBody.GetProperty("id").GetString()!;
        var (granted, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/roles/{lead}:grant", """{"permissions":["files.files.read","files.files.create","notification.notifications.read","notification.notifications.create"]}""", admin));
        Assert.Equal(HttpStatusCode.OK, granted.StatusCode);
        var (assigned, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/roles/{lead}:assign", $$"""{"userIds":["{{dave}}","{{bob}}"]}""", admin));
        Assert.Equal(HttpStatusCode.OK, assigned.StatusCode);
        var daveToken = (await AuthFlowTests.LoginAsync(server, "dave", "User@123456", "contoso")).GetProperty("accessToken").GetString()!;
        var bobToken = (await AuthFlowTests.LoginAsync(server, "bob", "User@123456", "contoso")).GetProperty("accessToken").GetString()!;

        // 三个人各传一个文件、各建一条通知
        foreach (var (token, name) in new[] { (admin, "admin"), (daveToken, "dave"), (bobToken, "bob") })
        {
            using var form = new MultipartFormDataContent();
            var content = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(name));
            content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
            form.Add(content, "file", $"{name}.txt");
            using var upload = new HttpRequestMessage(HttpMethod.Post, "/api/files/upload") { Content = form };
            upload.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var (uploaded, _) = await server.SendAsync(upload);
            Assert.True(uploaded.IsSuccessStatusCode, $"{name}: {uploaded.StatusCode}");
            var (notified, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/notifications", $$"""{"title":"{{name}}"}""", token));
            Assert.Equal(HttpStatusCode.Created, notified.StatusCode);
        }

        async Task<List<string?>> Visible(string path, string property)
        {
            var (list, listBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, path, "{}", daveToken));
            Assert.Equal(HttpStatusCode.OK, list.StatusCode);
            return [.. listBody.GetProperty("items").EnumerateArray().Select(i => i.GetProperty(property).GetString()).Order(StringComparer.Ordinal)];
        }

        async Task SetScope(string scope)
        {
            var (updated, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/roles/{lead}", $$"""{"name":"Lead","dataScope":"{{scope}}"}""", admin));
            Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        }

        // 只看自己：只有自己上传的文件与自己建的通知
        Assert.Equal(["dave.txt"], await Visible("/api/files:list", "originalName"));
        Assert.Equal(["dave"], await Visible("/api/notifications:list", "title"));

        // 本部门及子部门：加上 rd 的 bob；全部：连管理员的也看得到
        await SetScope("DepartmentAndChildren");
        Assert.Equal(["bob.txt", "dave.txt"], await Visible("/api/files:list", "originalName"));
        Assert.Equal(["bob", "dave"], await Visible("/api/notifications:list", "title"));
        await SetScope("All");
        Assert.Equal(["admin.txt", "bob.txt", "dave.txt"], await Visible("/api/files:list", "originalName"));
        Assert.Equal(["admin", "bob", "dave"], await Visible("/api/notifications:list", "title"));
    }
}
