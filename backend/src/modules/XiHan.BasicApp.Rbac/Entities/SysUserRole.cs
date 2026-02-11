#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysUserRole
// Guid:7c28152c-d6e9-4396-addb-b479254bad11
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 02:50:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities.Base;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统用户角色关联实体
/// </summary>
[SugarTable("Sys_User_Role", "系统用户角色关联表")]
[SugarIndex("UX_SysUserRole_User_Role", nameof(UserId), OrderByType.Asc, nameof(RoleId), OrderByType.Asc, true)]
[SugarIndex("IX_SysUserRole_UserId", nameof(UserId), OrderByType.Asc)]
[SugarIndex("IX_SysUserRole_RoleId", nameof(RoleId), OrderByType.Asc)]
[SugarIndex("IX_SysUserRole_Status", nameof(Status), OrderByType.Asc)]
[SugarIndex("IX_SysUserRole_TenantId", nameof(TenantId), OrderByType.Asc)]
[SugarIndex("IX_SysUserRole_TenantId_Status", nameof(TenantId), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
public partial class SysUserRole : RbacCreationEntity<long>
{
    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(ColumnDescription = "用户ID", IsNullable = false)]
    public virtual long UserId { get; set; }

    /// <summary>
    /// 角色ID
    /// </summary>
    [SugarColumn(ColumnDescription = "角色ID", IsNullable = false)]
    public virtual long RoleId { get; set; }

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
