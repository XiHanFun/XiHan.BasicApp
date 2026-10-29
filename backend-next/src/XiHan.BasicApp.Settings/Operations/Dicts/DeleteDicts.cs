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

namespace XiHan.BasicApp.Settings.Operations.Dicts;

/// <summary>
/// 批量删除字典。
/// </summary>
/// <remarks>
/// <c>DELETE /api/dicts?ids=...</c>。逐项：看不见的 <c>SETTINGS_NOTFOUND_002</c>，另一侧的 <c>SETTINGS_DENIED_001</c>，
/// 内建的 <c>SETTINGS_CONFLICT_006</c>，还有字典项的 <c>SETTINGS_CONFLICT_004</c>（先删项，不级联）。
/// </remarks>
/// <param name="dicts">字典仓储。</param>
/// <param name="items">字典项仓储。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = SettingsPermissions.DictsDelete)]
public sealed class DeleteDicts(IRepository<SysDict, Guid> dicts, IRepository<SysDictItem, Guid> items) : IOperation<DictIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "dicts.delete";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(DictIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var owner = SettingsScope.OwnerOf(context);
        var found = (await dicts.FindManyAsync(request.Ids, context.CancellationToken).ConfigureAwait(false)).ToDictionary(static d => d.Id);
        var succeeded = new List<Guid>();
        var failed = new List<BatchFailure>();
        foreach (var id in request.Ids.Distinct())
        {
            if (!found.TryGetValue(id, out var dict) || !SettingsScope.IsVisible(dict, owner))
            {
                failed.Add(new BatchFailure(id, SettingsErrors.DictNotFound));
                continue;
            }

            if (!SettingsScope.IsOwnedBy(dict, owner))
            {
                failed.Add(new BatchFailure(id, SettingsErrors.OtherSide));
                continue;
            }

            if (dict.IsBuiltIn)
            {
                failed.Add(new BatchFailure(id, SettingsErrors.BuiltInProtected));
                continue;
            }

            var dictId = dict.Id;
            if (await items.CountAsync(EntityQuery.All<SysDictItem>().Where(new Specification<SysDictItem>(i => i.DictId == dictId)), context.CancellationToken).ConfigureAwait(false) > 0)
            {
                failed.Add(new BatchFailure(id, SettingsErrors.DictHasItems));
                continue;
            }

            var removed = await dicts.RemoveAsync(dict, context.CancellationToken).ConfigureAwait(false);
            if (removed.IsFailure)
            {
                return removed.Error!;
            }

            succeeded.Add(id);
        }

        return new BatchResult(succeeded, failed);
    }
}
