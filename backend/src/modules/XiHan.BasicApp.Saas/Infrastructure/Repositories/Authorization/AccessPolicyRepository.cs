// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 访问策略仓储实现
/// </summary>
public sealed class AccessPolicyRepository(
    ISqlSugarClientResolver clientResolver,
    IUnitOfWorkManager unitOfWorkManager)
    : SaasAggregateRepository<SysAccessPolicy>(clientResolver, unitOfWorkManager), IAccessPolicyRepository
{
    /// <summary>
    /// 全部层启用的策略
    /// </summary>
    public async Task<IReadOnlyList<SysAccessPolicy>> GetEnabledInAllTenantsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await CreateNoTenantQueryable()
            .Where(policy => policy.Status == EnableStatus.Enabled)
            .OrderBy(policy => policy.Priority)
            .OrderBy(policy => policy.BasicId)
            .ToListAsync(cancellationToken);
    }
}
