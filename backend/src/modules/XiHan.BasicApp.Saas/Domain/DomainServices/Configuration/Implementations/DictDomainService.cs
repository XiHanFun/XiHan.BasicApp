#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DictDomainService
// Guid:7f8e1471-bb13-4889-bc8e-c32c2416aa12
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Text.Json;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 字典领域服务实现
/// </summary>
public sealed class DictDomainService
    : IDictDomainService
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public DictDomainService(
        IDictRepository dictRepository,
        IDictItemRepository dictItemRepository)
    {
        _dictRepository = dictRepository;
        _dictItemRepository = dictItemRepository;
    }

    private readonly IDictItemRepository _dictItemRepository;
    private readonly IDictRepository _dictRepository;

    /// <inheritdoc />
    public async Task<DictCommandResult> CreateDictAsync(DictCreateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureEnum(command.Status, nameof(command.Status));
        var dictCode = Required(command.DictCode, 100, nameof(command.DictCode), "字典编码不能超过 100 个字符。");
        EnsureCodeHasNoWhitespace(dictCode, "字典编码不能包含空白字符。");
        if (await _dictRepository.AnyAsync(dict => dict.DictCode == dictCode, cancellationToken))
        {
            throw new InvalidOperationException("字典编码已存在。");
        }

        var dict = new SysDict
        {
            DictCode = dictCode,
            DictName = Required(command.DictName, 100, nameof(command.DictName), "字典名称不能超过 100 个字符。"),
            DictType = Required(command.DictType, 50, nameof(command.DictType), "字典类型不能超过 50 个字符。"),
            DictDescription = Optional(command.DictDescription, 500, nameof(command.DictDescription), "字典描述不能超过 500 个字符。"),
            IsBuiltIn = false,
            Status = command.Status,
            Sort = command.Sort,
            Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。")
        };

        return new DictCommandResult(await _dictRepository.AddAsync(dict, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<DictItemCommandResult> CreateDictItemAsync(DictItemCreateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateDictItemCreateCommand(command);
        var dict = await GetDictOrThrowAsync(command.DictId, cancellationToken);
        if (dict.Status != EnableStatus.Enabled)
        {
            throw new InvalidOperationException("系统字典未启用。");
        }

        _ = await ValidateParentAsync(command.DictId, command.ParentId, currentItemId: null, cancellationToken);
        var itemCode = Required(command.ItemCode, 100, nameof(command.ItemCode), "字典项编码不能超过 100 个字符。");
        EnsureCodeHasNoWhitespace(itemCode, "字典项编码不能包含空白字符。");
        if (await _dictItemRepository.AnyAsync(item => item.DictId == command.DictId && item.ItemCode == itemCode, cancellationToken))
        {
            throw new InvalidOperationException("字典项编码已存在。");
        }

        if (command.IsDefault)
        {
            await ClearDefaultItemsAsync(command.DictId, excludeItemId: null, cancellationToken);
        }

        var dictItem = new SysDictItem
        {
            DictId = command.DictId,
            ParentId = command.ParentId,
            ItemCode = itemCode,
            ItemName = Required(command.ItemName, 100, nameof(command.ItemName), "字典项名称不能超过 100 个字符。"),
            ItemValue = Optional(command.ItemValue, 500, nameof(command.ItemValue), "字典项值不能超过 500 个字符。"),
            ItemDescription = Optional(command.ItemDescription, 500, nameof(command.ItemDescription), "字典项描述不能超过 500 个字符。"),
            Metadata = OptionalJson(command.Metadata, "字典项元数据必须是合法 JSON。"),
            IsDefault = command.IsDefault,
            Status = command.Status,
            Sort = command.Sort,
            Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。")
        };

        return new DictItemCommandResult(await _dictItemRepository.AddAsync(dictItem, cancellationToken));
    }

    /// <inheritdoc />
    public async Task DeleteDictAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var dict = await GetDictOrThrowAsync(id, cancellationToken);
        if (dict.IsBuiltIn)
        {
            throw new InvalidOperationException("内置系统字典不能删除。");
        }

        if (await _dictItemRepository.AnyAsync(item => item.DictId == dict.BasicId, cancellationToken))
        {
            throw new InvalidOperationException("系统字典存在字典项，不能直接删除。");
        }

        if (!await _dictRepository.DeleteAsync(dict, cancellationToken))
        {
            throw new InvalidOperationException("系统字典删除失败。");
        }
    }

    /// <inheritdoc />
    public async Task DeleteDictItemAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var dictItem = await GetDictItemOrThrowAsync(id, cancellationToken);
        if (await _dictItemRepository.AnyAsync(item => item.ParentId == dictItem.BasicId, cancellationToken))
        {
            throw new InvalidOperationException("系统字典项存在子节点，不能直接删除。");
        }

        if (!await _dictItemRepository.DeleteAsync(dictItem, cancellationToken))
        {
            throw new InvalidOperationException("系统字典项删除失败。");
        }
    }

    /// <inheritdoc />
    public async Task<DictCommandResult> UpdateDictAsync(DictUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.BasicId, "系统字典主键必须大于 0。");
        var dict = await GetDictOrThrowAsync(command.BasicId, cancellationToken);
        dict.DictName = Required(command.DictName, 100, nameof(command.DictName), "字典名称不能超过 100 个字符。");
        dict.DictType = Required(command.DictType, 50, nameof(command.DictType), "字典类型不能超过 50 个字符。");
        dict.DictDescription = Optional(command.DictDescription, 500, nameof(command.DictDescription), "字典描述不能超过 500 个字符。");
        dict.Sort = command.Sort;
        dict.Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。");

        return new DictCommandResult(await _dictRepository.UpdateAsync(dict, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<DictItemCommandResult> UpdateDictItemAsync(DictItemUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateDictItemUpdateCommand(command);
        var dictItem = await GetDictItemOrThrowAsync(command.BasicId, cancellationToken);
        _ = await ValidateParentAsync(dictItem.DictId, command.ParentId, dictItem.BasicId, cancellationToken);
        if (command.IsDefault)
        {
            await ClearDefaultItemsAsync(dictItem.DictId, dictItem.BasicId, cancellationToken);
        }

        dictItem.ParentId = command.ParentId;
        dictItem.ItemName = Required(command.ItemName, 100, nameof(command.ItemName), "字典项名称不能超过 100 个字符。");
        dictItem.ItemValue = Optional(command.ItemValue, 500, nameof(command.ItemValue), "字典项值不能超过 500 个字符。");
        dictItem.ItemDescription = Optional(command.ItemDescription, 500, nameof(command.ItemDescription), "字典项描述不能超过 500 个字符。");
        dictItem.Metadata = OptionalJson(command.Metadata, "字典项元数据必须是合法 JSON。");
        dictItem.IsDefault = command.IsDefault;
        dictItem.Sort = command.Sort;
        dictItem.Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。");

        return new DictItemCommandResult(await _dictItemRepository.UpdateAsync(dictItem, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<DictItemCommandResult> UpdateDictItemStatusAsync(DictItemStatusChangeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.BasicId, "系统字典项主键必须大于 0。");
        EnsureEnum(command.Status, nameof(command.Status));

        var dictItem = await GetDictItemOrThrowAsync(command.BasicId, cancellationToken);
        dictItem.Status = command.Status;
        dictItem.Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。") ?? dictItem.Remark;

        return new DictItemCommandResult(await _dictItemRepository.UpdateAsync(dictItem, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<DictCommandResult> UpdateDictStatusAsync(DictStatusChangeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.BasicId, "系统字典主键必须大于 0。");
        EnsureEnum(command.Status, nameof(command.Status));

        var dict = await GetDictOrThrowAsync(command.BasicId, cancellationToken);
        dict.Status = command.Status;
        dict.Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。") ?? dict.Remark;

        return new DictCommandResult(await _dictRepository.UpdateAsync(dict, cancellationToken));
    }

    private static void EnsureCodeHasNoWhitespace(string value, string message)
    {
        if (value.Any(char.IsWhiteSpace))
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void ValidateDictItemCreateCommand(DictItemCreateCommand command)
    {
        EnsureId(command.DictId, "系统字典主键必须大于 0。");
        EnsureOptionalId(command.ParentId, nameof(command.ParentId), "父级字典项主键必须大于 0。");
        EnsureEnum(command.Status, nameof(command.Status));
    }

    private static void ValidateDictItemUpdateCommand(DictItemUpdateCommand command)
    {
        EnsureId(command.BasicId, "系统字典项主键必须大于 0。");
        EnsureOptionalId(command.ParentId, nameof(command.ParentId), "父级字典项主键必须大于 0。");
    }

    private Task<bool> ClearDefaultItemsAsync(long dictId, long? excludeItemId, CancellationToken cancellationToken)
    {
        return excludeItemId.HasValue
            ? _dictItemRepository.UpdateAsync(
                item => new SysDictItem { IsDefault = false },
                item => item.DictId == dictId && item.BasicId != excludeItemId.Value && item.IsDefault,
                cancellationToken)
            : _dictItemRepository.UpdateAsync(
                item => new SysDictItem { IsDefault = false },
                item => item.DictId == dictId && item.IsDefault,
                cancellationToken);
    }

    private async Task EnsureNoParentCycleAsync(SysDictItem parent, long? currentItemId, CancellationToken cancellationToken)
    {
        if (!currentItemId.HasValue)
        {
            return;
        }

        var visited = new HashSet<long> { currentItemId.Value };
        var cursor = parent;
        while (cursor.ParentId.HasValue)
        {
            if (cursor.ParentId.Value == currentItemId.Value)
            {
                throw new InvalidOperationException("系统字典项父子关系不能形成环路。");
            }

            if (!visited.Add(cursor.BasicId))
            {
                throw new InvalidOperationException("系统字典项父子关系存在环路。");
            }

            var next = await _dictItemRepository.GetByIdAsync(cursor.ParentId.Value, cancellationToken);
            if (next is null)
            {
                return;
            }

            cursor = next;
        }
    }

    private async Task<SysDictItem> GetDictItemOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        EnsureId(id, "系统字典项主键必须大于 0。");
        return await _dictItemRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("系统字典项不存在。");
    }

    private async Task<SysDict> GetDictOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        EnsureId(id, "系统字典主键必须大于 0。");
        return await _dictRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("系统字典不存在。");
    }

    private async Task<SysDictItem?> ValidateParentAsync(long dictId, long? parentId, long? currentItemId, CancellationToken cancellationToken)
    {
        EnsureOptionalId(parentId, nameof(parentId), "父级字典项主键必须大于 0。");
        if (!parentId.HasValue)
        {
            return null;
        }

        if (currentItemId.HasValue && parentId.Value == currentItemId.Value)
        {
            throw new InvalidOperationException("系统字典项不能选择自身作为父级。");
        }

        var parent = await _dictItemRepository.GetByIdAsync(parentId.Value, cancellationToken)
            ?? throw new InvalidOperationException("父级字典项不存在。");
        if (parent.DictId != dictId)
        {
            throw new InvalidOperationException("父级字典项必须属于同一个系统字典。");
        }

        await EnsureNoParentCycleAsync(parent, currentItemId, cancellationToken);
        return parent;
    }

    private static void EnsureEnum<TEnum>(TEnum value, string paramName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(paramName, "枚举值无效。");
        }
    }

    private static void EnsureId(long id, string message)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), message);
        }
    }

    private static void EnsureOptionalId(long? id, string paramName, string message)
    {
        if (id is <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? Optional(string? value, int maxLength, string paramName, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }

        return normalized;
    }

    private static string? OptionalJson(string? value, string message)
    {
        var normalized = NormalizeNullable(value);
        if (normalized is null)
        {
            return null;
        }

        try
        {
            using var _ = JsonDocument.Parse(normalized);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(message, exception);
        }

        return normalized;
    }

    private static string Required(string? value, int maxLength, string paramName, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }

        return normalized;
    }
}
