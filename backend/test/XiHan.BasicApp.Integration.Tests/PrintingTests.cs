// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts.Printing;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.BasicApp.Printing;
using XiHan.BasicApp.Printing.Domain.Services;
using XiHan.Framework.Security.Abstractions.Permissions;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证打印模块的作用域解析、写入规则、数据源目录、演示数据与模块开关
/// </summary>
/// <remarks>
/// 作用域：平台只有全局模板、不要求开放；租户的自动作用域先本租户、停用或没有再回退到启用且开放的全局模板，租户与全局作用域不回退；
/// 解析缓存写入即失效；模板 JSON 最小校验、数据源须已登记、编码本侧唯一、只删停用的、并发戳过期即冲突；平台写全局模板另要全局管理；
/// 数据源目录持有读取或使用其一即可；演示开关打开写 6 张全局模板与 5 个演示数据源（只首写）；关掉模块宿主照常启动、页面不下发；数据源注册表在启动时校验
/// </remarks>
public sealed class PrintingTests : IDisposable
{
    private const string Admin = "printing-admin";
    private const string Writer = "printing-writer";
    private const string User = "printing-user";
    private const string Reader = "printing-reader";

    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    /// <summary>
    /// 最小的合法模板 JSON
    /// </summary>
    private static string Document(string name) => $$"""{"schemaVersion":1,"name":"{{name}}","pages":[{"id":"page-1","paper":"A4","elements":[]}]}""";

    private async Task<BasicAppTestServer> StartAsync(IEnumerable<KeyValuePair<string, string?>>? settings = null)
    {
        var server = await BasicAppTestServer.StartAsync(_database.ConnectionString, settings: settings);
        await server.SeedDefaultsAsync();
        await server.SeedAsync([], [
            .. PrintingPermissions.All.Select(static p => new PermissionGrant(GrantSubject.ForRole(Admin), p.Code)),
            .. PrintingPermissions.All.Where(static p => p.Code != PrintingPermissions.PrintTemplatesGlobalManage).Select(static p => new PermissionGrant(GrantSubject.ForRole(Writer), p.Code)),
            new PermissionGrant(GrantSubject.ForRole(User), PrintingPermissions.PrintTemplatesUse),
            new PermissionGrant(GrantSubject.ForRole(Reader), PrintingPermissions.PrintTemplatesRead),
        ]);
        return server;
    }

    private static Task<(HttpResponseMessage Response, JsonElement Body)> CreateAsync(BasicAppTestServer server, string token, string code, string name, bool open = false, string? dataSource = null, string? json = null)
        => server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/print-templates", JsonSerializer.Serialize(new { templateCode = code, templateName = name, templateJson = json ?? Document(name), allowTenantUse = open, dataSourceCode = dataSource }), token));

    private static Task<(HttpResponseMessage Response, JsonElement Body)> ResolveAsync(BasicAppTestServer server, string token, string code, string scope = "Auto")
        => server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/print-templates:resolve?templateCode={Uri.EscapeDataString(code)}&scope={scope}", token: token));

