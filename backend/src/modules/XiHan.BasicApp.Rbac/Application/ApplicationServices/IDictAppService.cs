#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IDictAppService
// Guid:aa5d565c-7953-4b28-b903-a896f636eb97
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:47:17
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Application.Dtos;
using XiHan.BasicApp.Core.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Rbac.Application.ApplicationServices;

/// <summary>
/// 字典应用服务
/// </summary>
public interface IDictAppService
    : ICrudApplicationService<DictDto, long, DictCreateDto, DictUpdateDto, BasicAppPRDto>
{
    /// <summary>
    /// 根据字典编码获取字典
    /// </summary>
    /// <param name="dictCode"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    Task<DictDto?> GetDictByCodeAsync(string dictCode, long? tenantId = null);

    /// <summary>
    /// 根据字典ID获取字典项
    /// </summary>
    /// <param name="dictId"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    Task<IReadOnlyList<DictItemDto>> GetDictItemsAsync(long dictId, long? tenantId = null);

    /// <summary>
    /// 根据字典项ID获取字典项
    /// </summary>
    /// <param name="dictItemId"></param>
    /// <returns></returns>
    Task<DictItemDto?> GetDictItemByIdAsync(long dictItemId);

    /// <summary>
    /// 创建字典项
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    Task<DictItemDto> CreateItemAsync(DictItemCreateDto input);

    /// <summary>
    /// 更新字典项
    /// </summary>
    /// <param name="dictItemId"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    Task<DictItemDto> UpdateItemAsync(long dictItemId, DictItemUpdateDto input);

    /// <summary>
    /// 删除字典项
    /// </summary>
    /// <param name="dictItemId"></param>
    /// <returns></returns>
    Task<bool> DeleteItemAsync(long dictItemId);
}
