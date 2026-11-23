// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Plugins.Domain.Entities;
using XiHan.BasicApp.Plugins.Domain.Packages;
using XiHan.BasicApp.Plugins.Domain.Services;
using XiHan.BasicApp.Plugins.Infrastructure;
using XiHan.BasicApp.Plugins.Queries;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Plugins.Operations.Plugins;

/// <summary>
/// 插件列表
/// </summary>
/// <remarks>
/// <c>POST /api/plugins:list</c>。筛选与排序见 <see cref="PluginQueries"/>
/// </remarks>
/// <param name="queries">插件查询</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = PluginsPermissions.PluginsRead)]
public sealed class ListPlugins(PluginQueries queries) : IOperation<PageRequest, PageResult<PluginResponse>>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "plugins.list";

    /// <inheritdoc />
    public ValueTask<Result<PageResult<PluginResponse>>> HandleAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return queries.ListAsync(request, context);
    }
}

/// <summary>
/// 插件详情
/// </summary>
/// <remarks>
/// <c>GET /api/plugins/{id}</c>。当前版本的贡献、申请的授予、已批准的授予与全部版本；不存在 <c>PLUGINS_NOTFOUND_101</c>
/// </remarks>
/// <param name="plugins">插件仓储</param>
/// <param name="service">插件服务</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = PluginsPermissions.PluginsRead)]
public sealed class GetPlugin(IRepository<SysPlugin, Guid> plugins, PluginService service) : IOperation<GetPluginRequest, PluginDetailResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "plugins.get";

    /// <inheritdoc />
    public async ValueTask<Result<PluginDetailResponse>> HandleAsync(GetPluginRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return (await plugins.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var plugin)
            ? await service.DetailAsync(plugin, context.CancellationToken).ConfigureAwait(false)
            : PluginService.NotFound(request.Id);
    }
}

/// <summary>
/// 核对一个已上传的插件包
/// </summary>
/// <remarks>
/// <c>GET /api/plugins:inspect?fileId=</c>。只读：解包核对，给出贡献与申请的授予，不落库。包与清单的规则见 <see cref="PluginInspector"/>，清单 ID 或短名已经安装过 <c>PLUGINS_CONFLICT_101</c>
/// </remarks>
/// <param name="service">插件服务</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = PluginsPermissions.PluginsInstall)]
public sealed class InspectPlugin(PluginService service) : IOperation<InspectPluginRequest, PluginInspectionResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "plugins.inspect";

    /// <inheritdoc />
    public async ValueTask<Result<PluginInspectionResponse>> HandleAsync(InspectPluginRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var inspected = await service.InspectAsync(request.FileId, context.CancellationToken).ConfigureAwait(false);
        return inspected.TryGetValue(out var inspection) ? PluginInspectionResponse.From(inspection) : inspected.Error!;
    }
}

/// <summary>
/// 安装一个已上传的插件包
/// </summary>
/// <remarks>
/// <c>POST /api/plugins</c>。核对同 <c>plugins.inspect</c>；授予要含全部必须项（缺了 <c>PLUGINS_DENIED_101</c>），另外只能有可选项（<c>PLUGINS_VALIDATION_106</c>）。装好是停用状态
/// </remarks>
/// <param name="service">插件服务</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = PluginsPermissions.PluginsInstall)]
public sealed class InstallPlugin(PluginService service) : IOperation<InstallPluginRequest, PluginDetailResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "plugins.install";

    /// <inheritdoc />
    public async ValueTask<Result<PluginDetailResponse>> HandleAsync(InstallPluginRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var installed = await service.InstallAsync(request, context.CancellationToken).ConfigureAwait(false);
        return installed.TryGetValue(out var plugin)
            ? await service.DetailAsync(plugin, context.CancellationToken).ConfigureAwait(false)
            : installed.Error!;
    }
}

