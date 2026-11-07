// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Services;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Primitives;
using XiHan.Framework.Security.Abstractions.Permissions;

namespace XiHan.BasicApp.Access.Infrastructure;

/// <summary>
/// 把 Access 的授予规则接进框架的权限判定
/// </summary>
/// <remarks>
/// ADR-0039 的 <see cref="IPermissionFilter"/>，单例
/// 框架的授予命中之后，按本包的合并次序重新判一次——
/// 用户与角色的 Deny、授予与指派的有效期、令牌里已不算数的角色都在这里拿掉。与 <see cref="EffectivePermissions"/> 同源（<see cref="GrantEvaluator"/>）
/// 作用侧在这里统一拦：租户主体持 <c>*</c> 也调不到只属于平台的权限码，反过来同样（4.x 同）。这一条与模仿态的拒绝清单（<see cref="ImpersonationLimits"/>）对任何主体都生效
/// 不是本应用用户的主体（外部签发的令牌）不做授予上的收窄
/// </remarks>
/// <param name="evaluator">授予全貌</param>
/// <param name="catalog">权限目录</param>
public sealed class AccessPermissionFilter(GrantEvaluator evaluator, PermissionCatalog catalog) : IPermissionFilter
{
    /// <inheritdoc />
    public async ValueTask<Result<bool>> AllowsAsync(SecurityContext security, string permission, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(security);
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);
        if (!catalog.AllowsSide(permission, platformSide: security.Tenant is null) || ImpersonationLimits.Denies(security, permission))
        {
            return false;
        }

        var view = await evaluator.ViewAsync(security, cancellationToken).ConfigureAwait(false);
        return view.Map(found => !found.TryGetValue(out var grants) || grants.Allows(permission));
    }
}
