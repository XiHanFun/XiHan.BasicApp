// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Access.Domain.Entities;

/// <summary>
/// 角色自定义数据范围的一行（平台库实体，随角色分侧）
/// </summary>
/// <remarks>
/// <see cref="DataScope.Custom"/> 的角色能看到的部门，一部门一行：范围的种类存在角色上，只有自定义才要部门清单，所以拆成关联表；平台角色只能是全部，没有这类行
/// 部门在租户库、角色在平台库，<see cref="DepartmentId"/> 只记主键：保存时校验部门都在当前租户，部门删除不连带清理；<see cref="IncludeChildren"/> 在判定时经部门闭包表展开，部门树变了不必改这里
/// 只随角色的数据范围整体替换（先删后写、同一部门合成一行），物理删除；范围改成非自定义即清空，角色删除时连同移除
/// </remarks>
[SugarTable(TableName = "sys_role_data_scope", TableDescription = "角色自定义数据范围的一行（平台库实体，随角色分侧）")]
[SugarIndex("ux_sys_role_data_scope_role_id_department_id", nameof(RoleId), OrderByType.Asc, nameof(DepartmentId), OrderByType.Asc, true)]
public sealed class SysRoleDataScope : AppRecord, IOwnedByTenant
{
    /// <inheritdoc />
    [SugarColumn(ColumnName = "owner_tenant_id", Length = 512, IsNullable = false, ColumnDescription = "属主租户 ID")]
    public string OwnerTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 角色
    /// </summary>
    [SugarColumn(ColumnName = "role_id", IsNullable = false, ColumnDescription = "角色")]
    public Guid RoleId { get; set; }

    /// <summary>
    /// 部门（租户库 <c>SysDepartment</c> 主键）
    /// </summary>
    [SugarColumn(ColumnName = "department_id", IsNullable = false, ColumnDescription = "部门（租户库 SysDepartment 主键）")]
    public Guid DepartmentId { get; set; }

    /// <summary>
    /// 是否连同子部门
    /// </summary>
    [SugarColumn(ColumnName = "include_children", IsNullable = false, ColumnDescription = "是否连同子部门")]
    public bool IncludeChildren { get; set; }
}
