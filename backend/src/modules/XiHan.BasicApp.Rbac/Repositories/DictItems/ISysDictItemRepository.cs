#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysDictItemRepository
// Guid:a9b2c3d4-e5f6-7890-abcd-ef1234567898
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.DictItems;

/// <summary>
/// 系统字典项仓储接口
/// </summary>
public interface ISysDictItemRepository : IRepositoryBase<SysDictItem, long>
{
    /// <summary>
    /// 根据字典ID获取字典项列表
    /// </summary>
    /// <param name="dictId">字典ID</param>
    /// <returns></returns>
    Task<List<SysDictItem>> GetByDictIdAsync(long dictId);

    /// <summary>
    /// 根据字典编码获取字典项列表
    /// </summary>
    /// <param name="dictCode">字典编码</param>
    /// <returns></returns>
    Task<List<SysDictItem>> GetByDictCodeAsync(string dictCode);

    /// <summary>
    /// 根据字典编码和字典项编码获取字典项
    /// </summary>
    /// <param name="dictCode">字典编码</param>
    /// <param name="itemCode">字典项编码</param>
    /// <returns></returns>
    Task<SysDictItem?> GetByCodeAsync(string dictCode, string itemCode);

    /// <summary>
    /// 根据父级ID获取子项列表
    /// </summary>
    /// <param name="parentId">父级ID</param>
    /// <returns></returns>
    Task<List<SysDictItem>> GetByParentIdAsync(long parentId);
}
