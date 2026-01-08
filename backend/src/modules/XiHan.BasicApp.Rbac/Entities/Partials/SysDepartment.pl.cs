#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysDepartment.pl
// Guid:ec28152c-d6e9-4396-addb-b479254bad18
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/8/14 3:25:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统部门实体扩展
/// </summary>
public partial class SysDepartment
{
    /// <summary>
    /// 父级部门
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [Navigate(NavigateType.OneToOne, nameof(ParentId))]
    public virtual SysDepartment? ParentDepartment { get; set; }

    /// <summary>
    /// 子部门列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [Navigate(NavigateType.OneToMany, nameof(ParentId))]
    public virtual List<SysDepartment>? Children { get; set; }

    /// <summary>
    /// 负责人
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [Navigate(NavigateType.OneToOne, nameof(LeaderId))]
    public virtual SysUser? Leader { get; set; }

    /// <summary>
    /// 用户部门关联列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [Navigate(NavigateType.OneToMany, nameof(SysUserDepartment.DepartmentId))]
    public virtual List<SysUserDepartment>? UserDepartments { get; set; }

    /// <summary>
    /// 用户列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [Navigate(typeof(SysUserDepartment), nameof(SysUserDepartment.DepartmentId), nameof(SysUserDepartment.UserId))]
    public virtual List<SysUser>? Users { get; set; }

    /// <summary>
    /// 部门审计日志列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [Navigate(NavigateType.OneToMany, nameof(SysAuditLog.DepartmentId))]
    public virtual List<SysAuditLog>? AuditLogs { get; set; }
}
