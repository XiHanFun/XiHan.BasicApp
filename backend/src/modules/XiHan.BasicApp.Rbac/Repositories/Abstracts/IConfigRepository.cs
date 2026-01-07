#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IConfigRepository
// Guid:b8c9d0e1-f2a3-4b5c-4d5e-7f8a9b0c1d2e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Abstracts;

/// <summary>
/// 配置仓储接口
/// </summary>
public interface IConfigRepository : IAggregateRootRepository<SysConfig, long>
{
    /// <summary>
    /// 根据配置键查询配置
    /// </summary>
    /// <param name="configKey">配置键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>配置实体</returns>
    Task<SysConfig?> GetByConfigKeyAsync(string configKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查配置键是否存在
    /// </summary>
    /// <param name="configKey">配置键</param>
    /// <param name="excludeConfigId">排除的配置ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否存在</returns>
    Task<bool> ExistsByConfigKeyAsync(string configKey, long? excludeConfigId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据配置组获取配置列表
    /// </summary>
    /// <param name="configGroup">配置组</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>配置列表</returns>
    Task<List<SysConfig>> GetByConfigGroupAsync(string configGroup, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取所有系统配置
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>配置字典（Key-Value）</returns>
    Task<Dictionary<string, string>> GetAllConfigsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量更新配置
    /// </summary>
    /// <param name="configs">配置字典</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> BatchUpdateAsync(Dictionary<string, string> configs, CancellationToken cancellationToken = default);
}
