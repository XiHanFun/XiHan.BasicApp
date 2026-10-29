// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Settings.Domain.Entities;
using XiHan.BasicApp.Settings.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Settings.Operations.DictItems;

/// <summary>
/// 新建字典项。
/// </summary>
/// <remarks>
/// <c>POST /api/dict-items</c>。字典要在本侧（另一侧 <c>SETTINGS_DENIED_001</c>）且启用（停用 <c>SETTINGS_CONFLICT_007</c>）；
/// 编码字典内唯一；上级在同一字典；元数据是 JSON；设为缺省会取消字典里其他的缺省项。
/// </remarks>
/// <param name="items">字典项仓储。</param>
/// <param name="service">字典域服务。</param>
/// <param name="ids">标识生成器。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = SettingsPermissions.DictItemsCreate)]
public sealed class CreateDictItem(IRepository<SysDictItem, Guid> items, DictService service, IIdGenerator<Guid> ids) : IOperation<CreateDictItemRequest, DictItemResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "dict-items.create";

    /// <inheritdoc />
    public async ValueTask<Result<DictItemResponse>> HandleAsync(CreateDictItemRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var owner = SettingsScope.OwnerOf(context);
        if (!(await service.FindVisibleAsync(request.DictId, owner, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var dict))
        {
            return Error.NotFound(SettingsErrors.DictNotFound, SettingsMessages.DictNotFound, MessageArguments.Of(("id", request.DictId)));
        }

        if (!SettingsScope.IsOwnedBy(dict, owner))
        {
            return Error.Denied(SettingsErrors.OtherSide, SettingsMessages.OtherSide, MessageArguments.Of(("id", dict.Id)));
        }

        if (dict.Status != SettingStatus.Active)
        {
            return Error.Conflict(SettingsErrors.DictDisabled, SettingsMessages.DictDisabled, MessageArguments.Of(("id", dict.Id)));
        }

        var code = request.Code.Trim();
        var all = await service.ItemsOfAsync(dict.Id, context.CancellationToken).ConfigureAwait(false);
        if (all.Any(i => string.Equals(i.Code, code, StringComparison.Ordinal)))
        {
            return Error.Conflict(SettingsErrors.DictItemCodeTaken, SettingsMessages.DictItemCodeTaken, MessageArguments.Of(("code", code)));
        }

        var checkedParent = DictService.CheckParent(request.ParentId, null, all);
        if (checkedParent.IsFailure)
        {
            return checkedParent.Error!;
        }

        var checkedMetadata = DictService.CheckMetadata(request.Metadata);
        if (checkedMetadata.IsFailure)
        {
            return checkedMetadata.Error!;
        }

        var item = new SysDictItem
        {
            Id = ids.NewId(),
            OwnerTenantId = dict.OwnerTenantId,
            DictId = dict.Id,
            ParentId = request.ParentId,
            Code = code,
            Name = request.Name.Trim(),
            Value = string.IsNullOrWhiteSpace(request.Value) ? null : request.Value.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Metadata = string.IsNullOrWhiteSpace(request.Metadata) ? null : request.Metadata,
            IsDefault = request.IsDefault,
            Order = request.Order,
        };
        var added = await items.AddAsync(item, context.CancellationToken).ConfigureAwait(false);
        if (added.IsFailure)
        {
            return added.Error!;
        }

        if (item.IsDefault)
        {
            var cleared = await service.ClearOtherDefaultsAsync(dict.Id, item.Id, context.CancellationToken).ConfigureAwait(false);
            if (cleared.IsFailure)
            {
                return cleared.Error!;
            }
        }

        return DictItemResponse.From(item, DictService.DepthOf(item, [.. all, item]));
    }
}
