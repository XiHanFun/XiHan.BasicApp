#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DictAppService
// Guid:7916f366-9883-4cf6-a61a-55e1bb99cfae
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/06 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Uow.Attributes;
using static XiHan.BasicApp.Saas.Application.AppServices.SaasCommandValidation;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 系统字典命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "系统字典")]
public sealed class DictAppService
    : SaasApplicationService, IDictAppService
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public DictAppService(
        IDictRepository dictRepository,
        IDictItemRepository dictItemRepository)
    {
        _dictRepository = dictRepository;
        _dictItemRepository = dictItemRepository;
    }

    private readonly IDictRepository _dictRepository;
    private readonly IDictItemRepository _dictItemRepository;

    /// <summary>
    /// 创建系统字典
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Dict.Create)]
    public async Task<DictDetailDto> CreateDictAsync(DictCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateDictCreateInput(input);
        var dictCode = Required(input.DictCode, 100, nameof(input.DictCode), "字典编码不能超过 100 个字符。");
        EnsureCodeHasNoWhitespace(dictCode, "字典编码不能包含空白字符。");
        if (await _dictRepository.AnyAsync(dict => dict.DictCode == dictCode, cancellationToken))
        {
            throw new InvalidOperationException("字典编码已存在。");
        }

        var dict = new SysDict
        {
            DictCode = dictCode,
            DictName = Required(input.DictName, 100, nameof(input.DictName), "字典名称不能超过 100 个字符。"),
            DictType = Required(input.DictType, 50, nameof(input.DictType), "字典类型不能超过 50 个字符。"),
            DictDescription = Optional(input.DictDescription, 500, nameof(input.DictDescription), "字典描述不能超过 500 个字符。"),
            IsBuiltIn = false,
            Status = input.Status,
            Sort = input.Sort,
            Remark = Optional(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。")
        };

        var savedDict = await _dictRepository.AddAsync(dict, cancellationToken);
        return DictApplicationMapper.ToDetailDto(savedDict);
    }

    /// <summary>
    /// 更新系统字典
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Dict.Update)]
    public async Task<DictDetailDto> UpdateDictAsync(DictUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateDictUpdateInput(input);
        var dict = await GetDictOrThrowAsync(input.BasicId, cancellationToken);
        dict.DictName = Required(input.DictName, 100, nameof(input.DictName), "字典名称不能超过 100 个字符。");
        dict.DictType = Required(input.DictType, 50, nameof(input.DictType), "字典类型不能超过 50 个字符。");
        dict.DictDescription = Optional(input.DictDescription, 500, nameof(input.DictDescription), "字典描述不能超过 500 个字符。");
        dict.Sort = input.Sort;
        dict.Remark = Optional(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。");

        var savedDict = await _dictRepository.UpdateAsync(dict, cancellationToken);
        return DictApplicationMapper.ToDetailDto(savedDict);
    }

    /// <summary>
    /// 更新系统字典状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Dict.Status)]
    public async Task<DictDetailDto> UpdateDictStatusAsync(DictStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(input.BasicId, "系统字典主键必须大于 0。");
        EnsureEnum(input.Status, nameof(input.Status));

        var dict = await GetDictOrThrowAsync(input.BasicId, cancellationToken);
        dict.Status = input.Status;
        dict.Remark = Optional(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。") ?? dict.Remark;

        var savedDict = await _dictRepository.UpdateAsync(dict, cancellationToken);
        return DictApplicationMapper.ToDetailDto(savedDict);
    }

    /// <summary>
    /// 删除系统字典
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Dict.Delete)]
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

    /// <summary>
    /// 创建系统字典项
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Dict.Create)]
    public async Task<DictItemDetailDto> CreateDictItemAsync(DictItemCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateDictItemCreateInput(input);
        var dict = await GetDictOrThrowAsync(input.DictId, cancellationToken);
        if (dict.Status != EnableStatus.Enabled)
        {
            throw new InvalidOperationException("系统字典未启用。");
        }

        _ = await ValidateParentAsync(input.DictId, input.ParentId, currentItemId: null, cancellationToken);
        var itemCode = Required(input.ItemCode, 100, nameof(input.ItemCode), "字典项编码不能超过 100 个字符。");
        EnsureCodeHasNoWhitespace(itemCode, "字典项编码不能包含空白字符。");
        if (await _dictItemRepository.AnyAsync(item => item.DictId == input.DictId && item.ItemCode == itemCode, cancellationToken))
        {
            throw new InvalidOperationException("字典项编码已存在。");
        }

        if (input.IsDefault)
        {
            await ClearDefaultItemsAsync(input.DictId, excludeItemId: null, cancellationToken);
        }

        var dictItem = new SysDictItem
        {
            DictId = input.DictId,
            ParentId = input.ParentId,
            ItemCode = itemCode,
            ItemName = Required(input.ItemName, 100, nameof(input.ItemName), "字典项名称不能超过 100 个字符。"),
            ItemValue = Optional(input.ItemValue, 500, nameof(input.ItemValue), "字典项值不能超过 500 个字符。"),
            ItemDescription = Optional(input.ItemDescription, 500, nameof(input.ItemDescription), "字典项描述不能超过 500 个字符。"),
            Metadata = OptionalJson(input.Metadata, "字典项元数据必须是合法 JSON。"),
            IsDefault = input.IsDefault,
            Status = input.Status,
            Sort = input.Sort,
            Remark = Optional(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。")
        };

        var savedDictItem = await _dictItemRepository.AddAsync(dictItem, cancellationToken);
        return DictApplicationMapper.ToItemDetailDto(savedDictItem);
    }

    /// <summary>
    /// 更新系统字典项
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Dict.Update)]
    public async Task<DictItemDetailDto> UpdateDictItemAsync(DictItemUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateDictItemUpdateInput(input);
        var dictItem = await GetDictItemOrThrowAsync(input.BasicId, cancellationToken);
        _ = await ValidateParentAsync(dictItem.DictId, input.ParentId, dictItem.BasicId, cancellationToken);
        if (input.IsDefault)
        {
            await ClearDefaultItemsAsync(dictItem.DictId, dictItem.BasicId, cancellationToken);
        }

        dictItem.ParentId = input.ParentId;
        dictItem.ItemName = Required(input.ItemName, 100, nameof(input.ItemName), "字典项名称不能超过 100 个字符。");
        dictItem.ItemValue = Optional(input.ItemValue, 500, nameof(input.ItemValue), "字典项值不能超过 500 个字符。");
        dictItem.ItemDescription = Optional(input.ItemDescription, 500, nameof(input.ItemDescription), "字典项描述不能超过 500 个字符。");
        dictItem.Metadata = OptionalJson(input.Metadata, "字典项元数据必须是合法 JSON。");
        dictItem.IsDefault = input.IsDefault;
        dictItem.Sort = input.Sort;
        dictItem.Remark = Optional(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。");

        var savedDictItem = await _dictItemRepository.UpdateAsync(dictItem, cancellationToken);
        return DictApplicationMapper.ToItemDetailDto(savedDictItem);
    }

    /// <summary>
    /// 更新系统字典项状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Dict.Status)]
    public async Task<DictItemDetailDto> UpdateDictItemStatusAsync(DictItemStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(input.BasicId, "系统字典项主键必须大于 0。");
        EnsureEnum(input.Status, nameof(input.Status));

        var dictItem = await GetDictItemOrThrowAsync(input.BasicId, cancellationToken);
        dictItem.Status = input.Status;
        dictItem.Remark = Optional(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。") ?? dictItem.Remark;

        var savedDictItem = await _dictItemRepository.UpdateAsync(dictItem, cancellationToken);
        return DictApplicationMapper.ToItemDetailDto(savedDictItem);
    }

    /// <summary>
    /// 删除系统字典项
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Dict.Delete)]
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

    private async Task<SysDict> GetDictOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        EnsureId(id, "系统字典主键必须大于 0。");
        return await _dictRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("系统字典不存在。");
    }

    private async Task<SysDictItem> GetDictItemOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        EnsureId(id, "系统字典项主键必须大于 0。");
        return await _dictItemRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("系统字典项不存在。");
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

    private static void ValidateDictCreateInput(DictCreateDto input)
    {
        EnsureEnum(input.Status, nameof(input.Status));
    }

    private static void ValidateDictUpdateInput(DictUpdateDto input)
    {
        EnsureId(input.BasicId, "系统字典主键必须大于 0。");
    }

    private static void ValidateDictItemCreateInput(DictItemCreateDto input)
    {
        EnsureId(input.DictId, "系统字典主键必须大于 0。");
        EnsureOptionalId(input.ParentId, nameof(input.ParentId), "父级字典项主键必须大于 0。");
        EnsureEnum(input.Status, nameof(input.Status));
    }

    private static void ValidateDictItemUpdateInput(DictItemUpdateDto input)
    {
        EnsureId(input.BasicId, "系统字典项主键必须大于 0。");
        EnsureOptionalId(input.ParentId, nameof(input.ParentId), "父级字典项主键必须大于 0。");
    }

    private static void EnsureCodeHasNoWhitespace(string value, string message)
    {
        if (value.Any(char.IsWhiteSpace))
        {
            throw new InvalidOperationException(message);
        }
    }
}
