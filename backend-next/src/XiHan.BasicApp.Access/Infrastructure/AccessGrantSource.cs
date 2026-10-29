// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Security.Abstractions.Permissions;

namespace XiHan.BasicApp.Access.Infrastructure;

/// <summary>
/// 角色继承与委托接进框架的权限判定（ADR-0045 的 <see cref="IPermissionGrantSource"/>，单例）：给出上级角色与委托带来的授予，
/// 让框架在存储授予没命中时也去问 <see cref="AccessPermissionFilter"/>；精确结论（Deny、有效期、链上的拒绝）仍由过滤器按 <see cref="GrantEvaluator"/> 判定。
/// 不是本应用用户的主体（外部签发的令牌）没有额外授予。
/// </summary>
/// <param name="evaluator">授予全貌。</param>
public sealed class AccessGrantSource(GrantEvaluator evaluator) : IPermissionGrantSource
{
    /// <inheritdoc />
    public async ValueTask<IReadOnlyCollection<string>> GetGrantsAsync(SecurityContext security, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(security);
        var view = await evaluator.ViewAsync(security, cancellationToken).ConfigureAwait(false);
        return view is null ? [] : view.ExtraGrants();
    }
}
