// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Immutable;
using Microsoft.Extensions.Primitives;
using XiHan.BasicApp.Contracts.Security;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Plugins.Infrastructure;

/// <summary>
/// 已加载插件的运行期贡献（单例）
/// </summary>
/// <remarks>
/// 插件加载成功后放进来、卸载时撤回，每次变动触发变更令牌：权限码随之进出 <see cref="RegisteredPermissions"/>（权限目录、审计、套餐），
/// 回调权限码经 <see cref="PluginGrantSource"/> 给插件主体，批准了 <c>data-scope:all</c> 的插件主体不受数据范围限制
/// </remarks>
public sealed class PluginRuntimeCatalog : IRuntimePermissionSource, IPluginDataScopes, IDisposable
{
    private readonly Lock _gate = new();
    private ImmutableDictionary<string, LoadedPlugin> _loaded = ImmutableDictionary.Create<string, LoadedPlugin>(StringComparer.Ordinal);
    private CancellationTokenSource _changes = new();

    /// <summary>
    /// 当前已加载的插件，按短名排序
    /// </summary>
    public IReadOnlyList<LoadedPlugin> Loaded => [.. Volatile.Read(ref _loaded).Values.OrderBy(static p => p.Name, StringComparer.Ordinal)];

    /// <inheritdoc />
    public IReadOnlyList<PermissionCatalogSection> Sections => [.. Loaded.Select(static p => new PermissionCatalogSection(p.Name, p.Permissions))];

    /// <summary>
    /// 按清单 ID 找已加载的插件
    /// </summary>
    /// <param name="manifestId">清单 ID</param>
    public Maybe<LoadedPlugin> Find(string manifestId)
    {
        ArgumentNullException.ThrowIfNull(manifestId);
        return Volatile.Read(ref _loaded).TryGetValue(manifestId, out var plugin) ? Maybe.Some(plugin) : Maybe.None<LoadedPlugin>();
    }

    /// <inheritdoc />
    public bool IsUnrestricted(string manifestId) => Find(manifestId).TryGetValue(out var plugin) && plugin.UnrestrictedData;

    /// <inheritdoc />
    public IChangeToken GetChangeToken() => new CancellationChangeToken(Volatile.Read(ref _changes).Token);

    /// <summary>
    /// 放进一个已加载的插件（同一清单 ID 即替换）
    /// </summary>
    /// <param name="plugin">插件的运行期贡献</param>
    public void Put(LoadedPlugin plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        lock (_gate)
        {
            Volatile.Write(ref _loaded, _loaded.SetItem(plugin.ManifestId, plugin));
        }

        Notify();
    }

    /// <summary>
    /// 撤回一个插件
    /// </summary>
    /// <param name="manifestId">清单 ID</param>
    /// <returns>原来是否在</returns>
    public bool Remove(string manifestId)
    {
        ArgumentNullException.ThrowIfNull(manifestId);
        lock (_gate)
        {
            if (!_loaded.ContainsKey(manifestId))
            {
                return false;
            }

            Volatile.Write(ref _loaded, _loaded.Remove(manifestId));
        }

        Notify();
        return true;
    }

    /// <inheritdoc />
    public void Dispose() => Volatile.Read(ref _changes).Dispose();

    /// <summary>
    /// 触发当前的令牌并换上新的
    /// </summary>
    /// <remarks>
    /// 订阅方的回调在这里同步执行，抛出的异常传给变动的一方
    /// </remarks>
    private void Notify()
    {
        using var previous = Interlocked.Exchange(ref _changes, new CancellationTokenSource());
        previous.Cancel();
    }
}
