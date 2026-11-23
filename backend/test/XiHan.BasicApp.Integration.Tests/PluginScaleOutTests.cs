// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.BasicApp.Plugins.Infrastructure;
using XiHan.Framework.Kernel.Plugins;
using XiHan.Framework.Localization.Abstractions.Messages;
using XiHan.Framework.Primitives.Manifests;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证插件在两个实例之间对齐
/// </summary>
/// <remarks>
/// 两个宿主共用一个库与文件存储，各有自己的解包目录与不起进程的替身插件宿主：在第一个实例上启用、升级、停用，第二个实例跟着加载、换版本、卸下
/// 没配消息代理时广播只在本实例里，第二个实例靠定时对齐追上；配了 RabbitMQ 时广播跨实例，第二个实例收到即对齐，不等定时
/// </remarks>
public sealed class PluginScaleOutTests : IDisposable
{
    private const string RabbitMQVariable = "XIHAN_TEST_RABBITMQ";
    private const string Cache = "binding:cache";
    private const string Config = "config:XiHan:Weather";
    private static readonly TimeSpan _wait = TimeSpan.FromSeconds(15);

    private readonly TestDatabase _database = new();
    private readonly string _root = Path.Combine(Path.GetTempPath(), "xihan-plugin-scale-out-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        _database.Dispose();
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public async Task WithoutABroker_TheOtherInstanceCatchesUpOnItsScheduledRound()
    {
        var (first, second, platform) = await StartPairAsync([new("BasicApp:Plugins:ReconcileIntervalSeconds", "1")]);
        await using var runningFirst = first;
        await using var runningSecond = second;
        await FollowAsync(first, second, platform);
    }

    [Fact]
    public async Task WithRabbitMQ_TheOtherInstanceReconcilesOnTheBroadcast()
    {
        var rabbit = Environment.GetEnvironmentVariable(RabbitMQVariable);
        Assert.SkipWhen(string.IsNullOrWhiteSpace(rabbit), $"{RabbitMQVariable} is not set; this case only runs against a real broker.");

        // 定时对齐拉到一小时：第二个实例只能靠广播跟上
        var (first, second, platform) = await StartPairAsync(
        [
            new("XiHan:Bindings:queue:value", rabbit),
            new("XiHan:Messaging:RabbitMQ:QueuePrefix", $"xihan-basicapp-test-{Guid.NewGuid():N}."),
            new("BasicApp:Plugins:ReconcileIntervalSeconds", "3600"),
        ]);
        await using var runningFirst = first;
        await using var runningSecond = second;
        await FollowAsync(first, second, platform);
    }

    /// <summary>
    /// 第一个实例上装、启用、升级、停用，第二个实例逐步跟上
    /// </summary>
    private static async Task FollowAsync(BasicAppTestServer first, BasicAppTestServer second, string platform)
    {
        var reconciler = second.Services.GetRequiredService<PluginReconciler>();
        var host = (StandInPluginHost)second.Services.GetRequiredKeyedService<IPluginHost>(PluginIsolation.OutOfProcess);
        var registered = second.Services.GetRequiredService<RegisteredPermissions>();

        var fileId = await PluginsTests.UploadAsync(first, platform, PluginsTests.Package());
        var (installed, installedBody) = await PluginsTests.InstallAsync(first, platform, fileId, Cache, Config);
        Assert.True(installed.StatusCode == HttpStatusCode.OK, installedBody.GetRawText());
        var id = installedBody.GetProperty("plugin").GetProperty("id").GetString()!;
        var (enabled, enabledBody) = await first.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/plugins:enable", JsonSerializer.Serialize(new { ids = new[] { id } }), platform));
        Assert.True(enabled.StatusCode == HttpStatusCode.OK, enabledBody.GetRawText());

        // 第二个实例自己从文件存储取包解开、加载，插件的权限码在它那里也登记上
        await WaitUntilAsync(() => reconciler.Loaded.TryGetValue(PluginsTests.ManifestId, out var loaded) && loaded.Version == "1.0.0", "the second instance loads 1.0.0");
        Assert.NotNull(registered.Find(PluginsTests.Read));

        var upgradeFile = await PluginsTests.UploadAsync(first, platform, PluginsTests.Package(manifest: static m => m["version"] = "2.0.0"));
        var stamp = (await first.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/plugins/{id}", token: platform))).Body.GetProperty("plugin").GetProperty("concurrencyStamp").GetString()!;
        var (upgraded, upgradedBody) = await PluginsTests.UpgradeAsync(first, platform, id, stamp, upgradeFile, Cache, Config);
        Assert.True(upgraded.StatusCode == HttpStatusCode.OK, upgradedBody.GetRawText());
        await WaitUntilAsync(() => reconciler.Loaded.TryGetValue(PluginsTests.ManifestId, out var loaded) && loaded.Version == "2.0.0", "the second instance switches to 2.0.0");

        var (disabled, disabledBody) = await first.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/plugins:disable", JsonSerializer.Serialize(new { ids = new[] { id } }), platform));
        Assert.True(disabled.StatusCode == HttpStatusCode.OK, disabledBody.GetRawText());
        await WaitUntilAsync(() => !reconciler.Loaded.ContainsKey(PluginsTests.ManifestId), "the second instance unloads the plugin");
        Assert.Null(registered.Find(PluginsTests.Read));
        Assert.Equal([PluginsTests.ManifestId, PluginsTests.ManifestId], host.Unloads);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, string what)
    {
        var deadline = DateTimeOffset.UtcNow + _wait;
        while (!condition())
        {
            Assert.True(DateTimeOffset.UtcNow < deadline, $"Timed out waiting until {what}.");
            await Task.Delay(TimeSpan.FromMilliseconds(100));
        }
    }

    /// <summary>
    /// 起两个共用一个库与文件存储的实例，各有自己的解包目录与替身插件宿主
    /// </summary>
    private async Task<(BasicAppTestServer First, BasicAppTestServer Second, string Platform)> StartPairAsync(KeyValuePair<string, string?>[] settings)
    {
        var first = await StartAsync("first", settings);
        await first.SeedDefaultsAsync();
        await first.RunSeedersAsync();
        var platform = (await AuthFlowTests.LoginAsync(first, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;
        var second = await StartAsync("second", settings);
        return (first, second, platform);
    }

    private Task<BasicAppTestServer> StartAsync(string instance, KeyValuePair<string, string?>[] settings)
        => BasicAppTestServer.StartAsync(
            _database.ConnectionString,
            settings: [new("BasicApp:Files:LocalRootPath", Path.Combine(_root, "files")), new("BasicApp:Plugins:CacheDirectory", Path.Combine(_root, instance)), .. settings],
            configure: static builder => builder.Services.AddKeyedSingleton<IPluginHost>(PluginIsolation.OutOfProcess, static (services, _) => new StandInPluginHost(services.GetRequiredService<XiHan.Framework.Kernel.Operations.OperationRegistry>(), services.GetRequiredService<MessageCatalogRegistry>())));
}
