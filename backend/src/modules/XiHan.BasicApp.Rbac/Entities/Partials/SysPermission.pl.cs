#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysPermission.pl
// Guid:fc28152c-d6e9-4396-addb-b479254bad19
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/8/14 3:30:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统权限实体扩展
/// </summary>
public partial class SysPermission
{
    /// <summary>
    /// 关联的资源
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [Navigate(NavigateType.OneToOne, nameof(ResourceId))]
    public virtual SysResource? Resource { get; set; }

    /// <summary>
    /// 关联的操作
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [Navigate(NavigateType.OneToOne, nameof(OperationId))]
    public virtual SysOperation? Operation { get; set; }

    /// <summary>
    /// 角色权限关联列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [Navigate(NavigateType.OneToMany, nameof(SysRolePermission.PermissionId))]
    public virtual List<SysRolePermission>? RolePermissions { get; set; }

    /// <summary>
    /// 角色列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [Navigate(typeof(SysRolePermission), nameof(SysRolePermission.PermissionId), nameof(SysRolePermission.RoleId))]
    public virtual List<SysRole>? Roles { get; set; }

    /// <summary>
    /// 用户权限关联列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [Navigate(NavigateType.OneToMany, nameof(SysUserPermission.PermissionId))]
    public virtual List<SysUserPermission>? UserPermissions { get; set; }

    /// <summary>
    /// 用户列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [Navigate(typeof(SysUserPermission), nameof(SysUserPermission.PermissionId), nameof(SysUserPermission.UserId))]
    public virtual List<SysUser>? Users { get; set; }
}
