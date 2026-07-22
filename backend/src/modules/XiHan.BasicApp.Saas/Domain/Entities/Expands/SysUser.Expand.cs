// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
