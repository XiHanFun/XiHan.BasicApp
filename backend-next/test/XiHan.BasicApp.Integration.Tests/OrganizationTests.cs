// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.BasicApp.Organization;
using XiHan.Framework.Security.Abstractions.Permissions;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证组织包的部门、岗位与归属。
/// </summary>
/// <remarks>
/// 部门树与闭包表（创建、移动、循环拒绝、删除受子部门 / 成员约束）、岗位（编码唯一、被引用不能删）、归属（注册地核对、主部门唯一、按部门列出带名称）、租户隔离、平台主体被拒、演示种子。
/// </remarks>
public sealed class OrganizationTests : IDisposable
{
    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private async Task<BasicAppTestServer> StartAsync()
    {
        var server = await BasicAppTestServer.StartAsync(_database.ConnectionString);
        await server.SeedDefaultsAsync();
        // acme 的 admin 角色拿到组织全部权限；平台角色 root 拿到 *（用来验证「只在租户侧」是操作自己的第二道闸）
        await server.SeedAsync([], [.. OrganizationPermissions.All.Select(static p => new PermissionGrant(GrantSubject.ForRole("admin"), p.Code)), new PermissionGrant(GrantSubject.ForRole("root"), PermissionGrant.All)]);
        return server;
    }

    [Fact]
    public async Task Departments_Positions_Memberships_WorkWithinTheTenant()
    {
        await using var server = await StartAsync();
        var acme = TestTokens.Issue("alice", "acme", "admin");

        // 平台主体即使持有 * 也进不来：组织只在租户侧
        var (platform, platformBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/departments:tree", token: TestTokens.Issue("root", null, "root")));
        Assert.Equal(HttpStatusCode.Forbidden, platform.StatusCode);
        Assert.Equal("ORGANIZATION_DENIED_001", platformBody.GetProperty("code").GetString());

        // 用户：两个本租户用户
        var (u1, u1Body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users", """{"userName":"bob","displayName":"Bob"}""", acme));
        Assert.Equal(HttpStatusCode.Created, u1.StatusCode);
        var bob = u1Body.GetProperty("id").GetString()!;
        var (u2, u2Body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users", """{"userName":"carol","displayName":"Carol"}""", acme));
        Assert.Equal(HttpStatusCode.Created, u2.StatusCode);
        var carol = u2Body.GetProperty("id").GetString()!;

        // 部门：顶层 + 两个子部门；编码重复 409；上级不存在 404；负责人必须是本租户用户
        var (hqCreated, hqBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/departments", $$"""{"code":"hq","name":"Headquarters","leaderUserId":"{{bob}}"}""", acme));
        Assert.Equal(HttpStatusCode.Created, hqCreated.StatusCode);
        var hq = hqBody.GetProperty("id").GetString()!;
        Assert.Equal(bob, hqBody.GetProperty("leaderUserId").GetString());
        var (rdCreated, rdBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/departments", $$"""{"code":"rd","name":"R&D","parentId":"{{hq}}","order":10}""", acme));
        Assert.Equal(HttpStatusCode.Created, rdCreated.StatusCode);
        var rd = rdBody.GetProperty("id").GetString()!;
        var (mktCreated, mktBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/departments", $$"""{"code":"mkt","name":"Marketing","parentId":"{{hq}}","order":20}""", acme));
        Assert.Equal(HttpStatusCode.Created, mktCreated.StatusCode);
        var mkt = mktBody.GetProperty("id").GetString()!;
        var (duplicate, duplicateBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/departments", """{"code":"rd","name":"Again"}""", acme));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal("ORGANIZATION_CONFLICT_001", duplicateBody.GetProperty("code").GetString());
        var (noParent, noParentBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/departments", $$"""{"code":"x","name":"X","parentId":"{{Guid.NewGuid()}}"}""", acme));
        Assert.Equal(HttpStatusCode.NotFound, noParent.StatusCode);
        Assert.Equal("ORGANIZATION_NOTFOUND_001", noParentBody.GetProperty("code").GetString());
        var (foreignLeader, foreignLeaderBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/departments", $$"""{"code":"y","name":"Y","leaderUserId":"{{Guid.NewGuid()}}"}""", acme));
        Assert.Equal(HttpStatusCode.BadRequest, foreignLeader.StatusCode);
        Assert.Equal("ORGANIZATION_VALIDATION_001", foreignLeaderBody.GetProperty("code").GetString());

        // 树与详情（祖先链、负责人、子部门数）
        var (tree, treeBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/departments:tree", token: acme));
        Assert.Equal(HttpStatusCode.OK, tree.StatusCode);
        Assert.Equal(3, treeBody.GetProperty("total").GetInt64());
        var root = Assert.Single(treeBody.GetProperty("items").EnumerateArray());
        Assert.Equal("hq", root.GetProperty("department").GetProperty("code").GetString());
        Assert.Equal(["rd", "mkt"], root.GetProperty("children").EnumerateArray().Select(static n => n.GetProperty("department").GetProperty("code").GetString()).ToList());
        var (detail, detailBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/departments/{rd}", token: acme));
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        Assert.Equal(["hq"], detailBody.GetProperty("ancestors").EnumerateArray().Select(static a => a.GetProperty("code").GetString()).ToList());
        Assert.Equal(0, detailBody.GetProperty("childCount").GetInt64());
        var (hqDetail, hqDetailBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/departments/{hq}", token: acme));
        Assert.Equal(HttpStatusCode.OK, hqDetail.StatusCode);
        Assert.Equal("bob", hqDetailBody.GetProperty("leaderUserName").GetString());
        Assert.Equal(2, hqDetailBody.GetProperty("childCount").GetInt64());

        // 岗位：创建、编码重复、列表
        var (posCreated, posBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/positions", """{"code":"manager","name":"Manager"}""", acme));
        Assert.Equal(HttpStatusCode.Created, posCreated.StatusCode);
        var manager = posBody.GetProperty("id").GetString()!;
        var (posDuplicate, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/positions", """{"code":"manager","name":"Again"}""", acme));
        Assert.Equal(HttpStatusCode.Conflict, posDuplicate.StatusCode);
        var (posList, posListBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/positions:list", """{"keyword":"man"}""", acme));
        Assert.Equal(HttpStatusCode.OK, posList.StatusCode);
        Assert.Equal(1, posListBody.GetProperty("total").GetInt64());

        // 归属：第一条自动主部门；重复 409；别的租户 / 不存在的用户 400；按部门列出带名称；显式设主时另一条取消
        var (m1, m1Body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/memberships", $$"""{"userId":"{{bob}}","departmentId":"{{hq}}","positionId":"{{manager}}","employeeNumber":"0001"}""", acme));
        Assert.Equal(HttpStatusCode.Created, m1.StatusCode);
        Assert.True(m1Body.GetProperty("isPrimary").GetBoolean());
        Assert.Equal("Manager", m1Body.GetProperty("positionName").GetString());
        Assert.Equal("Headquarters", m1Body.GetProperty("departmentName").GetString());
        var bobHq = m1Body.GetProperty("id").GetString()!;
        var (m1Again, m1AgainBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/memberships", $$"""{"userId":"{{bob}}","departmentId":"{{hq}}"}""", acme));
        Assert.Equal(HttpStatusCode.Conflict, m1Again.StatusCode);
        Assert.Equal("ORGANIZATION_CONFLICT_005", m1AgainBody.GetProperty("code").GetString());
        var (foreign, foreignBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/memberships", $$"""{"userId":"{{Guid.NewGuid()}}","departmentId":"{{hq}}"}""", acme));
        Assert.Equal(HttpStatusCode.BadRequest, foreign.StatusCode);
        Assert.Equal("ORGANIZATION_VALIDATION_001", foreignBody.GetProperty("code").GetString());
        var (m2, m2Body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/memberships", $$"""{"userId":"{{bob}}","departmentId":"{{rd}}","isPrimary":true}""", acme));
        Assert.Equal(HttpStatusCode.Created, m2.StatusCode);
        Assert.True(m2Body.GetProperty("isPrimary").GetBoolean());
        var bobRd = m2Body.GetProperty("id").GetString()!;
        var (m3, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/memberships", $$"""{"userId":"{{carol}}","departmentId":"{{rd}}"}""", acme));
        Assert.Equal(HttpStatusCode.Created, m3.StatusCode);
        var (byUser, byUserBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/memberships:list", $$"""{"filters":[{"field":"userId","operator":"eq","value":"{{bob}}"}],"sorts":[{"field":"createdAt"}]}""", acme));
        Assert.Equal(HttpStatusCode.OK, byUser.StatusCode);
        Assert.Equal([false, true], byUserBody.GetProperty("items").EnumerateArray().Select(static m => m.GetProperty("isPrimary").GetBoolean()).ToList());
        var (byDepartment, byDepartmentBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/memberships:list", $$"""{"filters":[{"field":"departmentId","operator":"eq","value":"{{rd}}"}]}""", acme));
        Assert.Equal(HttpStatusCode.OK, byDepartment.StatusCode);
        Assert.Equal(["bob", "carol"], byDepartmentBody.GetProperty("items").EnumerateArray().Select(static m => m.GetProperty("userName").GetString()).OrderBy(static n => n, StringComparer.Ordinal).ToList());
        var (treeAfter, treeAfterBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/departments:tree", token: acme));
        Assert.Equal(HttpStatusCode.OK, treeAfter.StatusCode);
        var rootAfter = Assert.Single(treeAfterBody.GetProperty("items").EnumerateArray());
        Assert.Equal(1, rootAfter.GetProperty("memberCount").GetInt64());
        Assert.Equal(2, rootAfter.GetProperty("children")[0].GetProperty("memberCount").GetInt64());

        // 归属修改：岗位必须存在；设主回到 hq
        var (badPosition, badPositionBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/memberships/{bobHq}", $$"""{"positionId":"{{Guid.NewGuid()}}"}""", acme));
        Assert.Equal(HttpStatusCode.NotFound, badPosition.StatusCode);
        Assert.Equal("ORGANIZATION_NOTFOUND_002", badPositionBody.GetProperty("code").GetString());
        var (updated, updatedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/memberships/{bobHq}", $$"""{"positionId":"{{manager}}","isPrimary":true,"jobGrade":"P7"}""", acme));
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.True(updatedBody.GetProperty("isPrimary").GetBoolean());
        Assert.Equal("P7", updatedBody.GetProperty("jobGrade").GetString());

        // 移动：rd 移到 mkt 下，祖先链变成 [mkt, hq]；hq 移到 rd 下是循环；移到不存在的上级 404
        var (moved, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/departments/{rd}:move", $$"""{"parentId":"{{mkt}}"}""", acme));
        Assert.Equal(HttpStatusCode.OK, moved.StatusCode);
        var (rdDetail, rdDetailBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/departments/{rd}", token: acme));
        Assert.Equal(HttpStatusCode.OK, rdDetail.StatusCode);
        Assert.Equal(["mkt", "hq"], rdDetailBody.GetProperty("ancestors").EnumerateArray().Select(static a => a.GetProperty("code").GetString()).ToList());
        var (cyclic, cyclicBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/departments/{hq}:move", $$"""{"parentId":"{{rd}}"}""", acme));
        Assert.Equal(HttpStatusCode.BadRequest, cyclic.StatusCode);
        Assert.Equal("ORGANIZATION_VALIDATION_002", cyclicBody.GetProperty("code").GetString());
        var (toTop, toTopBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/departments/{rd}:move", "{}", acme));
        Assert.Equal(HttpStatusCode.OK, toTop.StatusCode);
        Assert.Equal(JsonValueKind.Null, toTopBody.GetProperty("parentId").ValueKind);
        var (treeMoved, treeMovedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/departments:tree", token: acme));
        Assert.Equal(HttpStatusCode.OK, treeMoved.StatusCode);
        Assert.Equal(["hq", "rd"], treeMovedBody.GetProperty("items").EnumerateArray().Select(static n => n.GetProperty("department").GetProperty("code").GetString()).ToList());

        // 删除：有子部门的 hq 与有成员的 rd 逐项拒绝；被引用的岗位拒绝；移出成员后 rd 可删，bob 的主部门回到 hq
        var (deleteBusy, deleteBusyBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Delete, $"/api/departments?ids={hq}&ids={rd}", token: acme));
        Assert.Equal(HttpStatusCode.OK, deleteBusy.StatusCode);
        Assert.Equal(2, deleteBusyBody.GetProperty("failed").GetArrayLength());
        Assert.All(deleteBusyBody.GetProperty("failed").EnumerateArray(), static f => Assert.Equal("ORGANIZATION_CONFLICT_002", f.GetProperty("code").GetString()));
        var (posBusy, posBusyBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Delete, $"/api/positions?ids={manager}", token: acme));
        Assert.Equal(HttpStatusCode.OK, posBusy.StatusCode);
        Assert.Equal("ORGANIZATION_CONFLICT_004", posBusyBody.GetProperty("failed")[0].GetProperty("code").GetString());
        var (rdMembers, rdMembersBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/memberships:list", $$"""{"filters":[{"field":"departmentId","operator":"eq","value":"{{rd}}"}]}""", acme));
        Assert.Equal(HttpStatusCode.OK, rdMembers.StatusCode);
        var rdMemberIds = string.Join("&ids=", rdMembersBody.GetProperty("items").EnumerateArray().Select(static m => m.GetProperty("id").GetString()));
        var (removedMembers, removedMembersBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Delete, $"/api/memberships?ids={rdMemberIds}", token: acme));
        Assert.Equal(HttpStatusCode.OK, removedMembers.StatusCode);
        Assert.Equal(2, removedMembersBody.GetProperty("succeeded").GetArrayLength());
        Assert.Equal(bobRd, bobRd);
        var (bobLeft, bobLeftBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/memberships:list", $$"""{"filters":[{"field":"userId","operator":"eq","value":"{{bob}}"}]}""", acme));
        Assert.Equal(HttpStatusCode.OK, bobLeft.StatusCode);
        var only = Assert.Single(bobLeftBody.GetProperty("items").EnumerateArray());
        Assert.Equal(hq, only.GetProperty("departmentId").GetString());
        Assert.True(only.GetProperty("isPrimary").GetBoolean());
        var (deleted, deletedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Delete, $"/api/departments?ids={rd}", token: acme));
        Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);
        Assert.Equal(1, deletedBody.GetProperty("succeeded").GetArrayLength());

        // 租户隔离：globex 什么都看不到
        var (other, otherBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/departments:tree", token: TestTokens.Issue("gina", "globex", "admin")));
        Assert.Equal(HttpStatusCode.OK, other.StatusCode);
        Assert.Equal(0, otherBody.GetProperty("total").GetInt64());
    }

    [Fact]
    public async Task DemoOrganization_IsSeededIntoTheDemoTenant_OnlyWhenDemoDataIsEnabled()
    {
        await using var server = await BasicAppTestServer.StartAsync(_database.ConnectionString, settings: [new("BasicApp:Seed:EnableDemoData", "true")]);
        await server.RunSeedersAsync();
        await server.RunSeedersAsync();
        var admin = (await AuthFlowTests.LoginAsync(server, "admin", "Demo@123456", "demo")).GetProperty("accessToken").GetString()!;
        var (tree, treeBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/departments:tree", token: admin));
        Assert.Equal(HttpStatusCode.OK, tree.StatusCode);
        Assert.Equal(3, treeBody.GetProperty("total").GetInt64());
        var root = Assert.Single(treeBody.GetProperty("items").EnumerateArray());
        Assert.Equal("hq", root.GetProperty("department").GetProperty("code").GetString());
        Assert.Equal(1, root.GetProperty("memberCount").GetInt64());
        Assert.Equal(["rd", "mkt"], root.GetProperty("children").EnumerateArray().Select(static n => n.GetProperty("department").GetProperty("code").GetString()).ToList());
        var (positions, positionsBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/positions:list", "{}", admin));
        Assert.Equal(HttpStatusCode.OK, positions.StatusCode);
        Assert.Equal(["manager", "engineer"], positionsBody.GetProperty("items").EnumerateArray().Select(static p => p.GetProperty("code").GetString()).ToList());
    }
}
