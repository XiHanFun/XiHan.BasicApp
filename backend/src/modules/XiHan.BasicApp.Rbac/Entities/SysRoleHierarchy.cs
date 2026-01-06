#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysRoleHierarchy
// Guid:4c5d6e7f-8901-2345-def0-234567890123
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/7 10:20:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities.Base;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统角色继承关系实体
/// 支持角色多继承，子角色继承父角色的所有权限
/// </summary>
[SugarTable("Sys_Role_Hierarchy", "系统角色继承关系表")]
[SugarIndex("IX_SysRoleHierarchy_Parent_Child", nameof(ParentRoleId), OrderByType.Asc, nameof(ChildRoleId), OrderByType.Asc, true)]
[SugarIndex("IX_SysRoleHierarchy_ChildRoleId", nameof(ChildRoleId), OrderByType.Asc)]
[SugarIndex("IX_SysRoleHierarchy_Depth", nameof(Depth), OrderByType.Asc)]
public partial class SysRoleHierarchy : RbacFullAuditedEntity<long>
{
    /// <summary>
    /// 父角色ID
    /// </summary>
    [SugarColumn(ColumnDescription = "父角色ID", IsNullable = false)]
    public virtual long ParentRoleId { get; set; }

    /// <summary>
    /// 子角色ID
    /// </summary>
    [SugarColumn(ColumnDescription = "子角色ID", IsNullable = false)]
    public virtual long ChildRoleId { get; set; }

    /// <summary>
    /// 继承深度（0=直接继承，>0=间接继承的层级数）
    /// </summary>
    [SugarColumn(ColumnDescription = "继承深度")]
    public virtual int Depth { get; set; } = 0;

    /// <summary>
    /// 是否直接继承（true=直接，false=通过中间角色间接继承）
    /// </summary>
    [SugarColumn(ColumnDescription = "是否直接继承")]
    public virtual bool IsDirect { get; set; } = true;

    /// <summary>
    /// 继承路径（如：1 > 3 > 5，从父到子的完整路径）
    /// </summary>
    [SugarColumn(ColumnDescription = "继承路径", Length = 500, IsNullable = true)]
    public virtual string? InheritancePath { get; set; }

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
