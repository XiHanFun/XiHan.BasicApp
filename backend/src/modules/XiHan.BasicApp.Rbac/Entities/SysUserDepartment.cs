#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysUserDepartment
// Guid:ac28152c-d6e9-4396-addb-b479254bad14
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 03:05:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities.Base;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统用户部门关联实体
/// </summary>
[SugarTable("Sys_User_Department", "系统用户部门关联表")]
[SugarIndex("UX_SysUserDepartment_User_Department", nameof(UserId), OrderByType.Asc, nameof(DepartmentId), OrderByType.Asc, true)]
[SugarIndex("IX_SysUserDepartment_UserId", nameof(UserId), OrderByType.Asc)]
[SugarIndex("IX_SysUserDepartment_DepartmentId", nameof(DepartmentId), OrderByType.Asc)]
[SugarIndex("IX_SysUserDepartment_IsMain", nameof(IsMain), OrderByType.Asc)]
[SugarIndex("IX_SysUserDepartment_Status", nameof(Status), OrderByType.Asc)]
public partial class SysUserDepartment : RbacCreationEntity<long>
{
    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(ColumnDescription = "用户ID", IsNullable = false)]
    public virtual long UserId { get; set; }

    /// <summary>
    /// 部门ID
    /// </summary>
    [SugarColumn(ColumnDescription = "部门ID", IsNullable = false)]
    public virtual long DepartmentId { get; set; }

    /// <summary>
    /// 是否主部门
    /// </summary>
    [SugarColumn(ColumnDescription = "是否主部门")]
    public virtual bool IsMain { get; set; } = false;

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
