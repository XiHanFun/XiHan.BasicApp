#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IDictRepository
// Guid:cc4fc63c-2819-46e2-b0e4-8f86f58ab8d7
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:36:01
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 平台字典仓储接口
/// </summary>
public interface IDictRepository : IAggregateRootRepository<SysDict, long>
{
    /// <summary>
    /// 根据字典编码获取字典
    /// </summary>
    Task<SysDict?> GetByDictCodeAsync(string dictCode, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据字典ID获取字典项列表
    /// </summary>
    Task<IReadOnlyList<SysDictItem>> GetDictItemsAsync(long dictId, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据字典项ID获取字典项
    /// </summary>
    Task<SysDictItem?> GetDictItemByIdAsync(long dictItemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据字典ID和字典项编码获取字典项
    /// </summary>
    Task<SysDictItem?> GetDictItemByCodeAsync(long dictId, string itemCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 新增字典项
    /// </summary>
    Task<SysDictItem> AddDictItemAsync(SysDictItem dictItem, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新字典项
    /// </summary>
    Task<SysDictItem> UpdateDictItemAsync(SysDictItem dictItem, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除字典项
    /// </summary>
    Task<bool> DeleteDictItemAsync(SysDictItem dictItem, CancellationToken cancellationToken = default);
}
