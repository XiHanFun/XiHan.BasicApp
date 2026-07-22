// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.Permissions;

/// <summary>
/// SaaS 平台级权限划分（单一事实源）
/// </summary>
/// <remarks>
/// 集中维护「平台专属」权限码并界定「可授予租户」的范围，供多处复用，避免各种子复制粘贴造成口径漂移：
/// - 租户版本(Enterprise)白名单排除：全部权限减去 <see cref="PlatformOnlyCodes"/>；
/// - 租户管理员(tenant_admin)授权：仅 Saas 模块自身权限再减去平台专属（见 <see cref="IsTenantGrantable"/>）。
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
    /// 该权限码是否可授予租户：仅 Saas 模块自身权限（以模块前缀界定）且非平台专属。
    /// </summary>
    /// <remarks>
    /// 以 Saas 模块前缀界定，天然排除其它模块的权限——Saas 无需知晓任何外部模块的权限命名。
    /// </remarks>
    /// <param name="code">权限码</param>
    public static bool IsTenantGrantable(string code)
    {
        return code.StartsWith(SaasPermissionCodes.Module + ":", StringComparison.OrdinalIgnoreCase)
            && !PlatformOnlyCodes.Contains(code);
    }
}
