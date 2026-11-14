// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Organization.Domain.Entities;

/// <summary>
/// 部门闭包表（租户库实体）
/// </summary>
/// <remarks>
/// 每个部门到自己（深度 0）与到每个祖先（深度 1、2、…）各一行
/// 创建时写入、移动时整棵子树重算、删除时连同移除；由 <see cref="Services.DepartmentService"/> 维护，操作不直接写
/// </remarks>
[SugarTable(TableName = "sys_department_hierarchy", TableDescription = "部门闭包表（租户库实体）")]
[SugarIndex("ux_sys_department_hierarchy_tenant_id_ancestor_id_descendant_id", nameof(TenantId), OrderByType.Asc, nameof(AncestorId), OrderByType.Asc, nameof(DescendantId), OrderByType.Asc, true)]
[SugarIndex("ix_sys_department_hierarchy_tenant_id_descendant_id", nameof(TenantId), OrderByType.Asc, nameof(DescendantId), OrderByType.Asc)]
public sealed class SysDepartmentHierarchy : TenantScopedEntity
{
    /// <summary>
    /// 祖先部门（深度 0 时是自己）
    /// </summary>
    [SugarColumn(ColumnName = "ancestor_id", IsNullable = false, ColumnDescription = "祖先部门（深度 0 时是自己）")]
    public Guid AncestorId { get; set; }

    /// <summary>
    /// 子孙部门
    /// </summary>
    [SugarColumn(ColumnName = "descendant_id", IsNullable = false, ColumnDescription = "子孙部门")]
    public Guid DescendantId { get; set; }

    /// <summary>
    /// 层级距离：0 是自己，1 是直接上级
    /// </summary>
    [SugarColumn(ColumnName = "depth", IsNullable = false, ColumnDescription = "层级距离：0 是自己，1 是直接上级")]
    public int Depth { get; set; }
}
