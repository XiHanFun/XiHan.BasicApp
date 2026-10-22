// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 访问策略仓储接口
/// </summary>
public interface IAccessPolicyRepository : ISaasAggregateRepository<SysAccessPolicy>
{
    /// <summary>
    /// 全部层（平台与各租户）启用的策略，求值快照用
    /// </summary>
    Task<IReadOnlyList<SysAccessPolicy>> GetEnabledInAllTenantsAsync(CancellationToken cancellationToken = default);
}
