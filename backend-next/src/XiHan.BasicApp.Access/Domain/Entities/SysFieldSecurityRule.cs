// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.BasicApp.Contracts.Security;

namespace XiHan.BasicApp.Access.Domain.Entities;

/// <summary>
/// 字段安全规则（平台库实体，随所属侧）：一个目标（角色、用户或部门）在一个实体字段上的读取方式与能否改写（方案 5.9）。
/// 同一侧同一目标同一字段只有一条；规则只在所属一侧生效（平台规则管平台主体，租户规则管本租户的主体）。
/// </summary>
public sealed class SysFieldSecurityRule : AppAuditedEntity, IOwnedByTenant
{
    /// <summary>
    /// 部分脱敏保留位数的上限（4.x 同）。
    /// </summary>
    public const int MaxKeep = 32;

    /// <summary>
    /// 固定文本的长度上限（4.x 同）。
    /// </summary>
    public const int MaxReplacementLength = 100;

    /// <inheritdoc />
    public string OwnerTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 目标类型。
    /// </summary>
    public FieldSecurityTargetKind TargetKind { get; set; }

    /// <summary>
    /// 目标 ID（角色、用户或部门）。
    /// </summary>
    public Guid TargetId { get; set; }

    /// <summary>
    /// 实体码（在字段安全目录里登记过，如 <c>identity.users</c>）。
    /// </summary>
    [StringLength(64)]
    public string Entity { get; set; } = string.Empty;

    /// <summary>
    /// 字段名（与接口字段同名）。
    /// </summary>
    [StringLength(64)]
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// 读取方式。
    /// </summary>
    public FieldMaskStrategy Strategy { get; set; }

    /// <summary>
    /// 部分脱敏保留前几位。
    /// </summary>
    public int? KeepHead { get; set; }

    /// <summary>
    /// 部分脱敏保留后几位。
    /// </summary>
    public int? KeepTail { get; set; }

    /// <summary>
    /// 固定文本。
    /// </summary>
    [StringLength(MaxReplacementLength)]
    public string? Replacement { get; set; }

    /// <summary>
    /// 能否改写：明文规则只能只读；脱敏且可改即只写。
    /// </summary>
    public bool IsEditable { get; set; }

    /// <summary>
    /// 状态。
    /// </summary>
    public FieldRuleStatus Status { get; set; } = FieldRuleStatus.Active;

    /// <summary>
    /// 备注。
    /// </summary>
    [StringLength(512)]
    public string? Remark { get; set; }
}
