// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 约束规则违规明细
/// </summary>
/// <param name="RuleId">约束规则主键</param>
/// <param name="RuleCode">约束规则编码</param>
/// <param name="RuleName">约束规则名称</param>
/// <param name="ConstraintType">约束类型</param>
/// <param name="ConstraintGroup">违规的约束分组（同组内为互斥集合）</param>
/// <param name="MatchedTargetIds">命中的目标角色ID（已按继承链展开后的有效命中）</param>
/// <param name="ViolationAction">规则声明的违规处理方式</param>
public sealed record ConstraintViolation(
    long RuleId,
    string RuleCode,
    string RuleName,
    ConstraintType ConstraintType,
    int ConstraintGroup,
    IReadOnlyList<long> MatchedTargetIds,
    ViolationAction ViolationAction);

/// <summary>
/// 约束规则执法评估结果
/// </summary>
/// <param name="Violations">违规明细集合（按规则优先级降序排列）</param>
public sealed record ConstraintEnforcementResult(IReadOnlyList<ConstraintViolation> Violations)
{
    /// <summary>
    /// 无违规的通过结果
    /// </summary>
    public static ConstraintEnforcementResult Pass { get; } = new([]);

    /// <summary>
    /// 是否存在违规
    /// </summary>
    public bool HasViolations => Violations.Count > 0;

    /// <summary>
    /// 首个必须阻断写入的违规（拒绝 / 需审批均按失败关闭处理；警告 / 记录日志放行）
    /// </summary>
    public ConstraintViolation? FirstBlockingViolation => Violations.FirstOrDefault(
        violation => violation.ViolationAction is ViolationAction.Deny or ViolationAction.RequireApproval);
}
