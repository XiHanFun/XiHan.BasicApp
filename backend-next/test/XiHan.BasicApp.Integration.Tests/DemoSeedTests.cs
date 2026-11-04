// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using XiHan.BasicApp.Integration.Tests.Infrastructure;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证演示访问策略、演示字典与演示通知三个种子
/// </summary>
/// <remarks>
/// 都看 <c>BasicApp:Seed:EnableDemoData</c>，打开才写；只首写：运营改过的不覆盖、删掉的不补
/// </remarks>
public sealed class DemoSeedTests : IDisposable
{
    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private static List<string?> Values(JsonElement items, string field) => items.EnumerateArray().Select(i => i.GetProperty(field).GetString()).ToList();

    private static JsonElement Find(JsonElement items, string field, string value) => items.EnumerateArray().Single(i => i.GetProperty(field).GetString() == value);

    private static Task<string> DemoAdminAsync(BasicAppTestServer server)
        => LoginAsync(server, "admin", "Demo@123456", "demo");

    private static async Task<string> LoginAsync(BasicAppTestServer server, string userName, string password, string? tenant = null)
        => (await AuthFlowTests.LoginAsync(server, userName, password, tenant)).GetProperty("accessToken").GetString()!;

    [Fact]
    public async Task AccessPolicies_AreLogOnlyInTheDemoTenant_AndWrittenOnce()
    {
        await using var server = await BasicAppTestServer.StartAsync(_database.ConnectionString, settings: [new("BasicApp:Seed:EnableDemoData", "true")]);
        await server.RunSeedersAsync();
        var admin = await DemoAdminAsync(server);

        // 两条都在演示租户：只记录、启用，内容取同名模板
        var (list, listBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/policies:list", """{"keyword":"demo."}""", admin));
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var items = listBody.GetProperty("items");
        Assert.Equal(["demo.off-hours", "demo.overseas"], Values(items, "code").Order(StringComparer.Ordinal));
        var offHours = Find(items, "code", "demo.off-hours");
        Assert.Equal("LogOnly", offHours.GetProperty("action").GetString());
        Assert.Equal("Active", offHours.GetProperty("status").GetString());
        Assert.Equal("preset.off-hours", offHours.GetProperty("templateCode").GetString());
        var overseas = Find(items, "code", "demo.overseas");
        Assert.Equal("preset.overseas", overseas.GetProperty("templateCode").GetString());
        var (detail, detailBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/policies/{offHours.GetProperty("id").GetString()}", token: admin));
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        Assert.Equal(2, detailBody.GetProperty("conditions").GetArrayLength());

        // 平台一侧不写
        var superadmin = await LoginAsync(server, "superadmin", "SuperAdmin@123");
        var (_, platformBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/policies:list", """{"keyword":"demo."}""", superadmin));
        Assert.Equal(0, platformBody.GetProperty("items").GetArrayLength());

        // 运营停用一条、删掉一条，种子再跑都不动
        var offHoursId = offHours.GetProperty("id").GetString()!;
        var (disabled, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/policies/{offHoursId}:set-status", """{"status":"Disabled"}""", admin));
        Assert.Equal(HttpStatusCode.OK, disabled.StatusCode);
        var (removed, removedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Delete, $"/api/policies?ids={overseas.GetProperty("id").GetString()}", token: admin));
        Assert.Equal(HttpStatusCode.OK, removed.StatusCode);
        Assert.Equal(1, removedBody.GetProperty("succeeded").GetArrayLength());
        await server.RunSeedersAsync();
        var (_, afterSeed) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/policies:list", """{"keyword":"demo."}""", admin));
        var left = Assert.Single(afterSeed.GetProperty("items").EnumerateArray());
        Assert.Equal("demo.off-hours", left.GetProperty("code").GetString());
        Assert.Equal("Disabled", left.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Dicts_ArePlatformWide_WithADefault_ADisabledItem_AndATree_AndWrittenOnce()
    {
        await using var server = await BasicAppTestServer.StartAsync(_database.ConnectionString, settings: [new("BasicApp:Seed:EnableDemoData", "true")]);
        await server.RunSeedersAsync();
        var superadmin = await LoginAsync(server, "superadmin", "SuperAdmin@123");
        var admin = await DemoAdminAsync(server);

        // 平铺字典：有缺省项，也有停用的项；租户也能取选项
        var (level, levelBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/dicts:options?code=demo_customer_level", token: admin));
        Assert.Equal(HttpStatusCode.OK, level.StatusCode);
        var levels = levelBody.GetProperty("items");
        Assert.Equal(["vip", "normal", "potential", "blocked"], Values(levels, "value"));
        Assert.Equal("重要客户", Find(levels, "value", "vip").GetProperty("label").GetString());
        Assert.True(Find(levels, "value", "normal").GetProperty("isDefault").GetBoolean());
        Assert.True(Find(levels, "value", "blocked").GetProperty("disabled").GetBoolean());
        Assert.False(Find(levels, "value", "vip").GetProperty("disabled").GetBoolean());

        // 树形字典：大区下挂城市
        var (region, regionBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/dicts:options?code=demo_region", token: admin));
        Assert.Equal(HttpStatusCode.OK, region.StatusCode);
        var regions = regionBody.GetProperty("items");
        Assert.Equal(["east", "shanghai", "hangzhou", "north", "beijing"], Values(regions, "value"));
        Assert.Equal("east", Find(regions, "value", "hangzhou").GetProperty("parentValue").GetString());
        Assert.Equal("north", Find(regions, "value", "beijing").GetProperty("parentValue").GetString());
        Assert.Equal(JsonValueKind.Null, Find(regions, "value", "north").GetProperty("parentValue").ValueKind);

        // 运营删掉一个字典（先删项，不级联），种子再跑不补
        var (dicts, dictsBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/dicts:list", """{"keyword":"demo_"}""", superadmin));
        Assert.Equal(HttpStatusCode.OK, dicts.StatusCode);
        var levelDictId = Find(dictsBody.GetProperty("items"), "code", "demo_customer_level").GetProperty("id").GetString()!;
        var (_, levelItems) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/dict-items?dictId={levelDictId}", token: superadmin));
        var itemIds = string.Join('&', levelItems.GetProperty("items").EnumerateArray().Select(static i => $"ids={i.GetProperty("id").GetString()}"));
        var (removedItems, removedItemsBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Delete, $"/api/dict-items?{itemIds}", token: superadmin));
        Assert.Equal(HttpStatusCode.OK, removedItems.StatusCode);
        Assert.Equal(4, removedItemsBody.GetProperty("succeeded").GetArrayLength());
        var (removed, removedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Delete, $"/api/dicts?ids={levelDictId}", token: superadmin));
        Assert.Equal(HttpStatusCode.OK, removed.StatusCode);
        Assert.Equal(1, removedBody.GetProperty("succeeded").GetArrayLength());
        await server.RunSeedersAsync();
        var (_, afterSeed) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/dicts:list", """{"keyword":"demo_"}""", superadmin));
        Assert.Equal(["demo_region"], Values(afterSeed.GetProperty("items"), "code"));
    }

    [Fact]
    public async Task Notifications_AreDrafts_TargetedPerSide_AndWrittenOnce()
    {
        await using var server = await BasicAppTestServer.StartAsync(_database.ConnectionString, settings: [new("BasicApp:Seed:EnableDemoData", "true")]);
        await server.RunSeedersAsync();
        var superadmin = await LoginAsync(server, "superadmin", "SuperAdmin@123");
        var admin = await DemoAdminAsync(server);

        // 平台：上手指南是草稿，全员、横幅、弹窗、必读并确认
        var (platform, platformBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/notifications:list", "{}", superadmin));
        Assert.Equal(HttpStatusCode.OK, platform.StatusCode);
        var guide = Assert.Single(platformBody.GetProperty("items").EnumerateArray());
        Assert.Equal("XiHan BasicApp 系统功能总览与上手指南", guide.GetProperty("title").GetString());
        Assert.False(guide.GetProperty("isPublished").GetBoolean());
        Assert.Equal("All", guide.GetProperty("targetType").GetString());
        Assert.True(guide.GetProperty("needConfirm").GetBoolean());
        Assert.True(guide.GetProperty("isMandatory").GetBoolean());
        Assert.True(guide.GetProperty("isBanner").GetBoolean());
        Assert.True(guide.GetProperty("isPopup").GetBoolean());
        var (guideDetail, guideDetailBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/notifications/{guide.GetProperty("id").GetString()}", token: superadmin));
        Assert.Equal(HttpStatusCode.OK, guideDetail.StatusCode);
        Assert.Equal("Markdown", guideDetailBody.GetProperty("contentFormat").GetString());
        Assert.StartsWith("# 欢迎使用 XiHan BasicApp", guideDetailBody.GetProperty("content").GetString(), StringComparison.Ordinal);

        // 演示租户：按部门（研发部）与按角色（普通员工）定向，都是草稿
        var (tenant, tenantBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/notifications:list", "{}", admin));
        Assert.Equal(HttpStatusCode.OK, tenant.StatusCode);
        var items = tenantBody.GetProperty("items");
        Assert.Equal(["研发部周会：周五下午三点", "请尽快修改初始密码"], Values(items, "title").Order(StringComparer.Ordinal));
        var meeting = Find(items, "title", "研发部周会：周五下午三点");
        Assert.False(meeting.GetProperty("isPublished").GetBoolean());
        Assert.Equal("Department", meeting.GetProperty("targetType").GetString());
        var (departments, departmentsBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/departments:tree", token: admin));
        Assert.Equal(HttpStatusCode.OK, departments.StatusCode);
        var rd = departmentsBody.GetProperty("items")[0].GetProperty("children").EnumerateArray().Select(static n => n.GetProperty("department")).Single(static d => d.GetProperty("code").GetString() == "rd");
        Assert.Equal([rd.GetProperty("id").GetString()], meeting.GetProperty("targetIds").EnumerateArray().Select(static t => t.GetString()));
        var password = Find(items, "title", "请尽快修改初始密码");
        Assert.Equal("Role", password.GetProperty("targetType").GetString());
        Assert.Equal("Urgent", password.GetProperty("priority").GetString());
        Assert.True(password.GetProperty("needConfirm").GetBoolean());

        // 发布按目标展开收件人：普通员工的三位成员
        var (published, publishedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/notifications/{password.GetProperty("id").GetString()}:publish", "{}", admin));
        Assert.Equal(HttpStatusCode.OK, published.StatusCode);
        Assert.Equal(3, publishedBody.GetProperty("recipientCount").GetInt32());

        // 运营删掉草稿、发布过的也不动：种子再跑不补、不覆盖
        var (removed, removedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Delete, $"/api/notifications?ids={meeting.GetProperty("id").GetString()}", token: admin));
        Assert.Equal(HttpStatusCode.OK, removed.StatusCode);
        Assert.Equal(1, removedBody.GetProperty("succeeded").GetArrayLength());
        var (removedGuide, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Delete, $"/api/notifications?ids={guide.GetProperty("id").GetString()}", token: superadmin));
        Assert.Equal(HttpStatusCode.OK, removedGuide.StatusCode);
        await server.RunSeedersAsync();
        var (_, afterSeed) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/notifications:list", "{}", admin));
        var left = Assert.Single(afterSeed.GetProperty("items").EnumerateArray());
        Assert.Equal("请尽快修改初始密码", left.GetProperty("title").GetString());
        Assert.True(left.GetProperty("isPublished").GetBoolean());
        var (_, platformAfterSeed) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/notifications:list", "{}", superadmin));
        Assert.Equal(0, platformAfterSeed.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task NothingIsWritten_WhenDemoDataIsOff()
    {
        await using var server = await BasicAppTestServer.StartAsync(_database.ConnectionString);
        await server.RunSeedersAsync();
        var superadmin = await LoginAsync(server, "superadmin", "SuperAdmin@123");
        var (_, dicts) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/dicts:list", """{"keyword":"demo_"}""", superadmin));
        Assert.Equal(0, dicts.GetProperty("items").GetArrayLength());
        var (_, notifications) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/notifications:list", "{}", superadmin));
        Assert.Equal(0, notifications.GetProperty("items").GetArrayLength());
        var (_, policies) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/policies:list", """{"keyword":"demo."}""", superadmin));
        Assert.Equal(0, policies.GetProperty("items").GetArrayLength());
    }
}
