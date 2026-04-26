#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ConstraintRuleRepository
// Guid:b5eb4d65-1ed2-49b8-980d-32ed98dbe37f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 约束规则仓储实现
/// </summary>
public sealed class ConstraintRuleRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysConstraintRule>(clientResolver), IConstraintRuleRepository
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
