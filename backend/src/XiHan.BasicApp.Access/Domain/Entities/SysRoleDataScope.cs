// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Access.Domain.Entities;

/// <summary>
/// 角色自定义数据范围的一行（平台库实体，随角色分侧）
/// </summary>
/// <remarks>
/// <see cref="DataScope.Custom"/> 的角色能看到的部门
/// 保存时整体替换；角色删除时连同移除
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
