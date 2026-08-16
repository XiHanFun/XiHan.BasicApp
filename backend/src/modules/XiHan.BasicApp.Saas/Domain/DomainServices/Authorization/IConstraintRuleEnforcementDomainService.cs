// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 约束规则执法领域服务
/// </summary>
/// <remarks>
/// 职责：在角色写入路径（用户角色授予 = SSD、会话角色激活 = DSD）执行约束规则评估。
/// 与 <see cref="IConstraintRuleDomainService"/>（规则配置 CRUD）互补：本服务只读规则并判定违规。
/// </remarks>
public interface IConstraintRuleEnforcementDomainService
{
    /// <summary>
    /// 评估指定角色集合在指定约束类型下的违规情况
    /// </summary>
    /// <param name="roleIds">待评估的角色ID集合（用户当前有效角色 + 拟新增角色，无需预先展开继承链）</param>
    /// <param name="constraintType">约束类型（SSD 用于用户角色授予、DSD 用于会话角色激活）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>违规评估结果；无违规时返回 <see cref="ConstraintEnforcementResult.Pass"/></returns>
    Task<ConstraintEnforcementResult> EvaluateRoleAssignmentsAsync(
        IEnumerable<long> roleIds,
        ConstraintType constraintType,
        CancellationToken cancellationToken = default);
}
