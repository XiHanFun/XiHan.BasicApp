#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysDictItemService
// Guid:l1m2n3o4-p5q6-7890-abcd-ef1234567890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 16:50:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Services.DictItems.Dtos;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Services.DictItems;

/// <summary>
/// 系统字典项服务接口
/// </summary>
public interface ISysDictItemService : ICrudApplicationService<DictItemDto, XiHanBasicAppIdType, CreateDictItemDto, UpdateDictItemDto>
{
    /// <summary>
    /// 根据字典ID获取字典项列表
    /// </summary>
    /// <param name="dictId">字典ID</param>
    /// <returns></returns>
    Task<List<DictItemDto>> GetByDictIdAsync(XiHanBasicAppIdType dictId);

    /// <summary>
    /// 根据字典编码获取字典项列表
    /// </summary>
    /// <param name="dictCode">字典编码</param>
    /// <returns></returns>
    Task<List<DictItemDto>> GetByDictCodeAsync(string dictCode);

    /// <summary>
    /// 根据字典编码和字典项编码获取字典项
    /// </summary>
    /// <param name="dictCode">字典编码</param>
    /// <param name="itemCode">字典项编码</param>
    /// <returns></returns>
    Task<DictItemDto?> GetByCodeAsync(string dictCode, string itemCode);

    /// <summary>
    /// 根据父级ID获取子项列表
    /// </summary>
    /// <param name="parentId">父级ID</param>
    /// <returns></returns>
    Task<List<DictItemDto>> GetByParentIdAsync(XiHanBasicAppIdType parentId);
}

