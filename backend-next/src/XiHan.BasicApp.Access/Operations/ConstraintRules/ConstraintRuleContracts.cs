// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Domain.Services;
using XiHan.BasicApp.Access.Operations.Policies;

namespace XiHan.BasicApp.Access.Operations.ConstraintRules;

/// <summary>
/// 约束规则的一个规则项（请求）。
/// </summary>
/// <param name="TargetId">目标：角色 ID、权限码或用户 ID（按规则的目标类型）。</param>
/// <param name="GroupNo">组号（职责分离与互斥按组判定）。</param>
/// <param name="Remark">备注。</param>
public sealed record ConstraintItemRequest(
    [Required, StringLength(128, MinimumLength = 1)] string TargetId,
    [Range(0, 99)] int GroupNo = 0,
    [StringLength(500)] string? Remark = null)
{
    /// <summary>
    /// 转成规则项定义。
    /// </summary>
    public ConstraintItemDefinition ToDefinition() => new(TargetId, GroupNo, Remark);
}

/// <summary>
/// 约束规则的一个规则项（响应）。
/// </summary>
/// <param name="TargetId">目标。</param>
/// <param name="TargetName">目标的显示名；目标已不在时为空。</param>
/// <param name="TargetCode">角色编码、权限码或登录名。</param>
/// <param name="GroupNo">组号。</param>
/// <param name="Remark">备注。</param>
public sealed record ConstraintItemResponse(string TargetId, string? TargetName, string? TargetCode, int GroupNo, string? Remark);

/// <summary>
/// 一条约束规则。
/// </summary>
/// <param name="Id">规则 ID。</param>
/// <param name="Code">编码。</param>
/// <param name="Name">名称。</param>
/// <param name="Type">约束类型。</param>
/// <param name="TargetType">目标类型。</param>
/// <param name="MaxAllowed">数量上限（条件类不用）。</param>
/// <param name="Conditions">条件类的条件。</param>
/// <param name="ViolationAction">违规处理方式。</param>
/// <param name="Priority">优先级（越大越先）。</param>
/// <param name="Status">状态。</param>
/// <param name="EffectiveAt">生效时间。</param>
/// <param name="ExpiresAt">失效时间。</param>
/// <param name="IsEffective">此刻是否生效（启用且在有效期内）。</param>
/// <param name="Description">说明。</param>
/// <param name="Remark">备注。</param>
/// <param name="Items">规则项。</param>
/// <param name="CreatedAt">创建时间。</param>
/// <param name="ModifiedAt">修改时间。</param>
public sealed record ConstraintRuleResponse(
    Guid Id,
    string Code,
    string Name,
    ConstraintType Type,
    ConstraintTargetType TargetType,
    int MaxAllowed,
    IReadOnlyList<AccessPolicyConditionResponse> Conditions,
    ConstraintViolationAction ViolationAction,
    int Priority,
    ConstraintRuleStatus Status,
    DateTimeOffset? EffectiveAt,
    DateTimeOffset? ExpiresAt,
    bool IsEffective,
    string? Description,
    string? Remark,
    IReadOnlyList<ConstraintItemResponse> Items,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt);

/// <summary>
/// <c>constraint-rules.get</c> 的请求。
/// </summary>
/// <param name="Id">规则 ID。</param>
public sealed record GetConstraintRuleRequest([Required] Guid Id);

