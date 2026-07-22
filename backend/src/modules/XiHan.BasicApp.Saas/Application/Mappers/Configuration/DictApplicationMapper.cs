// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 系统字典应用层映射器
/// </summary>
public static class DictApplicationMapper
{
    /// <summary>
    /// 映射系统字典创建命令
    /// </summary>
    public static DictCreateCommand ToCreateCommand(DictCreateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new DictCreateCommand(
            input.DictCode,
            input.DictName,
            input.DictType,
            input.DictDescription,
            input.Status,
            input.Sort,
            input.Remark);
    }

    /// <summary>
    /// 映射系统字典更新命令
    /// </summary>
    public static DictUpdateCommand ToUpdateCommand(DictUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new DictUpdateCommand(
            input.BasicId,
            input.DictName,
            input.DictType,
            input.DictDescription,
            input.Sort,
            input.Remark);
    }

    /// <summary>
    /// 映射系统字典状态命令
    /// </summary>
    public static DictStatusChangeCommand ToStatusCommand(DictStatusUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new DictStatusChangeCommand(input.BasicId, input.Status, input.Remark);
    }

    /// <summary>
    /// 映射系统字典项创建命令
    /// </summary>
    public static DictItemCreateCommand ToItemCreateCommand(DictItemCreateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new DictItemCreateCommand(
            input.DictId,
            input.ParentId,
            input.ItemCode,
            input.ItemName,
            input.ItemValue,
            input.ItemDescription,
            input.Metadata,
            input.IsDefault,
            input.Status,
            input.Sort,
            input.Remark);
    }

    /// <summary>
    /// 映射系统字典项更新命令
    /// </summary>
    public static DictItemUpdateCommand ToItemUpdateCommand(DictItemUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new DictItemUpdateCommand(
            input.BasicId,
            input.ParentId,
            input.ItemName,
            input.ItemValue,
            input.ItemDescription,
            input.Metadata,
            input.IsDefault,
            input.Sort,
            input.Remark);
    }

    /// <summary>
    /// 映射系统字典项状态命令
    /// </summary>
    public static DictItemStatusChangeCommand ToItemStatusCommand(DictItemStatusUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new DictItemStatusChangeCommand(input.BasicId, input.Status, input.Remark);
    }

    /// <summary>
    /// 映射系统字典列表项
    /// </summary>
    /// <param name="dict">系统字典实体</param>
    /// <returns>系统字典列表项 DTO</returns>
    public static DictListItemDto ToListItemDto(SysDict dict)
    {
        ArgumentNullException.ThrowIfNull(dict);

        return new DictListItemDto
        {
            BasicId = dict.BasicId,
            DictCode = dict.DictCode,
            DictName = dict.DictName,
            DictType = dict.DictType,
            DictDescription = dict.DictDescription,
            IsBuiltIn = dict.IsBuiltIn,
            Status = dict.Status,
            Sort = dict.Sort,
            CreatedTime = dict.CreatedTime,
            ModifiedTime = dict.ModifiedTime
        };
    }

    /// <summary>
    /// 映射系统字典详情
    /// </summary>
    /// <param name="dict">系统字典实体</param>
    /// <returns>系统字典详情 DTO</returns>
    public static DictDetailDto ToDetailDto(SysDict dict)
    {
        ArgumentNullException.ThrowIfNull(dict);

        var item = ToListItemDto(dict);
        return new DictDetailDto
        {
            BasicId = item.BasicId,
            DictCode = item.DictCode,
            DictName = item.DictName,
            DictType = item.DictType,
            DictDescription = item.DictDescription,
            IsBuiltIn = item.IsBuiltIn,
            Status = item.Status,
            Sort = item.Sort,
            CreatedTime = item.CreatedTime,
            CreatedId = dict.CreatedId,
            CreatedBy = dict.CreatedBy,
            ModifiedTime = item.ModifiedTime,
            ModifiedId = dict.ModifiedId,
            ModifiedBy = dict.ModifiedBy
        };
    }

    /// <summary>
    /// 映射系统字典项列表项
    /// </summary>
    /// <param name="dictItem">系统字典项实体</param>
    /// <returns>系统字典项列表项 DTO</returns>
    public static DictItemListItemDto ToItemListItemDto(SysDictItem dictItem)
    {
        ArgumentNullException.ThrowIfNull(dictItem);

        return new DictItemListItemDto
        {
            BasicId = dictItem.BasicId,
            DictId = dictItem.DictId,
            ParentId = dictItem.ParentId,
            ItemCode = dictItem.ItemCode,
            ItemName = dictItem.ItemName,
            ItemValue = dictItem.ItemValue,
            ItemDescription = dictItem.ItemDescription,
            IsDefault = dictItem.IsDefault,
            Status = dictItem.Status,
            Sort = dictItem.Sort,
            CreatedTime = dictItem.CreatedTime,
            ModifiedTime = dictItem.ModifiedTime
        };
    }

    /// <summary>
    /// 映射系统字典项详情
    /// </summary>
    /// <param name="dictItem">系统字典项实体</param>
    /// <returns>系统字典项详情 DTO</returns>
    public static DictItemDetailDto ToItemDetailDto(SysDictItem dictItem)
    {
        ArgumentNullException.ThrowIfNull(dictItem);

        var item = ToItemListItemDto(dictItem);
        return new DictItemDetailDto
        {
            BasicId = item.BasicId,
            DictId = item.DictId,
            ParentId = item.ParentId,
            ItemCode = item.ItemCode,
            ItemName = item.ItemName,
            ItemValue = item.ItemValue,
            ItemDescription = item.ItemDescription,
            IsDefault = item.IsDefault,
            Status = item.Status,
            Sort = item.Sort,
            CreatedTime = item.CreatedTime,
            CreatedId = dictItem.CreatedId,
            CreatedBy = dictItem.CreatedBy,
            ModifiedTime = item.ModifiedTime,
            ModifiedId = dictItem.ModifiedId,
            ModifiedBy = dictItem.ModifiedBy
        };
    }

    /// <summary>
    /// 映射系统字典项树节点
    /// </summary>
    /// <param name="dictItem">系统字典项实体</param>
    /// <returns>系统字典项树节点 DTO</returns>
    public static DictItemTreeNodeDto ToItemTreeNodeDto(SysDictItem dictItem)
    {
        ArgumentNullException.ThrowIfNull(dictItem);

        return new DictItemTreeNodeDto
        {
            BasicId = dictItem.BasicId,
            DictId = dictItem.DictId,
            ParentId = dictItem.ParentId,
            ItemCode = dictItem.ItemCode,
            ItemName = dictItem.ItemName,
            ItemValue = dictItem.ItemValue,
            ItemDescription = dictItem.ItemDescription,
            IsDefault = dictItem.IsDefault,
            Status = dictItem.Status,
            Sort = dictItem.Sort
        };
    }
}
