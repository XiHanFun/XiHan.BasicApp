// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Organization.Domain.Entities;

/// <summary>
/// 部门闭包表（租户库实体）
/// </summary>
/// <remarks>
/// 闭包表：每个部门到自己（深度 0）与到每个祖先（深度 1、2、…）各一行，「全部下级」「全部上级」都是一次按索引的查询、不用递归，因为数据范围、按部门找人、字段安全沿上级链展开都在请求路径上
/// 代价是写放大：挂载要按上级的祖先链写多行，移动要重算整棵子树；组织架构的改动远少于读取，这个取舍划算
/// 只由 <see cref="Services.DepartmentService"/> 维护，操作不直接写：闭包行必须与 <see cref="SysDepartment.ParentId"/> 一致，移动时还要先拒绝移到自己或子孙之下
/// 它是由树派生的数据，所以不带审计列、不进数据变更日志，行只增删不修改，删除即物理删除；没有软删除，（租户, 祖先, 子孙）用的是普通唯一索引而不是部分唯一索引
/// 深度 0 的自身行让按部门找人一次取到部门自身与全部下级；两个索引分别对应按祖先取子孙与按子孙取祖先这两种读法
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
