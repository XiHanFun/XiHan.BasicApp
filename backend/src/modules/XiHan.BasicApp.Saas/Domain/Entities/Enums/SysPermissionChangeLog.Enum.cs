#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright (c)2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysPermissionChangeLog.Enum
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 权限变更类型枚举
/// </summary>
public enum PermissionChangeType
{
    /// <summary>
    /// 角色授予权限
    /// </summary>
    [Description("角色授予权限")]
    RoleGrantPermission = 0,

    /// <summary>
    /// 角色撤销权限
    /// </summary>
    [Description("角色撤销权限")]
    RoleRevokePermission = 1,

    /// <summary>
    /// 用户直授权限
    /// </summary>
    [Description("用户直授权限")]
    UserGrantPermission = 2,

    /// <summary>
    /// 用户撤销直授权限
    /// </summary>
    [Description("用户撤销直授权限")]
    UserRevokePermission = 3,

    /// <summary>
    /// 用户分配角色
    /// </summary>
    [Description("用户分配角色")]
    UserAssignRole = 4,

    /// <summary>
    /// 用户移除角色
    /// </summary>
    [Description("用户移除角色")]
    UserRemoveRole = 5,

    /// <summary>
    /// 用户直授权限拒绝
    /// </summary>
    [Description("用户直授权限拒绝")]
    UserDenyPermission = 6,

    /// <summary>
    /// 角色权限拒绝
    /// </summary>
    [Description("角色权限拒绝")]
    RoleDenyPermission = 7
}