/// <summary>
/// <c>constraint-rules.create</c> 的请求。
/// </summary>
/// <param name="Code">编码（同一侧唯一，不含空白）。</param>
/// <param name="Name">名称。</param>
/// <param name="Type">约束类型。</param>
/// <param name="TargetType">目标类型（职责分离与条件为角色，互斥为权限，基数为角色或用户）。</param>
/// <param name="Items">规则项，1 到 200 个（职责分离与互斥至少 2 个）。</param>
/// <param name="MaxAllowed">数量上限（缺省 1）。</param>
/// <param name="Conditions">条件类的条件。</param>
/// <param name="ViolationAction">违规处理方式。</param>
/// <param name="Priority">优先级（越大越先）。</param>
/// <param name="Status">状态。</param>
/// <param name="EffectiveAt">生效时间。</param>
/// <param name="ExpiresAt">失效时间。</param>
/// <param name="Description">说明。</param>
/// <param name="Remark">备注。</param>
public sealed record CreateConstraintRuleRequest(
    [Required, StringLength(100, MinimumLength = 1)] string Code,
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    ConstraintType Type,
    ConstraintTargetType TargetType,
    [Required, MinLength(1), MaxLength(SysConstraintRule.MaxItems)] IReadOnlyList<ConstraintItemRequest> Items,
    [Range(1, 10000)] int MaxAllowed = 1,
    [MaxLength(SysAccessPolicy.MaxConditions)] IReadOnlyList<AccessPolicyConditionRequest>? Conditions = null,
    ConstraintViolationAction ViolationAction = ConstraintViolationAction.Deny,
    [Range(0, 100000)] int Priority = 0,
    ConstraintRuleStatus Status = ConstraintRuleStatus.Active,
    DateTimeOffset? EffectiveAt = null,
    DateTimeOffset? ExpiresAt = null,
    [StringLength(1000)] string? Description = null,
    [StringLength(500)] string? Remark = null)
{
    /// <summary>
    /// 转成规则内容。
    /// </summary>
    public ConstraintRuleDefinition ToDefinition()
        => new(Code, Name, Type, TargetType, MaxAllowed, [.. (Conditions ?? []).Select(static c => c.ToDefinition())], ViolationAction, Priority, Status, EffectiveAt, ExpiresAt, Description, Remark, [.. Items.Select(static i => i.ToDefinition())]);
}

/// <summary>
/// <c>constraint-rules.update</c> 的请求：整体修改（含启停与规则项）。
/// </summary>
/// <param name="Id">规则 ID。</param>
/// <param name="Code">编码。</param>
/// <param name="Name">名称。</param>
/// <param name="Type">约束类型。</param>
/// <param name="TargetType">目标类型。</param>
/// <param name="Items">规则项（整组替换）。</param>
/// <param name="MaxAllowed">数量上限。</param>
/// <param name="Conditions">条件类的条件。</param>
/// <param name="ViolationAction">违规处理方式。</param>
/// <param name="Priority">优先级。</param>
/// <param name="Status">状态。</param>
/// <param name="EffectiveAt">生效时间。</param>
/// <param name="ExpiresAt">失效时间。</param>
/// <param name="Description">说明。</param>
/// <param name="Remark">备注。</param>
public sealed record UpdateConstraintRuleRequest(
    [Required] Guid Id,
    [Required, StringLength(100, MinimumLength = 1)] string Code,
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    ConstraintType Type,
    ConstraintTargetType TargetType,
    [Required, MinLength(1), MaxLength(SysConstraintRule.MaxItems)] IReadOnlyList<ConstraintItemRequest> Items,
    [Range(1, 10000)] int MaxAllowed = 1,
    [MaxLength(SysAccessPolicy.MaxConditions)] IReadOnlyList<AccessPolicyConditionRequest>? Conditions = null,
    ConstraintViolationAction ViolationAction = ConstraintViolationAction.Deny,
    [Range(0, 100000)] int Priority = 0,
    ConstraintRuleStatus Status = ConstraintRuleStatus.Active,
    DateTimeOffset? EffectiveAt = null,
    DateTimeOffset? ExpiresAt = null,
    [StringLength(1000)] string? Description = null,
    [StringLength(500)] string? Remark = null)
{
    /// <summary>
    /// 转成规则内容。
    /// </summary>
    public ConstraintRuleDefinition ToDefinition()
        => new(Code, Name, Type, TargetType, MaxAllowed, [.. (Conditions ?? []).Select(static c => c.ToDefinition())], ViolationAction, Priority, Status, EffectiveAt, ExpiresAt, Description, Remark, [.. Items.Select(static i => i.ToDefinition())]);
}

/// <summary>
/// <c>constraint-rules.set-status</c> 的请求。
/// </summary>
/// <param name="Id">规则 ID。</param>
/// <param name="Status">启用或停用。</param>
public sealed record SetConstraintRuleStatusRequest([Required] Guid Id, ConstraintRuleStatus Status);

/// <summary>
/// <c>constraint-rules.delete</c> 的请求。
/// </summary>
/// <param name="Ids">规则 ID，1 到 100 个。</param>
public sealed record ConstraintRuleIdsRequest([Required, MinLength(1), MaxLength(100)] IReadOnlyList<Guid> Ids);
