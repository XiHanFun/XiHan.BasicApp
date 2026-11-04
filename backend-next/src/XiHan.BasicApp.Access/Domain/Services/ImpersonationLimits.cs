// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Frozen;
using XiHan.BasicApp.Identity;
using XiHan.BasicApp.Identity.Infrastructure;
using XiHan.Framework.Kernel.Security;

namespace XiHan.BasicApp.Access.Domain.Services;

/// <summary>
/// 模仿态拒绝的权限码
/// </summary>
/// <remarks>
/// 接管账号、变更授权、销毁数据与外带凭证（4.x 同）：模仿是有硬期限的临时身份，不能借它把权限固化下来
/// 在授予命中之后判，所以 <c>*</c> 也拦；再次发起模仿由 <c>impersonation.start</c> 自己拒绝
/// </remarks>
public static class ImpersonationLimits
{
    private static readonly FrozenSet<string> _denied = new[]
    {
        // 接管账号
        IdentityPermissions.UsersResetPassword,
        IdentityPermissions.UsersResetTwoFactor,
        IdentityPermissions.UsersLock,
        IdentityPermissions.UsersLoginPolicy,

        // 变更授权
        AccessPermissions.RolesUpdate,
        AccessPermissions.RolesGrant,
        AccessPermissions.RolesAssign,
        AccessPermissions.RolesSetParents,
        AccessPermissions.UserGrantsGrant,
        AccessPermissions.UserDataScopesUpdate,
        AccessPermissions.DelegationsCreate,
        AccessPermissions.DelegationsRevoke,
        AccessPermissions.DelegationsDelete,

        // 销毁数据
        IdentityPermissions.UsersDelete,
        AccessPermissions.RolesDelete,
        "tenancy.tenants.delete",
        "tenancy.tenants.initialize-database",

        // 外带凭证（重置客户端密钥在这个权限码上）
        "openplatform.oauth-apps.update",
    }.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>
    /// 这次调用是不是模仿态，且权限码在拒绝之列
    /// </summary>
    /// <param name="security">安全上下文</param>
    /// <param name="permission">权限码</param>
    /// <returns>拒绝时为 <see langword="true"/></returns>
    public static bool Denies(SecurityContext security, string permission)
    {
        ArgumentNullException.ThrowIfNull(security);
        return _denied.Contains(permission) && security.Principal.ClaimValues(IdentityClaims.Actor).Any();
    }
}
