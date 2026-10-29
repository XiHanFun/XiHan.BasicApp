// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.Access.Domain.Entities;

/// <summary>
/// 约束规则（平台库实体，随所属侧）。
/// </summary>
/// <remarks>
/// 见方案 5.9 与第 10 节第 7 项。静态职责分离、动态职责分离、权限互斥、基数与条件五种，违规按处理方式拒绝、警告、只记录或需审批（审批流程未接入，按拒绝）。
/// 规则项是这条规则约束的目标（角色、权限码或用户），同一组号的项是一组；数量上限写在 <see cref="MaxAllowed"/>，条件类的条件以 JSON 存在 <see cref="Conditions"/>。
/// </remarks>
public sealed class SysConstraintRule : AppAuditedEntity, IOwnedByTenant
{
    /// <summary>
    /// 每条规则最多几个规则项。
    /// </summary>
    public const int MaxItems = 200;

    /// <inheritdoc />
    public string OwnerTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 编码（同一侧唯一，不含空白）。
    /// </summary>
    [StringLength(100)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 名称。
    /// </summary>
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 约束类型。
    /// </summary>
    public ConstraintType Type { get; set; }

    /// <summary>
    /// 规则项的目标类型（由约束类型决定可选哪些）。
    /// </summary>
    public ConstraintTargetType TargetType { get; set; }

    /// <summary>
    /// 数量上限。
    /// </summary>
    /// <remarks>
    /// 职责分离与互斥为同一组里最多同时持有（激活）几个，基数为每个目标最多几个成员或角色；条件类不用。
    /// </remarks>
    public int MaxAllowed { get; set; } = 1;

    /// <summary>
    /// 条件类的条件。
    /// </summary>
    /// <remarks>
    /// JSON 数组：组号、属性、运算符、取反、值；其余类型为空。
    /// </remarks>
    [LongText]
    public string? Conditions { get; set; }

    /// <summary>
    /// 违规处理方式。
    /// </summary>
    public ConstraintViolationAction ViolationAction { get; set; }

    /// <summary>
    /// 优先级（越大越先求值）。
    /// </summary>
    /// <remarks>
    /// 4.x 同。
    /// </remarks>
    public int Priority { get; set; }

    /// <summary>
    /// 状态。
    /// </summary>
    public ConstraintRuleStatus Status { get; set; } = ConstraintRuleStatus.Active;

    /// <summary>
    /// 生效时间。
    /// </summary>
    /// <remarks>
    /// 为空即不限。
    /// </remarks>
    public DateTimeOffset? EffectiveAt { get; set; }

    /// <summary>
    /// 失效时间。
    /// </summary>
    /// <remarks>
    /// 为空即不限。
    /// </remarks>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// 说明。
    /// </summary>
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// 备注。
    /// </summary>
    [StringLength(500)]
    public string? Remark { get; set; }

    /// <summary>
    /// 某一时刻是否生效：启用且在有效期内。
    /// </summary>
    public bool IsEffectiveAt(DateTimeOffset now) => Status == ConstraintRuleStatus.Active && (EffectiveAt is null || EffectiveAt <= now) && (ExpiresAt is null || ExpiresAt > now);
}

/// <summary>
/// 约束规则的一个规则项（平台库实体）：随规则整体替换。
/// </summary>
public sealed class SysConstraintRuleItem : AppRecord, IOwnedByTenant
{
    /// <inheritdoc />
    public string OwnerTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 所属规则。
    /// </summary>
    public Guid RuleId { get; set; }

    /// <summary>
    /// 目标：角色 ID、权限码或用户 ID（按规则的目标类型）。
    /// </summary>
    [StringLength(128)]
    public string TargetId { get; set; } = string.Empty;

    /// <summary>
    /// 组号：职责分离与互斥按组判定，同一组的项互相排斥。
    /// </summary>
    public int GroupNo { get; set; }

    /// <summary>
    /// 备注。
    /// </summary>
    [StringLength(500)]
    public string? Remark { get; set; }
}

/// <summary>
/// 约束类型。
/// </summary>
/// <remarks>
/// 4.x 的先决条件、时间、位置与自定义没有求值，6.0 不保留；时间与位置用条件类表达。
/// </remarks>
public enum ConstraintType
{
    /// <summary>
    /// 静态职责分离：同一用户不能同时持有一组里超过上限个角色（按继承展开）。
    /// </summary>
    Ssd = 0,

    /// <summary>
    /// 动态职责分离：同一会话不能同时激活一组里超过上限个角色（按继承展开）。
    /// </summary>
    Dsd = 1,

    /// <summary>
    /// 权限互斥：同一角色或用户不能同时直接持有一组里超过上限个权限码。
    /// </summary>
    MutualExclusion = 2,

    /// <summary>
    /// 基数：角色的成员数上限，或用户持有的角色数上限。
    /// </summary>
    Cardinality = 3,

    /// <summary>
    /// 条件：指派规则项里的角色时，按条件引擎判定被指派的用户与这次请求是否满足条件。
    /// </summary>
    Condition = 4,
}

/// <summary>
/// 约束规则项的目标类型。
/// </summary>
public enum ConstraintTargetType
{
    /// <summary>
    /// 角色。
    /// </summary>
    Role = 0,

    /// <summary>
    /// 权限码。
    /// </summary>
    Permission = 1,

    /// <summary>
    /// 用户。
    /// </summary>
    User = 2,
}

/// <summary>
/// 违规处理方式。
/// </summary>
/// <remarks>
/// 4.x 同。
/// </remarks>
public enum ConstraintViolationAction
{
    /// <summary>
    /// 拒绝这次变更。
    /// </summary>
    Deny = 0,

    /// <summary>
    /// 放行并记一条权限变更日志。
    /// </summary>
    Warning = 1,

    /// <summary>
    /// 放行并记一条权限变更日志（与警告只差在级别）。
    /// </summary>
    Log = 2,

    /// <summary>
    /// 需要审批。
    /// </summary>
    /// <remarks>
    /// 审批流程未接入，按拒绝处理（4.x 同）。
    /// </remarks>
    RequireApproval = 3,
}

/// <summary>
/// 约束规则的状态。
/// </summary>
public enum ConstraintRuleStatus
{
    /// <summary>
    /// 启用。
    /// </summary>
    Active = 0,

    /// <summary>
    /// 停用。
    /// </summary>
    Disabled = 1,
}
