#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
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
    RoleGrantPermission = 0,

    /// <summary>
    /// 角色撤销权限
    /// </summary>
    RoleRevokePermission = 1,

    /// <summary>
    /// 用户直授权限
    /// </summary>
    UserGrantPermission = 2,

    /// <summary>
    /// 用户撤销直授权限
    /// </summary>
    UserRevokePermission = 3,

    /// <summary>
    /// 用户分配角色
    /// </summary>
    UserAssignRole = 4,

    /// <summary>
    /// 用户移除角色
    /// </summary>
    UserRemoveRole = 5,

    /// <summary>
    /// 用户直授权限拒绝
    /// </summary>
    UserDenyPermission = 6,

    /// <summary>
    /// 角色权限拒绝
    /// </summary>
    RoleDenyPermission = 7
}

