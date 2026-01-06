#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysConstraintRule
// Guid:6e7f8901-2345-6789-0123-456789012345
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/7 10:30:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities.Base;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统约束规则实体
/// 定义 RBAC 约束规则（静态职责分离SSD、动态职责分离DSD、互斥约束等）
/// </summary>
[SugarTable("Sys_Constraint_Rule", "系统约束规则表")]
[SugarIndex("IX_SysConstraintRule_RuleCode", nameof(RuleCode), OrderByType.Asc, true)]
[SugarIndex("IX_SysConstraintRule_Type", nameof(ConstraintType), OrderByType.Asc)]
[SugarIndex("IX_SysConstraintRule_IsEnabled", nameof(IsEnabled), OrderByType.Asc)]
public partial class SysConstraintRule : RbacFullAuditedEntity<long>
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
    /// 约束目标类型（Role/Permission/User）
    /// </summary>
    [SugarColumn(ColumnDescription = "约束目标类型", Length = 50, IsNullable = false)]
    public virtual string TargetType { get; set; } = "Role";

    /// <summary>
    /// 约束参数（JSON格式）
    /// 示例：
    /// SSD: { "conflictRoles": [1, 2, 3], "maxAllowed": 1 }
    /// DSD: { "conflictRoles": [4, 5], "timeWindow": "8h" }
    /// Cardinality: { "targetType": "Role", "maxCount": 5 }
    /// Prerequisite: { "requiredRole": 1, "targetRole": 2 }
    /// </summary>
    [SugarColumn(ColumnDescription = "约束参数", ColumnDataType = "text", IsNullable = false)]
    public virtual string Parameters { get; set; } = "{}";

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
    /// 规则优先级（数字越大优先级越高）
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
    /// 状态
    /// </summary>
    [SugarColumn(ColumnDescription = "状态")]
    public virtual YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
