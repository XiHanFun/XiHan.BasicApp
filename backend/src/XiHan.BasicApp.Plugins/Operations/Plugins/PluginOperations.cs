// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Plugins.Domain.Entities;
using XiHan.BasicApp.Plugins.Domain.Packages;
using XiHan.BasicApp.Plugins.Domain.Services;
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
/// <c>POST /api/plugins/{id}:grant</c>。整组替换，规则同安装；并发戳过期由框架仓储报
/// </remarks>
/// <param name="plugins">插件仓储</param>
/// <param name="service">插件服务</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = PluginsPermissions.PluginsGrant)]
public sealed class GrantPlugin(IRepository<SysPlugin, Guid> plugins, PluginService service) : IOperation<GrantPluginRequest, PluginDetailResponse>
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
        return replaced.IsFailure ? replaced.Error! : await service.DetailAsync(plugin, context.CancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// 批量卸载插件
/// </summary>
/// <remarks>
/// <c>DELETE /api/plugins?ids=...</c>。逐项：不存在 <c>PLUGINS_NOTFOUND_101</c>；卸载软删除插件与全部版本、删掉授予，同一插件之后可以再装
/// </remarks>
/// <param name="plugins">插件仓储</param>
/// <param name="service">插件服务</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = PluginsPermissions.PluginsDelete)]
public sealed class DeletePlugins(IRepository<SysPlugin, Guid> plugins, PluginService service) : IOperation<PluginIdsRequest, BatchResult>
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

        return new BatchResult(succeeded, failed);
    }
}
