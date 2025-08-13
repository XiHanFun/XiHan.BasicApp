#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysUserDepartment
// Guid:ac28152c-d6e9-4396-addb-b479254bad14
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/8/14 3:05:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Data.SqlSugar.Entities;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 用户部门关联实体
/// </summary>
[SugarTable("sys_user_department", "用户部门关联表")]
public partial class SysUserDepartment : SugarEntityWithAudit<long>
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
