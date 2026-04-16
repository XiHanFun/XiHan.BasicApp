#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysConstraintRule
// Guid:6e7f8901-2345-6789-0123-456789012345
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/07 10:30:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统约束规则实体
/// 定义 RBAC 约束规则（静态职责分离SSD、动态职责分离DSD、互斥约束等）
/// </summary>
/// <remarks>
/// 规则生命周期由 IsEnabled 唯一控制：
/// - true：规则生效（还须在有效期 EffectiveFrom~EffectiveTo 范围内）
/// - false：规则停用/归档
/// 服务层判断"规则是否生效"：IsEnabled == true AND 当前时间在有效期内
/// </remarks>
[SugarTable("SysConstraintRule", "系统约束规则表")]
[SugarIndex("IX_{table}_TeId", nameof(TenantId), OrderByType.Asc)]
[SugarIndex("IX_{table}_CrTi", nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_MoTi", nameof(ModifiedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_MoId", nameof(ModifiedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_IsDe", nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("UX_{table}_TeId_RuCo", nameof(TenantId), OrderByType.Asc, nameof(RuleCode), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_CoTy", nameof(ConstraintType), OrderByType.Asc)]
[SugarIndex("IX_{table}_IsEn", nameof(IsEnabled), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_CoTy", nameof(TenantId), OrderByType.Asc, nameof(ConstraintType), OrderByType.Asc)]
[SugarIndex("IX_{table}_IsGl", nameof(IsGlobal), OrderByType.Asc)]
public partial class SysConstraintRule : BasicAppFullAuditedEntity
{
    /// <summary>
    /// 规则编码（唯一标识）
    /// </summary>
    [SugarColumn(ColumnDescription = "规则编码", Length = 100, IsNullable = false)]
    public virtual string RuleCode { get; set; } = string.Empty;

    /// <summary>
    /// 规则名称
    /// </summary>
    [SugarColumn(ColumnDescription = "规则名称", Length = 200, IsNullable = false)]
    public virtual string RuleName { get; set; } = string.Empty;

    /// <summary>
    /// 约束类型
    /// </summary>
    [SugarColumn(ColumnDescription = "约束类型")]
    public virtual ConstraintType ConstraintType { get; set; } = ConstraintType.SSD;

    /// <summary>
    /// 约束目标类型
    /// </summary>
    [SugarColumn(ColumnDescription = "约束目标类型")]
    public virtual ConstraintTargetType TargetType { get; set; } = ConstraintTargetType.Role;

    /// <summary>
    /// 约束参数（JSON格式，存储非 ID 类配置项，具体目标 ID 请使用 SysConstraintRuleItem）
    /// 示例：
    /// SSD: { "maxAllowed": 1 }
    /// DSD: { "timeWindow": "8h" }
    /// Cardinality: { "maxCount": 5 }
    /// Temporal: { "allowedHours": "09:00-18:00", "allowedDays": "Mon-Fri" }
    /// </summary>
    [SugarColumn(ColumnDescription = "约束参数", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? Parameters { get; set; }

    /// <summary>
    /// 是否平台级全局约束规则（全局规则对所有租户生效，TenantId 为空）
    /// </summary>
    [SugarColumn(ColumnDescription = "是否全局约束规则")]
    public virtual bool IsGlobal { get; set; } = false;

    /// <summary>
    /// 是否启用
    /// </summary>
    [SugarColumn(ColumnDescription = "是否启用")]
    public virtual bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 违规处理方式
    /// </summary>
    [SugarColumn(ColumnDescription = "违规处理方式")]
    public virtual ViolationAction ViolationAction { get; set; } = ViolationAction.Deny;

    /// <summary>
    /// 规则描述
    /// </summary>
    [SugarColumn(ColumnDescription = "规则描述", Length = 1000, IsNullable = true)]
    public virtual string? Description { get; set; }

    /// <summary>
    /// 规则优先级（数字越大优先级越高，与 SysPermission.Priority 方向一致）
    /// </summary>
    [SugarColumn(ColumnDescription = "规则优先级")]
    public virtual int Priority { get; set; } = 0;

    /// <summary>
    /// 生效时间
    /// </summary>
    [SugarColumn(ColumnDescription = "生效时间", IsNullable = true)]
    public virtual DateTimeOffset? EffectiveFrom { get; set; }

    /// <summary>
    /// 失效时间
    /// </summary>
    [SugarColumn(ColumnDescription = "失效时间", IsNullable = true)]
    public virtual DateTimeOffset? EffectiveTo { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
