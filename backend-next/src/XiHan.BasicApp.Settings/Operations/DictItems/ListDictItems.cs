// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Settings.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Settings.Operations.DictItems;

/// <summary>
/// 字典的全部项：<c>GET /api/dict-items?dictId=</c>（深度优先、带层级，最多 5000 项；字典不可见 <c>SETTINGS_NOTFOUND_002</c>）。
/// 只读目录类操作，同时作为 MCP / AI 工具暴露（5.4）。
/// </summary>
/// <param name="service">字典域服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http | Expose.Mcp | Expose.Ai, Permission = SettingsPermissions.DictItemsRead)]
public sealed class ListDictItems(DictService service) : IOperation<ListDictItemsRequest, DictItemsResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "dict-items.list";

    /// <inheritdoc />
    public async ValueTask<Result<DictItemsResponse>> HandleAsync(ListDictItemsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (!(await service.FindVisibleAsync(request.DictId, SettingsScope.OwnerOf(context), context.CancellationToken).ConfigureAwait(false)).HasValue)
        {
            return Error.NotFound(SettingsErrors.DictNotFound, SettingsMessages.DictNotFound, MessageArguments.Of(("id", request.DictId)));
        }

        var all = await service.ItemsOfAsync(request.DictId, context.CancellationToken).ConfigureAwait(false);
        return new DictItemsResponse(DictService.Flatten(all).Select(static e => DictItemResponse.From(e.Item, e.Depth)).ToList());
    }
}
