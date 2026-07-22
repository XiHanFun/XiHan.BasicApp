// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 约束规则仓储实现
/// </summary>
public sealed class ConstraintRuleRepository(
    ISqlSugarClientResolver clientResolver,
    IUnitOfWorkManager unitOfWorkManager)
    : SaasAggregateRepository<SysConstraintRule>(clientResolver, unitOfWorkManager), IConstraintRuleRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<SysConstraintRule>> GetActiveRulesAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(rule => rule.Status == EnableStatus.Enabled)
            .Where(rule => rule.EffectiveTime == null || rule.EffectiveTime <= now)
            .Where(rule => rule.ExpirationTime == null || rule.ExpirationTime > now)
            .OrderBy(rule => rule.Priority)
            .ToListAsync(cancellationToken);
    }
}
