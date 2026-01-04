#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysDictService
// Guid:j1k2l3m4-n5o6-7890-abcd-ef1234567890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 16:40:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Extensions;
using XiHan.BasicApp.Rbac.Repositories.Dicts;
using XiHan.BasicApp.Rbac.Services.Dicts.Dtos;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Services.Dicts;

/// <summary>
/// 系统字典服务实现
/// </summary>
public class SysDictService : CrudApplicationServiceBase<SysDict, DictDto, XiHanBasicAppIdType, CreateDictDto, UpdateDictDto>, ISysDictService
{
    private readonly ISysDictRepository _dictRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysDictService(ISysDictRepository dictRepository) : base(dictRepository)
    {
        _dictRepository = dictRepository;
    }

    #region 业务特定方法

    /// <summary>
    /// 根据字典编码获取字典
    /// </summary>
    public async Task<DictDto?> GetByCodeAsync(string dictCode)
    {
        var dict = await _dictRepository.GetByCodeAsync(dictCode);
        return dict?.ToDto();
    }

    /// <summary>
    /// 根据字典类型获取字典列表
    /// </summary>
    public async Task<List<DictDto>> GetByTypeAsync(string dictType)
    {
        var dicts = await _dictRepository.GetByTypeAsync(dictType);
        return dicts.ToDto();
    }

    /// <summary>
    /// 检查字典编码是否存在
    /// </summary>
    public async Task<bool> ExistsByCodeAsync(string dictCode, XiHanBasicAppIdType? excludeId = null)
    {
        return await _dictRepository.ExistsByCodeAsync(dictCode, excludeId);
    }

    #endregion 业务特定方法

    #region 映射方法实现

    /// <summary>
    /// 映射实体到DTO
    /// </summary>
    protected override Task<DictDto> MapToEntityDtoAsync(SysDict entity)
    {
        return Task.FromResult(entity.ToDto());
    }

    /// <summary>
    /// 映射 DictDto 到实体（基类方法，不推荐直接使用）
    /// </summary>
    protected override Task<SysDict> MapToEntityAsync(DictDto dto)
    {
        var entity = new SysDict
        {
            DictCode = dto.DictCode,
            DictName = dto.DictName,
            DictType = dto.DictType,
            DictDescription = dto.DictDescription,
            IsBuiltIn = dto.IsBuiltIn,
            Status = dto.Status,
            Sort = dto.Sort,
            Remark = dto.Remark
        };

        return Task.FromResult(entity);
    }

    /// <summary>
    /// 映射 DictDto 到现有实体（基类方法，不推荐直接使用）
    /// </summary>
    protected override Task MapToEntityAsync(DictDto dto, SysDict entity)
    {
        entity.DictName = dto.DictName;
        entity.DictType = dto.DictType;
        entity.DictDescription = dto.DictDescription;
        entity.Status = dto.Status;
        entity.Sort = dto.Sort;
        entity.Remark = dto.Remark;

        return Task.CompletedTask;
    }

    /// <summary>
    /// 映射创建DTO到实体
    /// </summary>
    protected override Task<SysDict> MapToEntityAsync(CreateDictDto createDto)
    {
        var entity = new SysDict
        {
            DictCode = createDto.DictCode,
            DictName = createDto.DictName,
            DictType = createDto.DictType,
            DictDescription = createDto.DictDescription,
            Sort = createDto.Sort,
            Remark = createDto.Remark
        };

        return Task.FromResult(entity);
    }

    /// <summary>
    /// 映射更新DTO到现有实体
    /// </summary>
    protected override Task MapToEntityAsync(UpdateDictDto updateDto, SysDict entity)
    {
        if (updateDto.DictName != null) entity.DictName = updateDto.DictName;
        if (updateDto.DictType != null) entity.DictType = updateDto.DictType;
        if (updateDto.DictDescription != null) entity.DictDescription = updateDto.DictDescription;
        if (updateDto.Status.HasValue) entity.Status = updateDto.Status.Value;
        if (updateDto.Sort.HasValue) entity.Sort = updateDto.Sort.Value;
        if (updateDto.Remark != null) entity.Remark = updateDto.Remark;

        return Task.CompletedTask;
    }

    #endregion 映射方法实现
}
