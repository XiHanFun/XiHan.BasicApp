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

using Mapster;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Repositories.DictItems;
using XiHan.BasicApp.Rbac.Services.DictItems.Dtos;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Services.DictItems;

/// <summary>
/// 系统字典项服务实现
/// </summary>
public class SysDictItemService : CrudApplicationServiceBase<SysDictItem, DictItemDto, long, CreateDictItemDto, UpdateDictItemDto>, ISysDictItemService
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
    public async Task<List<DictItemDto>> GetByDictIdAsync(long dictId)
    {
        var dictItems = await _dictItemRepository.GetByDictIdAsync(dictId);
        return dictItems.Adapt<List<DictItemDto>>();
    }

    /// <summary>
    /// 根据字典编码获取字典项列表
    /// </summary>
    public async Task<List<DictItemDto>> GetByDictCodeAsync(string dictCode)
    {
        var dictItems = await _dictItemRepository.GetByDictCodeAsync(dictCode);
        return dictItems.Adapt<List<DictItemDto>>();
    }

    /// <summary>
    /// 根据字典编码和字典项编码获取字典项
    /// </summary>
    public async Task<DictItemDto?> GetByCodeAsync(string dictCode, string itemCode)
    {
        var dictItem = await _dictItemRepository.GetByCodeAsync(dictCode, itemCode);
        return dictItem.Adapt<DictItemDto>();
    }

    /// <summary>
    /// 根据父级ID获取子项列表
    /// </summary>
    public async Task<List<DictItemDto>> GetByParentIdAsync(long parentId)
    {
        var dictItems = await _dictItemRepository.GetByParentIdAsync(parentId);
        return dictItems.Adapt<List<DictItemDto>>();
    }

    #endregion 业务特定方法
}
