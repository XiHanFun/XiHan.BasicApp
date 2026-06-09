#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysRolePermission.Enum
// Guid:0c98a8d6-ce4a-426f-8464-817906b616d4
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 权限操作枚举
/// </summary>
/// <remarks>
/// 在 SysRolePermission 中：Deny 仅覆盖当前角色继承链，不影响其他独立角色
/// 在 SysUserPermission 中：Deny 为最终裁决，覆盖所有角色级别的 Grant
/// 详见 SysRolePermission / SysUserPermission 实体注释
/// </remarks>
public enum PermissionAction
{
    /// <summary>
    /// 授予权限
    /// </summary>
    [Description("授予权限")]
    Grant = 0,

    /// <summary>
    /// 拒绝权限
    /// </summary>
    [Description("拒绝权限")]
    Deny = 1
}