/// <summary>
/// 改一个插件已批准的授予
/// </summary>
/// <remarks>
/// <c>POST /api/plugins/{id}:grant</c>。整组替换，规则同安装；并发戳过期由框架仓储报。启用着的插件提交后按新的授予重新加载
/// </remarks>
/// <param name="plugins">插件仓储</param>
/// <param name="service">插件服务</param>
/// <param name="changes">本次调用的插件改动</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = PluginsPermissions.PluginsGrant)]
public sealed class GrantPlugin(IRepository<SysPlugin, Guid> plugins, PluginService service, PluginChanges changes) : IOperation<GrantPluginRequest, PluginDetailResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "plugins.grant";

    /// <inheritdoc />
    public async ValueTask<Result<PluginDetailResponse>> HandleAsync(GrantPluginRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (!(await plugins.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var plugin))
        {
            return PluginService.NotFound(request.Id);
        }

        var detail = await service.DetailAsync(plugin, context.CancellationToken).ConfigureAwait(false);
        var approved = PluginService.NormalizeGrants(request.Grants, detail.RequiredGrants, detail.OptionalGrants);
        if (!approved.TryGetValue(out var granted))
        {
            return approved.Error!;
        }

        // 改授予也算改插件：交回的并发戳过期即失败，并发的两次授权只有一次生效
        plugin.ConcurrencyStamp = request.ConcurrencyStamp;
        var touched = await plugins.UpdateAsync(plugin, context.CancellationToken).ConfigureAwait(false);
        if (touched.IsFailure)
        {
            return touched.Error!;
        }

        var replaced = await service.ReplaceGrantsAsync(plugin.Id, granted, context.CancellationToken).ConfigureAwait(false);
        if (replaced.IsFailure)
        {
            return replaced.Error!;
        }

        changes.Request();
        return await service.DetailAsync(plugin, context.CancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// 批量卸载插件
/// </summary>
/// <remarks>
/// <c>DELETE /api/plugins?ids=...</c>。逐项：不存在 <c>PLUGINS_NOTFOUND_101</c>；卸载软删除插件与全部版本、删掉授予，同一插件之后可以再装。加载着的插件提交后即卸下
/// </remarks>
/// <param name="plugins">插件仓储</param>
/// <param name="service">插件服务</param>
/// <param name="changes">本次调用的插件改动</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = PluginsPermissions.PluginsDelete)]
public sealed class DeletePlugins(IRepository<SysPlugin, Guid> plugins, PluginService service, PluginChanges changes) : IOperation<PluginIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "plugins.delete";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(PluginIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = (await plugins.FindManyAsync(request.Ids, context.CancellationToken).ConfigureAwait(false)).ToDictionary(static p => p.Id);
        var succeeded = new List<Guid>();
        var failed = new List<BatchFailure>();
        foreach (var id in request.Ids.Distinct())
        {
            if (!found.TryGetValue(id, out var plugin))
            {
                failed.Add(BatchFailure.From(id, PluginService.NotFound(id)));
                continue;
            }

            var removed = await service.UninstallAsync(plugin, context.CancellationToken).ConfigureAwait(false);
            if (removed.IsFailure)
            {
                return removed.Error!;
            }

            succeeded.Add(id);
        }

        if (succeeded.Count > 0)
        {
            changes.Request();
        }

        return new BatchResult(succeeded, failed);
    }
}

/// <summary>
/// 批量启用插件
/// </summary>
/// <remarks>
/// <c>POST /api/plugins:enable</c>。逐项：不存在 <c>PLUGINS_NOTFOUND_101</c>。提交后本节点立即加载，加载失败不改变这里的结果，错误码记在插件上（列表与详情的 <c>lastErrorCode</c>、<c>plugins.status</c>）；已启用的再启用一次即重试加载
/// </remarks>
/// <param name="plugins">插件仓储</param>
/// <param name="changes">本次调用的插件改动</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = PluginsPermissions.PluginsUpdate)]
public sealed class EnablePlugins(IRepository<SysPlugin, Guid> plugins, PluginChanges changes) : IOperation<PluginIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "plugins.enable";

    /// <inheritdoc />
    public ValueTask<Result<BatchResult>> HandleAsync(PluginIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return PluginSwitch.SetAsync(plugins, changes, request.Ids, enabled: true, context.CancellationToken);
    }
}

