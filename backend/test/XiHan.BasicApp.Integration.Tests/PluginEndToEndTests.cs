// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using XiHan.BasicApp.Integration.Tests.Infrastructure;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 真插件进程的全程：示例插件 CrmPlugin
/// </summary>
/// <remarks>
/// 方案 E1 的退出条件：安装 → 启用 → HTTP 调用 → 租户隔离 → 套餐门控 → 分组文档出现插件 → 停用后路由与分组都撤回 → 卸载清理；插件的失败按握手的消息目录翻译
/// </remarks>
public sealed class PluginEndToEndTests : IDisposable
{
    private readonly TestDatabase _database = new();
    private readonly string _root = Path.Combine(Path.GetTempPath(), "xihan-plugin-e2e-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        _database.Dispose();
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public async Task SamplePlugin_ServesTenants_FollowsTheEdition_JoinsTheDocs_AndLeavesWithoutATrace()
    {
        var (server, platform, contoso) = await SettingsTestHost.StartAsync(_database.ConnectionString, configure: builder => builder.Configuration.AddInMemoryCollection(
        [
            new("BasicApp:Files:LocalRootPath", Path.Combine(_root, "files")),
            new("BasicApp:Plugins:CacheDirectory", Path.Combine(_root, "cache")),
        ]));
        await using var _ = server;

        // 安装：示例插件不要框架授予，装好是停用的；启用即在本节点起插件进程
        var fileId = await PluginsTests.UploadAsync(server, platform, CrmPluginPackage.Build());
        var (installed, installedBody) = await PluginsTests.InstallAsync(server, platform, fileId);
        Assert.True(installed.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created, installedBody.GetRawText());
        var id = installedBody.GetProperty("plugin").GetProperty("id").GetString()!;
        var (enabled, enabledBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/plugins:enable", JsonSerializer.Serialize(new { ids = new[] { id } }), platform));
        Assert.True(enabled.StatusCode == HttpStatusCode.OK, enabledBody.GetRawText());
        var (_, status) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/plugins/{id}:status", token: platform));
        Assert.True(status.GetProperty("isLoaded").GetBoolean(), status.GetRawText());
        Assert.Equal("Ready", status.GetProperty("state").GetString());

        // 套餐门控：插件码缺省不在任何套餐里，租户管理员（全部权限）也调不了
        Assert.Equal(HttpStatusCode.Forbidden, (await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/crm-customers", token: contoso))).Response.StatusCode);
        var enterprise = await EditionsTests.EditionIdAsync(server, platform, "enterprise");
        string[] plugin = [CrmPluginPackage.Read, CrmPluginPackage.Create, CrmPluginPackage.Delete];
        await SetEditionAsync(server, platform, enterprise, add: plugin);

        // HTTP 调用：经宿主的完整管道转交插件进程
        var (created, customer) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/crm-customers", """{"name":"Ada","email":"ada@contoso.test"}""", contoso));
        Assert.True(created.StatusCode == HttpStatusCode.Created, customer.GetRawText());
        var customerId = customer.GetProperty("id").GetString()!;
        var (listed, list) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/crm-customers", token: contoso));
        Assert.True(listed.StatusCode == HttpStatusCode.OK, list.GetRawText());
        Assert.Equal(["Ada"], list.GetProperty("items").EnumerateArray().Select(static c => c.GetProperty("name").GetString()!));

        // 插件的失败按握手带来的消息目录翻译成调用方的语言
        var (rejected, problem) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/crm-customers", """{"name":"  "}""", contoso, acceptLanguage: "zh-CN"));
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        Assert.Equal(("CRM_VALIDATION_001", "客户名称不能为空。"), (problem.GetProperty("code").GetString(), problem.GetProperty("detail").GetString()));

        // 租户隔离：另一个租户看不到、也删不掉 contoso 的客户
        var fabrikam = await TenantAsync(server, platform, "fabrikam", enterprise);
        var (_, empty) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/crm-customers", token: fabrikam));
        Assert.Empty(empty.GetProperty("items").EnumerateArray());
        var (missing, missingBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Delete, $"/api/crm-customers/{customerId}", token: fabrikam));
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal("CRM_NOTFOUND_001", missingBody.GetProperty("code").GetString());

        // 插件页面：带视图描述与插件操作的路由，前端据此渲染
        var (viewed, views) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/plugin-views", token: contoso));
        Assert.True(viewed.StatusCode == HttpStatusCode.OK, views.GetRawText());
        var crm = Assert.Single(views.GetProperty("plugins").EnumerateArray());
        Assert.Equal((CrmPluginPackage.Name, CrmPluginPackage.ManifestId, "1.0.0"), (crm.GetProperty("name").GetString(), crm.GetProperty("manifestId").GetString(), crm.GetProperty("version").GetString()));
        var page = Assert.Single(crm.GetProperty("pages").EnumerateArray());
        Assert.Equal(("crm.customers", "plugins/crm/customers/index", "schema"), (page.GetProperty("code").GetString(), page.GetProperty("component").GetString(), page.GetProperty("viewKind").GetString()));
        Assert.Equal("crm-customers.list", JsonElement.Parse(page.GetProperty("viewJson").GetString()!).GetProperty("list").GetString());
        Assert.Equal(
            ["crm-customers.create POST /api/crm-customers", "crm-customers.delete DELETE /api/crm-customers/{id}", "crm-customers.list GET /api/crm-customers"],
            crm.GetProperty("operations").EnumerateArray().Select(static o => $"{o.GetProperty("id").GetString()} {o.GetProperty("method").GetString()} {o.GetProperty("path").GetString()}"));

        // 接口文档：插件一组，全量文档的 x-tagGroups 里有它，分组文档只有它的操作；标题取插件的消息目录
        var (_, full) = await DocumentAsync(server, "/openapi.json");
        var group = Assert.Single(full.GetProperty("x-tagGroups").EnumerateArray(), static g => g.GetProperty("name").GetString() == "CRM");
        Assert.Equal(["crm-customers"], group.GetProperty("tags").EnumerateArray().Select(static t => t.GetString()!));
        var (grouped, document) = await DocumentAsync(server, "/openapi/crm.json");
        Assert.Equal(HttpStatusCode.OK, grouped.StatusCode);
        Assert.Equal("CRM", document.GetProperty("info").GetProperty("title").GetString());
        Assert.Equal(["/api/crm-customers", "/api/crm-customers/{id}"], document.GetProperty("paths").EnumerateObject().Select(static p => p.Name).Order(StringComparer.Ordinal));

        // 套餐收回插件码：租户立即用不了
        await SetEditionAsync(server, platform, enterprise, remove: plugin);
        Assert.Equal(HttpStatusCode.Forbidden, (await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/crm-customers", token: contoso))).Response.StatusCode);

        // 停用：路由、分组文档与插件页面一起撤回
        var (disabled, disabledBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/plugins:disable", JsonSerializer.Serialize(new { ids = new[] { id } }), platform));
        Assert.True(disabled.StatusCode == HttpStatusCode.OK, disabledBody.GetRawText());
        Assert.Equal(HttpStatusCode.NotFound, (await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/crm-customers", token: contoso))).Response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await DocumentAsync(server, "/openapi/crm.json")).Response.StatusCode);
        Assert.DoesNotContain((await DocumentAsync(server, "/openapi.json")).Body.GetProperty("x-tagGroups").EnumerateArray(), static g => g.GetProperty("name").GetString() == "CRM");
        var (_, hidden) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/plugin-views", token: contoso));
        Assert.Empty(hidden.GetProperty("plugins").EnumerateArray());

        // 卸载：安装记录与包都不在，同一个包可以再装
        var (deleted, deletedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Delete, $"/api/plugins?ids={id}", token: platform));
        Assert.True(deleted.StatusCode == HttpStatusCode.OK && deletedBody.GetProperty("failed").GetArrayLength() == 0, deletedBody.GetRawText());
        Assert.Equal(HttpStatusCode.NotFound, (await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/plugins/{id}", token: platform))).Response.StatusCode);
        var (again, againBody) = await PluginsTests.InstallAsync(server, platform, await PluginsTests.UploadAsync(server, platform, CrmPluginPackage.Build()));
        Assert.True(again.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created, againBody.GetRawText());
    }

    /// <summary>
    /// 给套餐加上或去掉一批权限码
    /// </summary>
    private static async Task SetEditionAsync(BasicAppTestServer server, string platform, string edition, string[]? add = null, string[]? remove = null)
    {
        var (_, current) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/editions/{edition}", token: platform));
        var permissions = current.GetProperty("permissions").EnumerateArray().Select(static p => p.GetString()!).Except(remove ?? []).Concat(add ?? []).Distinct().ToArray();
        var (set, setBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/editions/{edition}:set-permissions", JsonSerializer.Serialize(new { permissions }), platform));
        Assert.True(set.StatusCode == HttpStatusCode.OK, setBody.GetRawText());
    }

    /// <summary>
    /// 开通一个租户并以它的管理员登录
    /// </summary>
    private static async Task<string> TenantAsync(BasicAppTestServer server, string platform, string tenantId, string edition)
    {
        var (created, createdBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/tenants", JsonSerializer.Serialize(new { tenantId, name = tenantId, editionId = edition }), platform));
        Assert.True(created.StatusCode == HttpStatusCode.Created, createdBody.GetRawText());
        var (initialized, initializedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/tenants/{createdBody.GetProperty("id").GetString()}:initialize-admin", """{"userName":"admin","password":"Admin@123456"}""", platform));
        Assert.True(initialized.StatusCode == HttpStatusCode.OK, initializedBody.GetRawText());
        return (await AuthFlowTests.LoginAsync(server, "admin", "Admin@123456", tenantId)).GetProperty("accessToken").GetString()!;
    }

    /// <summary>
    /// 取一份英文的接口文档
    /// </summary>
    private static Task<(HttpResponseMessage Response, JsonElement Body)> DocumentAsync(BasicAppTestServer server, string path)
        => server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, path, acceptLanguage: "en-US"));
}
