// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 访问策略条件仓储实现
/// </summary>
public sealed class AccessPolicyConditionRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysAccessPolicyCondition>(clientResolver), IAccessPolicyConditionRepository
{
    /// <summary>
    /// 一条策略的条件
    /// </summary>
    public async Task<IReadOnlyList<SysAccessPolicyCondition>> GetByPolicyIdAsync(long policyId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await CreateQueryable()
            .Where(condition => condition.PolicyId == policyId)
            .OrderBy(condition => condition.ConditionGroup)
            .OrderBy(condition => condition.BasicId)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 多条策略的条件，跨全部租户
    /// </summary>
    public async Task<IReadOnlyList<SysAccessPolicyCondition>> GetByPolicyIdsInAllTenantsAsync(IReadOnlyCollection<long> policyIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(policyIds);
        if (policyIds.Count == 0)
        {
            return [];
        }

        cancellationToken.ThrowIfCancellationRequested();
        var ids = policyIds.Distinct().ToArray();
        return await CreateNoTenantQueryable()
            .Where(condition => ids.Contains(condition.PolicyId))
            .OrderBy(condition => condition.ConditionGroup)
            .OrderBy(condition => condition.BasicId)
            .ToListAsync(cancellationToken);
    }
}
