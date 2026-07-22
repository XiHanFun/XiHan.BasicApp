// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 约束规则项命令
/// </summary>
public sealed record ConstraintRuleItemCommand(ConstraintTargetType TargetType, long TargetId, int ConstraintGroup, string? Remark);

/// <summary>
/// 约束规则创建命令
/// </summary>
public sealed record ConstraintRuleCreateCommand(
    string RuleCode,
    string RuleName,
    ConstraintType ConstraintType,
    ConstraintTargetType TargetType,
    string? Parameters,
    EnableStatus Status,
    ViolationAction ViolationAction,
    string? Description,
    int Priority,
    DateTimeOffset? EffectiveTime,
    DateTimeOffset? ExpirationTime,
    string? Remark,
    IReadOnlyList<ConstraintRuleItemCommand> Items);

/// <summary>
/// 约束规则更新命令
/// </summary>
public sealed record ConstraintRuleUpdateCommand(
    long BasicId,
    string RuleName,
    ConstraintType ConstraintType,
    ConstraintTargetType TargetType,
    string? Parameters,
    ViolationAction ViolationAction,
    string? Description,
    int Priority,
    DateTimeOffset? EffectiveTime,
    DateTimeOffset? ExpirationTime,
    string? Remark,
    IReadOnlyList<ConstraintRuleItemCommand> Items);

/// <summary>
/// 约束规则状态命令
/// </summary>
public sealed record ConstraintRuleStatusCommand(long BasicId, EnableStatus Status, string? Remark);

/// <summary>
/// 约束规则命令结果
/// </summary>
public sealed record ConstraintRuleCommandResult(long RuleId);
