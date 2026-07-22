// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 约束规则领域服务
/// </summary>
public interface IConstraintRuleDomainService
{
    /// <summary>
    /// 创建约束规则
    /// </summary>
    Task<ConstraintRuleCommandResult> CreateConstraintRuleAsync(ConstraintRuleCreateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除约束规则
    /// </summary>
    Task DeleteConstraintRuleAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新约束规则
    /// </summary>
    Task<ConstraintRuleCommandResult> UpdateConstraintRuleAsync(ConstraintRuleUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新约束规则状态
    /// </summary>
    Task<ConstraintRuleCommandResult> UpdateConstraintRuleStatusAsync(ConstraintRuleStatusCommand command, CancellationToken cancellationToken = default);
}