/// <summary>
/// 批量停用插件
/// </summary>
/// <remarks>
/// <c>POST /api/plugins:disable</c>。逐项：不存在 <c>PLUGINS_NOTFOUND_101</c>。提交后本节点立即卸下：权限码与回调授予随之撤回，在途调用照常完成或以框架的 <c>PLUGINS_UNAVAILABLE_001</c> 结束
/// </remarks>
/// <param name="plugins">插件仓储</param>
/// <param name="changes">本次调用的插件改动</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = PluginsPermissions.PluginsUpdate)]
public sealed class DisablePlugins(IRepository<SysPlugin, Guid> plugins, PluginChanges changes) : IOperation<PluginIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "plugins.disable";

    /// <inheritdoc />
    public ValueTask<Result<BatchResult>> HandleAsync(PluginIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return PluginSwitch.SetAsync(plugins, changes, request.Ids, enabled: false, context.CancellationToken);
    }
}

/// <summary>
/// 插件在应答节点上的运行状态
/// </summary>
/// <remarks>
/// <c>GET /api/plugins/{id}:status</c>。是否已加载、加载的版本、宿主报告的生命周期状态与本节点最近一次的错误码；不存在 <c>PLUGINS_NOTFOUND_101</c>
/// </remarks>
/// <param name="plugins">插件仓储</param>
/// <param name="reconciler">插件对齐服务</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = PluginsPermissions.PluginsRead)]
public sealed class GetPluginStatus(IRepository<SysPlugin, Guid> plugins, PluginReconciler reconciler) : IOperation<GetPluginRequest, PluginStatusResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "plugins.status";

    /// <inheritdoc />
    public async ValueTask<Result<PluginStatusResponse>> HandleAsync(GetPluginRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (!(await plugins.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var plugin))
        {
            return PluginService.NotFound(request.Id);
        }

        var loaded = reconciler.Loaded.GetValueOrDefault(plugin.ManifestId);
        var status = reconciler.HostStatusOf(plugin.ManifestId, plugin.Isolation);
        var error = reconciler.Failures.TryGetValue(plugin.Id, out var failure) ? failure.Error.Code : status?.Error?.Code;
        return new PluginStatusResponse(plugin.Id, plugin.ManifestId, plugin.IsEnabled, loaded is not null, loaded?.Version, status?.State, error);
    }
}

/// <summary>
/// 启停的共用写法
/// </summary>
internal static class PluginSwitch
{
    /// <summary>
    /// 逐项改启用状态，有一项成功即标记对齐
    /// </summary>
    public static async ValueTask<Result<BatchResult>> SetAsync(IRepository<SysPlugin, Guid> plugins, PluginChanges changes, IReadOnlyList<Guid> ids, bool enabled, CancellationToken cancellationToken)
    {
        var found = (await plugins.FindManyAsync(ids, cancellationToken).ConfigureAwait(false)).ToDictionary(static p => p.Id);
        var succeeded = new List<Guid>();
        var failed = new List<BatchFailure>();
        foreach (var id in ids.Distinct())
        {
            if (!found.TryGetValue(id, out var plugin))
            {
                failed.Add(BatchFailure.From(id, PluginService.NotFound(id)));
                continue;
            }

            if (plugin.IsEnabled != enabled)
            {
                plugin.IsEnabled = enabled;
                var updated = await plugins.UpdateAsync(plugin, cancellationToken).ConfigureAwait(false);
                if (updated.IsFailure)
                {
                    return updated.Error!;
                }
            }

            succeeded.Add(id);
        }

        if (succeeded.Count > 0)
        {
            changes.Request();
        }

        return new BatchResult(succeeded, failed);
    }
}
