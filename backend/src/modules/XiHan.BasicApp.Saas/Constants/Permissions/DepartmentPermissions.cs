#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DepartmentPermissions
// Guid:D4E5F6A7-B8C9-4012-D345-6789ABCDEF01
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/06 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Constants.Permissions;

/// <summary>
/// 部门模块权限编码
/// </summary>
public static class DepartmentPermissions
{
    /// <summary>
    /// 部门查看权限
    /// </summary>
    public const string View = "system:department:view";

    /// <summary>
    /// 部门创建权限
    /// </summary>
    public const string Create = "system:department:create";

    /// <summary>
    /// 部门编辑权限
    /// </summary>
    public const string Edit = "system:department:edit";

    /// <summary>
    /// 部门删除权限
    /// </summary>
    public const string Delete = "system:department:delete";
}
