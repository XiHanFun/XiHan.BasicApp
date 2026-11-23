// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Contracts.Pages;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.BasicApp.Plugins;
using XiHan.BasicApp.Plugins.Domain.Packages;
using XiHan.BasicApp.Plugins.Infrastructure;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.BasicApp.Tenancy.Domain.Entities;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Kernel.Plugins;
using XiHan.Framework.Localization.Abstractions.Messages;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;
using XiHan.Framework.Primitives.Manifests;
using XiHan.Framework.Security.Database.Grants;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证插件对齐服务：启停、改授予、启动恢复、加载失败与换版本退回
/// </summary>
/// <remarks>
/// 插件宿主换成不起进程的替身（键控服务先登记即顶替框架的进程外宿主）；包从 Files 取出、核对散列后解到本机缓存目录，宿主拿到的是那个目录与去掉 BasicApp 自己那几类的授予
/// 加载成功：运行期贡献进权限目录，状态操作报已加载；失败：错误码记在插件上、健康项降级，再启用一次即重试；换版本失败重载旧版本
/// </remarks>
public sealed class PluginReconcilerTests : IDisposable
{
    private const string Cache = "binding:cache";
    private const string Config = "config:XiHan:Weather";

    private readonly TestDatabase _database = new();
    private readonly string _root = Path.Combine(Path.GetTempPath(), "xihan-plugin-reconciler-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        _database.Dispose();
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private string CacheDirectory => Path.Combine(_root, "cache");

    private async Task<(BasicAppTestServer Server, string Platform, StandInPluginHost Host)> StartAsync(params KeyValuePair<string, string?>[] settings)
    {
        var server = await BasicAppTestServer.StartAsync(
            _database.ConnectionString,
            settings: [new("BasicApp:Files:LocalRootPath", Path.Combine(_root, "files")), new("BasicApp:Plugins:CacheDirectory", CacheDirectory), .. settings],
            configure: static builder => builder.Services.AddKeyedSingleton<IPluginHost>(PluginIsolation.OutOfProcess, static (services, _) => new StandInPluginHost(services.GetRequiredService<XiHan.Framework.Kernel.Operations.OperationRegistry>(), services.GetRequiredService<MessageCatalogRegistry>())));
        await server.SeedDefaultsAsync();
        await server.RunSeedersAsync();
        var platform = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;
        return (server, platform, (StandInPluginHost)server.Services.GetRequiredKeyedService<IPluginHost>(PluginIsolation.OutOfProcess));
    }

    private static async Task<string> InstallAsync(BasicAppTestServer server, string token, byte[] package, params string[] grants)
    {
        var fileId = await PluginsTests.UploadAsync(server, token, package);
        var (installed, body) = await PluginsTests.InstallAsync(server, token, fileId, grants);
        Assert.True(installed.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created, body.GetRawText());
        return body.GetProperty("plugin").GetProperty("id").GetString()!;
    }

    private static async Task SwitchAsync(BasicAppTestServer server, string token, string id, bool enable)
    {
        var (switched, body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, enable ? "/api/plugins:enable" : "/api/plugins:disable", JsonSerializer.Serialize(new { ids = new[] { id } }), token));
        Assert.True(switched.StatusCode == HttpStatusCode.OK, body.GetRawText());
        Assert.Equal(id, Assert.Single(body.GetProperty("succeeded").EnumerateArray()).GetString());
    }

