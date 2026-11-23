// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Contracts.Pages;
using XiHan.BasicApp.Plugins.Domain.Entities;
using XiHan.BasicApp.Plugins.Domain.Packages;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Kernel.Plugins;
using XiHan.Framework.Localization.Abstractions.Messages;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;
using XiHan.Framework.Primitives.Manifests;

namespace XiHan.BasicApp.Plugins.Infrastructure;

/// <summary>
/// 把本节点加载的插件对齐到库里启用的插件（单例）
/// </summary>
/// <remarks>
/// 方案 5.4：启动时与管理操作提交后各对齐一次。已安装插件的页面变了先把菜单对齐过去（<see cref="PluginPages"/>）；再卸掉不再启用的，逐个加载启用的：版本或授予变了先卸后载；加载成功即把运行期贡献放进 <see cref="PluginRuntimeCatalog"/>
/// 加载失败只让这个插件失败：错误码记进 <c>sys_plugin.last_error_code</c>，节点的健康项降级；换版本失败时重载旧版本，旧版本也起不来就留着失败
/// 宿主加载成功后再核对插件的消息目录（随握手进本地化器）：接口文档的分组名与说明、每个资源标签的显示名要有 en-US，缺了即卸掉、记为失败
/// 插件宿主按隔离级别取键控服务 <see cref="IPluginHost"/>，没装的隔离级别 <c>PLUGINS_UNAVAILABLE_101</c>
/// 一次对齐有总时限（<see cref="PluginsOptions.ReconcileTimeoutSeconds"/>），超时的插件记为失败，下一次对齐再试；对齐之间串行
/// </remarks>
/// <param name="scopes">作用域工厂</param>
/// <param name="services">根容器（取键控的插件宿主）</param>
/// <param name="cache">本机解包目录</param>
/// <param name="catalog">已加载插件的运行期贡献</param>
/// <param name="pages">已安装插件的页面</param>
/// <param name="options">插件模块选项</param>
/// <param name="logger">日志</param>
public sealed partial class PluginReconciler(IServiceScopeFactory scopes, IServiceProvider services, PluginCache cache, PluginRuntimeCatalog catalog, PluginPages pages, PluginsOptions options, ILogger<PluginReconciler> logger) : IDisposable
{
    /// <summary>
    /// 插件消息目录必有的源语言
    /// </summary>
    private const string SourceLocale = "en-US";

    private bool _menusStale;

    private readonly SemaphoreSlim _gate = new(1, 1);
    private ImmutableDictionary<string, PluginRelease> _loaded = ImmutableDictionary.Create<string, PluginRelease>(StringComparer.Ordinal);
    private ImmutableDictionary<Guid, PluginFailure> _failures = ImmutableDictionary<Guid, PluginFailure>.Empty;

    /// <summary>
    /// 本节点当前加载的版本，按清单 ID
    /// </summary>
    public IReadOnlyDictionary<string, PluginRelease> Loaded => Volatile.Read(ref _loaded);

    /// <summary>
    /// 本节点上加载失败的插件，按插件 ID
    /// </summary>
    public IReadOnlyDictionary<Guid, PluginFailure> Failures => Volatile.Read(ref _failures);

    /// <summary>
    /// 插件宿主报告的状态
    /// </summary>
    /// <param name="manifestId">清单 ID</param>
    /// <param name="isolation">隔离级别</param>
    /// <returns>状态；没装这个隔离级别的宿主或宿主里没有它为空</returns>
    public PluginStatus? HostStatusOf(string manifestId, PluginIsolation isolation)
        => services.GetKeyedService<IPluginHost>(isolation)?.Plugins.FirstOrDefault(p => string.Equals(p.Manifest.Id, manifestId, StringComparison.Ordinal));

    /// <summary>
    /// 对齐一次
    /// </summary>
    /// <remarks>
    /// 超过总时限即停下，没轮到的插件留到下一次；外部取消照常抛出
    /// </remarks>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task ReconcileAsync(CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.ReconcileTimeoutSeconds));
        try
        {
            await _gate.WaitAsync(timeout.Token).ConfigureAwait(false);
            try
            {
                await AlignAsync(timeout.Token).ConfigureAwait(false);
            }
            finally
            {
                _gate.Release();
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            LogTimedOut(logger, options.ReconcileTimeoutSeconds);
        }
    }

    /// <inheritdoc />
    public void Dispose() => _gate.Dispose();

