#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysDepartmentHierarchy
// Guid:5889aba2-71e8-48e2-8c6c-2cc2f403ed0a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/28 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities.Base;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统部门继承关系表实体
/// 支持部门多继承，子部门继承父部门的权限
/// </summary>
[SugarTable("Sys_Department_Hierarchy", "系统部门继承关系表")]
[SugarIndex("UX_SysDepartmentHierarchy_Ancestor_Descendant", nameof(AncestorId), OrderByType.Asc, nameof(DescendantId), OrderByType.Asc, true)]
[SugarIndex("IX_SysDepartmentHierarchy_DescendantId", nameof(DescendantId), OrderByType.Asc)]
[SugarIndex("IX_SysDepartmentHierarchy_Depth", nameof(Depth), OrderByType.Asc)]
[SugarIndex("IX_SysDepartmentHierarchy_AncestorId_Depth", nameof(AncestorId), OrderByType.Asc, nameof(Depth), OrderByType.Asc)]
public partial class SysDepartmentHierarchy : RbacCreationEntity<long>
{
    /// <summary>
    /// 祖先部门ID
    /// </summary>
    /// <remarks>
    /// 包含所有上级部门，包括自己（Depth=0）
    /// </remarks>
    [SugarColumn(ColumnDescription = "祖先部门ID", IsNullable = false)]
    public virtual long AncestorId { get; set; }

    /// <summary>
    /// 后代部门ID
    /// </summary>
    /// <remarks>
    /// 包含所有下级部门，包括自己（Depth=0）
    /// </remarks>
    [SugarColumn(ColumnDescription = "后代部门ID", IsNullable = false)]
    public virtual long DescendantId { get; set; }

    /// <summary>
    /// 层级深度
    /// </summary>
    /// <remarks>
    /// - 0: 自己（自关联记录）
    /// - 1: 直接子部门
    /// - 2: 孙部门
    /// - n: n级子部门
    /// </remarks>
    [SugarColumn(ColumnDescription = "层级深度")]
    public virtual int Depth { get; set; } = 0;

    /// <summary>
    /// 路径（从祖先到后代的完整路径）
    /// </summary>
    /// <remarks>
    /// 格式：祖先ID/...中间ID.../后代ID
    /// 例如：1/3/5/7 表示从部门1到部门7的路径
    /// 用于快速显示部门层级关系和面包屑导航
    /// </remarks>
    [SugarColumn(ColumnDescription = "路径", Length = 1000, IsNullable = true)]
    public virtual string? Path { get; set; }

    /// <summary>
    /// 路径名称（用于显示）
    /// </summary>
    /// <remarks>
    /// 格式：祖先名称/...中间名称.../后代名称
    /// 例如：集团总部/华东大区/上海分公司/技术部
    /// 冗余字段，便于直接显示完整路径
    /// </remarks>
    [SugarColumn(ColumnDescription = "路径名称", Length = 1000, IsNullable = true)]
    public virtual string? PathName { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
