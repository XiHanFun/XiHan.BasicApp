// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.Framework.Data.SqlSugar.Routing;
using XiHan.Framework.Domain.Entities.Abstracts;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 访问策略条件实体：同组内且、组间或
/// </summary>
/// <remarks>
/// 列与 <see cref="SysPermissionCondition"/> 同形；随策略整体替换，租户与策略一致。
/// </remarks>
[SugarTable(TableName = "Sys_Access_Policy_Condition", TableDescription = "访问策略条件表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_PoId", nameof(TenantId), OrderByType.Asc, nameof(PolicyId), OrderByType.Asc)]
[PlatformDataSource]
public partial class SysAccessPolicyCondition : BasicAppCreationEntity, IStrictMultiTenantEntity
{
    /// <summary>
    /// 所属策略主键
    /// </summary>
    [SugarColumn(ColumnName = "Policy_Id", ColumnDescription = "策略主键")]
    public virtual long PolicyId { get; set; }

    /// <summary>
    /// 条件分组
    /// </summary>
    [SugarColumn(ColumnName = "Condition_Group", ColumnDescription = "条件分组")]
    public virtual int ConditionGroup { get; set; }

    /// <summary>
    /// 属性名称（subject.* / resource.* / environment.*）
    /// </summary>
    [SugarColumn(ColumnName = "Attribute_Name", ColumnDescription = "属性名称", Length = 200, IsNullable = false)]
    public virtual string AttributeName { get; set; } = string.Empty;

    /// <summary>
    /// 操作符
    /// </summary>
    [SugarColumn(ColumnName = "Operator", ColumnDescription = "操作符")]
    public virtual ConditionOperator Operator { get; set; } = ConditionOperator.Equals;

    /// <summary>
    /// 是否取反
    /// </summary>
    [SugarColumn(ColumnName = "Is_Negated", ColumnDescription = "是否取反")]
    public virtual bool IsNegated { get; set; }

    /// <summary>
    /// 条件值类型
    /// </summary>
    [SugarColumn(ColumnName = "Value_Type", ColumnDescription = "条件值类型")]
    public virtual ConfigDataType ValueType { get; set; } = ConfigDataType.String;

    /// <summary>
    /// 条件值
    /// </summary>
    [SugarColumn(ColumnName = "Condition_Value", ColumnDescription = "条件值", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = false)]
    public virtual string ConditionValue { get; set; } = string.Empty;

    /// <summary>
    /// 条件说明
    /// </summary>
    [SugarColumn(ColumnName = "Description", ColumnDescription = "条件说明", Length = 500, IsNullable = true)]
    public virtual string? Description { get; set; }
}
