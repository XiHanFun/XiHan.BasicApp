#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysDictItemService
// Guid:m1n2o3p4-q5r6-7890-abcd-ef1234567890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 16:55:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Extensions;
using XiHan.BasicApp.Rbac.Repositories.DictItems;
using XiHan.BasicApp.Rbac.Services.DictItems.Dtos;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Services.DictItems;

/// <summary>
/// 系统字典项服务实现
/// </summary>
public class SysDictItemService : CrudApplicationServiceBase<SysDictItem, DictItemDto, XiHanBasicAppIdType, CreateDictItemDto, UpdateDictItemDto>, ISysDictItemService
{
    private readonly ISysDictItemRepository _dictItemRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysDictItemService(ISysDictItemRepository dictItemRepository) : base(dictItemRepository)
    {
        _dictItemRepository = dictItemRepository;
    }

    #region 业务特定方法

    /// <summary>
    /// 根据字典ID获取字典项列表
    /// </summary>
    public async Task<List<DictItemDto>> GetByDictIdAsync(XiHanBasicAppIdType dictId)
    {
        var dictItems = await _dictItemRepository.GetByDictIdAsync(dictId);
        return dictItems.ToDto();
    }

    /// <summary>
    /// 根据字典编码获取字典项列表
    /// </summary>
    public async Task<List<DictItemDto>> GetByDictCodeAsync(string dictCode)
    {
        var dictItems = await _dictItemRepository.GetByDictCodeAsync(dictCode);
        return dictItems.ToDto();
    }

    /// <summary>
    /// 根据字典编码和字典项编码获取字典项
    /// </summary>
    public async Task<DictItemDto?> GetByCodeAsync(string dictCode, string itemCode)
    {
        var dictItem = await _dictItemRepository.GetByCodeAsync(dictCode, itemCode);
        return dictItem?.ToDto();
    }

    /// <summary>
    /// 根据父级ID获取子项列表
    /// </summary>
    public async Task<List<DictItemDto>> GetByParentIdAsync(XiHanBasicAppIdType parentId)
    {
        var dictItems = await _dictItemRepository.GetByParentIdAsync(parentId);
        return dictItems.ToDto();
    }

    #endregion 业务特定方法

    #region 映射方法实现

    /// <summary>
    /// 映射实体到DTO
    /// </summary>
    protected override Task<DictItemDto> MapToEntityDtoAsync(SysDictItem entity)
    {
        return Task.FromResult(entity.ToDto());
    }

    /// <summary>
    /// 映射 DictItemDto 到实体（基类方法，不推荐直接使用）
    /// </summary>
    protected override Task<SysDictItem> MapToEntityAsync(DictItemDto dto)
    {
        var entity = new SysDictItem
        {
            DictId = dto.DictId,
            DictCode = dto.DictCode,
            ParentId = dto.ParentId,
            ItemCode = dto.ItemCode,
            ItemName = dto.ItemName,
            ItemValue = dto.ItemValue,
            ItemDescription = dto.ItemDescription,
            ExtendField1 = dto.ExtendField1,
            ExtendField2 = dto.ExtendField2,
            ExtendField3 = dto.ExtendField3,
            IsDefault = dto.IsDefault,
            Status = dto.Status,
            Sort = dto.Sort,
            Remark = dto.Remark
        };

        return Task.FromResult(entity);
    }

    /// <summary>
    /// 映射 DictItemDto 到现有实体（基类方法，不推荐直接使用）
    /// </summary>
    protected override Task MapToEntityAsync(DictItemDto dto, SysDictItem entity)
    {
        entity.ParentId = dto.ParentId;
        entity.ItemName = dto.ItemName;
        entity.ItemValue = dto.ItemValue;
        entity.ItemDescription = dto.ItemDescription;
        entity.ExtendField1 = dto.ExtendField1;
        entity.ExtendField2 = dto.ExtendField2;
        entity.ExtendField3 = dto.ExtendField3;
        entity.IsDefault = dto.IsDefault;
        entity.Status = dto.Status;
        entity.Sort = dto.Sort;
        entity.Remark = dto.Remark;

        return Task.CompletedTask;
    }

    /// <summary>
    /// 映射创建DTO到实体
    /// </summary>
    protected override Task<SysDictItem> MapToEntityAsync(CreateDictItemDto createDto)
    {
        var entity = new SysDictItem
        {
            DictId = createDto.DictId,
            DictCode = createDto.DictCode,
            ParentId = createDto.ParentId,
            ItemCode = createDto.ItemCode,
            ItemName = createDto.ItemName,
            ItemValue = createDto.ItemValue,
            ItemDescription = createDto.ItemDescription,
            ExtendField1 = createDto.ExtendField1,
            ExtendField2 = createDto.ExtendField2,
            ExtendField3 = createDto.ExtendField3,
            IsDefault = createDto.IsDefault,
            Sort = createDto.Sort,
            Remark = createDto.Remark
        };

        return Task.FromResult(entity);
    }

    /// <summary>
    /// 映射更新DTO到现有实体
    /// </summary>
    protected override Task MapToEntityAsync(UpdateDictItemDto updateDto, SysDictItem entity)
    {
        if (updateDto.ParentId.HasValue) entity.ParentId = updateDto.ParentId;
        if (updateDto.ItemName != null) entity.ItemName = updateDto.ItemName;
        if (updateDto.ItemValue != null) entity.ItemValue = updateDto.ItemValue;
        if (updateDto.ItemDescription != null) entity.ItemDescription = updateDto.ItemDescription;
        if (updateDto.ExtendField1 != null) entity.ExtendField1 = updateDto.ExtendField1;
        if (updateDto.ExtendField2 != null) entity.ExtendField2 = updateDto.ExtendField2;
        if (updateDto.ExtendField3 != null) entity.ExtendField3 = updateDto.ExtendField3;
        if (updateDto.IsDefault.HasValue) entity.IsDefault = updateDto.IsDefault.Value;
        if (updateDto.Status.HasValue) entity.Status = updateDto.Status.Value;
        if (updateDto.Sort.HasValue) entity.Sort = updateDto.Sort.Value;
        if (updateDto.Remark != null) entity.Remark = updateDto.Remark;

        return Task.CompletedTask;
    }

    #endregion 映射方法实现
}

