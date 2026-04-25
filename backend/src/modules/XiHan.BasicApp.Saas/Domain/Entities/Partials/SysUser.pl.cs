#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysUser.pl
// Guid:bc28152c-d6e9-4396-addb-b479254bad15
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 03:10:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统用户实体扩展
/// </summary>
public partial class SysUser
{
    /// <summary>
    /// 用户角色关联列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysUserRole.UserId))]
    public virtual List<SysUserRole>? UserRoles { get; set; }

    /// <summary>
    /// 用户权限关联列表（直授）
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysUserPermission.UserId))]
    public virtual List<SysUserPermission>? UserPermissions { get; set; }

    /// <summary>
    /// 用户安全状态
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToOne, nameof(BasicId), nameof(SysUserSecurity.UserId))]
    public virtual SysUserSecurity? Security { get; set; }

    /// <summary>
    /// 用户部门关联列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysUserDepartment.UserId))]
    public virtual List<SysUserDepartment>? UserDepartments { get; set; }
}
