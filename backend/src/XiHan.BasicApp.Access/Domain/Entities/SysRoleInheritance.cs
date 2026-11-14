// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Access.Domain.Entities;

/// <summary>
/// 角色继承的一条直接边（平台库实体，随所属侧）
/// </summary>
/// <remarks>
/// <see cref="RoleId"/> 继承 <see cref="ParentId"/> 的授予（方案 5.9）
/// 只存直接上级，祖先按边广度优先展开；停用的上级截断经过它的继承。只在同一侧的角色之间，内置角色不参与；解除即删除
/// </remarks>
[SugarTable(TableName = "sys_role_inheritance", TableDescription = "角色继承的一条直接边（平台库实体，随所属侧）")]
[SugarIndex("ux_sys_role_inheritance_owner_tenant_id_role_id_parent_id", nameof(OwnerTenantId), OrderByType.Asc, nameof(RoleId), OrderByType.Asc, nameof(ParentId), OrderByType.Asc, true)]
public sealed class SysRoleInheritance : AppRecord, IOwnedByTenant
{
    /// <inheritdoc />
    [SugarColumn(ColumnName = "owner_tenant_id", Length = 512, IsNullable = false, ColumnDescription = "属主租户 ID")]
    public string OwnerTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 下级角色（继承者）
    /// </summary>
    [SugarColumn(ColumnName = "role_id", IsNullable = false, ColumnDescription = "下级角色（继承者）")]
    public Guid RoleId { get; set; }

    /// <summary>
    /// 直接上级角色
    /// </summary>
    [SugarColumn(ColumnName = "parent_id", IsNullable = false, ColumnDescription = "直接上级角色")]
    public Guid ParentId { get; set; }
}
