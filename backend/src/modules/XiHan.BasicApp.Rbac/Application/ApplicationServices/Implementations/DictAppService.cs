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
using XiHan.BasicApp.Rbac.Application.Dtos;
using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Application.ApplicationServices.Implementations;

/// <summary>
/// 字典应用服务
/// </summary>
public class DictAppService : ApplicationServiceBase, IDictAppService
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
}