    private async Task AlignAsync(CancellationToken cancellationToken)
    {
        var snapshot = await StoreScope.ReadPrimaryAsync(scopes, SnapshotAsync, cancellationToken).ConfigureAwait(false);
        await AlignMenusAsync(snapshot.Pages, cancellationToken).ConfigureAwait(false);
        var desired = snapshot.Releases.ToDictionary(static r => r.ManifestId, StringComparer.Ordinal);
        foreach (var loaded in Loaded.Values.Where(l => !desired.ContainsKey(l.ManifestId)).ToList())
        {
            await UnloadAsync(loaded, cancellationToken).ConfigureAwait(false);
        }

        // 不再启用的插件（含从没加载成功的）不算失败
        var enabled = desired.Values.Select(static r => r.PluginId).ToHashSet();
        ImmutableInterlocked.Update(ref _failures, static (failures, keep) => failures.RemoveRange(failures.Keys.Where(id => !keep.Contains(id))), enabled);

        foreach (var release in desired.Values.OrderBy(static r => r.Name, StringComparer.Ordinal))
        {
            var previous = Loaded.GetValueOrDefault(release.ManifestId);
            if (previous is not null && previous.SameAs(release))
            {
                continue;
            }

            if (previous is not null)
            {
                await UnloadAsync(previous, cancellationToken).ConfigureAwait(false);
            }

            var loaded = await LoadAsync(release, cancellationToken).ConfigureAwait(false);
            await RecordAsync(release, loaded.IsFailure ? loaded.Error : null, cancellationToken).ConfigureAwait(false);
            if (loaded.IsSuccess)
            {
                Forget(release.PluginId);
            }
            else if (previous is not null && previous.VersionId != release.VersionId)
            {
                // 换版本失败：退回旧版本，新版本的失败留着给管理员看，直到它加载成功
                var restored = await LoadAsync(previous, cancellationToken).ConfigureAwait(false);
                if (restored.IsFailure)
                {
                    LogRestoreFailed(logger, previous.ManifestId, previous.Version, restored.Error!.Code);
                }
            }
        }
    }

    /// <summary>
    /// 库里的插件：启用的应当加载成什么样，已安装的有哪些页面
    /// </summary>
    private static async Task<PluginSnapshot> SnapshotAsync(IServiceProvider provider, CancellationToken cancellationToken)
    {
        var plugins = await provider.GetRequiredService<IRepository<SysPlugin, Guid>>().ListAsync(EntityQuery.All<SysPlugin>(), cancellationToken).ConfigureAwait(false);
        if (plugins.Count == 0)
        {
            return new PluginSnapshot([], []);
        }

        var versions = (await provider.GetRequiredService<IRepository<SysPluginVersion, Guid>>().FindManyAsync([.. plugins.Select(static p => p.CurrentVersionId)], cancellationToken).ConfigureAwait(false)).ToDictionary(static v => v.Id);
        var pages = plugins
            .OrderBy(static p => p.Name, StringComparer.Ordinal)
            .Select(p => new PageCatalogSection(p.Name, PluginPages.PagesOf(versions[p.CurrentVersionId].DescriptorJson)))
            .Where(static s => s.Pages.Count > 0)
            .ToList();
        var enabled = plugins.Where(static p => p.IsEnabled).ToList();
        if (enabled.Count == 0)
        {
            return new PluginSnapshot([], pages);
        }

        var ids = enabled.Select(static p => p.Id).ToList();
        var grants = (await provider.GetRequiredService<IRepository<SysPluginGrant, Guid>>().ListAsync(EntityQuery.All<SysPluginGrant>().Where(new Specification<SysPluginGrant>(g => ids.Contains(g.PluginId))), cancellationToken).ConfigureAwait(false))
            .ToLookup(static g => g.PluginId, static g => g.GrantValue);
        return new PluginSnapshot(
            [.. enabled.Select(p =>
            {
                var version = versions[p.CurrentVersionId];
                return new PluginRelease(p.Id, p.ManifestId, p.Name, p.Isolation, version.Id, version.Version, version.FileId, version.Sha256, version.DescriptorJson, [.. grants[p.Id].Order(StringComparer.Ordinal)], p.LastErrorCode);
            })],
            pages);
    }

    /// <summary>
    /// 已安装插件的页面变了：换上新的一份，再把菜单对齐过去
    /// </summary>
    /// <remarks>
    /// 对齐失败（如菜单表一时写不进）记日志，下一次对齐再试
    /// </remarks>
    private async Task AlignMenusAsync(IReadOnlyList<PageCatalogSection> installed, CancellationToken cancellationToken)
    {
        if (!pages.Replace(installed) && !_menusStale)
        {
            return;
        }

        var aligned = await StoreScope.WriteAsync(scopes, static (provider, ct) => provider.GetRequiredService<IPageMenuAligner>().AlignAsync(ct), cancellationToken).ConfigureAwait(false);
        _menusStale = aligned.IsFailure;
        if (aligned.IsFailure)
        {
            LogMenusFailed(logger, aligned.Error!.Code);
        }
    }

