// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json.Nodes;
using XiHan.Framework.Kernel.Capabilities;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Kernel.Plugins;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Localization.Abstractions.Messages;
using XiHan.Framework.Primitives;
using XiHan.Framework.Primitives.Manifests;

namespace XiHan.BasicApp.Integration.Tests.Infrastructure;

/// <summary>
/// 不起进程的替身插件宿主
/// </summary>
/// <remarks>
/// 照框架进程外宿主的约定：从解包目录读清单，授予不全即拒；成功时以清单 ID 为归属把贡献的权限码登记进登记表、把消息目录登记进本地化，卸载时按归属撤回
/// 消息目录代替握手带来的那份：只有旁挂清单短名的接口文档分组名与说明（en-US）；<see cref="WithoutCatalog"/> 为真时不登记
/// 记下每次加载的目录与授予；<see cref="FailWith"/> 让指定的清单加载失败
/// </remarks>
/// <param name="registry">操作登记表</param>
/// <param name="catalogs">消息目录登记表</param>
public sealed class StandInPluginHost(OperationRegistry registry, MessageCatalogRegistry catalogs) : IPluginHost
{
    private readonly Lock _gate = new();
    private readonly Dictionary<string, PluginStatus> _plugins = new(StringComparer.Ordinal);
    private readonly List<StandInLoad> _loads = [];
    private readonly List<string> _unloads = [];

    /// <summary>
    /// 让某次加载失败：返回非空即以它失败
    /// </summary>
    public Func<Manifest, Error?>? FailWith { get; set; }

    /// <summary>
    /// 加载时不登记消息目录
    /// </summary>
    public bool WithoutCatalog { get; set; }

    /// <summary>
    /// 每次加载
    /// </summary>
    public IReadOnlyList<StandInLoad> Loads
    {
        get
        {
            lock (_gate)
            {
                return [.. _loads];
            }
        }
    }

    /// <summary>
    /// 每次卸载的清单 ID
    /// </summary>
    public IReadOnlyList<string> Unloads
    {
        get
        {
            lock (_gate)
            {
                return [.. _unloads];
            }
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<PluginStatus> Plugins
    {
        get
        {
            lock (_gate)
            {
                return [.. _plugins.Values.OrderBy(static p => p.Manifest.Id, StringComparer.Ordinal)];
            }
        }
    }

    /// <inheritdoc />
    public Task<Result<PluginStatus>> LoadAsync(PluginSource source, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        var manifest = ManifestReader.ReadFile(Path.Combine(source.Directory, ManifestReader.FileName)).ValueOrThrow();
        lock (_gate)
        {
            _loads.Add(new StandInLoad(manifest.Id, manifest.Version, source.Directory, source.Permissions.Granted));
            if (_plugins.ContainsKey(manifest.Id))
            {
                return Task.FromResult<Result<PluginStatus>>(Error.Conflict("PLUGINS_CONFLICT_001", "plugins.standin.loaded"));
            }

            if (source.Permissions.Missing(manifest).Concat(source.Permissions.MissingConfigurationGrants(manifest)).Any())
            {
                return Task.FromResult<Result<PluginStatus>>(Error.Denied("PLUGINS_DENIED_001", "plugins.standin.denied"));
            }

            if (FailWith?.Invoke(manifest) is { } error)
            {
                return Task.FromResult<Result<PluginStatus>>(error);
            }

            var registered = registry.AddRange(manifest.Id, [], [.. manifest.Contributes.Permissions.Select(static c => new PermissionDefinition(c, c))]);
            if (registered.IsFailure)
            {
                return Task.FromResult<Result<PluginStatus>>(registered.Error!);
            }

            if (!WithoutCatalog)
            {
                catalogs.Add(manifest.Id, [CatalogOf(source.Directory)]);
            }

            var status = new PluginStatus(manifest, LifecycleState.Ready);
            _plugins[manifest.Id] = status;
            return Task.FromResult<Result<PluginStatus>>(status);
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// 替身的消息目录：旁挂清单短名的分组名与说明
    /// </summary>
    private static MessageCatalog CatalogOf(string directory)
    {
        var name = JsonNode.Parse(File.ReadAllText(Path.Combine(directory, "basicapp.plugin.json")))!["name"]!.GetValue<string>().Replace('-', '_');
        return new MessageCatalog("standin." + name, new Dictionary<string, IReadOnlyDictionary<string, string>>
        {
            ["en-US"] = new Dictionary<string, string> { [$"openapi.groups.{name}.name"] = name, [$"openapi.groups.{name}.description"] = "Stand-in plugin." },
        });
    }

    public Task<Result> UnloadAsync(string manifestId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(manifestId);
        lock (_gate)
        {
            if (!_plugins.Remove(manifestId))
            {
                return Task.FromResult<Result>(Error.NotFound("PLUGINS_NOTFOUND_001", "plugins.standin.not_found"));
            }

            registry.RemoveOwnedBy(manifestId);
            catalogs.RemoveOwnedBy(manifestId);
            _unloads.Add(manifestId);
            return Task.FromResult(Result.Success());
        }
    }
}

/// <summary>
/// 替身宿主记下的一次加载
/// </summary>
/// <param name="ManifestId">清单 ID</param>
/// <param name="Version">清单的版本号</param>
/// <param name="Directory">解包目录</param>
/// <param name="Grants">交给宿主的授予</param>
public sealed record StandInLoad(string ManifestId, string Version, string Directory, IReadOnlySet<string> Grants);
