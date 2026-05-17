#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ConstraintRuleCommandModels
// Guid:9335b9c1-c637-4387-98cb-ce03635ab407
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