    /// <summary>
    /// 一次读库的结果
    /// </summary>
    /// <param name="Releases">启用的插件应当加载的样子</param>
    /// <param name="Pages">已安装插件的页面</param>
    private sealed record PluginSnapshot(IReadOnlyList<PluginRelease> Releases, IReadOnlyList<PageCatalogSection> Pages);

    /// <summary>
    /// 加载一个版本
    /// </summary>
    /// <remarks>
    /// 宿主里留着同一清单 ID 的残留（上次加载半途取消、握手失败后宿主还在重试）先卸掉；加载失败时宿主若把它留作失败也卸掉，免得宿主自己反复重试
    /// </remarks>
    private async Task<Result> LoadAsync(PluginRelease release, CancellationToken cancellationToken)
    {
        if (services.GetKeyedService<IPluginHost>(release.Isolation) is not { } host)
        {
            return Fail(release, Error.Unavailable(PluginsErrors.IsolationUnavailable, PluginsMessages.IsolationUnavailable, MessageArguments.Of(("isolation", PluginsOptions.NameOf(release.Isolation)))));
        }

        var directory = await cache.EnsureAsync(release, cancellationToken).ConfigureAwait(false);
        if (!directory.TryGetValue(out var path))
        {
            return Fail(release, directory.Error!);
        }

        var frontend = await ReadFrontendAsync(release, path, cancellationToken).ConfigureAwait(false);
        if (!frontend.TryGetValue(out var files))
        {
            return Fail(release, frontend.Error!);
        }

        await RemoveFromHostAsync(host, release.ManifestId, cancellationToken).ConfigureAwait(false);
        var loaded = await host.LoadAsync(new PluginSource(path, release.HostGrant), cancellationToken).ConfigureAwait(false);
        if (!loaded.TryGetValue(out var status))
        {
            await RemoveFromHostAsync(host, release.ManifestId, cancellationToken).ConfigureAwait(false);
            return Fail(release, loaded.Error!);
        }

        var localizer = services.GetRequiredService<IMessageLocalizer>();
        if (MissingKey(release, localizer) is { } missing)
        {
            await RemoveFromHostAsync(host, release.ManifestId, cancellationToken).ConfigureAwait(false);
            return Fail(release, Error.Validation(PluginsErrors.CatalogIncomplete, PluginsMessages.CatalogIncomplete, MessageArguments.Of(("name", release.Name), ("key", missing))));
        }

        Volatile.Write(ref _loaded, _loaded.SetItem(release.ManifestId, release));
        catalog.Put(release.ToLoaded(files, key => localizer.Find(key, SourceLocale).TryGetValue(out var template) ? template : null));
        LogLoaded(logger, release.ManifestId, release.Version, status.State);
        return Result.Success();
    }

    /// <summary>
    /// 读入前端文件：页面的视图描述与 10 种区域的文案
    /// </summary>
    /// <remarks>
    /// 安装时核对过文件在包里，这里读不出同样当包不可用
    /// </remarks>
    private async Task<Result<IReadOnlyDictionary<string, string>>> ReadFrontendAsync(PluginRelease release, string directory, CancellationToken cancellationToken)
    {
        var files = new Dictionary<string, string>(StringComparer.Ordinal);
        var paths = release.ReadDescriptor().Pages.Select(static p => p.View.File).Concat(PluginInspector.Locales.Select(PluginTexts.EntryOf)).Distinct(StringComparer.Ordinal);
        foreach (var file in paths)
        {
            try
            {
                files[file] = await File.ReadAllTextAsync(Path.Combine(directory, file), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                LogFrontendUnreadable(logger, exception, release.ManifestId, file);
                return Error.Unavailable(PluginsErrors.PackageUnavailable, PluginsMessages.PackageUnavailable, MessageArguments.Of(("name", release.Name), ("version", release.Version)));
            }
        }

        return files;
    }

    /// <summary>
    /// 插件的消息目录缺的第一个接口文档用键
    /// </summary>
    /// <remarks>
    /// 分组名与说明（组名即短名）、插件每个资源的标签显示名，都按 en-US 找；资源取宿主里归属这个插件的操作。键里的连字符换成下划线，与接口文档生成器的找法一致
    /// </remarks>
    private string? MissingKey(PluginRelease release, IMessageLocalizer localizer)
    {
        var group = release.Name.Replace('-', '_');
        var resources = services.GetRequiredService<OperationRegistry>().All()
            .Where(d => string.Equals(d.OwnerId, release.ManifestId, StringComparison.Ordinal))
            .Select(static d => d.Contract.Resource.Replace('-', '_'))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal);
        return new[] { $"openapi.groups.{group}.name", $"openapi.groups.{group}.description" }
            .Concat(resources.Select(static r => $"openapi.tags.{r}.name"))
            .FirstOrDefault(key => !localizer.Find(key, SourceLocale).HasValue);
    }

