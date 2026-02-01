#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysRolePermission
// Guid:8c28152c-d6e9-4396-addb-b479254bad12
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 02:55:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;

using XiHan.BasicApp.Rbac.Entities.Base;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统角色权限关联实体
/// </summary>
[SugarTable("Sys_Role_Permission", "系统角色权限关联表")]
[SugarIndex("UX_SysRolePermission_Role_Permission", nameof(RoleId), OrderByType.Asc, nameof(PermissionId), OrderByType.Asc, true)]
[SugarIndex("IX_SysRolePermission_RoleId", nameof(RoleId), OrderByType.Asc)]
[SugarIndex("IX_SysRolePermission_PermissionId", nameof(PermissionId), OrderByType.Asc)]
[SugarIndex("IX_SysRolePermission_Status", nameof(Status), OrderByType.Asc)]
public partial class SysRolePermission : RbacCreationEntity<long>
{
    /// <summary>
    /// 角色ID
    /// </summary>
    [SugarColumn(ColumnDescription = "角色ID", IsNullable = false)]
    public virtual long RoleId { get; set; }

    /// <summary>
    /// 权限ID
    /// </summary>
    [SugarColumn(ColumnDescription = "权限ID", IsNullable = false)]
    public virtual long PermissionId { get; set; }

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
