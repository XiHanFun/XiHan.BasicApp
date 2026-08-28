// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 模仿登录的固定口径：会话时长边界、可发起模仿的成员类型、模仿态禁用的权限码。
/// </summary>
public static class ImpersonationDefaults
{
    /// <summary>
    /// 模仿会话默认存活分钟数（可由配置 <c>saas.auth.impersonation.session-minutes</c> 覆盖）。
    /// </summary>
    public const int DefaultSessionMinutes = 30;

    /// <summary>
    /// 模仿会话存活分钟数下限。
    /// </summary>
    public const int MinSessionMinutes = 1;

    /// <summary>
    /// 模仿会话存活分钟数上限。
    /// </summary>
    public const int MaxSessionMinutes = 480;

    /// <summary>
    /// 模仿事由最大长度。
    /// </summary>
    public const int MaxReasonLength = 200;

    /// <summary>
    /// 可发起模仿的租户成员类型（非超管发起时的必要条件），同时也是不可被非超管模仿的成员类型。
    /// </summary>
    public static IReadOnlySet<TenantMemberType> AdministrativeMemberTypes { get; } =
        new HashSet<TenantMemberType>
        {
            TenantMemberType.Owner,
            TenantMemberType.Admin,
            TenantMemberType.PlatformAdmin
        };

    /// <summary>
    /// 模仿态禁用的权限码：接管账号、变更授权、销毁数据与再次发起模仿。
    /// </summary>
    /// <remarks>
    /// 由 <c>SaasPermissionChecker</c> 在鉴权入口短路，覆盖全部带 <c>[PermissionAuthorize]</c> 的端点；
    /// 未挂权限码的自助端点由 <c>SaasApplicationService.EnsureNotImpersonating</c> 单独把关。
    /// </remarks>
    public static IReadOnlySet<string> DeniedPermissionCodes { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // 接管账号
            SaasPermissionCodes.UserSecurity.ResetPassword,
            SaasPermissionCodes.UserSecurity.ResetTwoFactor,
            SaasPermissionCodes.UserSecurity.Lock,
            SaasPermissionCodes.UserSecurity.LoginPolicy,

            // 变更授权
            SaasPermissionCodes.RolePermission.Grant,
            SaasPermissionCodes.RolePermission.Update,
            SaasPermissionCodes.RolePermission.Status,
            SaasPermissionCodes.RolePermission.Revoke,
            SaasPermissionCodes.UserPermission.Grant,
            SaasPermissionCodes.UserPermission.Update,
            SaasPermissionCodes.UserPermission.Status,
            SaasPermissionCodes.UserPermission.Revoke,
            SaasPermissionCodes.UserRole.Grant,
            SaasPermissionCodes.UserRole.Update,
            SaasPermissionCodes.UserRole.Status,
            SaasPermissionCodes.UserRole.Revoke,
            SaasPermissionCodes.RoleDataScope.Grant,
            SaasPermissionCodes.RoleDataScope.Update,
            SaasPermissionCodes.RoleDataScope.Status,
            SaasPermissionCodes.RoleDataScope.Revoke,
            SaasPermissionCodes.UserDataScope.Grant,
            SaasPermissionCodes.UserDataScope.Update,
            SaasPermissionCodes.UserDataScope.Status,
            SaasPermissionCodes.UserDataScope.Revoke,
            SaasPermissionCodes.RoleHierarchy.Create,
            SaasPermissionCodes.RoleHierarchy.Delete,
            SaasPermissionCodes.PermissionDelegation.Create,
            SaasPermissionCodes.PermissionDelegation.Update,
            SaasPermissionCodes.PermissionDelegation.Status,
            SaasPermissionCodes.PermissionDelegation.Revoke,

            // 销毁数据
            SaasPermissionCodes.User.Delete,
            SaasPermissionCodes.Role.Delete,
            SaasPermissionCodes.Tenant.Delete,
            SaasPermissionCodes.Tenant.InitDb,

            // 外带凭证
            SaasPermissionCodes.OAuthApp.Secret,

            // 再次发起模仿
            SaasPermissionCodes.Impersonation.Start,
            SaasPermissionCodes.Impersonation.CrossTenant
        };

    /// <summary>
    /// 归一模仿会话时长：越界回落到上下限。
    /// </summary>
    /// <param name="minutes">配置的分钟数。</param>
    /// <returns>归一后的时长。</returns>
    public static TimeSpan NormalizeSessionLifetime(int minutes)
    {
        return TimeSpan.FromMinutes(Math.Clamp(minutes, MinSessionMinutes, MaxSessionMinutes));
    }
}
