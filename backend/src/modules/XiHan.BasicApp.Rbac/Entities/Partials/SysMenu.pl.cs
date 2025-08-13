#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysMenu.pl
// Guid:dc28152c-d6e9-4396-addb-b479254bad17
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/8/14 3:20:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统菜单实体扩展
/// </summary>
public partial class SysMenu
{
    /// <summary>
    /// 父级菜单
    /// </summary>
    [Navigate(NavigateType.OneToOne, nameof(ParentId))]
    public virtual SysMenu? ParentMenu { get; set; }

    /// <summary>
    /// 子菜单列表
    /// </summary>
    [Navigate(NavigateType.OneToMany, nameof(ParentId))]
    public virtual List<SysMenu>? Children { get; set; }

    /// <summary>
    /// 角色菜单关联列表
    /// </summary>
    [Navigate(NavigateType.OneToMany, nameof(SysRoleMenu.MenuId))]
    public virtual List<SysRoleMenu>? RoleMenus { get; set; }

    /// <summary>
    /// 角色列表
    /// </summary>
    [Navigate(typeof(SysRoleMenu), nameof(SysRoleMenu.MenuId), nameof(SysRoleMenu.RoleId))]
    public virtual List<SysRole>? Roles { get; set; }
}
