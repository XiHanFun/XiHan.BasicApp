// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Security.Abstractions.Permissions;

namespace XiHan.BasicApp.Access.Infrastructure;

/// <summary>
/// 把 Access 的授予规则接进框架的权限判定。
/// </summary>
/// <remarks>
/// ADR-0039 的 <see cref="IPermissionFilter"/>，单例。
/// 框架的授予命中之后，按本包的合并次序重新判一次——
/// 用户与角色的 Deny、授予与指派的有效期、令牌里已不算数的角色都在这里拿掉。与 <see cref="EffectivePermissions"/> 同源（<see cref="GrantEvaluator"/>）。
/// 不是本应用用户的主体（外部签发的令牌）不收窄。
/// </remarks>
/// <param name="evaluator">授予全貌。</param>
public sealed class AccessPermissionFilter(GrantEvaluator evaluator) : IPermissionFilter
{
    /// <inheritdoc />
    public async ValueTask<bool> AllowsAsync(SecurityContext security, string permission, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(security);
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);
        var view = await evaluator.ViewAsync(security, cancellationToken).ConfigureAwait(false);
        return view is null || view.Allows(permission);
    }
}
