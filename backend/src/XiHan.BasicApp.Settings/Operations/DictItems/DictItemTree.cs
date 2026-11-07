// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Settings.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Settings.Operations.DictItems;

/// <summary>
/// 字典的全部项（树）
/// </summary>
/// <remarks>
/// <c>GET /api/dict-items:tree?dictId=</c>（声明只读，请求没有嵌套对象，按 ADR-0041 走 GET）：深度优先、带层级的全部项，给上级选择与级联取值用；字典不可见 <c>SETTINGS_NOTFOUND_002</c>
/// 只读目录类操作，同时作为 MCP / AI 工具暴露（5.4）；分页检索走 <c>dict-items.list</c>
/// </remarks>
/// <param name="service">字典域服务</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http | Expose.Mcp | Expose.Ai, Permission = SettingsPermissions.DictItemsRead)]
public sealed class DictItemTree(DictService service) : IOperation<DictItemTreeRequest, DictItemTreeResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "dict-items.tree";

    /// <inheritdoc />
    public async ValueTask<Result<DictItemTreeResponse>> HandleAsync(DictItemTreeRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (!(await service.FindVisibleAsync(request.DictId, SettingsScope.OwnerOf(context), context.CancellationToken).ConfigureAwait(false)).HasValue)
        {
            return Error.NotFound(SettingsErrors.DictNotFound, SettingsMessages.DictNotFound, MessageArguments.Of(("id", request.DictId)));
        }

        var all = await service.ItemsOfAsync(request.DictId, context.CancellationToken).ConfigureAwait(false);
        return new DictItemTreeResponse(DictService.Flatten(all).Select(static e => DictItemResponse.From(e.Item, e.Depth)).ToList());
    }
}
