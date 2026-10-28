// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Settings.Domain.Entities;
using XiHan.BasicApp.Settings.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Settings.Operations.DictItems;

/// <summary>
/// 字典项详情：<c>GET /api/dict-items/{id}</c>（带层级）。
/// </summary>
/// <param name="items">字典项仓储。</param>
/// <param name="service">字典域服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = SettingsPermissions.DictItemsRead)]
public sealed class GetDictItem(IRepository<SysDictItem, Guid> items, DictService service) : IOperation<GetDictItemRequest, DictItemResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "dict-items.get";

    /// <inheritdoc />
    public async ValueTask<Result<DictItemResponse>> HandleAsync(GetDictItemRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (!(await items.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var item) || !SettingsScope.IsVisible(item, SettingsScope.OwnerOf(context)))
        {
            return Error.NotFound(SettingsErrors.DictItemNotFound, SettingsMessages.DictItemNotFound, MessageArguments.Of(("id", request.Id)));
        }

        var all = await service.ItemsOfAsync(item.DictId, context.CancellationToken).ConfigureAwait(false);
        return DictItemResponse.From(item, DictService.DepthOf(item, all));
    }
}
