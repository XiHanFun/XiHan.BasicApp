// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Seeding;
using XiHan.BasicApp.Contracts.Settings;
using XiHan.BasicApp.Settings.Domain.Entities;
using XiHan.BasicApp.Settings.Domain.Services;
using XiHan.BasicApp.Settings.Infrastructure;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Settings.Seeds;

/// <summary>
/// 按各包登记的 <see cref="DictCatalogSection"/> 写平台的内建字典
/// </summary>
/// <remarks>
/// 目录类种子：字典的名称、分类、说明与登记的项（名称、附加值、上级）每次对齐，没变不写；状态、排序号与缺省项只在首次写入，之后归运营；运营另加的项不动
/// 运营先手建了同码的平台字典即收为内建；租户已有同码的字典即 <c>SETTINGS_CONFLICT_002</c>（字典不覆盖），不替它改，要由人处理
/// </remarks>
/// <param name="dicts">字典仓储</param>
/// <param name="items">字典项仓储</param>
/// <param name="catalog">各包登记的内建字典</param>
/// <param name="ids">标识生成器</param>
/// <param name="sets">字典选项缓存</param>
public sealed class DictCatalogSeeder(IRepository<SysDict, Guid> dicts, IRepository<SysDictItem, Guid> items, DictCatalog catalog, IIdGenerator<Guid> ids, DictOptionSets sets) : ISeeder
{
    /// <inheritdoc />
    public string Name => "settings.dicts";

    /// <inheritdoc />
    public int Order => SeedOrders.PlatformData + 4;

    /// <inheritdoc />
    public async ValueTask<Result> SeedAsync(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        const string platform = TenantIds.Platform;
        foreach (var descriptor in catalog.All)
        {
            var code = descriptor.Code;
            var found = await dicts.ListAsync(EntityQuery.All<SysDict>().Where(new Specification<SysDict>(d => d.Code == code)), context.CancellationToken).ConfigureAwait(false);
            var dict = found.FirstOrDefault(static d => d.OwnerTenantId == platform);
            if (dict is null && found.Count > 0)
            {
                return Error.Conflict(SettingsErrors.DictCodeTaken, SettingsMessages.DictCodeTaken, MessageArguments.Of(("code", code)));
            }

            var created = dict is null;
            var changed = created;
            if (dict is null)
            {
                dict = new SysDict { Id = ids.NewId(), OwnerTenantId = platform, Code = code, IsBuiltIn = true, Status = SettingStatus.Active, Order = descriptor.Order };
                Align(dict, descriptor);
                var added = await dicts.AddAsync(dict, context.CancellationToken).ConfigureAwait(false);
                if (added.IsFailure)
                {
                    return added;
                }
            }
            else if (!dict.IsBuiltIn || !IsAligned(dict, descriptor))
            {
                dict.IsBuiltIn = true;
                Align(dict, descriptor);
                var updated = await dicts.UpdateAsync(dict, context.CancellationToken).ConfigureAwait(false);
                if (updated.IsFailure)
                {
                    return updated;
                }

                changed = true;
            }

            var aligned = await AlignItemsAsync(dict, descriptor, created, context.CancellationToken).ConfigureAwait(false);
            if (!aligned.TryGetValue(out var itemsChanged))
            {
                return aligned.Error!;
            }

            if (changed || itemsChanged)
            {
                await sets.InvalidateAsync(platform, code, context.CancellationToken).ConfigureAwait(false);
            }
        }

        return Result.Success();
    }

    private async Task<Result<bool>> AlignItemsAsync(SysDict dict, DictDescriptor descriptor, bool created, CancellationToken cancellationToken)
    {
        var dictId = dict.Id;
        var existing = created
            ? new Dictionary<string, SysDictItem>(StringComparer.Ordinal)
            : (await items.ListAsync(EntityQuery.All<SysDictItem>().Where(new Specification<SysDictItem>(i => i.DictId == dictId)), cancellationToken).ConfigureAwait(false)).ToDictionary(static i => i.Code, StringComparer.Ordinal);
        var rows = new Dictionary<string, SysDictItem>(StringComparer.Ordinal);
        var changed = false;
        foreach (var (item, index) in descriptor.Items.Select(static (item, index) => (item, index)))
        {
            var parentId = item.Parent is null ? (Guid?)null : rows[item.Parent].Id;
            if (existing.TryGetValue(item.Code, out var row))
            {
                rows[item.Code] = row;
                if (row.Name == item.Name && row.Value == item.Value && row.ParentId == parentId)
                {
                    continue;
                }

                row.Name = item.Name;
                row.Value = item.Value;
                row.ParentId = parentId;
                var updated = await items.UpdateAsync(row, cancellationToken).ConfigureAwait(false);
                if (updated.IsFailure)
                {
                    return updated.Error!;
                }

                changed = true;
                continue;
            }

            row = new SysDictItem
            {
                Id = ids.NewId(),
                OwnerTenantId = dict.OwnerTenantId,
                DictId = dict.Id,
                ParentId = parentId,
                Code = item.Code,
                Name = item.Name,
                Value = item.Value,
                IsDefault = created && item.IsDefault,
                Status = SettingStatus.Active,
                Order = (index + 1) * 10,
            };
            var added = await items.AddAsync(row, cancellationToken).ConfigureAwait(false);
            if (added.IsFailure)
            {
                return added.Error!;
            }

            rows[item.Code] = row;
            changed = true;
        }

        return changed;
    }

    private static bool IsAligned(SysDict dict, DictDescriptor descriptor)
        => dict.Name == descriptor.Name
            && dict.Category == descriptor.Category
            && dict.Description == descriptor.Description;

    private static void Align(SysDict dict, DictDescriptor descriptor)
    {
        dict.Name = descriptor.Name;
        dict.Category = descriptor.Category;
        dict.Description = descriptor.Description;
    }
}
