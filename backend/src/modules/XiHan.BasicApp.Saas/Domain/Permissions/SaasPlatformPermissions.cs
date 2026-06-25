#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasPlatformPermissions
// Guid:2e6b9a14-7c3d-4f08-9a55-8b1c2d3e4f60
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Domain.Permissions;

/// <summary>
/// SaaS 平台级权限划分（单一事实源）
/// </summary>
/// <remarks>
/// 集中维护「平台专属」与「开发工具」两类权限码，供多处复用，避免在各种子里复制粘贴造成口径漂移：
/// - 租户版本(Enterprise)白名单排除：全部权限减去 <see cref="PlatformOnlyCodes"/>；
/// - 租户管理员(tenant_admin)授权：全部权限减去平台专属与开发工具（见 <see cref="IsTenantGrantable"/>）。
/// </remarks>
public static class SaasPlatformPermissions
{
    /// <summary>
    /// 平台专属权限码：仅平台超级管理员可拥有，租户管理员的「全部权限」与企业版白名单均排除之。
    /// </summary>
    public static readonly IReadOnlySet<string> PlatformOnlyCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        SaasPermissionCodes.Tenant.Create,
        SaasPermissionCodes.Tenant.Update,
        SaasPermissionCodes.Tenant.Status,
        SaasPermissionCodes.TenantEdition.Read,
        SaasPermissionCodes.TenantEdition.Create,
        SaasPermissionCodes.TenantEdition.Update,
        SaasPermissionCodes.TenantEdition.Status,
        SaasPermissionCodes.TenantEdition.Default,
        SaasPermissionCodes.TenantEditionPermission.Read,
        SaasPermissionCodes.TenantEditionPermission.Grant,
        SaasPermissionCodes.TenantEditionPermission.Update,
        SaasPermissionCodes.TenantEditionPermission.Revoke,
        SaasPermissionCodes.Resource.Create,
        SaasPermissionCodes.Resource.Update,
        SaasPermissionCodes.Resource.Status,
        SaasPermissionCodes.Resource.Delete,
        SaasPermissionCodes.Operation.Create,
        SaasPermissionCodes.Operation.Update,
        SaasPermissionCodes.Operation.Status,
        SaasPermissionCodes.Operation.Delete,
        SaasPermissionCodes.Menu.Create,
        SaasPermissionCodes.Menu.Update,
        SaasPermissionCodes.Menu.Status,
        SaasPermissionCodes.Menu.Delete,
        SaasPermissionCodes.Cache.Read,
        SaasPermissionCodes.Cache.Clear,
        SaasPermissionCodes.Server.Read
    };

    /// <summary>
    /// 是否开发工具权限（代码生成）：平台级开发功能，仅超级管理员可拥有，租户管理员的「全部权限」亦排除之。
    /// </summary>
    /// <param name="code">权限码</param>
    public static bool IsDevelopmentToolCode(string code)
    {
        return code.StartsWith("code_gen:", StringComparison.OrdinalIgnoreCase)
            || code.StartsWith("code_gen_api:", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 该权限码是否可授予租户（排除平台专属与开发工具）。
    /// </summary>
    /// <param name="code">权限码</param>
    public static bool IsTenantGrantable(string code)
    {
        return !PlatformOnlyCodes.Contains(code) && !IsDevelopmentToolCode(code);
    }
}
