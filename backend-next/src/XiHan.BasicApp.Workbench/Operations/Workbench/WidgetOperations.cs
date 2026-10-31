// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts.Workbench;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Primitives;
using XiHan.Framework.Security.Abstractions.Permissions;
using SecurityPolicies = XiHan.BasicApp.Contracts.Security.Policies;

namespace XiHan.BasicApp.Workbench.Operations.Workbench;

/// <summary>
/// <c>workbench.widgets</c> 的请求（没有参数）。
/// </summary>
public sealed record WorkbenchWidgetsRequest;

/// <summary>
/// 调用方看得到的一个小组件。
/// </summary>
/// <param name="Key">键。</param>
/// <param name="Kind">取数形态。</param>
public sealed record WorkbenchWidgetResponse(string Key, WorkbenchWidgetKind Kind);

/// <summary>
/// 调用方看得到的小组件。
/// </summary>
/// <param name="Items">小组件，按键排序。</param>
public sealed record WorkbenchWidgetsResponse(IReadOnlyList<WorkbenchWidgetResponse> Items);

/// <summary>
/// <c>workbench.widget-data</c> 的请求。
/// </summary>
/// <param name="Key">小组件的键。</param>
/// <param name="Days">统计最近几天，含今天（趋势、构成类用，缺省 30）。</param>
/// <param name="Offset">查看者所在时区相对 UTC 的分钟数（东八区为 480），「今天」与按日分桶据此算。</param>
public sealed record WorkbenchWidgetDataRequest(
    [Required, StringLength(100, MinimumLength = 1)] string Key,
    [Range(1, 366)] int Days = 30,
    [Range(-840, 840)] int Offset = 0);

/// <summary>
/// 各包登记的小组件与权限判定。
/// </summary>
/// <param name="providers">各包的小组件提供者。</param>
/// <param name="permissions">权限判定。</param>
public sealed class WidgetCatalog(IEnumerable<IWorkbenchWidgetProvider> providers, IPermissionChecker permissions)
{
    /// <summary>
    /// 调用方看得到的小组件。
    /// </summary>
    public async ValueTask<IReadOnlyList<WorkbenchWidget>> VisibleAsync(InvocationContext context)
    {
        var security = context.GetSecurity();
        var visible = new List<WorkbenchWidget>();
        foreach (var widget in providers.SelectMany(static p => p.Widgets))
        {
            if (widget.Permission is null || await permissions.HasPermissionAsync(security, widget.Permission, context.CancellationToken).ConfigureAwait(false))
            {
                visible.Add(widget);
            }
        }

        return visible.OrderBy(static w => w.Key, StringComparer.Ordinal).ToList();
    }

    /// <summary>
    /// 按键找到小组件与登记它的提供者，并核对权限。
    /// </summary>
    public async ValueTask<Result<IWorkbenchWidgetProvider>> ResolveAsync(string key, InvocationContext context)
    {
        foreach (var provider in providers)
        {
            if (provider.Widgets.FirstOrDefault(w => string.Equals(w.Key, key, StringComparison.Ordinal)) is not { } widget)
            {
                continue;
            }

            if (widget.Permission is not null && !await permissions.HasPermissionAsync(context.GetSecurity(), widget.Permission, context.CancellationToken).ConfigureAwait(false))
            {
                return Error.Denied(WorkbenchErrors.WidgetDenied, WorkbenchMessages.WidgetDenied, MessageArguments.Of(("key", key)));
            }

            return Result.Success(provider);
        }

        return Error.NotFound(WorkbenchErrors.WidgetNotFound, WorkbenchMessages.WidgetNotFound, MessageArguments.Of(("key", key)));
    }
}

/// <summary>
/// 列出调用方看得到的工作台小组件。
/// </summary>
/// <remarks>
/// <c>GET /api/workbench:widgets</c>（自助操作，只要求已认证）。汇总各包登记的小组件，没声明权限的人人可见，声明了的按调用方的权限筛。
/// </remarks>
/// <param name="catalog">小组件目录。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Policy = SecurityPolicies.Authenticated)]
public sealed class ListWidgets(WidgetCatalog catalog) : IOperation<WorkbenchWidgetsRequest, WorkbenchWidgetsResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "workbench.widgets";

    /// <inheritdoc />
    public async ValueTask<Result<WorkbenchWidgetsResponse>> HandleAsync(WorkbenchWidgetsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var visible = await catalog.VisibleAsync(context).ConfigureAwait(false);
        return new WorkbenchWidgetsResponse(visible.Select(static w => new WorkbenchWidgetResponse(w.Key, w.Kind)).ToList());
    }
}

/// <summary>
/// 取一个工作台小组件的数据。
/// </summary>
/// <remarks>
/// <c>GET /api/workbench:widget-data</c>（自助操作，只要求已认证）。先核对小组件声明的权限，再交给登记它的包取当前一侧的数据；
/// 没有这个键 <c>WORKBENCH_NOTFOUND_001</c>，没有权限 <c>WORKBENCH_DENIED_001</c>。
/// </remarks>
/// <param name="catalog">小组件目录。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Policy = SecurityPolicies.Authenticated)]
public sealed class GetWidgetData(WidgetCatalog catalog) : IOperation<WorkbenchWidgetDataRequest, WorkbenchWidgetData>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "workbench.widget-data";

    /// <inheritdoc />
    public async ValueTask<Result<WorkbenchWidgetData>> HandleAsync(WorkbenchWidgetDataRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var resolved = await catalog.ResolveAsync(request.Key, context).ConfigureAwait(false);
        if (!resolved.TryGetValue(out var provider))
        {
            return resolved.Error!;
        }

        return await provider.GetAsync(request.Key, new WorkbenchWidgetQuery(request.Days, TimeSpan.FromMinutes(request.Offset)), context).ConfigureAwait(false);
    }
}
