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

using Mapster;
using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
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
        return dict.Adapt<DictDto>();
    }

    /// <summary>
    /// 根据字典类型获取字典列表
    /// </summary>
    public async Task<List<DictDto>> GetByTypeAsync(string dictType)
    {
        var dicts = await _dictRepository.GetByTypeAsync(dictType);
        return dicts.Adapt<List<DictDto>>();
    }

    /// <summary>
    /// 检查字典编码是否存在
    /// </summary>
    public async Task<bool> ExistsByCodeAsync(string dictCode, XiHanBasicAppIdType? excludeId = null)
    {
        return await _dictRepository.ExistsByCodeAsync(dictCode, excludeId);
    }

    #endregion 业务特定方法
}
