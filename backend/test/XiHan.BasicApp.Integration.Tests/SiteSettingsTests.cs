// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.BasicApp.Settings.Domain.Services;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证站点设置与租户品牌
/// </summary>
/// <remarks>
/// 平台的站点设置是内建参数 <c>settings.site</c>（没改即缺省值），租户的同键参数是它的品牌、只写要改的字段；
/// <c>site.resolve</c> 匿名：登录了按令牌里的租户，没登录按访问的域名找绑定了它的租户（只用于展示与预填）；
/// 图片经宿主端点上传、存平台侧、匿名可读，换掉或去掉即回收
/// </remarks>
public sealed class SiteSettingsTests : IDisposable
{
    private static readonly byte[] _png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52];

    private readonly TestDatabase _database = new();
    private readonly string _root = Path.Combine(Path.GetTempPath(), "xihan-site-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        _database.Dispose();
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private Task<(BasicAppTestServer Server, string Platform, string Tenant)> StartAsync()
        => SettingsTestHost.StartAsync(_database.ConnectionString, configure: builder => builder.Configuration.AddInMemoryCollection([new("BasicApp:Files:LocalRootPath", _root)]));

    private static async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(BasicAppTestServer server, HttpMethod method, string path, object? body, string? token)
    {
        var (response, json) = await server.SendAsync(BasicAppTestServer.Request(method, path, body is null ? null : JsonSerializer.Serialize(body), token, "en-US"));
        return (response.StatusCode, json);
    }

    private static async Task<(HttpStatusCode Status, JsonElement Body)> UploadAsync(BasicAppTestServer server, string slot, byte[] bytes, string? token)
    {
        var request = BasicAppTestServer.Request(HttpMethod.Post, $"/api/site/assets/{slot}", token: token, acceptLanguage: "en-US");
        var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        request.Content = new MultipartFormDataContent { { content, "file", "image.bin" } };
        var (response, json) = await server.SendAsync(request);
        return (response.StatusCode, json);
    }

    private static async Task<string> TenantKeyAsync(BasicAppTestServer server, string platform, string tenantId)
    {
        var (_, list) = await SendAsync(server, HttpMethod.Post, "/api/tenants:list", new { keyword = tenantId }, platform);
        return list.GetProperty("items").EnumerateArray().Single(t => t.GetProperty("tenantId").GetString() == tenantId).GetProperty("id").GetString()!;
    }

    private static Task<(HttpStatusCode Status, JsonElement Body)> BindDomainAsync(BasicAppTestServer server, string platform, string id, string name, string? domain)
        => SendAsync(server, HttpMethod.Put, $"/api/tenants/{id}", new { name, domain }, platform);

    [Fact]
    public async Task PlatformSettings_StartAtTheDefaults_AndTenantsOverrideOnlyWhatTheySet()
    {
        var (server, platform, tenant) = await StartAsync();
        await using var running = server;

        // 没改过：缺省值，匿名就能取
        var (anonymous, resolved) = await SendAsync(server, HttpMethod.Get, "/api/site:resolve", null, null);
        Assert.Equal(HttpStatusCode.OK, anonymous);
        Assert.Equal(("XiHan.BasicApp", "XiHanFun", "2016"), (resolved.GetProperty("profile").GetProperty("name").GetString(), resolved.GetProperty("profile").GetProperty("copyrightOwner").GetString(), resolved.GetProperty("profile").GetProperty("copyrightSince").GetString()));
        Assert.Equal(JsonValueKind.Null, resolved.GetProperty("tenantId").ValueKind);
        Assert.Equal(SiteSettings.Default, (await SettingsTestHost.InvokeAsync(server, null, async ctx => (await ctx.Services.GetRequiredService<SiteProfiles>().OwnAsync("platform", ctx.CancellationToken)).ValueOrThrow())));

        // 平台改站点设置：写错的链接拒绝，改好后匿名取到新的
        var (badLink, badLinkBody) = await SendAsync(server, HttpMethod.Put, "/api/site", new { name = "Acme Cloud", copyrightUrl = "javascript:alert(1)" }, platform);
        Assert.Equal((HttpStatusCode.BadRequest, "SETTINGS_VALIDATION_011"), (badLink, badLinkBody.GetProperty("code").GetString()));
        var (updated, updatedBody) = await SendAsync(server, HttpMethod.Put, "/api/site", new { name = "  Acme Cloud ", subtitle = "One console", copyrightOwner = "Acme", copyrightUrl = "https://acme.test", icp = "ICP-1" }, platform);
        Assert.True(updated == HttpStatusCode.OK, updatedBody.ToString());
        Assert.True(updatedBody.GetProperty("isPlatform").GetBoolean());
        Assert.Equal("Acme Cloud", updatedBody.GetProperty("own").GetProperty("name").GetString());
        Assert.Equal(JsonValueKind.Null, updatedBody.GetProperty("own").GetProperty("copyrightSince").ValueKind);
        Assert.Equal("Acme Cloud", (await SendAsync(server, HttpMethod.Get, "/api/site:resolve", null, null)).Body.GetProperty("profile").GetProperty("name").GetString());

        // 租户：自己没设的字段为空，另带沿用的平台设置
        var (got, gotBody) = await SendAsync(server, HttpMethod.Get, "/api/site", null, tenant);
        Assert.Equal(HttpStatusCode.OK, got);
        Assert.False(gotBody.GetProperty("isPlatform").GetBoolean());
        Assert.Equal(JsonValueKind.Null, gotBody.GetProperty("own").GetProperty("name").ValueKind);
        Assert.Equal("Acme Cloud", gotBody.GetProperty("inherited").GetProperty("name").GetString());

        // 租户改自己的品牌：只覆盖设了的字段，平台的设置不受影响
        var (branded, brandedBody) = await SendAsync(server, HttpMethod.Put, "/api/site", new { name = "Contoso Portal", subtitle = "" }, tenant);
        Assert.True(branded == HttpStatusCode.OK, brandedBody.ToString());
        var mine = (await SendAsync(server, HttpMethod.Get, "/api/site:resolve", null, tenant)).Body.GetProperty("profile");
        Assert.Equal(("Contoso Portal", "One console", "Acme", "ICP-1"), (mine.GetProperty("name").GetString(), mine.GetProperty("subtitle").GetString(), mine.GetProperty("copyrightOwner").GetString(), mine.GetProperty("icp").GetString()));
        Assert.Equal("Acme Cloud", (await SendAsync(server, HttpMethod.Get, "/api/site:resolve", null, platform)).Body.GetProperty("profile").GetProperty("name").GetString());

        // 参数配置页改同一个参数：写错的值保存即拒绝
        var (wrong, wrongBody) = await SettingsTestHost.SetBuiltInConfigAsync(server, platform, SiteSettings.ConfigKey, """{"name":123}""");
        Assert.Equal((HttpStatusCode.BadRequest, "SETTINGS_VALIDATION_011"), (wrong, wrongBody.GetProperty("code").GetString()));

        // 没有权限的租户成员改不了
        await SendAsync(server, HttpMethod.Post, "/api/users", new { userName = "viewer", displayName = "viewer", password = "Viewer@123456" }, tenant);
        var viewer = (await AuthFlowTests.LoginAsync(server, "viewer", "Viewer@123456", "contoso")).GetProperty("accessToken").GetString()!;
        Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(server, HttpMethod.Put, "/api/site", new { name = "Mine" }, viewer)).Status);
        Assert.Equal("Contoso Portal", (await SendAsync(server, HttpMethod.Get, "/api/site:resolve", null, viewer)).Body.GetProperty("profile").GetProperty("name").GetString());
    }

    [Fact]
    public async Task SignedOutVisitors_SeeTheBrandOfTheTenantBoundToTheDomain_ForDisplayOnly()
    {
        var (server, platform, tenant) = await StartAsync();
        await using var running = server;
        await SendAsync(server, HttpMethod.Put, "/api/site", new { name = "Contoso Portal" }, tenant);
        var contoso = await TenantKeyAsync(server, platform, "contoso");

        // 平台给租户绑定域名：去空白、小写、去末尾的点
        var (bound, boundBody) = await BindDomainAsync(server, platform, contoso, "Contoso", " Portal.Contoso.Test. ");
        Assert.True(bound == HttpStatusCode.OK, boundBody.ToString());
        Assert.Equal("portal.contoso.test", boundBody.GetProperty("domain").GetString());

        // 没登录按访问的主机名（可带端口）找到租户：显示它的品牌、预填租户 ID；没绑定的域名即站点设置
        var (_, byDomain) = await SendAsync(server, HttpMethod.Get, "/api/site:resolve?host=PORTAL.contoso.test:9100", null, null);
        Assert.Equal(("Contoso Portal", "contoso"), (byDomain.GetProperty("profile").GetProperty("name").GetString(), byDomain.GetProperty("tenantId").GetString()));
        var (_, other) = await SendAsync(server, HttpMethod.Get, "/api/site:resolve?host=www.example.test", null, null);
        Assert.Equal(("XiHan.BasicApp", JsonValueKind.Null), (other.GetProperty("profile").GetProperty("name").GetString(), other.GetProperty("tenantId").ValueKind));

        // 登录了只认令牌：平台主体拿着租户的域名访问看到的仍是站点设置，也不预填
        var (_, signedIn) = await SendAsync(server, HttpMethod.Get, "/api/site:resolve?host=portal.contoso.test", null, platform);
        Assert.Equal(("XiHan.BasicApp", JsonValueKind.Null), (signedIn.GetProperty("profile").GetProperty("name").GetString(), signedIn.GetProperty("tenantId").ValueKind));

        // 一个域名只能绑一个租户
        var (created, createdBody) = await SendAsync(server, HttpMethod.Post, "/api/tenants", new { tenantId = "fabrikam", name = "Fabrikam", domain = "portal.contoso.test" }, platform);
        Assert.Equal((HttpStatusCode.Conflict, "TENANCY_CONFLICT_018"), (created, createdBody.GetProperty("code").GetString()));
        var (invalid, _) = await SendAsync(server, HttpMethod.Post, "/api/tenants", new { tenantId = "fabrikam", name = "Fabrikam", domain = "not a domain" }, platform);
        Assert.Equal(HttpStatusCode.BadRequest, invalid);

        // 停用的租户不再按域名显示；解绑后同样不显示
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, "/api/tenants:suspend", new { ids = new[] { contoso } }, platform)).Status);
        Assert.Equal(JsonValueKind.Null, (await SendAsync(server, HttpMethod.Get, "/api/site:resolve?host=portal.contoso.test", null, null)).Body.GetProperty("tenantId").ValueKind);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, "/api/tenants:resume", new { ids = new[] { contoso } }, platform)).Status);
        Assert.Equal("contoso", (await SendAsync(server, HttpMethod.Get, "/api/site:resolve?host=portal.contoso.test", null, null)).Body.GetProperty("tenantId").GetString());
        var (unbound, unboundBody) = await BindDomainAsync(server, platform, contoso, "Contoso", null);
        Assert.Equal((HttpStatusCode.OK, JsonValueKind.Null), (unbound, unboundBody.GetProperty("domain").ValueKind));
        Assert.Equal(JsonValueKind.Null, (await SendAsync(server, HttpMethod.Get, "/api/site:resolve?host=portal.contoso.test", null, null)).Body.GetProperty("tenantId").ValueKind);
    }

    [Fact]
    public async Task Images_AreUploadedPerSlot_ServedAnonymously_AndReleasedWhenReplacedOrRemoved()
    {
        var (server, platform, tenant) = await StartAsync();
        await using var running = server;

        // 只收图片：不认的内容拒绝；没有权限拒绝
        var (text, textBody) = await UploadAsync(server, "logo", "hello"u8.ToArray(), tenant);
        Assert.Equal((HttpStatusCode.BadRequest, "SETTINGS_VALIDATION_013"), (text, textBody.GetProperty("code").GetString()));
        Assert.Equal(HttpStatusCode.Unauthorized, (await UploadAsync(server, "logo", _png, null)).Status);
        Assert.Equal(HttpStatusCode.BadRequest, (await UploadAsync(server, "banner", _png, tenant)).Status);

        // 租户换 Logo：存进自己的品牌，匿名可读、可长期缓存
        var (uploaded, uploadedBody) = await UploadAsync(server, "logo", _png, tenant);
        Assert.True(uploaded == HttpStatusCode.OK, uploadedBody.ToString());
        var logo = uploadedBody.GetProperty("own").GetProperty("logo");
        var url = logo.GetProperty("url").GetString()!;
        Assert.Equal($"/api/site/assets/{logo.GetProperty("id").GetString()}", url);
        var served = await server.Client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, served.StatusCode);
        Assert.Equal("image/png", served.Content.Headers.ContentType?.MediaType);
        Assert.Contains("immutable", served.Headers.CacheControl?.ToString(), StringComparison.Ordinal);
        Assert.Equal("default-src 'none'; sandbox", served.Headers.GetValues("Content-Security-Policy").Single());
        Assert.Equal(_png, await served.Content.ReadAsByteArrayAsync());
        Assert.Equal(url, (await SendAsync(server, HttpMethod.Get, "/api/site:resolve", null, tenant)).Body.GetProperty("profile").GetProperty("logo").GetProperty("url").GetString());

        // 别的文件不能当站点图片读，也不能写进设置
        var otherFile = await UploadFileAsync(server, platform);
        Assert.Equal(HttpStatusCode.NotFound, (await server.Client.GetAsync($"/api/site/assets/{otherFile}")).StatusCode);
        // 通用上传自己填站点标签、公开级别与 HTML 内容类型：也不经这个匿名同源地址输出
        var page = await UploadFileAsync(server, platform, "<script>alert(1)</script>"u8.ToArray(), "text/html", "page.html", [("tags", "site"), ("accessLevel", "Public")]);
        Assert.Equal(HttpStatusCode.NotFound, (await server.Client.GetAsync($"/api/site/assets/{page}")).StatusCode);
        var (foreign, foreignBody) = await SendAsync(server, HttpMethod.Put, "/api/site", new { logo = otherFile }, tenant);
        Assert.Equal((HttpStatusCode.BadRequest, "SETTINGS_VALIDATION_012"), (foreign, foreignBody.GetProperty("code").GetString()));

        // 再换一次：旧图回收；保存时保留现在的图片照旧
        var (replaced, replacedBody) = await UploadAsync(server, "logo", _png, tenant);
        Assert.Equal(HttpStatusCode.OK, replaced);
        var next = replacedBody.GetProperty("own").GetProperty("logo").GetProperty("id").GetString()!;
        Assert.Equal(HttpStatusCode.NotFound, (await server.Client.GetAsync(url)).StatusCode);
        var (kept, keptBody) = await SendAsync(server, HttpMethod.Put, "/api/site", new { name = "Contoso", logo = next }, tenant);
        Assert.Equal((HttpStatusCode.OK, next), (kept, keptBody.GetProperty("own").GetProperty("logo").GetProperty("id").GetString()));
        Assert.Equal(HttpStatusCode.OK, (await server.Client.GetAsync($"/api/site/assets/{next}")).StatusCode);

        // 去掉：回收，生效的沿用平台的（平台没设即没有）
        var (removed, removedBody) = await SendAsync(server, HttpMethod.Put, "/api/site", new { name = "Contoso" }, tenant);
        Assert.Equal((HttpStatusCode.OK, JsonValueKind.Null), (removed, removedBody.GetProperty("own").GetProperty("logo").ValueKind));
        Assert.Equal(HttpStatusCode.NotFound, (await server.Client.GetAsync($"/api/site/assets/{next}")).StatusCode);
        Assert.Equal(JsonValueKind.Null, (await SendAsync(server, HttpMethod.Get, "/api/site:resolve", null, tenant)).Body.GetProperty("profile").GetProperty("logo").ValueKind);
    }

    private static Task<string> UploadFileAsync(BasicAppTestServer server, string token)
        => UploadFileAsync(server, token, _png, "image/png", "plain.png", []);

    private static async Task<string> UploadFileAsync(BasicAppTestServer server, string token, byte[] bytes, string contentType, string fileName, (string Name, string Value)[] fields)
    {
        var request = BasicAppTestServer.Request(HttpMethod.Post, "/api/files/upload", token: token);
        var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        var form = new MultipartFormDataContent { { content, "file", fileName } };
        foreach (var (name, value) in fields)
        {
            form.Add(new StringContent(value), name);
        }

        request.Content = form;
        var (response, body) = await server.SendAsync(request);
        Assert.True(response.StatusCode == HttpStatusCode.OK, body.ToString());
        return body.GetProperty("id").GetString()!;
    }
}
