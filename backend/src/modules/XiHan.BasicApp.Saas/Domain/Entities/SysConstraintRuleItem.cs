#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysConstraintRuleItem
// Guid:e5f6a7b8-c9d0-1234-ef01-345678901205
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/14 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 约束规则目标项实体
/// 将约束规则涉及的角色/权限/用户 ID 从 JSON 拆分为独立记录，保持引用完整性
/// </summary>
[SugarTable("Sys_Constraint_Rule_Item", "约束规则目标项表")]
[SugarIndex("UX_SysConstraintRuleItem_CrId_TaTy_TaId", nameof(ConstraintRuleId), OrderByType.Asc, nameof(TargetType), OrderByType.Asc, nameof(TargetId), OrderByType.Asc, true)]
[SugarIndex("IX_SysConstraintRuleItem_CrId", nameof(ConstraintRuleId), OrderByType.Asc)]
[SugarIndex("IX_SysConstraintRuleItem_TaId", nameof(TargetId), OrderByType.Asc)]
[SugarIndex("IX_SysConstraintRuleItem_TaTy", nameof(TargetType), OrderByType.Asc)]
[SugarIndex("IX_SysConstraintRuleItem_CoGr", nameof(ConstraintGroup), OrderByType.Asc)]
[SugarIndex("IX_SysConstraintRuleItem_St", nameof(Status), OrderByType.Asc)]
[SugarIndex("IX_SysConstraintRuleItem_TeId_CrId", nameof(TenantId), OrderByType.Asc, nameof(ConstraintRuleId), OrderByType.Asc)]
public partial class SysConstraintRuleItem : BasicAppCreationEntity
{
    /// <summary>
    /// 约束规则ID
    /// </summary>
    [SugarColumn(ColumnDescription = "约束规则ID", IsNullable = false)]
    public virtual long ConstraintRuleId { get; set; }

    /// <summary>
    /// 目标类型
    /// </summary>
    [SugarColumn(ColumnDescription = "目标类型")]
    public virtual ConstraintTargetType TargetType { get; set; } = ConstraintTargetType.Role;

    /// <summary>
    /// 目标ID（角色/权限/用户的 ID）
    /// </summary>
    [SugarColumn(ColumnDescription = "目标ID", IsNullable = false)]
    public virtual long TargetId { get; set; }

    /// <summary>
    /// 约束分组（同组为互斥集合；先决条件约束中 0=必备项、1=目标项）
    /// </summary>
    [SugarColumn(ColumnDescription = "约束分组")]
    public virtual int ConstraintGroup { get; set; } = 0;

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
