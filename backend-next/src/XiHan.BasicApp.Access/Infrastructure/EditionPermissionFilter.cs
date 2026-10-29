// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Security;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Security.Abstractions.Permissions;

namespace XiHan.BasicApp.Access.Infrastructure;

/// <summary>
/// 把套餐门控接进框架的权限判定。
/// </summary>
/// <remarks>
/// ADR-0039 的 <see cref="IPermissionFilter"/>，单例。
/// 授予命中之后，租户主体的权限码还须在其套餐白名单里，否则视为不持有（<c>SECURITY_DENIED_004</c>）。
/// 平台主体不门控；租户未绑定套餐（门控返回 <see langword="null"/>）不受限；没有任何 <see cref="IEditionGate"/> 时一律放行。与 <see cref="EffectivePermissions"/> 的收窄同源。
/// </remarks>
/// <param name="gates">套餐门控（Tenancy 实现，单例）。</param>
public sealed class EditionPermissionFilter(IEnumerable<IEditionGate> gates) : IPermissionFilter
{
    private readonly IEditionGate[] _gates = gates?.ToArray() ?? throw new ArgumentNullException(nameof(gates));

    /// <inheritdoc />
    public async ValueTask<bool> AllowsAsync(SecurityContext security, string permission, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(security);
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);
        if (security.Tenant?.Id is not { } tenant)
        {
            return true;
        }

        foreach (var gate in _gates)
        {
            if (await gate.AllowedAsync(tenant, cancellationToken).ConfigureAwait(false) is { } allowed && !allowed.Contains(permission))
            {
                return false;
            }
        }

        return true;
    }
}
