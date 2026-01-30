#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysConfigRepository
// Guid:f2a3b4c5-d6e7-8901-2345-678901f67890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/30 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Abstracts;

/// <summary>
/// 系统配置仓储接口
/// </summary>
public interface ISysConfigRepository : IAggregateRootRepository<SysConfig, long>
{
    /// <summary>
    /// 根据配置键获取配置
    /// </summary>
    Task<SysConfig?> GetByConfigKeyAsync(string configKey, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据配置组获取配置列表
    /// </summary>
    Task<List<SysConfig>> GetByConfigGroupAsync(string configGroup, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取所有配置
    /// </summary>
    Task<List<SysConfig>> GetAllConfigsAsync(long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量更新配置
    /// </summary>
    Task UpdateConfigsAsync(IEnumerable<SysConfig> configs, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查配置键是否存在
    /// </summary>
    Task<bool> ExistsByConfigKeyAsync(string configKey, long? excludeConfigId = null, long? tenantId = null, CancellationToken cancellationToken = default);
}
