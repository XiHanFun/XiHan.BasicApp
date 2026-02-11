#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RbacConstants
// Guid:4b2b3c4d-5e6f-7890-abcd-ef12345678a9
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/10/31 05:35:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Constants;

/// <summary>
/// RBAC 常量
/// </summary>
public static class RbacConstants
{
    /// <summary>
    /// 默认密码
    /// </summary>
    public const string DefaultPassword = "123456";

    /// <summary>
    /// 超级管理员角色编码
    /// </summary>
    public const string SuperAdminRoleCode = "super_admin";

    /// <summary>
    /// 管理员角色编码
    /// </summary>
    public const string AdminRoleCode = "admin";

    /// <summary>
    /// 普通用户角色编码
    /// </summary>
    public const string UserRoleCode = "user";

    /// <summary>
    /// 游客角色编码
    /// </summary>
    public const string GuestRoleCode = "guest";

    /// <summary>
    /// 系统用户名
    /// </summary>
    public const string SystemUserName = "system";

    /// <summary>
    /// 匿名用户名
    /// </summary>
    public const string AnonymousUserName = "anonymous";

    /// <summary>
    /// 默认租户编码
    /// </summary>
    public const string DefaultTenantCode = "default";
}
