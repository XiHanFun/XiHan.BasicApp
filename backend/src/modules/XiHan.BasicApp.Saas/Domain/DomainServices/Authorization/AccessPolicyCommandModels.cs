// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 访问策略条件命令
/// </summary>
public sealed record AccessPolicyConditionCommand(
    int ConditionGroup,
    string AttributeName,
    ConditionOperator Operator,
    bool IsNegated,
    ConfigDataType ValueType,
    string ConditionValue,
    string? Description);

/// <summary>
/// 访问策略创建命令；动作一律是只记录
/// </summary>
public sealed record AccessPolicyCreateCommand(
    string PolicyCode,
    string PolicyName,
    string? Description,
    int Priority,
    EnableStatus Status,
    DateTimeOffset? EffectiveTime,
    DateTimeOffset? ExpirationTime,
    string? TemplateCode,
    string? Remark,
    IReadOnlyList<AccessPolicyConditionCommand> Conditions);

/// <summary>
/// 访问策略更新命令（不改动作与状态）
/// </summary>
public sealed record AccessPolicyUpdateCommand(
    long BasicId,
    string PolicyName,
    string? Description,
    int Priority,
    DateTimeOffset? EffectiveTime,
    DateTimeOffset? ExpirationTime,
    string? Remark,
    IReadOnlyList<AccessPolicyConditionCommand> Conditions);

/// <summary>
/// 访问策略动作命令
/// </summary>
public sealed record AccessPolicyActionCommand(long BasicId, AccessPolicyAction Action);

/// <summary>
/// 访问策略状态命令
/// </summary>
public sealed record AccessPolicyStatusCommand(long BasicId, EnableStatus Status);

/// <summary>
/// 访问策略命令结果
/// </summary>
public sealed record AccessPolicyCommandResult(long PolicyId);
