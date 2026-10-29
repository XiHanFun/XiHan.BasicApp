// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Security.Abstractions.Conditions;

namespace XiHan.BasicApp.Access.Domain.Entities;

/// <summary>
/// 访问策略（平台库实体，随所属侧，方案 5.9）：按请求属性（地址、归属地、时段、User-Agent、主体、路径等）只记录、拒绝或放行。
/// 平台层管全部调用，租户层管本租户已登录的调用；层内放行压过拒绝、平台拒绝不看租户层（4.x 同）。新建一律只记录，动作与状态另有操作修改。
/// </summary>
public sealed class SysAccessPolicy : AppAuditedEntity, IOwnedByTenant
{
    /// <summary>
    /// 每条策略最多几个条件组（4.x 同）。
    /// </summary>
    public const int MaxGroups = 20;

    /// <summary>
    /// 每条策略最多几条条件（4.x 同）。
    /// </summary>
    public const int MaxConditions = 100;

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
    /// 说明。
    /// </summary>
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// 命中时的动作。
    /// </summary>
    public AccessPolicyAction Action { get; set; }

    /// <summary>
    /// 优先级（越小越先求值）。
    /// </summary>
    public int Priority { get; set; } = 100;

    /// <summary>
    /// 状态。
    /// </summary>
    public AccessPolicyStatus Status { get; set; } = AccessPolicyStatus.Active;

    /// <summary>
    /// 生效时间；为空即不限。
    /// </summary>
    public DateTimeOffset? EffectiveAt { get; set; }

    /// <summary>
    /// 失效时间；为空即不限。
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// 来自哪个预置模板。
    /// </summary>
    [StringLength(100)]
    public string? TemplateCode { get; set; }

    /// <summary>
    /// 备注。
    /// </summary>
    [StringLength(500)]
    public string? Remark { get; set; }

    /// <summary>
    /// 某一时刻是否在有效期内。
    /// </summary>
    public bool IsEffectiveAt(DateTimeOffset now) => (EffectiveAt is null || EffectiveAt <= now) && (ExpiresAt is null || ExpiresAt > now);
}

/// <summary>
/// 访问策略的一条条件（平台库实体）：随策略整体替换，组内且、组间或。
/// </summary>
public sealed class SysAccessPolicyCondition : AppRecord, IOwnedByTenant
{
    /// <inheritdoc />
    public string OwnerTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 所属策略。
    /// </summary>
    public Guid PolicyId { get; set; }

    /// <summary>
    /// 写入顺序（读回时按它排）。
    /// </summary>
    public int Ordinal { get; set; }

    /// <summary>
    /// 条件组号。
    /// </summary>
    public int GroupNo { get; set; }

    /// <summary>
    /// 属性名。
    /// </summary>
    [StringLength(200)]
    public string AttributeName { get; set; } = string.Empty;

    /// <summary>
    /// 运算符。
    /// </summary>
    public ConditionOperator Operator { get; set; }

    /// <summary>
    /// 是否取反。
    /// </summary>
    public bool IsNegated { get; set; }

    /// <summary>
    /// 条件值（判空类运算符不填）。
    /// </summary>
    [StringLength(SysPermissionCondition.MaxValueLength)]
    public string? Value { get; set; }

    /// <summary>
    /// 说明。
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }
}

/// <summary>
/// 访问策略命中时的动作（4.x 同）。
/// </summary>
public enum AccessPolicyAction
{
    /// <summary>
    /// 只记录命中。
    /// </summary>
    LogOnly = 0,

    /// <summary>
    /// 拒绝（同层有放行命中时不拒绝）。
    /// </summary>
    Deny = 1,

    /// <summary>
    /// 放行（压过同层的拒绝）。
    /// </summary>
    Allow = 2,
}

/// <summary>
/// 访问策略的状态。
/// </summary>
public enum AccessPolicyStatus
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
