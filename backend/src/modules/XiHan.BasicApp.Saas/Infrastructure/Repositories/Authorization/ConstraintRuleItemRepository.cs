// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 约束规则目标项仓储实现
/// </summary>
public sealed class ConstraintRuleItemRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysConstraintRuleItem>(clientResolver), IConstraintRuleItemRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<SysConstraintRuleItem>> GetByRuleIdAsync(long ruleId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(item => item.ConstraintRuleId == ruleId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task ReplaceByRuleIdAsync(long ruleId, IEnumerable<SysConstraintRuleItem> items, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await DeleteAsync(item => item.ConstraintRuleId == ruleId, cancellationToken);

        var list = items.ToList();
        if (list.Count > 0)
        {
            await AddRangeAsync(list, cancellationToken);
        }
    }
}
