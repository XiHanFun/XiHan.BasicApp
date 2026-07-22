// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 系统配置仓储接口
/// </summary>
public interface IConfigRepository : ISaasRepository<SysConfig>
{
    /// <summary>
    /// 根据配置键获取
    /// </summary>
    Task<SysConfig?> GetByKeyAsync(string configKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据配置键获取当前租户有效配置，租户级配置优先，全局配置兜底。
    /// </summary>
    Task<SysConfig?> GetEffectiveByKeyAsync(string configKey, long? tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查配置键是否存在
    /// </summary>
    Task<bool> ExistsKeyAsync(string configKey, long? excludeId = null, CancellationToken cancellationToken = default);
}
