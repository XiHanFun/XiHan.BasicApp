// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 约束规则执法领域服务
/// </summary>
/// <remarks>
/// 职责：在角色写入路径（用户角色授予、角色继承变更 = SSD，会话角色激活 = DSD）执行约束规则评估。
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

    /// <summary>
    /// 一次评估多组角色集合（规则与继承链只读一次）
    /// </summary>
    /// <param name="roleSets">待评估的角色集合，各组独立判定，无需预先展开继承链</param>
    /// <param name="constraintType">约束类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>与入参一一对应的评估结果</returns>
    Task<IReadOnlyList<ConstraintEnforcementResult>> EvaluateRoleSetsAsync(
        IReadOnlyList<IReadOnlyCollection<long>> roleSets,
        ConstraintType constraintType,
        CancellationToken cancellationToken = default);
}
