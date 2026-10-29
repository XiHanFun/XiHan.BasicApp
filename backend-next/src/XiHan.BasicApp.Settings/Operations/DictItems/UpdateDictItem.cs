// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Settings.Domain.Entities;
using XiHan.BasicApp.Settings.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Settings.Operations.DictItems;

/// <summary>
/// 修改字典项。
/// </summary>
/// <remarks>
/// <c>PUT /api/dict-items/{id}</c>（上级、名称、附加值、说明、元数据、缺省、排序、状态；编码不可改）。
/// 只能改本侧的；上级不能是自己或下级（<c>SETTINGS_VALIDATION_004</c>）；设为缺省会取消其他缺省项。
/// </remarks>
/// <param name="items">字典项仓储。</param>
/// <param name="service">字典域服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = SettingsPermissions.DictItemsUpdate)]
public sealed class UpdateDictItem(IRepository<SysDictItem, Guid> items, DictService service) : IOperation<UpdateDictItemRequest, DictItemResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "dict-items.update";

    /// <inheritdoc />
    public async ValueTask<Result<DictItemResponse>> HandleAsync(UpdateDictItemRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var owner = SettingsScope.OwnerOf(context);
        if (!(await items.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var item) || !SettingsScope.IsVisible(item, owner))
        {
            return Error.NotFound(SettingsErrors.DictItemNotFound, SettingsMessages.DictItemNotFound, MessageArguments.Of(("id", request.Id)));
        }

        if (!SettingsScope.IsOwnedBy(item, owner))
        {
            return Error.Denied(SettingsErrors.OtherSide, SettingsMessages.OtherSide, MessageArguments.Of(("id", request.Id)));
        }

        var all = await service.ItemsOfAsync(item.DictId, context.CancellationToken).ConfigureAwait(false);
        var checkedParent = DictService.CheckParent(request.ParentId, item.Id, all);
        if (checkedParent.IsFailure)
        {
            return checkedParent.Error!;
        }

        var checkedMetadata = DictService.CheckMetadata(request.Metadata);
        if (checkedMetadata.IsFailure)
        {
            return checkedMetadata.Error!;
        }

        item.ParentId = request.ParentId;
        item.Name = request.Name.Trim();
        item.Value = string.IsNullOrWhiteSpace(request.Value) ? null : request.Value.Trim();
        item.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        item.Metadata = string.IsNullOrWhiteSpace(request.Metadata) ? null : request.Metadata;
        item.IsDefault = request.IsDefault;
        item.Order = request.Order;
        item.Status = request.Status;
        var updated = await items.UpdateAsync(item, context.CancellationToken).ConfigureAwait(false);
        if (updated.IsFailure)
        {
            return updated.Error!;
        }

        if (item.IsDefault)
        {
            var cleared = await service.ClearOtherDefaultsAsync(item.DictId, item.Id, context.CancellationToken).ConfigureAwait(false);
            if (cleared.IsFailure)
            {
                return cleared.Error!;
            }
        }

        var current = all.Select(i => i.Id == item.Id ? item : i).ToList();
        return DictItemResponse.From(item, DictService.DepthOf(item, current));
    }
}
