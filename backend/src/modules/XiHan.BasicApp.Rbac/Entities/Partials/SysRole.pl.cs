#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysRole.pl
// Guid:cc28152c-d6e9-4396-addb-b479254bad16
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/8/14 3:15:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统角色实体扩展
/// </summary>
public partial class SysRole
{
    /// <summary>
    /// 用户角色关联列表
    /// </summary>
    [Navigate(NavigateType.OneToMany, nameof(SysUserRole.RoleId))]
    public virtual List<SysUserRole>? UserRoles { get; set; }

    /// <summary>
    /// 角色权限关联列表
    /// </summary>
    [Navigate(NavigateType.OneToMany, nameof(SysRolePermission.RoleId))]
    public virtual List<SysRolePermission>? RolePermissions { get; set; }

    /// <summary>
    /// 角色菜单关联列表
    /// </summary>
    [Navigate(NavigateType.OneToMany, nameof(SysRoleMenu.RoleId))]
    public virtual List<SysRoleMenu>? RoleMenus { get; set; }

    /// <summary>
    /// 用户列表
    /// </summary>
    [Navigate(typeof(SysUserRole), nameof(SysUserRole.RoleId), nameof(SysUserRole.UserId))]
    public virtual List<SysUser>? Users { get; set; }

    /// <summary>
    /// 权限列表
    /// </summary>
    [Navigate(typeof(SysRolePermission), nameof(SysRolePermission.RoleId), nameof(SysRolePermission.PermissionId))]
    public virtual List<SysPermission>? Permissions { get; set; }

    /// <summary>
    /// 菜单列表
    /// </summary>
    [Navigate(typeof(SysRoleMenu), nameof(SysRoleMenu.RoleId), nameof(SysRoleMenu.MenuId))]
    public virtual List<SysMenu>? Menus { get; set; }
}
