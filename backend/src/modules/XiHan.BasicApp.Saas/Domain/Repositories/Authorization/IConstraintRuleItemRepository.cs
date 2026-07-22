// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 约束规则项仓储接口
/// </summary>
public interface IConstraintRuleItemRepository : ISaasRepository<SysConstraintRuleItem>
{
    /// <summary>
    /// 根据规则ID获取规则项列表
    /// </summary>
    Task<IReadOnlyList<SysConstraintRuleItem>> GetByRuleIdAsync(long ruleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 全量替换规则项（先删后插）
    /// </summary>
    Task ReplaceByRuleIdAsync(long ruleId, IEnumerable<SysConstraintRuleItem> items, CancellationToken cancellationToken = default);
}
