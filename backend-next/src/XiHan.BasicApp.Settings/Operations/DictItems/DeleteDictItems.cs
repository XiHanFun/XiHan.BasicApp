// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Settings.Domain.Entities;
using XiHan.BasicApp.Settings.Domain.Services;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Settings.Operations.DictItems;

/// <summary>
/// 批量删除字典项。
/// </summary>
/// <remarks>
/// <c>DELETE /api/dict-items?ids=...</c>。逐项：看不见的 <c>SETTINGS_NOTFOUND_003</c>，另一侧的 <c>SETTINGS_DENIED_001</c>，还有下级的 <c>SETTINGS_CONFLICT_005</c>。
/// 同一批里先删下级再删上级也按删除时的状态判（入参顺序即处理顺序）。
/// </remarks>
/// <param name="items">字典项仓储。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = SettingsPermissions.DictItemsDelete)]
public sealed class DeleteDictItems(IRepository<SysDictItem, Guid> items) : IOperation<DictItemIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "dict-items.delete";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(DictItemIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var owner = SettingsScope.OwnerOf(context);
        var found = (await items.FindManyAsync(request.Ids, context.CancellationToken).ConfigureAwait(false)).ToDictionary(static i => i.Id);
        var succeeded = new List<Guid>();
        var failed = new List<BatchFailure>();
        foreach (var id in request.Ids.Distinct())
        {
            if (!found.TryGetValue(id, out var item) || !SettingsScope.IsVisible(item, owner))
            {
                failed.Add(new BatchFailure(id, SettingsErrors.DictItemNotFound));
                continue;
            }

            if (!SettingsScope.IsOwnedBy(item, owner))
            {
                failed.Add(new BatchFailure(id, SettingsErrors.OtherSide));
                continue;
            }

            var itemId = item.Id;
            if (await items.CountAsync(EntityQuery.All<SysDictItem>().Where(new Specification<SysDictItem>(i => i.ParentId == itemId)), context.CancellationToken).ConfigureAwait(false) > 0)
            {
                failed.Add(new BatchFailure(id, SettingsErrors.DictItemHasChildren));
                continue;
            }

            var removed = await items.RemoveAsync(item, context.CancellationToken).ConfigureAwait(false);
            if (removed.IsFailure)
            {
                return removed.Error!;
            }

            succeeded.Add(id);
        }

        return new BatchResult(succeeded, failed);
    }
}
