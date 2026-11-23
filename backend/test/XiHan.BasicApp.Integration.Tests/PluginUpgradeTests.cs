// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.BasicApp.Plugins;
using XiHan.BasicApp.Plugins.Domain.Packages;
using XiHan.BasicApp.Plugins.Infrastructure;
using XiHan.Framework.Kernel.Plugins;
using XiHan.Framework.Localization.Abstractions.Messages;
using XiHan.Framework.Primitives.Manifests;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证插件的升级与回退
/// </summary>
/// <remarks>
/// 升级核对对照当前版本给出贡献的增删，当前版本登记的操作与权限码不算冲突；包是别的插件、版本上传过或不比当前新都拒绝
/// 升级按新版本申请的授予整组替换、交回并发戳，启用着的插件提交后即换上新版本；回退换回已上传过的版本，授予按那个版本核对，加载的信任规则先判一次
/// 插件宿主是不起进程的替身
/// </remarks>
public sealed class PluginUpgradeTests : IDisposable
{
    private const string Cache = "binding:cache";
    private const string Config = "config:XiHan:Weather";
    private const string Radar = "weather.weather-radar.read";

    private readonly TestDatabase _database = new();
    private readonly string _root = Path.Combine(Path.GetTempPath(), "xihan-plugin-upgrade-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        _database.Dispose();
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private async Task<(BasicAppTestServer Server, string Platform, StandInPluginHost Host)> StartAsync(params KeyValuePair<string, string?>[] settings)
    {
        var server = await BasicAppTestServer.StartAsync(
            _database.ConnectionString,
            settings: [new("BasicApp:Files:LocalRootPath", Path.Combine(_root, "files")), new("BasicApp:Plugins:CacheDirectory", Path.Combine(_root, "cache")), .. settings],
            configure: static builder => builder.Services.AddKeyedSingleton<IPluginHost>(PluginIsolation.OutOfProcess, static (services, _) => new StandInPluginHost(services.GetRequiredService<XiHan.Framework.Kernel.Operations.OperationRegistry>(), services.GetRequiredService<MessageCatalogRegistry>())));
        await server.SeedDefaultsAsync();
        await server.RunSeedersAsync();
        var platform = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;
        return (server, platform, (StandInPluginHost)server.Services.GetRequiredKeyedService<IPluginHost>(PluginIsolation.OutOfProcess));
    }

    /// <summary>
    /// 2.0.0：多一个操作与它的权限码
    /// </summary>
    private static byte[] Version2(string version = "2.0.0") => PluginsTests.Package(
        manifest: m =>
        {
            m["version"] = version;
            m["contributes"]!["operations"]!.AsArray().Add("weather-radar.list");
            m["contributes"]!["permissions"]!.AsArray().Add(Radar);
        },
        descriptor: static d => d["permissions"]!.AsArray().Add(new JsonObject { ["code"] = Radar, ["scope"] = "Tenant" }));

    private static async Task<string> InstallAsync(BasicAppTestServer server, string token, byte[] package, params string[] grants)
    {
        var fileId = await PluginsTests.UploadAsync(server, token, package);
        var (installed, body) = await PluginsTests.InstallAsync(server, token, fileId, grants);
        Assert.True(installed.StatusCode == HttpStatusCode.OK, body.GetRawText());
        return body.GetProperty("plugin").GetProperty("id").GetString()!;
    }

    private static async Task<JsonElement> DetailAsync(BasicAppTestServer server, string token, string id)
    {
        var (got, body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/plugins/{id}", token: token));
        Assert.True(got.StatusCode == HttpStatusCode.OK, body.GetRawText());
        return body;
    }

    private static async Task<string> StampAsync(BasicAppTestServer server, string token, string id)
        => (await DetailAsync(server, token, id)).GetProperty("plugin").GetProperty("concurrencyStamp").GetString()!;

    private static async Task<(HttpResponseMessage Response, JsonElement Body)> InspectAsync(BasicAppTestServer server, string token, byte[] package, string? pluginId)
    {
        var fileId = await PluginsTests.UploadAsync(server, token, package);
        var query = pluginId is null ? $"fileId={fileId}" : $"fileId={fileId}&pluginId={pluginId}";
        return await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/plugins:inspect?{query}", token: token));
    }

    private static void AssertError(HttpResponseMessage response, JsonElement body, HttpStatusCode status, string code)
        => Assert.True(response.StatusCode == status && body.GetProperty("code").GetString() == code, $"expected {(int)status} {code}: {(int)response.StatusCode} {body.GetRawText()}");

    private static string[] Strings(JsonElement array) => [.. array.EnumerateArray().Select(static e => e.GetString()!)];

    private static string VersionIdOf(JsonElement detail, string version)
        => detail.GetProperty("versions").EnumerateArray().Single(v => v.GetProperty("version").GetString() == version).GetProperty("id").GetString()!;

    [Fact]
    public async Task InspectForUpgrade_ComparesWithTheCurrentVersion_AndRejectsOtherPackages()
    {
        var (server, platform, _) = await StartAsync();
        await using var _ = server;
        var id = await InstallAsync(server, platform, PluginsTests.Package(), Cache, Config, PluginInspector.DataScopeAll);

        var (inspected, body) = await InspectAsync(server, platform, Version2(), id);
        Assert.True(inspected.StatusCode == HttpStatusCode.OK, body.GetRawText());
        var upgrade = body.GetProperty("upgrade");
        Assert.Equal((id, "1.0.0"), (upgrade.GetProperty("pluginId").GetString(), upgrade.GetProperty("fromVersion").GetString()));
        Assert.Equal(["weather-radar.list"], Strings(upgrade.GetProperty("addedOperations")));
        Assert.Equal([Radar], Strings(upgrade.GetProperty("addedPermissions")));
        Assert.Empty(Strings(upgrade.GetProperty("removedOperations")));
        Assert.Empty(Strings(upgrade.GetProperty("removedPermissions")));
        Assert.Equal([Cache, Config, PluginInspector.DataScopeAll], Strings(upgrade.GetProperty("grants")));

        // 去掉页面：菜单节点会随之删掉
        var (withoutPage, withoutPageBody) = await InspectAsync(server, platform, PluginsTests.Package(manifest: static m => m["version"] = "1.1.0", descriptor: static d => d["pages"] = new JsonArray()), id);
        Assert.True(withoutPage.StatusCode == HttpStatusCode.OK, withoutPageBody.GetRawText());
        Assert.Equal(["weather.forecast"], Strings(withoutPageBody.GetProperty("upgrade").GetProperty("removedPages")));

        var (same, sameBody) = await InspectAsync(server, platform, PluginsTests.Package(), id);
        AssertError(same, sameBody, HttpStatusCode.Conflict, PluginsErrors.VersionUploaded);
        var (older, olderBody) = await InspectAsync(server, platform, PluginsTests.Package(manifest: static m => m["version"] = "0.9.0"), id);
        AssertError(older, olderBody, HttpStatusCode.Conflict, PluginsErrors.VersionNotNewer);
        // 预发布版本在正式版之前
        var (preview, previewBody) = await InspectAsync(server, platform, PluginsTests.Package(manifest: static m => m["version"] = "1.0.0-rc.1"), id);
        AssertError(preview, previewBody, HttpStatusCode.Conflict, PluginsErrors.VersionNotNewer);
        var (other, otherBody) = await InspectAsync(server, platform, PluginsTests.Package(manifest: static m => { m["id"] = "xihan.fabrikam.weather"; m["version"] = "2.0.0"; }), id);
        AssertError(other, otherBody, HttpStatusCode.BadRequest, PluginsErrors.UpgradeMismatch);
        var (unknown, unknownBody) = await InspectAsync(server, platform, Version2(), Guid.NewGuid().ToString());
        AssertError(unknown, unknownBody, HttpStatusCode.NotFound, PluginsErrors.PluginNotFound);
        // 不给插件 ID 仍是新装：已经装过
        var (fresh, freshBody) = await InspectAsync(server, platform, Version2(), null);
        AssertError(fresh, freshBody, HttpStatusCode.Conflict, PluginsErrors.AlreadyInstalled);
    }

    [Fact]
    public async Task Upgrade_LoadsTheNewVersion_AndRollbackSwitchesBetweenUploadedVersions()
    {
        var (server, platform, host) = await StartAsync();
        await using var _ = server;
        var id = await InstallAsync(server, platform, PluginsTests.Package(), Cache, Config);
        var (enabled, enabledBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/plugins:enable", JsonSerializer.Serialize(new { ids = new[] { id } }), platform));
        Assert.True(enabled.StatusCode == HttpStatusCode.OK, enabledBody.GetRawText());
        var registered = server.Services.GetRequiredService<RegisteredPermissions>();
        var reconciler = server.Services.GetRequiredService<PluginReconciler>();
        var fileId = await PluginsTests.UploadAsync(server, platform, Version2());
        var stamp = await StampAsync(server, platform, id);

        // 授予按新版本核对，并发戳过期即失败，两者都不改库
        var (missing, missingBody) = await PluginsTests.UpgradeAsync(server, platform, id, stamp, fileId, Cache);
        AssertError(missing, missingBody, HttpStatusCode.Forbidden, PluginsErrors.GrantsMissing);
        var (stale, staleBody) = await PluginsTests.UpgradeAsync(server, platform, id, "stale", fileId, Cache, Config);
        Assert.True(stale.StatusCode == HttpStatusCode.Conflict, staleBody.GetRawText());

        var (upgraded, upgradedBody) = await PluginsTests.UpgradeAsync(server, platform, id, stamp, fileId, Cache, Config);
        Assert.True(upgraded.StatusCode == HttpStatusCode.OK, upgradedBody.GetRawText());
        Assert.Equal("2.0.0", upgradedBody.GetProperty("plugin").GetProperty("version").GetString());
        Assert.Equal(["2.0.0", "1.0.0"], upgradedBody.GetProperty("versions").EnumerateArray().Select(static v => v.GetProperty("version").GetString()));
        Assert.Equal(["1.0.0", "2.0.0"], host.Loads.Select(static l => l.Version));
        Assert.Equal("2.0.0", reconciler.Loaded[PluginsTests.ManifestId].Version);
        Assert.NotNull(registered.Find(Radar));
        var v1 = VersionIdOf(upgradedBody, "1.0.0");
        var v2 = VersionIdOf(upgradedBody, "2.0.0");
        // 每个版本带上它申请的授予，换回时按它核对
        Assert.Equal([Cache, Config], Strings(upgradedBody.GetProperty("versions").EnumerateArray().Single(v => v.GetProperty("id").GetString() == v1).GetProperty("requiredGrants")));

        stamp = upgradedBody.GetProperty("plugin").GetProperty("concurrencyStamp").GetString()!;
        var (current, currentBody) = await PluginsTests.RollbackAsync(server, platform, id, stamp, v2, Cache, Config);
        AssertError(current, currentBody, HttpStatusCode.Conflict, PluginsErrors.VersionCurrent);
        var (unknown, unknownBody) = await PluginsTests.RollbackAsync(server, platform, id, stamp, Guid.NewGuid().ToString(), Cache, Config);
        AssertError(unknown, unknownBody, HttpStatusCode.NotFound, PluginsErrors.VersionNotFound);
        var (unknownGrant, unknownGrantBody) = await PluginsTests.RollbackAsync(server, platform, id, stamp, v1, Cache, Config, "binding:queue");
        AssertError(unknownGrant, unknownGrantBody, HttpStatusCode.BadRequest, PluginsErrors.GrantInvalid);

        // 换回 1.0.0：卸掉新版本贡献的权限码，菜单与授予照它
        var (rolledBack, rolledBackBody) = await PluginsTests.RollbackAsync(server, platform, id, stamp, v1, Cache, Config, PluginInspector.DataScopeAll);
        Assert.True(rolledBack.StatusCode == HttpStatusCode.OK, rolledBackBody.GetRawText());
        Assert.Equal("1.0.0", rolledBackBody.GetProperty("plugin").GetProperty("version").GetString());
        Assert.Equal([Cache, Config, PluginInspector.DataScopeAll], Strings(rolledBackBody.GetProperty("grants")));
        Assert.Equal("1.0.0", reconciler.Loaded[PluginsTests.ManifestId].Version);
        Assert.Equal("1.0.0", host.Loads[^1].Version);
        Assert.Null(registered.Find(Radar));

        // 上传过的版本不再走升级，用回退换上
        var (again, againBody) = await PluginsTests.UpgradeAsync(server, platform, id, rolledBackBody.GetProperty("plugin").GetProperty("concurrencyStamp").GetString()!, await PluginsTests.UploadAsync(server, platform, Version2()), Cache, Config);
        AssertError(again, againBody, HttpStatusCode.Conflict, PluginsErrors.VersionUploaded);
        var (forward, forwardBody) = await PluginsTests.RollbackAsync(server, platform, id, rolledBackBody.GetProperty("plugin").GetProperty("concurrencyStamp").GetString()!, v2, Cache, Config);
        Assert.True(forward.StatusCode == HttpStatusCode.OK, forwardBody.GetRawText());
        Assert.Equal("2.0.0", reconciler.Loaded[PluginsTests.ManifestId].Version);
    }

    [Fact]
    public async Task Rollback_RefusesAVersionThatIsNoLongerTrusted()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        KeyValuePair<string, string?>[] trust = [new("BasicApp:Plugins:TrustedPublishers:0:Name", "Contoso"), new("BasicApp:Plugins:TrustedPublishers:0:PublicKey", Convert.ToBase64String(key.ExportSubjectPublicKeyInfo()))];
        string id;
        string v1;
        string stamp;
        var (first, platform, _) = await StartAsync(trust);
        await using (first)
        {
            // 放行时装了没签名的 1.0.0，再升级到签过的 2.0.0
            id = await InstallAsync(first, platform, PluginsTests.Package(), Cache, Config);
            var fileId = await PluginsTests.UploadAsync(first, platform, PluginSignatureTests.Sign(Version2(), key));
            var (upgraded, body) = await PluginsTests.UpgradeAsync(first, platform, id, await StampAsync(first, platform, id), fileId, Cache, Config);
            Assert.True(upgraded.StatusCode == HttpStatusCode.OK, body.GetRawText());
            v1 = VersionIdOf(body, "1.0.0");
            stamp = body.GetProperty("plugin").GetProperty("concurrencyStamp").GetString()!;
        }

        // 不再放行没签名的包：换不回 1.0.0
        var (second, token, _) = await StartAsync([.. trust, new("BasicApp:Plugins:AllowUnsigned", "false")]);
        await using var _ = second;
        var (refused, refusedBody) = await PluginsTests.RollbackAsync(second, token, id, stamp, v1, Cache, Config);
        AssertError(refused, refusedBody, HttpStatusCode.Forbidden, PluginsErrors.PackageUnsigned);
        Assert.Equal("2.0.0", (await DetailAsync(second, token, id)).GetProperty("plugin").GetProperty("version").GetString());
    }
}
