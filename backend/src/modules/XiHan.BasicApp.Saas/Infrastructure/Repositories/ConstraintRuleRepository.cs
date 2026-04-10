#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ConstraintRuleRepository
// Guid:1ac5c5ff-3b8e-40d0-a4fd-978bb1f975d5
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/10 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.Data.SqlSugar.SplitTables;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 约束规则仓储实现
/// </summary>
public class ConstraintRuleRepository : SqlSugarRepositoryBase<SysConstraintRule, long>, IConstraintRuleRepository
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext"></param>
    /// <param name="splitTableExecutor"></param>
    /// <param name="serviceProvider"></param>
    public ConstraintRuleRepository(
        ISqlSugarDbContext dbContext,
        ISqlSugarSplitTableExecutor splitTableExecutor,
        IServiceProvider serviceProvider)
        : base(dbContext, splitTableExecutor, serviceProvider)
    {
    }

    /// <inheritdoc />
    public async Task<SysConstraintRule?> GetByRuleCodeAsync(string ruleCode, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ruleCode);

        var query = CreateTenantQueryable().Where(rule => rule.RuleCode == ruleCode);
        query = tenantId.HasValue ? query.Where(rule => rule.TenantId == tenantId.Value) : query.Where(rule => rule.TenantId == null);

        return await query.FirstAsync(cancellationToken);
    }
}
