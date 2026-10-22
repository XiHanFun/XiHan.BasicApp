// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 访问策略条件仓储接口
/// </summary>
public interface IAccessPolicyConditionRepository : ISaasRepository<SysAccessPolicyCondition>
{
    /// <summary>
    /// 一条策略的条件，按分组排序
    /// </summary>
    Task<IReadOnlyList<SysAccessPolicyCondition>> GetByPolicyIdAsync(long policyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 多条策略的条件，跨全部租户读，求值快照用
    /// </summary>
    Task<IReadOnlyList<SysAccessPolicyCondition>> GetByPolicyIdsInAllTenantsAsync(IReadOnlyCollection<long> policyIds, CancellationToken cancellationToken = default);
}
