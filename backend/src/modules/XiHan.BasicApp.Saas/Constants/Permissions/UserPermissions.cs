#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserPermissions
// Guid:A1B2C3D4-E5F6-4789-A012-3456789ABCDE
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/06 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Constants.Permissions;

/// <summary>
/// 用户模块权限编码
/// </summary>
public static class UserPermissions
{
    /// <summary>
    /// 用户查看权限
    /// </summary>
    public const string View = "system:user:view";

    /// <summary>
    /// 用户创建权限
    /// </summary>
    public const string Create = "system:user:create";

    /// <summary>
    /// 用户编辑权限
    /// </summary>
    public const string Edit = "system:user:edit";

    /// <summary>
    /// 用户删除权限
    /// </summary>
    public const string Delete = "system:user:delete";

    /// <summary>
    /// 用户导出权限
    /// </summary>
    public const string Export = "system:user:export";

    /// <summary>
    /// 用户导入权限
    /// </summary>
    public const string Import = "system:user:import";

    /// <summary>
    /// 用户重置密码权限
    /// </summary>
    public const string ResetPassword = "system:user:resetPassword";
}
