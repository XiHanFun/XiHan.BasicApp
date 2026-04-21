#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RoleBasicConstants
// Guid:4b8f2a3c-9d5e-4f6b-ac7d-2e3f4a5b6c7d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/06 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Constants.Basic;

/// <summary>
/// 角色基础常量
/// </summary>
public static class RoleBasicConstants
{
    public const long PlatformTenantId = 0;

    /// <summary>
    /// 超级管理员角色编码
    /// </summary>
    public const string SuperAdminRoleCode = "super_admin";

    /// <summary>
    /// 管理员角色编码
    /// </summary>
    public const string PlatformAdminRoleCode = "platform_admin";

    /// <summary>
    /// 租户管理员模板角色编码
    /// </summary>
    public const string TenantAdminRoleCode = "tenant_admin";

    /// <summary>
    /// 租户成员模板角色编码
    /// </summary>
    public const string TenantMemberRoleCode = "tenant_member";

    /// <summary>
    /// 租户只读模板角色编码
    /// </summary>
    public const string TenantViewerRoleCode = "tenant_viewer";
}
