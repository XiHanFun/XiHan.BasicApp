#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysRolePermission.pl
// Guid:1d28152c-d6e9-4396-addb-b479254bad21
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/8/14 3:40:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 角色权限关联实体扩展
/// </summary>
public partial class SysRolePermission
{
    /// <summary>
    /// 角色信息
    /// </summary>
    [Navigate(NavigateType.ManyToOne, nameof(RoleId))]
    public virtual SysRole? Role { get; set; }

    /// <summary>
    /// 权限信息
    /// </summary>
    [Navigate(NavigateType.ManyToOne, nameof(PermissionId))]
    public virtual SysPermission? Permission { get; set; }
}