    /// <summary>
    /// 卸载一个版本：先撤回运行期贡献，再让宿主停掉插件
    /// </summary>
    private async Task UnloadAsync(PluginRelease release, CancellationToken cancellationToken)
    {
        catalog.Remove(release.ManifestId);
        Volatile.Write(ref _loaded, _loaded.Remove(release.ManifestId));
        if (services.GetKeyedService<IPluginHost>(release.Isolation) is { } host)
        {
            await RemoveFromHostAsync(host, release.ManifestId, cancellationToken).ConfigureAwait(false);
        }

        LogUnloaded(logger, release.ManifestId, release.Version);
    }

    /// <summary>
    /// 宿主里有这个清单 ID 就卸掉
    /// </summary>
    private static async Task RemoveFromHostAsync(IPluginHost host, string manifestId, CancellationToken cancellationToken)
    {
        if (host.Plugins.Any(p => string.Equals(p.Manifest.Id, manifestId, StringComparison.Ordinal)))
        {
            // 卸载与查列表之间被别处卸掉了即 NOTFOUND，结果一样
            _ = await host.UnloadAsync(manifestId, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 记下本节点上的失败
    /// </summary>
    private Result Fail(PluginRelease release, Error error)
    {
        ImmutableInterlocked.Update(ref _failures, static (failures, failure) => failures.SetItem(failure.PluginId, failure), new PluginFailure(release.PluginId, release.ManifestId, release.Version, error));
        LogLoadFailed(logger, release.ManifestId, release.Version, error.Code);
        return error;
    }

    private void Forget(Guid pluginId) => ImmutableInterlocked.Update(ref _failures, static (failures, id) => failures.Remove(id), pluginId);

    /// <summary>
    /// 把加载结论写回库：错误码变了才写
    /// </summary>
    private async Task RecordAsync(PluginRelease release, Error? error, CancellationToken cancellationToken)
    {
        var code = error?.Code;
        if (string.Equals(code, release.LastErrorCode, StringComparison.Ordinal))
        {
            return;
        }

        var written = await StoreScope.WriteAsync(scopes, async (provider, ct) =>
        {
            var plugins = provider.GetRequiredService<IRepository<SysPlugin, Guid>>();
            if (!(await plugins.FindAsync(release.PluginId, ct).ConfigureAwait(false)).TryGetValue(out var plugin))
            {
                return Result.Success();
            }

            plugin.LastErrorCode = code;
            return await plugins.UpdateAsync(plugin, ct).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
        if (written.IsFailure)
        {
            LogRecordFailed(logger, release.ManifestId, written.Error!.Code);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Plugin {ManifestId} {Version} loaded ({State}).")]
    private static partial void LogLoaded(ILogger logger, string manifestId, string version, LifecycleState state);

    [LoggerMessage(Level = LogLevel.Error, Message = "The front-end file '{File}' of plugin {ManifestId} could not be read from its package directory.")]
    private static partial void LogFrontendUnreadable(ILogger logger, Exception exception, string manifestId, string file);

    [LoggerMessage(Level = LogLevel.Information, Message = "Plugin {ManifestId} {Version} unloaded.")]
    private static partial void LogUnloaded(ILogger logger, string manifestId, string version);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Plugin {ManifestId} {Version} failed to load: {Code}.")]
    private static partial void LogLoadFailed(ILogger logger, string manifestId, string version, string code);

    [LoggerMessage(Level = LogLevel.Error, Message = "Plugin {ManifestId} could not be restored to version {Version} after a failed version change: {Code}.")]
    private static partial void LogRestoreFailed(ILogger logger, string manifestId, string version, string code);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The load result of plugin {ManifestId} could not be recorded: {Code}.")]
    private static partial void LogRecordFailed(ILogger logger, string manifestId, string code);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Menus could not be aligned to the installed plugin pages: {Code}; retried on the next reconciliation.")]
    private static partial void LogMenusFailed(ILogger logger, string code);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Plugin reconciliation did not finish within {Seconds} s; the remaining plugins are retried on the next reconciliation.")]
    private static partial void LogTimedOut(ILogger logger, int seconds);
}
