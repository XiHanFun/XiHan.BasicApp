// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Access.Domain.Entities;

/// <summary>
/// 用户级数据范围覆盖（平台库实体，只在租户侧）
/// </summary>
/// <remarks>
/// 有这一行时完全取代该用户所有角色的数据范围（不取并集），没有即按角色
/// <see cref="DataScope.Custom"/> 的部门在 <see cref="SysUserDataScopeDepartment"/>
/// </remarks>
[SugarTable(TableName = "sys_user_data_scope", TableDescription = "用户级数据范围覆盖（平台库实体，只在租户侧）")]
[SugarIndex("ux_sys_user_data_scope_owner_tenant_id_user_id", nameof(OwnerTenantId), OrderByType.Asc, nameof(UserId), OrderByType.Asc, true)]
public sealed class SysUserDataScope : AppRecord, IOwnedByTenant
{
    /// <inheritdoc />
    [SugarColumn(ColumnName = "owner_tenant_id", Length = 512, IsNullable = false, ColumnDescription = "属主租户 ID")]
    public string OwnerTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 用户
    /// </summary>
    [SugarColumn(ColumnName = "user_id", IsNullable = false, ColumnDescription = "用户")]
    public Guid UserId { get; set; }

    /// <summary>
    /// 覆盖后的数据范围
    /// </summary>
    [SugarColumn(ColumnName = "data_scope", IsNullable = false, ColumnDescription = "覆盖后的数据范围")]
    public DataScope DataScope { get; set; }
}

/// <summary>
/// 用户级自定义数据范围的一个部门（平台库实体，随覆盖整体替换）
/// </summary>
[SugarTable(TableName = "sys_user_data_scope_department", TableDescription = "用户级自定义数据范围的一个部门（平台库实体，随覆盖整体替换）")]
[SugarIndex("ux_sys_user_data_scope_department_owner_user_department", nameof(OwnerTenantId), OrderByType.Asc, nameof(UserId), OrderByType.Asc, nameof(DepartmentId), OrderByType.Asc, true)]
public sealed class SysUserDataScopeDepartment : AppRecord, IOwnedByTenant
{
    /// <inheritdoc />
    [SugarColumn(ColumnName = "owner_tenant_id", Length = 512, IsNullable = false, ColumnDescription = "属主租户 ID")]
    public string OwnerTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 用户
    /// </summary>
    [SugarColumn(ColumnName = "user_id", IsNullable = false, ColumnDescription = "用户")]
    public Guid UserId { get; set; }

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
