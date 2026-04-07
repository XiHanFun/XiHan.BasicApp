#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantPermissions
// Guid:E5F6A7B8-C9D0-4123-E456-789ABCDEF012
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/06 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Constants.Permissions;

/// <summary>
/// 租户模块权限编码
/// </summary>
public static class TenantPermissions
{
    /// <summary>
    /// 租户查看权限
    /// </summary>
    public const string View = "system:tenant:view";

    /// <summary>
    /// 租户创建权限
    /// </summary>
    public const string Create = "system:tenant:create";

    /// <summary>
    /// 租户编辑权限
    /// </summary>
    public const string Edit = "system:tenant:edit";

    /// <summary>
    /// 租户删除权限
    /// </summary>
    public const string Delete = "system:tenant:delete";

    /// <summary>
    /// 租户配置权限
    /// </summary>
    public const string Config = "system:tenant:config";
}
