// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Services;
using XiHan.BasicApp.Contracts.Security;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Primitives;
using XiHan.Framework.Security.Abstractions;
using XiHan.Framework.Security.Abstractions.Permissions;

namespace XiHan.BasicApp.Access.Infrastructure;

/// <summary>
/// 当前主体的有效权限
/// </summary>
/// <remarks>
/// 本侧目录里 <see cref="GrantEvaluator"/> 判定持有的权限码（用户与角色的授予、Deny、有效期、此刻算数的角色，与权限过滤器同源），
/// 租户侧再与套餐白名单求交（<see cref="IEditionGate"/>，租户管理员的 <c>*</c> 也被收窄）。主体不是本应用用户时退回授予之并（通配展开）
/// </remarks>
/// <param name="store">框架授予存储</param>
/// <param name="catalog">权限目录</param>
/// <param name="options">安全选项（角色声明名）</param>
/// <param name="gates">套餐门控（Tenancy 实现；没有即不门控）</param>
/// <param name="evaluator">授予全貌</param>
public sealed class EffectivePermissions(IPermissionGrantStore store, PermissionCatalog catalog, SecurityOptions options, IEnumerable<IEditionGate> gates, GrantEvaluator evaluator)
{
    /// <summary>
    /// 解析为权限码集合
    /// </summary>
    public async Task<Result<IReadOnlySet<string>>> ResolveAsync(SecurityContext security, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(security);
        var tenant = security.Tenant?.Id;
        var side = catalog.ForSide(platformSide: tenant is null).Where(p => !ImpersonationLimits.Denies(security, p.Code)).ToList();
        var viewed = await evaluator.ViewAsync(security, cancellationToken).ConfigureAwait(false);
        if (!viewed.TryGetValue(out var found))
        {
            return viewed.Error!;
        }

        if (found.TryGetValue(out var view))
        {
            var held = side.Where(p => view.Allows(p.Code)).Select(static p => p.Code).ToHashSet(StringComparer.Ordinal);
            return Result.Success<IReadOnlySet<string>>(tenant is null ? held : await NarrowAsync(held, tenant, cancellationToken).ConfigureAwait(false));
        }

        // 授予存储读不到时把失败原样返回，不当作没有授予
        var own = await store.GetGrantsAsync(GrantSubject.ForUser(security.Principal.Id), tenant, cancellationToken).ConfigureAwait(false);
        if (!own.TryGetValue(out var granted))
        {
            return own.Error!;
        }

        var grants = new HashSet<string>(granted, StringComparer.Ordinal);
        foreach (var role in security.Principal.ClaimValues(options.RoleClaim))
        {
            var roleGrants = await store.GetGrantsAsync(GrantSubject.ForRole(role), tenant, cancellationToken).ConfigureAwait(false);
            if (!roleGrants.TryGetValue(out var inherited))
            {
                return roleGrants.Error!;
            }

            grants.UnionWith(inherited);
        }

        var all = grants.Contains(PermissionGrant.All);
        var effective = side.Where(p => all || grants.Contains(p.Code)).Select(static p => p.Code).ToHashSet(StringComparer.Ordinal);
        return Result.Success<IReadOnlySet<string>>(tenant is null ? effective : await NarrowAsync(effective, tenant, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// 某一侧可授予的权限目录
    /// </summary>
    /// <remarks>
    /// 平台侧是 Platform / Both，租户侧是 Tenant / Both；调用方在租户里时再按该租户的套餐白名单收窄（授予了也不会生效的不列出）
    /// </remarks>
    /// <param name="platformSide">是否平台侧</param>
    /// <param name="tenantId">调用方所在租户；在平台上下文时为空，不收窄</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task<IReadOnlyList<PermissionDescriptor>> CatalogAsync(bool platformSide, string? tenantId, CancellationToken cancellationToken)
    {
        var items = catalog.ForSide(platformSide);
        if (tenantId is null)
        {
            return items;
        }

        var allowed = await NarrowAsync(items.Select(static p => p.Code).ToHashSet(StringComparer.Ordinal), tenantId, cancellationToken).ConfigureAwait(false);
        return items.Where(p => allowed.Contains(p.Code)).ToList();
    }

    /// <summary>
    /// 按租户的套餐白名单收窄一组权限码（平台侧不调用）
    /// </summary>
    public async Task<HashSet<string>> NarrowAsync(HashSet<string> codes, string tenantId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(codes);
        foreach (var gate in gates)
        {
            if (await gate.AllowedAsync(tenantId, cancellationToken).ConfigureAwait(false) is { } allowed)
            {
                codes.IntersectWith(allowed);
            }
        }

        return codes;
    }
}
