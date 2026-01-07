#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IDictRepository
// Guid:a7b8c9d0-e1f2-4a5b-3c4d-6e7f8a9b0c1d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Abstracts;

/// <summary>
/// 字典仓储接口
/// </summary>
public interface IDictRepository : IAggregateRootRepository<SysDict, long>
{
    /// <summary>
    /// 根据字典编码查询字典
    /// </summary>
    /// <param name="dictCode">字典编码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>字典实体（包含字典项）</returns>
    Task<SysDict?> GetByDictCodeAsync(string dictCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查字典编码是否存在
    /// </summary>
    /// <param name="dictCode">字典编码</param>
    /// <param name="excludeDictId">排除的字典ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否存在</returns>
    Task<bool> ExistsByDictCodeAsync(string dictCode, long? excludeDictId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取字典及其所有字典项
    /// </summary>
    /// <param name="dictId">字典ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>字典实体（包含字典项）</returns>
    Task<SysDict?> GetWithItemsAsync(long dictId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据字典编码获取字典及其所有字典项
    /// </summary>
    /// <param name="dictCode">字典编码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>字典实体（包含字典项）</returns>
    Task<SysDict?> GetWithItemsByCodeAsync(string dictCode, CancellationToken cancellationToken = default);
}