    private static Task<(HttpResponseMessage Response, JsonElement Body)> ListAsync(BasicAppTestServer server, string token, string? scope = null)
        => server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/print-templates:list", scope is null ? """{"page":{"index":1,"size":50}}""" : $$"""{"page":{"index":1,"size":50},"filters":[{"field":"scope","operator":"eq","value":"{{scope}}"}]}""", token));

    private static Task<(HttpResponseMessage Response, JsonElement Body)> SetStatusAsync(BasicAppTestServer server, string token, JsonElement template, string status)
        => server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/print-templates/{template.GetProperty("id").GetString()}:set-status", JsonSerializer.Serialize(new { concurrencyStamp = template.GetProperty("concurrencyStamp").GetString(), status }), token));

    private static List<string?> Codes(JsonElement page) => page.GetProperty("items").EnumerateArray().Select(static t => t.GetProperty("templateCode").GetString()).ToList();

    [Fact]
    public async Task Templates_ResolveByScope_FallBack_AndInvalidateTheCacheOnWrite()
    {
        await using var server = await StartAsync();
        var platform = TestTokens.Issue("root", null, Admin);
        var acme = TestTokens.Issue("alice", "acme", Admin);
        var globex = TestTokens.Issue("gary", "globex", Admin);

        // 平台建全局模板（一张向租户开放、一张不开放）；租户建同编码的私有模板，「向租户开放」恒为否
        var (globalInvoice, globalInvoiceBody) = await CreateAsync(server, platform, "invoice", "Global invoice", open: true);
        Assert.Equal(HttpStatusCode.Created, globalInvoice.StatusCode);
        Assert.True(globalInvoiceBody.GetProperty("isGlobal").GetBoolean());
        Assert.True(globalInvoiceBody.GetProperty("allowTenantUse").GetBoolean());
        Assert.Equal("Active", globalInvoiceBody.GetProperty("status").GetString());
        var (internalTemplate, internalBody) = await CreateAsync(server, platform, "internal", "Internal only");
        Assert.Equal(HttpStatusCode.Created, internalTemplate.StatusCode);
        var (acmeInvoice, acmeInvoiceBody) = await CreateAsync(server, acme, "invoice", "Acme invoice", open: true);
        Assert.Equal(HttpStatusCode.Created, acmeInvoice.StatusCode);
        Assert.False(acmeInvoiceBody.GetProperty("isGlobal").GetBoolean());
        Assert.False(acmeInvoiceBody.GetProperty("allowTenantUse").GetBoolean());

        // 租户：自动先取本租户；全局作用域只取开放的全局模板；租户作用域不回退
        var (auto, autoBody) = await ResolveAsync(server, acme, "invoice");
        Assert.Equal(HttpStatusCode.OK, auto.StatusCode);
        Assert.Equal("Tenant", autoBody.GetProperty("resolvedScope").GetString());
        Assert.Equal("Auto", autoBody.GetProperty("requestedScope").GetString());
        Assert.Equal("Acme invoice", autoBody.GetProperty("templateName").GetString());
        Assert.Equal(acmeInvoiceBody.GetProperty("concurrencyStamp").GetString(), autoBody.GetProperty("concurrencyStamp").GetString());
        var (global, globalBody) = await ResolveAsync(server, acme, "invoice", "Global");
        Assert.Equal("Global", globalBody.GetProperty("resolvedScope").GetString());
        Assert.Equal("Global invoice", globalBody.GetProperty("templateName").GetString());
        Assert.Equal(HttpStatusCode.OK, global.StatusCode);
        var (fallback, fallbackBody) = await ResolveAsync(server, globex, "invoice");
        Assert.Equal(HttpStatusCode.OK, fallback.StatusCode);
        Assert.Equal("Global", fallbackBody.GetProperty("resolvedScope").GetString());
        var (tenantOnly, tenantOnlyBody) = await ResolveAsync(server, globex, "invoice", "Tenant");
        Assert.Equal(HttpStatusCode.NotFound, tenantOnly.StatusCode);
        Assert.Equal("PRINTING_NOTFOUND_002", tenantOnlyBody.GetProperty("code").GetString());
        var (closed, closedBody) = await ResolveAsync(server, acme, "internal");
        Assert.Equal(HttpStatusCode.NotFound, closed.StatusCode);
        Assert.Equal("PRINTING_NOTFOUND_002", closedBody.GetProperty("code").GetString());

        // 平台：自动与全局相同、不要求开放；平台没有租户作用域
        var (platformInternal, platformInternalBody) = await ResolveAsync(server, platform, "internal");
        Assert.Equal(HttpStatusCode.OK, platformInternal.StatusCode);
        Assert.Equal("Global", platformInternalBody.GetProperty("resolvedScope").GetString());
        var (platformTenant, platformTenantBody) = await ResolveAsync(server, platform, "invoice", "Tenant");
        Assert.Equal(HttpStatusCode.BadRequest, platformTenant.StatusCode);
        Assert.Equal("PRINTING_VALIDATION_004", platformTenantBody.GetProperty("code").GetString());

        // 停用租户模板：自动回退到全局模板（缓存里的租户模板随写入失效）
        var (disabled, disabledBody) = await SetStatusAsync(server, acme, acmeInvoiceBody, "Disabled");
        Assert.Equal(HttpStatusCode.OK, disabled.StatusCode);
        Assert.Equal("Disabled", disabledBody.GetProperty("status").GetString());
        Assert.NotEqual(acmeInvoiceBody.GetProperty("concurrencyStamp").GetString(), disabledBody.GetProperty("concurrencyStamp").GetString());
        var (afterDisable, afterDisableBody) = await ResolveAsync(server, acme, "invoice");
        Assert.Equal(HttpStatusCode.OK, afterDisable.StatusCode);
        Assert.Equal("Global", afterDisableBody.GetProperty("resolvedScope").GetString());

        // 改全局模板：别的租户立刻解析到新版本（平台这一项随写入失效）；带旧并发戳再改即冲突
        var globalId = globalInvoiceBody.GetProperty("id").GetString()!;
        var stamp = globalInvoiceBody.GetProperty("concurrencyStamp").GetString();
        var (renamed, renamedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/print-templates/{globalId}", JsonSerializer.Serialize(new { concurrencyStamp = stamp, templateName = "Global invoice v2", templateJson = Document("v2"), allowTenantUse = true, order = 5, remark = "second" }), platform));
        Assert.Equal(HttpStatusCode.OK, renamed.StatusCode);
        Assert.Equal("Global invoice v2", renamedBody.GetProperty("templateName").GetString());
        Assert.Contains("\"v2\"", renamedBody.GetProperty("templateJson").GetString(), StringComparison.Ordinal);
        var (afterRename, afterRenameBody) = await ResolveAsync(server, globex, "invoice");
        Assert.Equal(HttpStatusCode.OK, afterRename.StatusCode);
        Assert.Equal("Global invoice v2", afterRenameBody.GetProperty("templateName").GetString());
        Assert.Equal(renamedBody.GetProperty("concurrencyStamp").GetString(), afterRenameBody.GetProperty("concurrencyStamp").GetString());
        var (stale, staleBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/print-templates/{globalId}", JsonSerializer.Serialize(new { concurrencyStamp = stamp, templateName = "Lost update", templateJson = Document("x") }), platform));
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.StartsWith("PERSISTENCE_CONFLICT_", staleBody.GetProperty("code").GetString(), StringComparison.Ordinal);

        // 关掉全局模板的开放：租户再也解析不到
        var (closing, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/print-templates/{globalId}", JsonSerializer.Serialize(new { concurrencyStamp = renamedBody.GetProperty("concurrencyStamp").GetString(), templateName = "Global invoice v2", templateJson = Document("v2"), allowTenantUse = false }), platform));
        Assert.Equal(HttpStatusCode.OK, closing.StatusCode);
        var (afterClose, _) = await ResolveAsync(server, globex, "invoice");
        Assert.Equal(HttpStatusCode.NotFound, afterClose.StatusCode);

        // 列表：平台只有全局模板（全部状态，同排序号按编码），租户缺省本租户、全局作用域是启用且开放的；平台用租户作用域即校验失败
        var (platformList, platformListBody) = await ListAsync(server, platform);
        Assert.Equal(HttpStatusCode.OK, platformList.StatusCode);
        Assert.Equal(["internal", "invoice"], Codes(platformListBody));
        var (acmeList, acmeListBody) = await ListAsync(server, acme);
        Assert.Equal(["invoice"], Codes(acmeListBody));
        Assert.False(acmeListBody.GetProperty("items")[0].GetProperty("isGlobal").GetBoolean());
        Assert.False(acmeListBody.GetProperty("items")[0].TryGetProperty("templateJson", out _));
        var (openList, openListBody) = await ListAsync(server, acme, "Global");
        Assert.Equal(HttpStatusCode.OK, openList.StatusCode);
        Assert.Empty(Codes(openListBody));
        var (platformScoped, platformScopedBody) = await ListAsync(server, platform, "Tenant");
        Assert.Equal(HttpStatusCode.BadRequest, platformScoped.StatusCode);
        Assert.Equal("PRINTING_VALIDATION_004", platformScopedBody.GetProperty("code").GetString());

        // 详情：租户看不开放的全局模板即拒绝，别的租户的模板当作不存在；租户改不了全局模板
        var (closedGet, closedGetBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/print-templates/{internalBody.GetProperty("id").GetString()}", token: acme));
        Assert.Equal(HttpStatusCode.Forbidden, closedGet.StatusCode);
        Assert.Equal("PRINTING_DENIED_002", closedGetBody.GetProperty("code").GetString());
        var (foreignGet, foreignGetBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/print-templates/{acmeInvoiceBody.GetProperty("id").GetString()}", token: globex));
        Assert.Equal(HttpStatusCode.NotFound, foreignGet.StatusCode);
        Assert.Equal("PRINTING_NOTFOUND_001", foreignGetBody.GetProperty("code").GetString());
        var (ownGet, ownGetBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/print-templates/{acmeInvoiceBody.GetProperty("id").GetString()}", token: acme));
        Assert.Equal(HttpStatusCode.OK, ownGet.StatusCode);
        Assert.Contains("schemaVersion", ownGetBody.GetProperty("templateJson").GetString(), StringComparison.Ordinal);
        var (foreignUpdate, foreignUpdateBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/print-templates/{globalId}", JsonSerializer.Serialize(new { concurrencyStamp = "x", templateName = "Hijack", templateJson = Document("x") }), acme));
        Assert.Equal(HttpStatusCode.NotFound, foreignUpdate.StatusCode);
        Assert.Equal("PRINTING_NOTFOUND_001", foreignUpdateBody.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Templates_ValidateJsonDataSourcesAndCodes_AndDeleteOnlyDisabled()
    {
        await using var server = await StartAsync();
        var platform = TestTokens.Issue("root", null, Admin);
        var acme = TestTokens.Issue("alice", "acme", Admin);

        // 模板 JSON 的最小结构：合法 JSON、根是对象、schemaVersion 为 1、pages 是非空的对象数组
        foreach (var json in new[] { "not json", "[1]", """{"schemaVersion":2,"pages":[{}]}""", """{"schemaVersion":"1","pages":[{}]}""", """{"schemaVersion":1,"pages":[]}""", """{"schemaVersion":1,"pages":[1]}""" })
        {
            var (invalid, invalidBody) = await CreateAsync(server, platform, "bad", "Bad", json: json);
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
            Assert.Equal("PRINTING_VALIDATION_002", invalidBody.GetProperty("code").GetString());
        }

        // 数据源：没登记即拒绝，内置的可以绑；编码不含空白、本侧唯一
        var (unregistered, unregisteredBody) = await CreateAsync(server, platform, "bound", "Bound", dataSource: "nope.source");
        Assert.Equal(HttpStatusCode.BadRequest, unregistered.StatusCode);
        Assert.Equal("PRINTING_VALIDATION_003", unregisteredBody.GetProperty("code").GetString());
        var (bound, boundBody) = await CreateAsync(server, platform, "bound", "Bound", dataSource: BuiltInPrintDataSources.SystemPrintDemo.Code);
        Assert.Equal(HttpStatusCode.Created, bound.StatusCode);
        Assert.Equal("system.print-demo", boundBody.GetProperty("dataSourceCode").GetString());
        var (spaced, spacedBody) = await CreateAsync(server, platform, "has space", "Spaced");
        Assert.Equal(HttpStatusCode.BadRequest, spaced.StatusCode);
        Assert.True(spacedBody.GetProperty("errors").TryGetProperty("templateCode", out _));
        var (duplicate, duplicateBody) = await CreateAsync(server, platform, "bound", "Again");
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal("PRINTING_CONFLICT_001", duplicateBody.GetProperty("code").GetString());

        // 换成自由模板：数据源清空
        var (free, freeBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/print-templates/{boundBody.GetProperty("id").GetString()}", JsonSerializer.Serialize(new { concurrencyStamp = boundBody.GetProperty("concurrencyStamp").GetString(), templateName = "Bound", templateJson = Document("free"), dataSourceCode = (string?)null }), platform));
        Assert.Equal(HttpStatusCode.OK, free.StatusCode);
        Assert.Equal(JsonValueKind.Null, freeBody.GetProperty("dataSourceCode").ValueKind);

        // 删除：启用的逐项拒绝、别的一侧的当作不存在；停用后可删，同编码可以重建
        var id = freeBody.GetProperty("id").GetString()!;
        var (enabled, enabledBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Delete, $"/api/print-templates?ids={id}", token: platform));
        Assert.Equal(HttpStatusCode.OK, enabled.StatusCode);
        Assert.Equal("PRINTING_CONFLICT_002", enabledBody.GetProperty("failed")[0].GetProperty("code").GetString());
        var (foreign, foreignBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Delete, $"/api/print-templates?ids={id}", token: acme));
        Assert.Equal(HttpStatusCode.OK, foreign.StatusCode);
        Assert.Equal("PRINTING_NOTFOUND_001", foreignBody.GetProperty("failed")[0].GetProperty("code").GetString());
        var (disabled, _) = await SetStatusAsync(server, platform, freeBody, "Disabled");
        Assert.Equal(HttpStatusCode.OK, disabled.StatusCode);
        var (deleted, deletedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Delete, $"/api/print-templates?ids={id}", token: platform));
        Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);
        Assert.Equal(id, deletedBody.GetProperty("succeeded")[0].GetString());
        var (resolveDeleted, _) = await ResolveAsync(server, platform, "bound");
        Assert.Equal(HttpStatusCode.NotFound, resolveDeleted.StatusCode);
        var (recreated, _) = await CreateAsync(server, platform, "bound", "Bound again");
        Assert.Equal(HttpStatusCode.Created, recreated.StatusCode);
    }

    [Fact]
    public async Task GlobalTemplates_NeedGlobalManage_AndTheCatalogNeedsReadOrUse()
    {
        await using var server = await StartAsync();

        // 平台写全局模板另要全局管理；租户一侧不需要
        var (platformWriter, platformWriterBody) = await CreateAsync(server, TestTokens.Issue("pat", null, Writer), "receipt", "Receipt");
        Assert.Equal(HttpStatusCode.Forbidden, platformWriter.StatusCode);
        Assert.Equal("PRINTING_DENIED_001", platformWriterBody.GetProperty("code").GetString());
        var (tenantWriter, _) = await CreateAsync(server, TestTokens.Issue("tom", "acme", Writer), "receipt", "Receipt");
        Assert.Equal(HttpStatusCode.Created, tenantWriter.StatusCode);

        // 数据源目录：读取或使用其一即可，都没有即拒绝；演示开关关着只有内置数据源
        foreach (var role in new[] { User, Reader })
        {
            var (catalog, catalogBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/print-data-sources", token: TestTokens.Issue("ivy", "acme", role)));
            Assert.Equal(HttpStatusCode.OK, catalog.StatusCode);
            var source = Assert.Single(catalogBody.GetProperty("items").EnumerateArray());
            Assert.Equal("system.print-demo", source.GetProperty("code").GetString());
            var items = source.GetProperty("fields").EnumerateArray().Single(static f => f.GetProperty("key").GetString() == "items");
            Assert.Equal("table", items.GetProperty("kind").GetString());
            Assert.Equal(4, items.GetProperty("columns").GetArrayLength());
            Assert.Equal(JsonValueKind.Object, JsonDocument.Parse(source.GetProperty("sampleDataJson").GetString()!).RootElement.ValueKind);
        }

        var (guest, guestBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/print-data-sources", token: TestTokens.Issue("ivy", "acme", "guest")));
        Assert.Equal(HttpStatusCode.Forbidden, guest.StatusCode);
        Assert.Equal("SECURITY_DENIED_004", guestBody.GetProperty("code").GetString());
        var (anonymous, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/print-data-sources"));
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        // 只有使用权限：能解析，不能列表
        var (useOnly, _) = await ListAsync(server, TestTokens.Issue("ivy", "acme", User));
        Assert.Equal(HttpStatusCode.Forbidden, useOnly.StatusCode);
        var (resolved, _) = await ResolveAsync(server, TestTokens.Issue("ivy", "acme", User), "receipt");
        Assert.Equal(HttpStatusCode.OK, resolved.StatusCode);
    }

    [Fact]
    public async Task DemoData_WritesSixGlobalTemplatesAndFiveDataSources_Once()
    {
        await using var server = await StartAsync([new("BasicApp:Seed:EnableDemoData", "true")]);
        await server.RunSeedersAsync();
        var platform = TestTokens.Issue("root", null, Admin);

        var (list, listBody) = await ListAsync(server, platform);
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Equal(["demo.pickup-receipt", "demo.delivery-note", "demo.formats", "demo.inspection", "demo.outage-notice", "demo.element-overview"], Codes(listBody));
        var templates = listBody.GetProperty("items").EnumerateArray().ToList();
        Assert.All(templates, static t =>
        {
            Assert.True(t.GetProperty("isGlobal").GetBoolean());
            Assert.True(t.GetProperty("allowTenantUse").GetBoolean());
            Assert.Equal("Active", t.GetProperty("status").GetString());
        });
        Assert.Equal([10, 20, 30, 40, 50, 60], templates.Select(static t => t.GetProperty("order").GetInt32()));

        // 数据源：内置一个 + 演示五个，每张演示模板绑的数据源都在目录里
        var (catalog, catalogBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/print-data-sources", token: platform));
        Assert.Equal(HttpStatusCode.OK, catalog.StatusCode);
        var sources = catalogBody.GetProperty("items").EnumerateArray().Select(static s => s.GetProperty("code").GetString()).ToList();
        Assert.Equal(["demo.delivery-note", "demo.formats", "demo.inspection", "demo.outage-notice", "demo.pickup-receipt", "system.print-demo"], sources);
        Assert.All(templates, t => Assert.Contains(t.GetProperty("dataSourceCode").GetString(), sources));

        // 租户按编码解析到开放的全局模板，模板 JSON 是嵌入资源原样
        var (resolved, resolvedBody) = await ResolveAsync(server, TestTokens.Issue("alice", "acme", User), "demo.element-overview");
        Assert.Equal(HttpStatusCode.OK, resolved.StatusCode);
        Assert.Equal("Global", resolvedBody.GetProperty("resolvedScope").GetString());
        using (var document = JsonDocument.Parse(resolvedBody.GetProperty("templateJson").GetString()!))
        {
            Assert.Equal(1, document.RootElement.GetProperty("schemaVersion").GetInt32());
        }

        // 只首写：删掉一张再跑种子不补回
        var first = templates[0];
        var (disabled, disabledBody) = await SetStatusAsync(server, platform, first, "Disabled");
        Assert.Equal(HttpStatusCode.OK, disabled.StatusCode);
        var (deleted, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Delete, $"/api/print-templates?ids={disabledBody.GetProperty("id").GetString()}", token: platform));
        Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);
        await server.RunSeedersAsync();
        var (again, againBody) = await ListAsync(server, platform);
        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
        Assert.Equal(5, againBody.GetProperty("total").GetInt64());
    }

    [Fact]
    public async Task WithoutDemoData_NoTemplatesAreSeeded()
    {
        await using var server = await StartAsync();
        await server.RunSeedersAsync();
        var (list, listBody) = await ListAsync(server, TestTokens.Issue("root", null, Admin));
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Equal(0, listBody.GetProperty("total").GetInt64());
    }

    [Fact]
    public async Task PrintingModule_CanBeSwitchedOff()
    {
        await using var server = await BasicAppTestServer.StartAsync(_database.ConnectionString, settings: [new("BasicApp:Modules:Printing:Enabled", "false")]);
        await server.SeedDefaultsAsync();
        await server.RunSeedersAsync();
        var superadmin = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;
        var (permissions, permissionsBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:permissions", token: superadmin));
        Assert.Equal(HttpStatusCode.OK, permissions.StatusCode);
        Assert.DoesNotContain("printing.print-templates", permissionsBody.GetProperty("pages").EnumerateArray().Select(static p => p.GetString()));
        Assert.DoesNotContain(permissionsBody.GetProperty("permissions").EnumerateArray().Select(static p => p.GetString()), static p => p!.StartsWith("printing.", StringComparison.Ordinal));
        var (catalog, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/print-data-sources", token: superadmin));
        Assert.Equal(HttpStatusCode.NotFound, catalog.StatusCode);
        var (docs, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/openapi.json"));
        Assert.Equal(HttpStatusCode.OK, docs.StatusCode);

        await Assert.ThrowsAsync<ArgumentException>(() => BasicAppTestServer.StartAsync(_database.ConnectionString, settings: [new("BasicApp:Modules:Printing:Enabled", "maybe")]));
    }

    [Fact]
    public async Task PrintingModule_IsOnByDefault_WithItsPageAndPermissions()
    {
        await using var server = await BasicAppTestServer.StartAsync(_database.ConnectionString);
        await server.SeedDefaultsAsync();
        await server.RunSeedersAsync();
        var superadmin = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;
        var (permissions, permissionsBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:permissions", token: superadmin));
        Assert.Equal(HttpStatusCode.OK, permissions.StatusCode);
        Assert.Contains("printing.print-templates", permissionsBody.GetProperty("pages").EnumerateArray().Select(static p => p.GetString()));
        Assert.Contains(PrintingPermissions.PrintTemplatesGlobalManage, permissionsBody.GetProperty("permissions").EnumerateArray().Select(static p => p.GetString()));
        var page = MenuTrees.Find(permissionsBody.GetProperty("menus"), "printing.print-templates");
        Assert.Equal("/printing/print-templates", page.GetProperty("route").GetString());
        Assert.Equal("printing/print-templates/index", page.GetProperty("component").GetString());
    }

    [Fact]
    public async Task DataSourceRegistry_RejectsDuplicateCodesAndInvalidContracts_AtStartup()
    {
        var duplicate = await Assert.ThrowsAnyAsync<Exception>(() => BasicAppTestServer.StartAsync(_database.ConnectionString, configure: static builder => builder.Services.AddPrintDataSource(BuiltInPrintDataSources.SystemPrintDemo)));
        Assert.Contains("system.print-demo", duplicate.ToString(), StringComparison.Ordinal);

        var invalid = new PrintDataSourceDefinition("demo.bad", "Bad", [new PrintDataSourceField("chart", "Chart", "chart")], "{}");
        var badKind = await Assert.ThrowsAnyAsync<Exception>(() => BasicAppTestServer.StartAsync(_database.ConnectionString, configure: builder => builder.Services.AddPrintDataSource(invalid)));
        Assert.Contains("unknown kind 'chart'", badKind.ToString(), StringComparison.Ordinal);

        var columns = new PrintDataSourceDefinition("demo.columns", "Columns", [new PrintDataSourceField("name", "Name", Columns: [new PrintDataSourceTableColumn("x", "X")])], "[]");
        var stray = await Assert.ThrowsAnyAsync<Exception>(() => BasicAppTestServer.StartAsync(_database.ConnectionString, configure: builder => builder.Services.AddPrintDataSource(columns)));
        Assert.Contains("cannot declare table columns", stray.ToString(), StringComparison.Ordinal);
    }
}
