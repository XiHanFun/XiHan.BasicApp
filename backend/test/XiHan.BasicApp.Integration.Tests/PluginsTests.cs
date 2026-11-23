// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.BasicApp.Plugins;
using XiHan.BasicApp.Plugins.Domain.Packages;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证插件包的静态核对与插件管理操作
/// </summary>
/// <remarks>
/// 包是测试里手工拼的 zip，不起插件进程。核对：包大小、读不了的包与不安全的条目、旁挂清单版本与未知字段、隔离级别、管道中间件、短名文法与保留名、当前平台的入口、
/// 10 种区域文案、操作资源段、权限码、页面、回调与扩展点；管理：核对给出的授予、安装时必须项缺一不可、清单 ID 与短名已装即冲突、整组替换授予带并发戳、
/// 删除后能重装；插件管理只在平台一侧；模块开关关掉后操作与权限码都不出现
/// </remarks>
public sealed class PluginsTests : IDisposable
{
    internal const string Name = "weather";
    internal const string ManifestId = "xihan.contoso.weather";
    internal const string Read = "weather.weather.read";

    private readonly TestDatabase _database = new();
    private readonly string _root = Path.Combine(Path.GetTempPath(), "xihan-plugins-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        _database.Dispose();
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private async Task<(BasicAppTestServer Server, string Platform)> StartAsync(IReadOnlyList<KeyValuePair<string, string?>>? settings = null)
    {
        var server = await BasicAppTestServer.StartAsync(_database.ConnectionString, settings: [new("BasicApp:Files:LocalRootPath", _root), .. settings ?? []]);
        await server.SeedDefaultsAsync();
        await server.RunSeedersAsync();
        var platform = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;
        return (server, platform);
    }

    /// <summary>
    /// 合规的框架清单
    /// </summary>
    /// <remarks>
    /// 进程外插件，读一个配置节、要一个缓存绑定，贡献两个操作与一个权限码
    /// </remarks>
    private static JsonObject Manifest() => JsonNode.Parse($$"""
        {
          "id": "{{ManifestId}}",
          "version": "1.0.0",
          "primitives": "1",
          "stability": "experimental",
          "kind": "plugin",
          "isolation": "out-of-process",
          "requires": [ { "id": "kernel.plugins", "version": "1" } ],
          "bindings": [ { "name": "cache", "type": "cache:distributed" } ],
          "entry": "{{Name}}",
          "contributes": {
            "operations": [ "weather.get", "weather-alerts.list" ],
            "permissions": [ "{{Read}}" ],
            "config": { "section": "XiHan:Weather" }
          }
        }
        """)!.AsObject();

    /// <summary>
    /// 合规的旁挂清单：当前平台的运行时、一个权限码、一个声明式页面
    /// </summary>
    private static JsonObject Descriptor() => JsonNode.Parse($$"""
        {
          "pluginApi": "1",
          "name": "{{Name}}",
          "publisher": "Contoso",
          "runtimes": [ "{{RuntimeInformation.RuntimeIdentifier}}" ],
          "permissions": [ { "code": "{{Read}}", "scope": "Tenant" } ],
          "pages": [
            {
              "code": "weather.forecast",
              "route": "/plugins/weather/forecast",
              "titleKey": "weather.pages.forecast",
              "icon": "cloud",
              "order": 10,
              "permission": "{{Read}}",
              "view": { "kind": "schema", "file": "pages/forecast.json" }
            }
          ]
        }
        """)!.AsObject();

    /// <summary>
    /// 包里清单之外的文件：当前平台的可执行文件、10 种区域文案与页面描述
    /// </summary>
    private static Dictionary<string, byte[]> Files()
    {
        var files = new Dictionary<string, byte[]>(StringComparer.Ordinal)
        {
            [$"runtimes/{RuntimeInformation.RuntimeIdentifier}/{Name}"] = [0x7F, 0x45, 0x4C, 0x46],
            ["pages/forecast.json"] = Encoding.UTF8.GetBytes("""{"columns":[]}"""),
        };
        foreach (var locale in PluginInspector.Locales)
        {
            files[$"locales/{locale}.json"] = Encoding.UTF8.GetBytes("""{"weather":{"pages":{"forecast":"Forecast"}}}""");
        }

        return files;
    }

    /// <summary>
    /// 拼一个插件包
    /// </summary>
    /// <remarks>
    /// 三个回调各改清单、旁挂清单与文件
    /// </remarks>
    internal static byte[] Package(Action<JsonObject>? manifest = null, Action<JsonObject>? descriptor = null, Action<Dictionary<string, byte[]>>? files = null)
    {
        var manifestJson = Manifest();
        manifest?.Invoke(manifestJson);
        var descriptorJson = Descriptor();
        descriptor?.Invoke(descriptorJson);
        var entries = Files();
        entries["xihan.manifest.json"] = Encoding.UTF8.GetBytes(manifestJson.ToJsonString());
        entries["basicapp.plugin.json"] = Encoding.UTF8.GetBytes(descriptorJson.ToJsonString());
        files?.Invoke(entries);
        using var buffer = new MemoryStream();
        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (path, content) in entries)
            {
                using var stream = archive.CreateEntry(path).Open();
                stream.Write(content);
            }
        }

