#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RolePermissions
// Guid:B2C3D4E5-F6A7-4890-B123-456789ABCDEF
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/06 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Constants.Permissions;

/// <summary>
/// 角色模块权限编码
/// </summary>
public static class RolePermissions
{
    /// <summary>
    /// 角色查看权限
    /// </summary>
    public const string View = "system:role:view";

    /// <summary>
    /// 角色创建权限
    /// </summary>
    public const string Create = "system:role:create";

    /// <summary>
    /// 角色编辑权限
    /// </summary>
    public const string Edit = "system:role:edit";

    /// <summary>
    /// 角色删除权限
    /// </summary>
    public const string Delete = "system:role:delete";

    /// <summary>
    /// 角色分配权限
    /// </summary>
    public const string Assign = "system:role:assign";
}
