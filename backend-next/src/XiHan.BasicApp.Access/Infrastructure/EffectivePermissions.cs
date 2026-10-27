// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Services;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Security.Abstractions;
using XiHan.Framework.Security.Abstractions.Permissions;

namespace XiHan.BasicApp.Access.Infrastructure;

/// <summary>
/// 当前主体的有效权限：用户本身的授予 + 令牌里每个角色的授予（经框架 <see cref="IPermissionGrantStore"/>，带缓存），通配 <c>*</c> 展开成本侧目录的全部权限码，
/// 再按作用侧裁掉不属于本侧的码。与框架 <c>PermissionChecker</c> 的判定同源。
/// </summary>
/// <param name="store">框架授予存储。</param>
/// <param name="catalog">权限目录。</param>
/// <param name="options">安全选项（角色声明名）。</param>
public sealed class EffectivePermissions(IPermissionGrantStore store, PermissionCatalog catalog, SecurityOptions options)
{
    /// <summary>
    /// 解析为按权限码排序的集合。
    /// </summary>
    public async Task<IReadOnlySet<string>> ResolveAsync(SecurityContext security, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(security);
        var tenant = security.Tenant?.Id;
        var grants = new HashSet<string>(await store.GetGrantsAsync(GrantSubject.ForUser(security.Principal.Id), tenant, cancellationToken).ConfigureAwait(false), StringComparer.Ordinal);
        foreach (var role in security.Principal.ClaimValues(options.RoleClaim))
        {
            grants.UnionWith(await store.GetGrantsAsync(GrantSubject.ForRole(role), tenant, cancellationToken).ConfigureAwait(false));
        }

        var side = catalog.ForSide(platformSide: tenant is null);
        var all = grants.Contains(PermissionGrant.All);
        return side.Where(p => all || grants.Contains(p.Code)).Select(static p => p.Code).ToHashSet(StringComparer.Ordinal);
    }
}