        return buffer.ToArray();
    }

    internal static async Task<string> UploadAsync(BasicAppTestServer server, string token, byte[] content)
    {
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        form.Add(file, "file", "weather.xhplugin");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/files/upload") { Content = form };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var (uploaded, body) = await server.SendAsync(request);
        Assert.True(uploaded.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created, body.GetRawText());
        return body.GetProperty("id").GetString()!;
    }

    private static Task<(HttpResponseMessage Response, JsonElement Body)> InspectAsync(BasicAppTestServer server, string token, string fileId)
        => server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/plugins:inspect?fileId={fileId}", token: token));

    internal static Task<(HttpResponseMessage Response, JsonElement Body)> InstallAsync(BasicAppTestServer server, string token, string fileId, params string[] grants)
        => server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/plugins:install", JsonSerializer.Serialize(new { fileId, grants, remark = "weather plugin" }), token));

    private static Task<(HttpResponseMessage Response, JsonElement Body)> GrantAsync(BasicAppTestServer server, string token, string id, string stamp, params string[] grants)
        => server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/plugins/{id}:grant", JsonSerializer.Serialize(new { concurrencyStamp = stamp, grants }), token));

    private static Task<(HttpResponseMessage Response, JsonElement Body)> GetAsync(BasicAppTestServer server, string token, string id)
        => server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/plugins/{id}", token: token));

    private static string[] Strings(JsonElement array) => [.. array.EnumerateArray().Select(static e => e.GetString()!)];

    [Fact]
    public async Task Plugins_InspectInstallGrantAndDelete()
    {
        var (server, platform) = await StartAsync();
        await using var _ = server;
        var package = Package();
        var fileId = await UploadAsync(server, platform, package);

        // 核对：插件代码不跑，给出贡献与申请的授予（配置节、绑定必须批准，看全部数据可选）
        var (inspected, inspection) = await InspectAsync(server, platform, fileId);
        Assert.True(inspected.StatusCode == HttpStatusCode.OK, inspection.GetRawText());
        Assert.Equal((ManifestId, Name, "1.0.0", "Contoso", "OutOfProcess"), (inspection.GetProperty("manifestId").GetString(), inspection.GetProperty("name").GetString(), inspection.GetProperty("version").GetString(), inspection.GetProperty("publisher").GetString(), inspection.GetProperty("isolation").GetString()));
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(package)), inspection.GetProperty("sha256").GetString());
        Assert.Equal(package.Length, inspection.GetProperty("size").GetInt64());
        Assert.Equal(["binding:cache", "config:XiHan:Weather"], Strings(inspection.GetProperty("requiredGrants")));
        Assert.Equal([PluginInspector.DataScopeAll], Strings(inspection.GetProperty("optionalGrants")));
        var contributions = inspection.GetProperty("contributions");
        Assert.Equal(["weather-alerts.list", "weather.get"], Strings(contributions.GetProperty("operations")));
        var page = Assert.Single(contributions.GetProperty("pages").EnumerateArray());
        Assert.Equal(("weather.forecast", "/plugins/weather/forecast", Read), (page.GetProperty("code").GetString(), page.GetProperty("route").GetString(), page.GetProperty("permission").GetString()));
        Assert.Equal("Tenant", Assert.Single(contributions.GetProperty("permissions").EnumerateArray()).GetProperty("scope").GetString());

        // 必须项缺一不可，不认识的授予直接拒
        var (missing, missingBody) = await InstallAsync(server, platform, fileId, "config:XiHan:Weather");
        Assert.Equal(HttpStatusCode.Forbidden, missing.StatusCode);
        Assert.Equal(PluginsErrors.GrantsMissing, missingBody.GetProperty("code").GetString());
        var (unknown, unknownBody) = await InstallAsync(server, platform, fileId, "binding:cache", "config:XiHan:Weather", "binding:database");
        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
        Assert.Equal(PluginsErrors.GrantInvalid, unknownBody.GetProperty("code").GetString());

        var (installed, installedBody) = await InstallAsync(server, platform, fileId, "config:XiHan:Weather", "binding:cache", PluginInspector.DataScopeAll, "binding:cache");
        Assert.True(installed.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created, installedBody.GetRawText());
        var plugin = installedBody.GetProperty("plugin");
        var id = plugin.GetProperty("id").GetString()!;
        Assert.False(plugin.GetProperty("isEnabled").GetBoolean());
        Assert.Equal((ManifestId, Name, "1.0.0", "weather plugin"), (plugin.GetProperty("manifestId").GetString(), plugin.GetProperty("name").GetString(), plugin.GetProperty("version").GetString(), plugin.GetProperty("remark").GetString()));

        var (got, detail) = await GetAsync(server, platform, id);
        Assert.Equal(HttpStatusCode.OK, got.StatusCode);
        Assert.Equal(["binding:cache", "config:XiHan:Weather", PluginInspector.DataScopeAll], Strings(detail.GetProperty("grants")));
        var version = Assert.Single(detail.GetProperty("versions").EnumerateArray());
        Assert.Equal((fileId, true), (version.GetProperty("fileId").GetString(), version.GetProperty("isCurrent").GetBoolean()));
        Assert.Equal(["weather-alerts.list", "weather.get"], Strings(detail.GetProperty("contributions").GetProperty("operations")));

        var (listed, list) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/plugins:list", $$"""{"page":{"index":1,"size":20},"filters":[{"field":"name","operator":"contains","value":"wea"}],"sorts":[{"field":"createdAt","direction":"desc"}]}""", platform));
        Assert.True(listed.StatusCode == HttpStatusCode.OK, list.GetRawText());
        Assert.Equal(id, Assert.Single(list.GetProperty("items").EnumerateArray()).GetProperty("id").GetString());
        var (unsupported, unsupportedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/plugins:list", """{"page":{"index":1,"size":20},"filters":[{"field":"remark","operator":"eq","value":"x"}]}""", platform));
        Assert.Equal(HttpStatusCode.BadRequest, unsupported.StatusCode);
        Assert.Equal(PluginsErrors.QueryUnsupported, unsupportedBody.GetProperty("code").GetString());

        // 清单 ID 或短名已装：核对与安装都冲突
        var (again, againBody) = await InspectAsync(server, platform, fileId);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal(PluginsErrors.AlreadyInstalled, againBody.GetProperty("code").GetString());
        var renamed = await UploadAsync(server, platform, Package(manifest: static m => m["id"] = "xihan.fabrikam.weather"));
        var (sameName, sameNameBody) = await InstallAsync(server, platform, renamed, "binding:cache", "config:XiHan:Weather");
        Assert.Equal(HttpStatusCode.Conflict, sameName.StatusCode);
        Assert.Equal(PluginsErrors.AlreadyInstalled, sameNameBody.GetProperty("code").GetString());

        // 授予整组替换：并发戳过期即冲突，必须项照样缺一不可
        var stamp = detail.GetProperty("plugin").GetProperty("concurrencyStamp").GetString()!;
        var (stale, _) = await GrantAsync(server, platform, id, "stale", "binding:cache", "config:XiHan:Weather");
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        var (short_, shortBody) = await GrantAsync(server, platform, id, stamp, "binding:cache");
        Assert.Equal(HttpStatusCode.Forbidden, short_.StatusCode);
        Assert.Equal(PluginsErrors.GrantsMissing, shortBody.GetProperty("code").GetString());
        var (granted, grantedBody) = await GrantAsync(server, platform, id, stamp, "config:XiHan:Weather", "binding:cache");
        Assert.True(granted.StatusCode == HttpStatusCode.OK, grantedBody.GetRawText());
        Assert.NotEqual(stamp, grantedBody.GetProperty("plugin").GetProperty("concurrencyStamp").GetString());
        var (_, regranted) = await GetAsync(server, platform, id);
        Assert.Equal(["binding:cache", "config:XiHan:Weather"], Strings(regranted.GetProperty("grants")));

        // 删除：批量结果；删后查不到，同一个包能重装
        var (deleted, deletedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Delete, $"/api/plugins?ids={id}&ids={Guid.NewGuid()}", token: platform));
        Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);
        Assert.Equal(id, Assert.Single(deletedBody.GetProperty("succeeded").EnumerateArray()).GetString());
        Assert.Equal(PluginsErrors.PluginNotFound, Assert.Single(deletedBody.GetProperty("failed").EnumerateArray()).GetProperty("code").GetString());
        var (gone, goneBody) = await GetAsync(server, platform, id);
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
        Assert.Equal(PluginsErrors.PluginNotFound, goneBody.GetProperty("code").GetString());
        var (reinstalled, reinstalledBody) = await InstallAsync(server, platform, fileId, "binding:cache", "config:XiHan:Weather");
        Assert.True(reinstalled.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created, reinstalledBody.GetRawText());
        Assert.NotEqual(id, reinstalledBody.GetProperty("plugin").GetProperty("id").GetString());

        // 插件管理只在平台一侧
        var tenant = TestTokens.Issue("root", "acme", "admin");
        var (denied, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/plugins:list", """{"page":{"index":1,"size":20}}""", tenant));
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
    }

    [Fact]
    public async Task Inspect_RejectsPackagesThatBreakTheRules()
    {
        var (server, platform) = await StartAsync([new("BasicApp:Plugins:MaxPackageSizeMegabytes", "1")]);
        await using var _ = server;
        var rid = RuntimeInformation.RuntimeIdentifier;
        var cases = new (string Case, byte[] Package, string Code, string? Detail)[]
        {
            ("too large", new byte[(1024 * 1024) + 1], PluginsErrors.PackageInvalid, "max=1"),
            ("not a zip", Encoding.UTF8.GetBytes("not a zip"), PluginsErrors.PackageInvalid, null),
            ("unsafe entry", Package(files: static f => f["../escape.txt"] = [1]), PluginsErrors.PackageInvalid, "entry=../escape.txt"),
            ("no descriptor", Package(files: static f => f.Remove("basicapp.plugin.json")), PluginsErrors.PackageInvalid, "entry=basicapp.plugin.json"),
            ("unknown descriptor member", Package(descriptor: static d => d["entryPoint"] = "x"), PluginsErrors.DescriptorInvalid, "path=$.entryPoint"),
            ("plugin api", Package(descriptor: static d => d["pluginApi"] = "2"), PluginsErrors.DescriptorInvalid, "version=2"),
            ("wasm not allowed", Package(manifest: static m => m["isolation"] = "wasm"), PluginsErrors.ManifestRejected, "isolation=wasm"),
            ("pipeline", Package(manifest: static m => m["contributes"]!["pipeline"] = JsonNode.Parse("""[{"middleware":"Weather.Gate","stage":"authorize"}]""")), PluginsErrors.ManifestRejected, null),
            ("bad name", Package(descriptor: static d => d["name"] = "Weather_1"), PluginsErrors.NameInvalid, "name=Weather_1"),
            ("reserved name", Package(descriptor: static d => d["name"] = "installed"), PluginsErrors.NameInvalid, "name=installed"),
            ("built-in package name", Package(descriptor: static d => d["name"] = "files"), PluginsErrors.NameInvalid, "name=files"),
            ("runtime not listed", Package(descriptor: static d => d["runtimes"] = new JsonArray("other-x64")), PluginsErrors.PackageInvalid, $"runtime={rid}"),
            ("entry missing", Package(files: f => f.Remove($"runtimes/{rid}/{Name}")), PluginsErrors.PackageInvalid, $"runtime={rid}"),
            ("locale missing", Package(files: static f => f.Remove("locales/ko-KR.json")), PluginsErrors.PackageInvalid, "locale=ko-KR"),
            ("operation outside the name", Package(manifest: static m => m["contributes"]!["operations"] = new JsonArray("weather.get", "files.list")), PluginsErrors.ContributionInvalid, "id=files.list;name=weather"),
            ("codes mismatch", Package(descriptor: static d => d["permissions"] = JsonNode.Parse("""[{"code":"weather.weather.write"}]""")), PluginsErrors.ContributionInvalid, null),
            ("code of another resource", Package(manifest: static m => m["contributes"]!["permissions"] = new JsonArray("weather.forecasts.read"), descriptor: static d => { d["permissions"] = JsonNode.Parse("""[{"code":"weather.forecasts.read"}]"""); d["pages"] = new JsonArray(); }), PluginsErrors.ContributionInvalid, "code=weather.forecasts.read"),
            ("page route", Package(descriptor: static d => d["pages"]![0]!["route"] = "/weather/forecast"), PluginsErrors.ContributionInvalid, "code=weather.forecast"),
            ("page file missing", Package(files: static f => f.Remove("pages/forecast.json")), PluginsErrors.ContributionInvalid, "code=weather.forecast"),
            ("page view kind", Package(descriptor: static d => d["pages"]![0]!["view"]!["kind"] = "script"), PluginsErrors.ContributionInvalid, "code=weather.forecast"),
            ("unknown callback", Package(manifest: static m => m["permissions"] = new JsonArray(PluginInspector.PipelineCapability), descriptor: static d => d["callbacks"] = new JsonArray("weather.nothing.read")), PluginsErrors.ContributionInvalid, "code=weather.nothing.read"),
            ("callback without pipeline", Package(descriptor: static d => d["callbacks"] = new JsonArray(PluginsPermissions.PluginsRead)), PluginsErrors.ContributionInvalid, null),
            ("extends", Package(descriptor: static d => d["extends"] = JsonNode.Parse("""[{"point":"users.detail"}]""")), PluginsErrors.ContributionInvalid, null),
        };
        // 同一错误码下的几种情形按错误参数区分（形如 键=值，没有参数的为空）
        foreach (var (name, package, code, detail) in cases)
        {
            var (response, body) = await InspectAsync(server, platform, await UploadAsync(server, platform, package));
            var actual = body.TryGetProperty("details", out var details) ? string.Join(';', details.EnumerateObject().Select(static p => $"{p.Name}={(p.Value.ValueKind == JsonValueKind.String ? p.Value.GetString() : p.Value.GetRawText())}")) : null;
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest && body.GetProperty("code").GetString() == code && actual == detail, $"{name}: {(int)response.StatusCode} {body.GetRawText()}");
        }

        // 回调持有宿主已登记的码、清单申请了管道能力：回调进必须批准的授予
        var (callback, callbackBody) = await InspectAsync(server, platform, await UploadAsync(server, platform, Package(manifest: static m => m["permissions"] = new JsonArray(PluginInspector.PipelineCapability), descriptor: static d => d["callbacks"] = new JsonArray(PluginsPermissions.PluginsRead))));
        Assert.True(callback.StatusCode == HttpStatusCode.OK, callbackBody.GetRawText());
        Assert.Equal(["binding:cache", "callback:" + PluginsPermissions.PluginsRead, PluginInspector.PipelineCapability, "config:XiHan:Weather"], Strings(callbackBody.GetProperty("requiredGrants")));

        // 文件不在平台一侧或不存在
        var (absent, absentBody) = await InspectAsync(server, platform, Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.NotFound, absent.StatusCode);
        Assert.NotNull(absentBody.GetProperty("code").GetString());
    }

    [Fact]
    public async Task PluginsModule_CanBeSwitchedOff()
    {
        var (on, onToken) = await StartAsync();
        await using (on)
        {
            var (permissions, body) = await on.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:permissions", token: onToken));
            Assert.Equal(HttpStatusCode.OK, permissions.StatusCode);
            Assert.Contains(PluginsPermissions.PluginsInstall, Strings(body.GetProperty("permissions")));
        }

        var (off, offToken) = await StartAsync([new("BasicApp:Modules:Plugins:Enabled", "false")]);
        await using (off)
        {
            var (permissions, body) = await off.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:permissions", token: offToken));
            Assert.Equal(HttpStatusCode.OK, permissions.StatusCode);
            Assert.DoesNotContain(Strings(body.GetProperty("permissions")), static p => p.StartsWith("plugins.", StringComparison.Ordinal));
            var (list, _) = await off.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/plugins:list", """{"page":{"index":1,"size":20}}""", offToken));
            Assert.Equal(HttpStatusCode.NotFound, list.StatusCode);
        }
    }
}
