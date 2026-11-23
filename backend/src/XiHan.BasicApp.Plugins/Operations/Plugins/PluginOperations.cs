// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Contracts.Security;
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
/// <c>GET /api/plugins:inspect?fileId=&amp;pluginId=</c>。只读：解包核对，给出签名结论、贡献与申请的授予，不落库。包与清单的规则见 <see cref="PluginInspector"/>
/// 不给 <c>pluginId</c> 是新装：清单 ID 或短名已经安装过 <c>PLUGINS_CONFLICT_101</c>；给了是升级核对，规则见 <see cref="PluginService.InspectAsync"/>，结果带上与当前版本的对照
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
        var inspected = await service.InspectAsync(request.FileId, request.PluginId, context.CancellationToken).ConfigureAwait(false);
        return inspected.TryGetValue(out var inspection) ? PluginInspectionResponse.From(inspection) : inspected.Error!;
    }
}

/// <summary>
/// 安装一个已上传的插件包
/// </summary>
/// <remarks>
/// <c>POST /api/plugins</c>。核对同 <c>plugins.inspect</c>；授予要含全部必须项（缺了 <c>PLUGINS_DENIED_101</c>），另外只能有可选项（<c>PLUGINS_VALIDATION_106</c>）。装好是停用状态，页面节点提交后进「插件应用」目录（启用前不下发）
/// </remarks>
/// <param name="service">插件服务</param>
/// <param name="changes">本次调用的插件改动</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = PluginsPermissions.PluginsInstall)]
public sealed class InstallPlugin(PluginService service, PluginChanges changes) : IOperation<InstallPluginRequest, PluginDetailResponse>
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
        if (!installed.TryGetValue(out var plugin))
        {
            return installed.Error!;
        }

        changes.Request();
        return await service.DetailAsync(plugin, context.CancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// 把插件升级到一个已上传的新版本
/// </summary>
/// <remarks>
/// <c>POST /api/plugins/{id}:upgrade</c>。先用 <c>plugins.inspect</c> 带上 <c>pluginId</c> 看会变什么；核对同那里的升级，授予按新版本申请的整组替换（规则同安装），交回的并发戳过期由框架仓储报
/// 启用着的插件提交后即换上新版本：先卸旧版再载新版，新版起不来就重载旧版，错误码记在插件上；旧版本留着，可以用 <c>plugins.rollback</c> 换回
/// </remarks>
/// <param name="plugins">插件仓储</param>
/// <param name="service">插件服务</param>
/// <param name="changes">本次调用的插件改动</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = PluginsPermissions.PluginsInstall)]
public sealed class UpgradePlugin(IRepository<SysPlugin, Guid> plugins, PluginService service, PluginChanges changes) : IOperation<UpgradePluginRequest, PluginDetailResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "plugins.upgrade";

    /// <inheritdoc />
    public async ValueTask<Result<PluginDetailResponse>> HandleAsync(UpgradePluginRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (!(await plugins.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var plugin))
        {
            return PluginService.NotFound(request.Id);
        }

        var upgraded = await service.UpgradeAsync(plugin, request, context.CancellationToken).ConfigureAwait(false);
        if (upgraded.IsFailure)
        {
            return upgraded.Error!;
        }

        changes.Request();
        return await service.DetailAsync(plugin, context.CancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// 把插件换回一个已上传过的版本
/// </summary>
/// <remarks>
/// <c>POST /api/plugins/{id}:rollback</c>。版本取详情的 <c>versions[].id</c>，授予按那个版本申请的整组替换（<c>versions[].requiredGrants</c>，规则同安装）；规则与错误码见 <see cref="PluginService.RollbackAsync"/>
/// 启用着的插件提交后即换上那个版本，加载结果同升级；换掉的版本留着，可以再换回来
/// </remarks>
/// <param name="plugins">插件仓储</param>
/// <param name="service">插件服务</param>
/// <param name="changes">本次调用的插件改动</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = PluginsPermissions.PluginsInstall)]
public sealed class RollbackPlugin(IRepository<SysPlugin, Guid> plugins, PluginService service, PluginChanges changes) : IOperation<RollbackPluginRequest, PluginDetailResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "plugins.rollback";

    /// <inheritdoc />
    public async ValueTask<Result<PluginDetailResponse>> HandleAsync(RollbackPluginRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (!(await plugins.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var plugin))
        {
            return PluginService.NotFound(request.Id);
        }

        var switched = await service.RollbackAsync(plugin, request, context.CancellationToken).ConfigureAwait(false);
        if (switched.IsFailure)
        {
            return switched.Error!;
        }

        changes.Request();
        return await service.DetailAsync(plugin, context.CancellationToken).ConfigureAwait(false);
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
/// 插件全部版本贡献过的权限码，各处的引用在同一个工作单元里清掉（先用 <c>plugins.delete-preview</c> 看会清掉什么）；授权申请与日志是留档，不改
/// </remarks>
/// <param name="plugins">插件仓储</param>
/// <param name="service">插件服务</param>
/// <param name="references">各包对权限码的引用</param>
/// <param name="changes">本次调用的插件改动</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = PluginsPermissions.PluginsDelete)]
public sealed class DeletePlugins(IRepository<SysPlugin, Guid> plugins, PluginService service, IEnumerable<IPermissionReferences> references, PluginChanges changes) : IOperation<PluginIdsRequest, BatchResult>
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

            var codes = await service.PermissionsOfAsync(plugin.Id, context.CancellationToken).ConfigureAwait(false);
            foreach (var store in references)
            {
                var cleared = await store.RemoveAsync(codes, context.CancellationToken).ConfigureAwait(false);
                if (cleared.IsFailure)
                {
                    return cleared.Error!;
                }
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
/// 卸载一个插件会清掉的内容
/// </summary>
/// <remarks>
/// <c>GET /api/plugins/{id}:delete-preview</c>。插件全部版本贡献过的权限码，与各处对它们的引用：角色与用户授予、授予规则、权限条件、委托、互斥约束、运营节点的门控权限码、套餐白名单（卸载时清掉），以及各租户库里待审的授权申请（留档不改，批准时会因权限码不存在被拒）
/// 有一侧的库读不出即 <c>ACCESS_UNAVAILABLE_003</c>，不给出不全的清单；不存在 <c>PLUGINS_NOTFOUND_101</c>
/// </remarks>
/// <param name="plugins">插件仓储</param>
/// <param name="service">插件服务</param>
/// <param name="references">各包对权限码的引用</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = PluginsPermissions.PluginsDelete)]
public sealed class PreviewPluginDelete(IRepository<SysPlugin, Guid> plugins, PluginService service, IEnumerable<IPermissionReferences> references) : IOperation<GetPluginRequest, PluginDeletePreviewResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "plugins.delete-preview";

    /// <inheritdoc />
    public async ValueTask<Result<PluginDeletePreviewResponse>> HandleAsync(GetPluginRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (!(await plugins.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var plugin))
        {
            return PluginService.NotFound(request.Id);
        }

        var codes = await service.PermissionsOfAsync(plugin.Id, context.CancellationToken).ConfigureAwait(false);
        var found = new List<PermissionReferenceResponse>();
        foreach (var store in references)
        {
            var listed = await store.FindAsync(codes, context.CancellationToken).ConfigureAwait(false);
            if (!listed.TryGetValue(out var items))
            {
                return listed.Error!;
            }

            found.AddRange(items.Select(static r => new PermissionReferenceResponse(r.Kind, r.Side, r.Subject, r.Permission)));
        }

        return new PluginDeletePreviewResponse(plugin.Id, plugin.ManifestId, plugin.Name, codes, [.. found.OrderBy(static r => r.Kind).ThenBy(static r => r.Side, StringComparer.Ordinal).ThenBy(static r => r.Subject, StringComparer.Ordinal).ThenBy(static r => r.Permission, StringComparer.Ordinal)]);
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
