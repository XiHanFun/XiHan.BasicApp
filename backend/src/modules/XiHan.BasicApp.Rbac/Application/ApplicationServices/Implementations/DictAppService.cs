#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DictAppService
// Guid:df6eea2e-93d2-430b-8c9f-a426bc31bedf
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 06:35:42
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Rbac.Application;
using XiHan.BasicApp.Rbac.Application.Dtos;
using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Application.ApplicationServices.Implementations;

/// <summary>
/// 字典应用服务
/// </summary>
public class DictAppService
    : CrudApplicationServiceBase<SysDict, DictDto, long, DictCreateDto, DictUpdateDto, BasicAppPRDto>,
        IDictAppService
{
    private readonly IDictRepository _dictRepository;
    private readonly IDictItemRepository _dictItemRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dictRepository"></param>
    /// <param name="dictItemRepository"></param>
    public DictAppService(
        IDictRepository dictRepository,
        IDictItemRepository dictItemRepository)
        : base(dictRepository)
    {
        _dictRepository = dictRepository;
        _dictItemRepository = dictItemRepository;
    }

    /// <summary>
    /// 根据字典编码获取字典信息
    /// </summary>
    /// <param name="dictCode"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    public async Task<DictDto?> GetDictByCodeAsync(string dictCode, long? tenantId = null)
    {
        var entity = await _dictRepository.GetByDictCodeAsync(dictCode, tenantId);
        return entity?.Adapt<DictDto>();
    }

    /// <summary>
    /// 根据字典ID获取字典项
    /// </summary>
    /// <param name="dictId"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<DictItemDto>> GetDictItemsAsync(long dictId, long? tenantId = null)
    {
        var entities = await _dictItemRepository.GetByDictIdAsync(dictId, tenantId);
        return entities.Select(static entity => entity.Adapt<DictItemDto>()).ToArray();
    }

    /// <summary>
    /// 根据字典项ID获取字典项
    /// </summary>
    /// <param name="dictItemId"></param>
    /// <returns></returns>
    public async Task<DictItemDto?> GetDictItemByIdAsync(long dictItemId)
    {
        var entity = await _dictItemRepository.GetByIdAsync(dictItemId);
        return entity?.Adapt<DictItemDto>();
    }

    /// <summary>
    /// 创建字典
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public override async Task<DictDto> CreateAsync(DictCreateDto input)
    {
        input.ValidateAnnotations();

        var normalizedCode = input.DictCode.Trim();
        await EnsureDictCodeUniqueAsync(normalizedCode, null, input.TenantId);

        return await base.CreateAsync(input);
    }

    /// <summary>
    /// 更新字典
    /// </summary>
    /// <param name="id"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    public override async Task<DictDto> UpdateAsync(long id, DictUpdateDto input)
    {
        input.ValidateAnnotations();

        var dict = await _dictRepository.GetByIdAsync(id)
                   ?? throw new KeyNotFoundException($"未找到字典: {id}");

        var normalizedCode = input.DictCode.Trim();
        await EnsureDictCodeUniqueAsync(normalizedCode, id, dict.TenantId);

        await MapDtoToEntityAsync(input, dict);
        var updated = await _dictRepository.UpdateAsync(dict);
        return updated.Adapt<DictDto>();
    }

    /// <summary>
    /// 创建字典项
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public async Task<DictItemDto> CreateItemAsync(DictItemCreateDto input)
    {
        input.ValidateAnnotations();

        var dict = await _dictRepository.GetByIdAsync(input.DictId)
                   ?? throw new KeyNotFoundException($"未找到字典: {input.DictId}");

        var normalizedCode = input.ItemCode.Trim();
        await EnsureDictItemCodeUniqueAsync(input.DictId, normalizedCode, null);

        var entity = new SysDictItem
        {
            TenantId = input.TenantId ?? dict.TenantId,
            DictId = input.DictId,
            DictCode = dict.DictCode,
            ParentId = input.ParentId,
            ItemCode = normalizedCode,
            ItemName = input.ItemName.Trim(),
            ItemValue = input.ItemValue,
            Sort = input.Sort
        };

        var created = await _dictItemRepository.AddAsync(entity);
        return created.Adapt<DictItemDto>();
    }

    /// <summary>
    /// 更新字典项
    /// </summary>
    /// <param name="dictItemId"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    public async Task<DictItemDto> UpdateItemAsync(long dictItemId, DictItemUpdateDto input)
    {
        input.ValidateAnnotations();

        var dictItem = await _dictItemRepository.GetByIdAsync(dictItemId)
                       ?? throw new KeyNotFoundException($"未找到字典项: {dictItemId}");

        var dict = await _dictRepository.GetByIdAsync(input.DictId)
                   ?? throw new KeyNotFoundException($"未找到字典: {input.DictId}");

        var normalizedCode = input.ItemCode.Trim();
        await EnsureDictItemCodeUniqueAsync(input.DictId, normalizedCode, dictItemId);

        dictItem.DictId = input.DictId;
        dictItem.DictCode = dict.DictCode;
        dictItem.ParentId = input.ParentId;
        dictItem.ItemCode = normalizedCode;
        dictItem.ItemName = input.ItemName.Trim();
        dictItem.ItemValue = input.ItemValue;
        dictItem.Status = input.Status;
        dictItem.Sort = input.Sort;
        dictItem.Remark = input.Remark;

        var updated = await _dictItemRepository.UpdateAsync(dictItem);
        return updated.Adapt<DictItemDto>();
    }

    /// <summary>
    /// 删除字典项
    /// </summary>
    /// <param name="dictItemId"></param>
    /// <returns></returns>
    public async Task<bool> DeleteItemAsync(long dictItemId)
    {
        if (dictItemId <= 0)
        {
            return false;
        }

        var dictItem = await _dictItemRepository.GetByIdAsync(dictItemId);
        if (dictItem is null)
        {
            return false;
        }

        return await _dictItemRepository.DeleteAsync(dictItem);
    }

    /// <summary>
    /// 映射创建 DTO 到实体
    /// </summary>
    /// <param name="createDto"></param>
    /// <returns></returns>
    protected override Task<SysDict> MapDtoToEntityAsync(DictCreateDto createDto)
    {
        var entity = new SysDict
        {
            TenantId = createDto.TenantId,
            DictCode = createDto.DictCode.Trim(),
            DictName = createDto.DictName.Trim(),
            DictType = createDto.DictType.Trim(),
            DictDescription = createDto.DictDescription,
            Sort = createDto.Sort
        };

        return Task.FromResult(entity);
    }

    /// <summary>
    /// 映射更新 DTO 到实体
    /// </summary>
    /// <param name="updateDto"></param>
    /// <param name="entity"></param>
    protected override Task MapDtoToEntityAsync(DictUpdateDto updateDto, SysDict entity)
    {
        entity.DictCode = updateDto.DictCode.Trim();
        entity.DictName = updateDto.DictName.Trim();
        entity.DictType = updateDto.DictType.Trim();
        entity.DictDescription = updateDto.DictDescription;
        entity.Status = updateDto.Status;
        entity.Sort = updateDto.Sort;
        entity.Remark = updateDto.Remark;
        return Task.CompletedTask;
    }

    /// <summary>
    /// 校验字典编码唯一性
    /// </summary>
    /// <param name="dictCode"></param>
    /// <param name="excludeDictId"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private async Task EnsureDictCodeUniqueAsync(string dictCode, long? excludeDictId, long? tenantId)
    {
        var existing = await _dictRepository.GetByDictCodeAsync(dictCode, tenantId);
        if (existing is not null && (!excludeDictId.HasValue || existing.BasicId != excludeDictId.Value))
        {
            throw new InvalidOperationException($"字典编码 '{dictCode}' 已存在");
        }
    }

    /// <summary>
    /// 校验字典项编码唯一性（同一字典下）
    /// </summary>
    /// <param name="dictId"></param>
    /// <param name="itemCode"></param>
    /// <param name="excludeDictItemId"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private async Task EnsureDictItemCodeUniqueAsync(long dictId, string itemCode, long? excludeDictItemId)
    {
        var existing = await _dictItemRepository.GetFirstAsync(
            item => item.DictId == dictId && item.ItemCode == itemCode);

        if (existing is not null && (!excludeDictItemId.HasValue || existing.BasicId != excludeDictItemId.Value))
        {
            throw new InvalidOperationException($"字典项编码 '{itemCode}' 已存在");
        }
    }
}
