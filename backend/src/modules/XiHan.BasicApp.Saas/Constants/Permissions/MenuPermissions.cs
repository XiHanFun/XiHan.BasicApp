#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MenuPermissions
// Guid:C3D4E5F6-A7B8-4901-C234-56789ABCDEF0
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/06 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Constants.Permissions;

/// <summary>
/// 菜单模块权限编码
/// </summary>
public static class MenuPermissions
{
    /// <summary>
    /// 菜单查看权限
    /// </summary>
    public const string View = "system:menu:view";

    /// <summary>
    /// 菜单创建权限
    /// </summary>
    public const string Create = "system:menu:create";

    /// <summary>
    /// 菜单编辑权限
    /// </summary>
    public const string Edit = "system:menu:edit";

    /// <summary>
    /// 菜单删除权限
    /// </summary>
    public const string Delete = "system:menu:delete";
}
