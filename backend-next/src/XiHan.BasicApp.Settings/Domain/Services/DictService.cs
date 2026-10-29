// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Settings.Domain.Entities;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Settings.Domain.Services;

/// <summary>
/// 字典与字典项的规则。
/// </summary>
/// <remarks>
/// 编码占用（字典不覆盖）、项的树（同字典、无环、深度优先展平）、缺省项唯一、元数据是 JSON。
/// </remarks>
/// <param name="dicts">字典仓储。</param>
/// <param name="items">字典项仓储。</param>
public sealed class DictService(IRepository<SysDict, Guid> dicts, IRepository<SysDictItem, Guid> items)
{
    /// <summary>
    /// 一个字典最多列出的项数。
    /// </summary>
    public const int MaxItems = 5000;

    /// <summary>
    /// 编码是否已被占用。
    /// </summary>
    /// <remarks>
    /// 平台建字典查全库，租户建字典查平台与本租户（字典不覆盖，平台与租户之间也不能同码）。
    /// </remarks>
    public async Task<bool> IsCodeTakenAsync(string code, string owner, CancellationToken cancellationToken)
    {
        const string platform = TenantIds.Platform;
        var specification = SettingsScope.IsPlatform(owner)
            ? new Specification<SysDict>(d => d.Code == code)
            : new Specification<SysDict>(d => d.Code == code && (d.OwnerTenantId == owner || d.OwnerTenantId == platform));
        return await dicts.CountAsync(EntityQuery.All<SysDict>().Where(specification), cancellationToken).ConfigureAwait(false) > 0;
    }

    /// <summary>
    /// 在这一侧可见的字典里按编码找。
    /// </summary>
    public async Task<Maybe<SysDict>> FindVisibleByCodeAsync(string code, string owner, CancellationToken cancellationToken)
    {
        const string platform = TenantIds.Platform;
        var rows = await dicts.ListAsync(
            EntityQuery.All<SysDict>().Where(new Specification<SysDict>(d => d.Code == code && (d.OwnerTenantId == owner || d.OwnerTenantId == platform))),
            cancellationToken).ConfigureAwait(false);
        return rows.Count == 0 ? Maybe.None<SysDict>() : Maybe.Some(rows[0]);
    }

    /// <summary>
    /// 在这一侧可见的字典里按主键找。
    /// </summary>
    public async Task<Maybe<SysDict>> FindVisibleAsync(Guid id, string owner, CancellationToken cancellationToken)
    {
        var found = await dicts.FindAsync(id, cancellationToken).ConfigureAwait(false);
        return found.TryGetValue(out var dict) && SettingsScope.IsVisible(dict, owner) ? Maybe.Some(dict) : Maybe.None<SysDict>();
    }

    /// <summary>
    /// 字典的全部项（未排序，最多 <see cref="MaxItems"/> 项）。
    /// </summary>
    public async Task<IReadOnlyList<SysDictItem>> ItemsOfAsync(Guid dictId, CancellationToken cancellationToken)
        => await items.ListAsync(EntityQuery.All<SysDictItem>().Where(new Specification<SysDictItem>(i => i.DictId == dictId)).Page(0, MaxItems), cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// 深度优先展平字典项。
    /// </summary>
    /// <remarks>
    /// 同级按排序号、再按编码；上级缺失的项当顶层。
    /// </remarks>
    /// <returns>（项, 层级）列表。</returns>
    public static IReadOnlyList<(SysDictItem Item, int Depth)> Flatten(IReadOnlyList<SysDictItem> all)
    {
        ArgumentNullException.ThrowIfNull(all);
        var ids = all.Select(static i => i.Id).ToHashSet();
        var children = all
            .Where(i => i.ParentId.HasValue && ids.Contains(i.ParentId.Value))
            .GroupBy(static i => i.ParentId!.Value)
            .ToDictionary(static g => g.Key, static g => g.OrderBy(static i => i.Order).ThenBy(static i => i.Code, StringComparer.Ordinal).ToList());
        var roots = all
            .Where(i => !i.ParentId.HasValue || !ids.Contains(i.ParentId.Value))
            .OrderBy(static i => i.Order)
            .ThenBy(static i => i.Code, StringComparer.Ordinal);
        var result = new List<(SysDictItem, int)>(all.Count);
        var stack = new Stack<(SysDictItem Item, int Depth)>();
        foreach (var root in roots.Reverse())
        {
            stack.Push((root, 0));
        }

        while (stack.Count > 0)
        {
            var (item, depth) = stack.Pop();
            result.Add((item, depth));
            if (children.TryGetValue(item.Id, out var list))
            {
                for (var index = list.Count - 1; index >= 0; index--)
                {
                    stack.Push((list[index], depth + 1));
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 一项的层级（顶层为 0）。
    /// </summary>
    public static int DepthOf(SysDictItem item, IReadOnlyList<SysDictItem> all)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(all);
        var byId = all.ToDictionary(static i => i.Id);
        var depth = 0;
        var current = item;
        while (current.ParentId is { } parentId && byId.TryGetValue(parentId, out var parent) && depth < all.Count)
        {
            depth++;
            current = parent;
        }

        return depth;
    }

    /// <summary>
    /// 校验字典项的上级。
    /// </summary>
    /// <remarks>
    /// 上级在同一字典里，不是自己，也不是自己的下级（<paramref name="itemId"/> 为空即新建，只查存在）。
    /// </remarks>
    public static Result CheckParent(Guid? parentId, Guid? itemId, IReadOnlyList<SysDictItem> all)
    {
        ArgumentNullException.ThrowIfNull(all);
        if (parentId is not { } candidate)
        {
            return Result.Success();
        }

        var byId = all.ToDictionary(static i => i.Id);
        if (!byId.ContainsKey(candidate))
        {
            return Error.Validation(SettingsErrors.DictItemParentInvalid, SettingsMessages.DictItemParentInvalid);
        }

        // 从候选上级往上走，碰到自己就成环
        var steps = 0;
        Guid? cursor = candidate;
        while (cursor is { } current && steps <= all.Count)
        {
            if (itemId == current)
            {
                return Error.Validation(SettingsErrors.DictItemParentInvalid, SettingsMessages.DictItemParentInvalid);
            }

            cursor = byId.TryGetValue(current, out var node) ? node.ParentId : null;
            steps++;
        }

        return Result.Success();
    }

    /// <summary>
    /// 元数据为空或是 JSON。
    /// </summary>
    public static Result CheckMetadata(string? metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata))
        {
            return Result.Success();
        }

        try
        {
            using var document = JsonDocument.Parse(metadata);
            return Result.Success();
        }
        catch (JsonException)
        {
            return Error.Validation(SettingsErrors.MetadataNotJson, SettingsMessages.MetadataNotJson);
        }
    }

    /// <summary>
    /// 把字典里除 <paramref name="keep"/> 之外的缺省项都取消。
    /// </summary>
    public async Task<Result> ClearOtherDefaultsAsync(Guid dictId, Guid keep, CancellationToken cancellationToken)
    {
        var others = await items.ListAsync(EntityQuery.All<SysDictItem>().Where(new Specification<SysDictItem>(i => i.DictId == dictId && i.IsDefault && i.Id != keep)), cancellationToken).ConfigureAwait(false);
        foreach (var other in others)
        {
            other.IsDefault = false;
            var updated = await items.UpdateAsync(other, cancellationToken).ConfigureAwait(false);
            if (updated.IsFailure)
            {
                return updated;
            }
        }

        return Result.Success();
    }
}