    private static async Task<JsonElement> StatusAsync(BasicAppTestServer server, string token, string id)
    {
        var (got, body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/plugins/{id}:status", token: token));
        Assert.True(got.StatusCode == HttpStatusCode.OK, body.GetRawText());
        return body;
    }

    private static async Task<JsonElement> DetailAsync(BasicAppTestServer server, string token, string id)
    {
        var (got, body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/plugins/{id}", token: token));
        Assert.True(got.StatusCode == HttpStatusCode.OK, body.GetRawText());
        return body;
    }

    private static string? ErrorCodeOf(JsonElement element, string property) => element.GetProperty(property).ValueKind == JsonValueKind.Null ? null : element.GetProperty(property).GetString();

    [Fact]
    public async Task Enable_LoadsFromTheCache_GrantReloads_DisableUnloads()
    {
        var (server, platform, host) = await StartAsync();
        await using var _ = server;
        var id = await InstallAsync(server, platform, PluginsTests.Package(), Cache, Config, PluginInspector.DataScopeAll);
        var catalog = server.Services.GetRequiredService<PluginRuntimeCatalog>();
        var registered = server.Services.GetRequiredService<RegisteredPermissions>();

        // 装好是停用的：什么都没加载
        var idle = await StatusAsync(server, platform, id);
        Assert.Equal((false, false, JsonValueKind.Null), (idle.GetProperty("isEnabled").GetBoolean(), idle.GetProperty("isLoaded").GetBoolean(), idle.GetProperty("state").ValueKind));
        Assert.Empty(host.Loads);

        // 启用：从 Files 取包解到缓存目录，宿主拿到的授予去掉了 data-scope:all
        await SwitchAsync(server, platform, id, enable: true);
        var load = Assert.Single(host.Loads);
        var version = (await DetailAsync(server, platform, id)).GetProperty("versions")[0].GetProperty("id").GetString()!;
        Assert.Equal(Path.Combine(CacheDirectory, PluginsTests.ManifestId, Guid.Parse(version).ToString("N")), load.Directory);
        // 当前平台的运行时目录铺到根上，与框架清单放在一起：框架宿主按清单的 entry 相对插件目录找入口
        Assert.True(File.Exists(Path.Combine(load.Directory, "xihan.manifest.json")));
        Assert.True(File.Exists(Path.Combine(load.Directory, PluginsTests.Name)));
        Assert.False(Directory.Exists(Path.Combine(load.Directory, "runtimes")));
        Assert.Equal([Cache, Config], [.. load.Grants.Order(StringComparer.Ordinal)]);
        var loaded = await StatusAsync(server, platform, id);
        Assert.Equal((true, "1.0.0", "Ready"), (loaded.GetProperty("isLoaded").GetBoolean(), loaded.GetProperty("loadedVersion").GetString(), loaded.GetProperty("state").GetString()));
        Assert.Null(ErrorCodeOf(loaded, "errorCode"));
        Assert.NotNull(registered.Find(PluginsTests.Read));
        Assert.True(catalog.IsUnrestricted(PluginsTests.ManifestId));
        Assert.Equal("Ready", server.HealthStates()[PluginsHealthContributor.Name]);

        // 改授予：按新的一组重新加载；交回同一组不重载
        var stamp = (await DetailAsync(server, platform, id)).GetProperty("plugin").GetProperty("concurrencyStamp").GetString()!;
        var (granted, grantedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/plugins/{id}:grant", JsonSerializer.Serialize(new { concurrencyStamp = stamp, grants = new[] { Cache, Config } }), platform));
        Assert.True(granted.StatusCode == HttpStatusCode.OK, grantedBody.GetRawText());
        Assert.Equal((2, 1), (host.Loads.Count, host.Unloads.Count));
        Assert.False(catalog.IsUnrestricted(PluginsTests.ManifestId));
        var again = grantedBody.GetProperty("plugin").GetProperty("concurrencyStamp").GetString()!;
        var (same, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/plugins/{id}:grant", JsonSerializer.Serialize(new { concurrencyStamp = again, grants = new[] { Config, Cache } }), platform));
        Assert.Equal(HttpStatusCode.OK, same.StatusCode);
        Assert.Equal(2, host.Loads.Count);

        // 停用：宿主卸下，权限码撤回
        await SwitchAsync(server, platform, id, enable: false);
        Assert.Equal(2, host.Unloads.Count);
        Assert.Empty(host.Plugins);
        Assert.Null(registered.Find(PluginsTests.Read));
        Assert.False((await StatusAsync(server, platform, id)).GetProperty("isLoaded").GetBoolean());
    }

    [Fact]
    public async Task LoadFailure_IsRecorded_DegradesHealth_AndEnablingAgainRetries()
    {
        var (server, platform, host) = await StartAsync();
        await using var _ = server;
        var id = await InstallAsync(server, platform, PluginsTests.Package(), Cache, Config);
        host.FailWith = static _ => new Error("PLUGINS_UNEXPECTED_002", "plugins.standin.crashed");

        // 加载失败不改变启用的结果：错误码记在插件上，节点降级
        await SwitchAsync(server, platform, id, enable: true);
        var failed = await StatusAsync(server, platform, id);
        Assert.Equal((true, false, "PLUGINS_UNEXPECTED_002"), (failed.GetProperty("isEnabled").GetBoolean(), failed.GetProperty("isLoaded").GetBoolean(), ErrorCodeOf(failed, "errorCode")));
        Assert.Equal("PLUGINS_UNEXPECTED_002", ErrorCodeOf((await DetailAsync(server, platform, id)).GetProperty("plugin"), "lastErrorCode"));
        Assert.Equal("Degraded", server.HealthStates()[PluginsHealthContributor.Name]);
        Assert.Null(server.Services.GetRequiredService<RegisteredPermissions>().Find(PluginsTests.Read));

        // 原因排除后再启用一次即重试：错误码清掉，健康恢复
        host.FailWith = null;
        await SwitchAsync(server, platform, id, enable: true);
        var recovered = await StatusAsync(server, platform, id);
        Assert.True(recovered.GetProperty("isLoaded").GetBoolean());
        Assert.Null(ErrorCodeOf(recovered, "errorCode"));
        Assert.Null(ErrorCodeOf((await DetailAsync(server, platform, id)).GetProperty("plugin"), "lastErrorCode"));
        Assert.Equal("Ready", server.HealthStates()[PluginsHealthContributor.Name]);
    }

    [Fact]
    public async Task CatalogWithoutTheDocumentKeys_FailsTheLoad_AndTheHostUnloadsIt()
    {
        var (server, platform, host) = await StartAsync();
        await using var _ = server;
        var id = await InstallAsync(server, platform, PluginsTests.Package(), Cache, Config);
        host.WithoutCatalog = true;

        // 宿主加载成功，但插件的消息目录没有接口文档的分组名：这次加载算失败，插件被卸掉，贡献不进运行期目录
        await SwitchAsync(server, platform, id, enable: true);
        var failed = await StatusAsync(server, platform, id);
        Assert.Equal((false, PluginsErrors.CatalogIncomplete), (failed.GetProperty("isLoaded").GetBoolean(), ErrorCodeOf(failed, "errorCode")));
        Assert.Equal([PluginsTests.ManifestId], host.Unloads);
        Assert.Empty(host.Plugins);
        Assert.Null(server.Services.GetRequiredService<RegisteredPermissions>().Find(PluginsTests.Read));

        // 插件补上目录后再启用即加载
        host.WithoutCatalog = false;
        await SwitchAsync(server, platform, id, enable: true);
        Assert.True((await StatusAsync(server, platform, id)).GetProperty("isLoaded").GetBoolean());
    }

    [Fact]
    public async Task Startup_RestoresEnabledPlugins_ReExtractingFromFiles_AndDeleteUnloads()
    {
        string id;
        var (first, platform, _) = await StartAsync();
        await using (first)
        {
            id = await InstallAsync(first, platform, PluginsTests.Package(), Cache, Config);
            await SwitchAsync(first, platform, id, enable: true);
        }

        // 本机缓存没了：重启时从 Files 重新取包解开，启动完成时已经加载好
        Directory.Delete(CacheDirectory, recursive: true);
        var (second, token, host) = await StartAsync();
        await using var _ = second;
        var load = Assert.Single(host.Loads);
        Assert.True(File.Exists(Path.Combine(load.Directory, "xihan.manifest.json")));
        Assert.True((await StatusAsync(second, token, id)).GetProperty("isLoaded").GetBoolean());

        // 卸载：提交后即卸下
        var (deleted, deletedBody) = await second.SendAsync(BasicAppTestServer.Request(HttpMethod.Delete, $"/api/plugins?ids={id}", token: token));
        Assert.True(deleted.StatusCode == HttpStatusCode.OK, deletedBody.GetRawText());
        Assert.Equal([PluginsTests.ManifestId], host.Unloads);
        Assert.Empty(host.Plugins);
    }

    [Fact]
    public async Task VersionChangeFailure_ReloadsThePreviousVersion()
    {
        var (server, platform, host) = await StartAsync();
        await using var _ = server;
        var id = await InstallAsync(server, platform, PluginsTests.Package(), Cache, Config);
        await SwitchAsync(server, platform, id, enable: true);

        // 升级到 2.0.0，新版本起不来：提交后本节点先卸旧版、载新版失败、再重载旧版
        host.FailWith = static m => m.Version == "2.0.0" ? new Error("PLUGINS_TIMEOUT_001", "plugins.standin.timeout") : null;
        var fileId = await PluginsTests.UploadAsync(server, platform, PluginsTests.Package(manifest: static m => m["version"] = "2.0.0"));
        var stamp = (await DetailAsync(server, platform, id)).GetProperty("plugin").GetProperty("concurrencyStamp").GetString()!;
        var (upgraded, upgradedBody) = await PluginsTests.UpgradeAsync(server, platform, id, stamp, fileId, Cache, Config);
        Assert.True(upgraded.StatusCode == HttpStatusCode.OK, upgradedBody.GetRawText());
        var reconciler = server.Services.GetRequiredService<PluginReconciler>();

        Assert.Equal(["1.0.0", "2.0.0", "1.0.0"], host.Loads.Select(static l => l.Version));
        Assert.Equal("1.0.0", reconciler.Loaded[PluginsTests.ManifestId].Version);
        var status = await StatusAsync(server, platform, id);
        Assert.Equal(("1.0.0", "PLUGINS_TIMEOUT_001"), (status.GetProperty("loadedVersion").GetString(), ErrorCodeOf(status, "errorCode")));
        Assert.Equal("PLUGINS_TIMEOUT_001", ErrorCodeOf((await DetailAsync(server, platform, id)).GetProperty("plugin"), "lastErrorCode"));

        // 新版本能起来了：下一次对齐换上它
        host.FailWith = null;
        await reconciler.ReconcileAsync(CancellationToken.None);
        Assert.Equal("2.0.0", reconciler.Loaded[PluginsTests.ManifestId].Version);
        Assert.Null(ErrorCodeOf((await DetailAsync(server, platform, id)).GetProperty("plugin"), "lastErrorCode"));
    }

    [Fact]
    public async Task MissingHostOrChangedPackage_IsRecordedAsUnavailable()
    {
        var (server, platform, _) = await StartAsync(new("BasicApp:Plugins:Isolations:0", "out-of-process"), new("BasicApp:Plugins:Isolations:1", "wasm"));
        await using var _ = server;

        // 允许 Wasm 但宿主没装 Wasm 插件宿主
        var wasm = PluginsTests.Package(
            manifest: static m => { m["isolation"] = "wasm"; m["entry"] = "weather.wasm"; },
            files: static f => f["weather.wasm"] = [0x00, 0x61, 0x73, 0x6D]);
        var wasmId = await InstallAsync(server, platform, wasm, Cache, Config);
        await SwitchAsync(server, platform, wasmId, enable: true);
        Assert.Equal(PluginsErrors.IsolationUnavailable, ErrorCodeOf(await StatusAsync(server, platform, wasmId), "errorCode"));
        Assert.Equal("Degraded", server.HealthStates()[PluginsHealthContributor.Name]);
        await SwitchAsync(server, platform, wasmId, enable: false);
        Assert.Equal("Ready", server.HealthStates()[PluginsHealthContributor.Name]);
        var (deleted, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Delete, $"/api/plugins?ids={wasmId}", token: platform));
        Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);

        // 包文件在存储里被改了：散列对不上，不解包
        var id = await InstallAsync(server, platform, PluginsTests.Package(), Cache, Config);
        var objects = Directory.EnumerateFiles(Path.Combine(_root, "files"), "*.xhplugin", SearchOption.AllDirectories).ToList();
        Assert.NotEmpty(objects);
        foreach (var stored in objects)
        {
            await File.WriteAllBytesAsync(stored, new byte[new FileInfo(stored).Length]);
        }

        await SwitchAsync(server, platform, id, enable: true);
        var tampered = await StatusAsync(server, platform, id);
        Assert.True(ErrorCodeOf(tampered, "errorCode") == PluginsErrors.PackageUnavailable, $"{tampered.GetRawText()} objects: {string.Join(", ", objects)}");
        var version = (await DetailAsync(server, platform, id)).GetProperty("versions")[0].GetProperty("id").GetString()!;
        Assert.False(Directory.Exists(Path.Combine(CacheDirectory, PluginsTests.ManifestId, Guid.Parse(version).ToString("N"))));
    }

    [Fact]
    public async Task Pages_JoinTheMenu_AreDeliveredOnlyWhileLoaded_AndGoAwayOnUninstall()
    {
        var (server, platform, _) = await StartAsync();
        await using var _ = server;
        const string Page = "weather.forecast";
        async Task<string> OutlineAsync()
        {
            var (listed, body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/menus", token: platform));
            Assert.True(listed.StatusCode == HttpStatusCode.OK, body.GetRawText());
            return MenuTrees.Outline(body.GetProperty("items"));
        }

        async Task<string[]> DeliveredAsync()
        {
            var (got, body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:permissions", token: platform));
            Assert.True(got.StatusCode == HttpStatusCode.OK, body.GetRawText());
            return [.. body.GetProperty("pages").EnumerateArray().Select(static p => p.GetString()!)];
        }

        // 页面不要求权限码，平台管理员看得到；装好即进「插件应用」目录，没加载不下发
        var id = await InstallAsync(server, platform, PluginsTests.Package(descriptor: static d => d["pages"]![0]!["permission"] = null), Cache, Config);
        Assert.Contains($"{MenuDirectories.PluginApps}[{Page}]", await OutlineAsync(), StringComparison.Ordinal);
        Assert.DoesNotContain(Page, await DeliveredAsync());

        await SwitchAsync(server, platform, id, enable: true);
        Assert.Contains(Page, await DeliveredAsync());

        // 重跑菜单种子：已安装插件的页面也在登记里，节点不会被当成过期删掉
        await server.RunSeedersAsync();
        Assert.Contains(Page, await DeliveredAsync());

        // 停用只是不下发，节点留着；卸载才删
        await SwitchAsync(server, platform, id, enable: false);
        Assert.DoesNotContain(Page, await DeliveredAsync());
        Assert.Contains($"{MenuDirectories.PluginApps}[{Page}]", await OutlineAsync(), StringComparison.Ordinal);
        var (deleted, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Delete, $"/api/plugins?ids={id}", token: platform));
        Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);
        Assert.DoesNotContain(Page, await OutlineAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Uninstall_PreviewsTheReferences_ClearsThemEverywhere_AndReinstallStartsClean()
    {
        var (server, platform, tenant) = await SettingsTestHost.StartAsync(_database.ConnectionString, configure: builder =>
        {
            builder.Configuration.AddInMemoryCollection([new("BasicApp:Files:LocalRootPath", Path.Combine(_root, "files")), new("BasicApp:Plugins:CacheDirectory", CacheDirectory)]);
            builder.Services.AddKeyedSingleton<IPluginHost>(PluginIsolation.OutOfProcess, static (services, _) => new StandInPluginHost(services.GetRequiredService<XiHan.Framework.Kernel.Operations.OperationRegistry>(), services.GetRequiredService<MessageCatalogRegistry>()));
        });
        await using var _ = server;
        const string Read = PluginsTests.Read;
        var id = await InstallAsync(server, platform, PluginsTests.Package(), Cache, Config);
        await SwitchAsync(server, platform, id, enable: true);

        // 引用：租户角色的限期授予（授予与生效时段两行）、套餐白名单、以插件码门控的运营外链
        var (createdRole, roleBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/roles", """{"code":"forecaster","name":"Forecaster"}""", tenant));
        Assert.True(createdRole.StatusCode == HttpStatusCode.Created, roleBody.GetRawText());
        var roleId = roleBody.GetProperty("id").GetString()!;
        var (granted, grantedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/roles/{roleId}:grant", JsonSerializer.Serialize(new { permissions = new[] { Read }, expiresAt = DateTimeOffset.UtcNow.AddDays(30) }), tenant));
        Assert.True(granted.StatusCode == HttpStatusCode.OK && grantedBody.GetProperty("failed").GetArrayLength() == 0, grantedBody.GetRawText());
        var enterprise = await EditionsTests.EditionIdAsync(server, platform, "enterprise");
        var (_, edition) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/editions/{enterprise}", token: platform));
        string[] whitelist = [.. edition.GetProperty("permissions").EnumerateArray().Select(static p => p.GetString()!), Read];
        var (set, setBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/editions/{enterprise}:set-permissions", JsonSerializer.Serialize(new { permissions = whitelist }), platform));
        Assert.True(set.StatusCode == HttpStatusCode.OK, setBody.GetRawText());
        var (link, linkBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/menus", JsonSerializer.Serialize(new { kind = "Link", code = "weather-site", title = "Weather", externalUrl = "https://weather.example.com", permission = Read }), platform));
        Assert.True(link.StatusCode == HttpStatusCode.Created, linkBody.GetRawText());
        var linkId = linkBody.GetProperty("id").GetString()!;

        // 预览：全部版本的权限码与各处引用
        var (previewed, preview) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/plugins/{id}:delete-preview", token: platform));
        Assert.True(previewed.StatusCode == HttpStatusCode.OK, preview.GetRawText());
        Assert.Equal([Read], preview.GetProperty("permissions").EnumerateArray().Select(static p => p.GetString()!));
        var references = preview.GetProperty("references").EnumerateArray().Select(static r => $"{r.GetProperty("kind").GetString()}|{r.GetProperty("side").GetString()}|{r.GetProperty("subject").GetString()}").ToList();
        Assert.Contains("RoleGrant|contoso|forecaster", references);
        Assert.Contains("Edition|platform|enterprise", references);
        Assert.Contains("Menu|platform|custom.weather-site", references);
        Assert.Contains(references, static r => r.StartsWith("PermissionRule|contoso|", StringComparison.Ordinal));

        // 卸载：引用清干净，运营节点留着、只去掉门控权限码
        var (deleted, deletedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Delete, $"/api/plugins?ids={id}", token: platform));
        Assert.True(deleted.StatusCode == HttpStatusCode.OK && deletedBody.GetProperty("failed").GetArrayLength() == 0, deletedBody.GetRawText());
        Assert.Equal((0, 0, 0), await CountReferencesAsync(server, Read));
        var (_, menu) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/menus/{linkId}", token: platform));
        Assert.Equal(JsonValueKind.Null, menu.GetProperty("permission").ValueKind);

        // 重装同一个包：旧授予不会复活
        var again = await InstallAsync(server, platform, PluginsTests.Package(), Cache, Config);
        await SwitchAsync(server, platform, again, enable: true);
        Assert.Equal((0, 0, 0), await CountReferencesAsync(server, Read));
    }

    /// <summary>
    /// 库里对一个权限码的授予、生效时段规则与套餐白名单各有几行
    /// </summary>
    private static Task<(int Grants, int Rules, int Editions)> CountReferencesAsync(BasicAppTestServer server, string code)
        => StoreScope.ReadAsync(server.Services.GetRequiredService<IServiceScopeFactory>(), async (services, ct) =>
        {
            var grants = await services.GetRequiredService<IRepository<PermissionGrantRecord, Guid>>().ListAsync(EntityQuery.All<PermissionGrantRecord>().Where(new Specification<PermissionGrantRecord>(r => r.Permission == code)), ct);
            var rules = await services.GetRequiredService<IRepository<SysPermissionRule, Guid>>().ListAsync(EntityQuery.All<SysPermissionRule>().Where(new Specification<SysPermissionRule>(r => r.Permission == code)), ct);
            var editions = await services.GetRequiredService<IRepository<SysEditionGrant, Guid>>().ListAsync(EntityQuery.All<SysEditionGrant>().Where(new Specification<SysEditionGrant>(g => g.PermissionCode == code)), ct);
            return (grants.Count, rules.Count, editions.Count);
        }, CancellationToken.None);
}
