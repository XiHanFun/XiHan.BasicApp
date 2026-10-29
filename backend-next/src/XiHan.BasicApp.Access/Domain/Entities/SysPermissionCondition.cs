// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Security.Abstractions.Conditions;

namespace XiHan.BasicApp.Access.Domain.Entities;

/// <summary>
/// 权限条件的一条子句（平台库实体，随所属侧）：挂在一个授权绑定（侧 × 主体 × 权限码 × 授予或拒绝）上，方案 5.9 的 ABAC。
/// 同一绑定里同组的子句取且、组之间取或；挂在授予上是「满足才授予」，挂在拒绝上是「满足才拒绝」。绑定按键直接对应，不经规则行（普通授予没有规则行）；
/// 绑定没了（撤销、授予换成拒绝或反过来、删角色）即连同删除。值种类由属性登记决定，不另存值类型（4.x 的 ValueType）。
/// </summary>
public sealed class SysPermissionCondition : AppAuditedEntity, IOwnedByTenant
{
    /// <summary>
    /// 每个绑定最多几组（4.x 同）。
    /// </summary>
    public const int MaxGroups = 5;

    /// <summary>
    /// 每组最多几条（4.x 同）。
    /// </summary>
    public const int MaxClausesPerGroup = 10;

    /// <summary>
    /// 条件值的长度上限。
    /// </summary>
    public const int MaxValueLength = 2000;

    /// <inheritdoc />
    public string OwnerTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 主体类别（角色或用户）。
    /// </summary>
    public GrantSubjectKind SubjectKind { get; set; }

    /// <summary>
    /// 主体 ID。
    /// </summary>
    public Guid SubjectId { get; set; }

    /// <summary>
    /// 权限码。
    /// </summary>
    [StringLength(128)]
    public string Permission { get; set; } = string.Empty;

    /// <summary>
    /// 挂在授予上还是拒绝上。
    /// </summary>
    public PermissionAction Action { get; set; }

    /// <summary>
    /// 条件组号（同组取且，组间取或）。
    /// </summary>
    public int GroupNo { get; set; }

    /// <summary>
    /// 属性名（登记名、别名或族名，如 <c>environment.ip</c>）。
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
    [StringLength(MaxValueLength)]
    public string? Value { get; set; }

    /// <summary>
    /// 说明。
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// 状态：停用的子句不参与判定。
    /// </summary>
    public ConditionStatus Status { get; set; } = ConditionStatus.Active;
}

/// <summary>
/// 权限条件子句的状态。
/// </summary>
public enum ConditionStatus
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
