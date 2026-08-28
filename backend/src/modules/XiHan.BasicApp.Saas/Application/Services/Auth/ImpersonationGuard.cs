// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.Security.Extensions;
using XiHan.Framework.Security.Users;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 模仿态守卫：供未挂权限码的自助端点在方法体内显式拦截。
/// </summary>
/// <remarks>
/// 带 <c>[PermissionAuthorize]</c> 的端点由 <c>SaasPermissionChecker</c> 按
/// <see cref="ImpersonationDefaults.DeniedPermissionCodes"/> 统一短路，无需再调本守卫。
/// </remarks>
public static class ImpersonationGuard
{
    /// <summary>
    /// 处于模仿态时抛出禁止异常。
    /// </summary>
    /// <param name="currentUser">当前用户。</param>
    /// <param name="action">被拒操作的名称，用于提示文案。</param>
    public static void EnsureNotImpersonating(this ICurrentUser currentUser, string action)
    {
        ArgumentNullException.ThrowIfNull(currentUser);

        if (currentUser.IsImpersonating())
        {
            throw new UserFriendlyException($"模仿登录状态下不允许{action}。");
        }
    }
}
