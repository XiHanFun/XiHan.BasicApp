#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysDictRepository
// Guid:a3b4c5d6-e7f8-9012-3456-789012a78901
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/30 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Abstracts;

/// <summary>
/// 字典仓储接口
/// </summary>
/// <remarks>
/// 聚合范围：SysDict + SysDictItem
/// </remarks>
public interface ISysDictRepository : IAggregateRootRepository<SysDict, long>
{
    /// <summary>
    /// 根据字典编码获取字典
    /// </summary>
    Task<SysDict?> GetByDictCodeAsync(string dictCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取字典及字典项
    /// </summary>
    Task<SysDict?> GetWithItemsAsync(long dictId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据字典编码获取字典项列表
    /// </summary>
    Task<List<SysDictItem>> GetDictItemsByCodeAsync(string dictCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据字典ID获取字典项列表
    /// </summary>
    Task<List<SysDictItem>> GetDictItemsAsync(long dictId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 添加字典项
    /// </summary>
    Task<SysDictItem> AddDictItemAsync(SysDictItem dictItem, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新字典项
    /// </summary>
    Task UpdateDictItemAsync(SysDictItem dictItem, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除字典项
    /// </summary>
    Task DeleteDictItemAsync(long dictItemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查字典编码是否存在
    /// </summary>
    Task<bool> ExistsByDictCodeAsync(string dictCode, long? excludeDictId = null, CancellationToken cancellationToken = default);
}
