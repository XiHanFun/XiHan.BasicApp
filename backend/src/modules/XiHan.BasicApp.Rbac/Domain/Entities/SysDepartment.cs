#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysDepartment
// Guid:6c28152c-d6e9-4396-addb-b479254bad10
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 02:45:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统部门实体
/// </summary>
[SugarTable("Sys_Department", "系统部门表")]
[SugarIndex("IX_SysDepartment_DeCo", nameof(DepartmentCode), OrderByType.Asc, true)]
[SugarIndex("IX_SysDepartment_PaId", nameof(ParentId), OrderByType.Asc)]
[SugarIndex("IX_SysDepartment_St", nameof(Status), OrderByType.Asc)]
[SugarIndex("IX_SysDepartment_TeId_St", nameof(TenantId), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
[SugarIndex("IX_SysDepartment_DeTy", nameof(DepartmentType), OrderByType.Asc)]
[SugarIndex("IX_SysDepartment_LeId", nameof(LeaderId), OrderByType.Asc)]
public partial class SysDepartment : BasicAppAggregateRoot
{
    /// <summary>
    /// 父级部门ID
    /// </summary>
    [SugarColumn(ColumnDescription = "父级部门ID", IsNullable = true)]
    public virtual long? ParentId { get; set; }

    /// <summary>
    /// 部门名称
    /// </summary>
    [SugarColumn(ColumnDescription = "部门名称", Length = 100, IsNullable = false)]
    public virtual string DepartmentName { get; set; } = string.Empty;

    /// <summary>
    /// 部门编码
    /// </summary>
    [SugarColumn(ColumnDescription = "部门编码", Length = 100, IsNullable = false)]
    public virtual string DepartmentCode { get; set; } = string.Empty;

    /// <summary>
    /// 部门类型
    /// </summary>
    [SugarColumn(ColumnDescription = "部门类型")]
    public virtual DepartmentType DepartmentType { get; set; } = DepartmentType.Department;

    /// <summary>
    /// 负责人ID
    /// </summary>
    [SugarColumn(ColumnDescription = "负责人ID", IsNullable = true)]
    public virtual long? LeaderId { get; set; }

    /// <summary>
    /// 联系电话
    /// </summary>
    [SugarColumn(ColumnDescription = "联系电话", Length = 20, IsNullable = true)]
    public virtual string? Phone { get; set; }

    /// <summary>
    /// 邮箱
    /// </summary>
    [SugarColumn(ColumnDescription = "邮箱", Length = 100, IsNullable = true)]
    public virtual string? Email { get; set; }

    /// <summary>
    /// 地址
    /// </summary>
    [SugarColumn(ColumnDescription = "地址", Length = 500, IsNullable = true)]
    public virtual string? Address { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnDescription = "状态")]
    public virtual YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 排序
    /// </summary>
    [SugarColumn(ColumnDescription = "排序")]
    public virtual int Sort { get; set; } = 0;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
