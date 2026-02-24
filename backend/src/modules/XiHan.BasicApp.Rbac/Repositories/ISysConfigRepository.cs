#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysConfigRepository
// Guid:d0e1f2a3-b4c5-6789-abcd-ef1234567890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/31 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories;

/// <summary>
/// 系统配置仓储接口
/// </summary>
public interface ISysConfigRepository : IAggregateRootRepository<SysConfig, long>
{
    /// <summary>
    /// 根据配置键获取配置
    /// </summary>
    /// <param name="configKey">配置键</param>
    /// <param name="tenantId">租户ID（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>配置实体</returns>
    Task<SysConfig?> GetByConfigKeyAsync(string configKey, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查配置键是否存在
    /// </summary>
    /// <param name="configKey">配置键</param>
    /// <param name="excludeConfigId">排除的配置ID（用于更新时检查）</param>
    /// <param name="tenantId">租户ID（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否存在</returns>
    Task<bool> IsConfigKeyExistsAsync(string configKey, long? excludeConfigId = null, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取配置分组下的所有配置
    /// </summary>
    /// <param name="configGroup">配置分组</param>
    /// <param name="tenantId">租户ID（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>配置列表</returns>
    Task<List<SysConfig>> GetConfigsByGroupAsync(string configGroup, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取租户的所有配置
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>配置列表</returns>
    Task<List<SysConfig>> GetConfigsByTenantAsync(long tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存配置
    /// </summary>
    /// <param name="config">配置实体</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>保存的配置实体</returns>
    Task<SysConfig> SaveAsync(SysConfig config, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量保存配置
    /// </summary>
    /// <param name="configs">配置列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task SaveBatchAsync(List<SysConfig> configs, CancellationToken cancellationToken = default);
}
