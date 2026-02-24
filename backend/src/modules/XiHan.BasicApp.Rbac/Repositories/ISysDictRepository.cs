#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysDictRepository
// Guid:e1f2a3b4-c5d6-789a-bcde-f12345678901
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/31 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories;

/// <summary>
/// 系统字典仓储接口
/// </summary>
public interface ISysDictRepository : IAggregateRootRepository<SysDict, long>
{
    /// <summary>
    /// 根据字典编码获取字典
    /// </summary>
    /// <param name="dictCode">字典编码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>字典实体</returns>
    Task<SysDict?> GetByDictCodeAsync(string dictCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查字典编码是否存在
    /// </summary>
    /// <param name="dictCode">字典编码</param>
    /// <param name="excludeDictId">排除的字典ID（用于更新时检查）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否存在</returns>
    Task<bool> IsDictCodeExistsAsync(string dictCode, long? excludeDictId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据字典类型获取字典列表
    /// </summary>
    /// <param name="dictType">字典类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>字典列表</returns>
    Task<List<SysDict>> GetDictsByTypeAsync(string dictType, CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存字典
    /// </summary>
    /// <param name="dict">字典实体</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>保存的字典实体</returns>
    Task<SysDict> SaveAsync(SysDict dict, CancellationToken cancellationToken = default);

    /// <summary>
    /// 启用字典
    /// </summary>
    /// <param name="dictId">字典ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task EnableDictAsync(long dictId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 禁用字典
    /// </summary>
    /// <param name="dictId">字典ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DisableDictAsync(long dictId, CancellationToken cancellationToken = default);
}
