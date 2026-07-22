// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 约束规则仓储接口
/// </summary>
public interface IConstraintRuleRepository : ISaasAggregateRepository<SysConstraintRule>
{
    /// <summary>
    /// 获取当前生效的约束规则
    /// </summary>
    Task<IReadOnlyList<SysConstraintRule>> GetActiveRulesAsync(DateTimeOffset now, CancellationToken cancellationToken = default);
}
