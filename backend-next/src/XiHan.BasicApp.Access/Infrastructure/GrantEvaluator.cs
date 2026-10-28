// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Contracts;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Security.Abstractions;
using XiHan.Framework.Security.Abstractions.Permissions;

namespace XiHan.BasicApp.Access.Infrastructure;

/// <summary>
/// 一个授予来源（用户本身或一个角色）在判定那一刻的授予与规则。
/// </summary>
/// <param name="Grants">框架授予存储里的权限码（可含 <c>*</c>）。</param>
/// <param name="Rules">授予规则（Deny 与有效期）。</param>
public sealed record GrantSource(IReadOnlyCollection<string> Grants, IReadOnlyList<PermissionRuleView> Rules);

/// <summary>
/// 一个主体此刻的授予全貌：用户本身、此刻算数的角色（令牌声明 ∩ 有效指派），以及判定所用的时间。
/// 合并次序沿用 4.x：用户 Deny &gt; 用户 Grant &gt; 角色 Deny（只拿走这个角色自己的授予）&gt; 角色 Grant；不在有效期内的 Grant 与 Deny 都不算数。
/// </summary>
/// <param name="Now">判定时间。</param>
/// <param name="User">用户本身。</param>
/// <param name="Roles">此刻算数的角色与它们的来源。</param>
public sealed record GrantView(DateTimeOffset Now, GrantSource User, IReadOnlyList<(Guid RoleId, GrantSource Source)> Roles)
{
    /// <summary>
    /// 是否持有权限码。
    /// </summary>
    public bool Allows(string permission)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);
        if (Denies(User.Rules, permission))
        {
            return false;
        }

        if (Grants(User, permission))
        {
            return true;
        }

        foreach (var (_, source) in Roles)
        {
            if (!Denies(source.Rules, permission) && Grants(source, permission))
            {
                return true;
            }
        }

        return false;
    }

    private bool Denies(IReadOnlyList<PermissionRuleView> rules, string permission)
    {
        foreach (var rule in rules)
        {
            if (rule.Action == PermissionAction.Deny && (string.Equals(rule.Permission, permission, StringComparison.Ordinal) || string.Equals(rule.Permission, PermissionGrant.All, StringComparison.Ordinal)) && rule.IsEffectiveAt(Now))
            {
                return true;
            }
        }

        return false;
    }

    private bool Grants(GrantSource source, string permission)
        => (source.Grants.Contains(permission) && InWindow(source.Rules, permission)) || (source.Grants.Contains(PermissionGrant.All) && InWindow(source.Rules, PermissionGrant.All));

    // 没有规则行的授予就是普通授予；有 Grant 规则行时看它的有效期
    private bool InWindow(IReadOnlyList<PermissionRuleView> rules, string permission)
    {
        foreach (var rule in rules)
        {
            if (rule.Action == PermissionAction.Grant && string.Equals(rule.Permission, permission, StringComparison.Ordinal))
            {
                return rule.IsEffectiveAt(Now);
            }
        }

        return true;
    }
}

/// <summary>
/// 组装主体此刻的授予全貌（单例）：权限过滤器与 <c>auth.permissions</c> 同源。
/// 授予取框架授予存储（带缓存），规则与指派取 <see cref="PermissionRules"/> 与 <see cref="RoleAssignments"/>；主体 ID 不是本应用用户（外部签发的令牌）时没有全貌，不按本包规则收窄。
/// </summary>
/// <param name="store">框架授予存储。</param>
/// <param name="rules">授予规则。</param>
/// <param name="assignments">角色指派。</param>
/// <param name="options">安全选项（角色声明名）。</param>
/// <param name="clock">时间源。</param>
public sealed class GrantEvaluator(IPermissionGrantStore store, PermissionRules rules, RoleAssignments assignments, SecurityOptions options, TimeProvider clock)
{
    /// <summary>
    /// 组装全貌；不是本应用用户时为 <see langword="null"/>。
    /// </summary>
    public async Task<GrantView?> ViewAsync(SecurityContext security, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(security);
        if (!Guid.TryParse(security.Principal.Id, out var userId))
        {
            return null;
        }

        var tenant = security.Tenant?.Id;
        var owner = tenant ?? TenantIds.Platform;
        var now = clock.GetUtcNow();
        var user = new GrantSource(
            await store.GetGrantsAsync(GrantSubject.ForUser(security.Principal.Id), tenant, cancellationToken).ConfigureAwait(false),
            await rules.OfAsync(owner, GrantSubjectKind.User, userId, cancellationToken).ConfigureAwait(false));
        var holding = (await assignments.OfAsync(owner, userId, cancellationToken).ConfigureAwait(false)).Where(a => a.HoldsAt(now)).Select(static a => a.RoleId).ToHashSet();
        var roles = new List<(Guid, GrantSource)>();
        foreach (var claim in security.Principal.ClaimValues(options.RoleClaim))
        {
            if (!Guid.TryParse(claim, out var roleId) || !holding.Remove(roleId))
            {
                continue;
            }

            roles.Add((roleId, new GrantSource(
                await store.GetGrantsAsync(GrantSubject.ForRole(claim), tenant, cancellationToken).ConfigureAwait(false),
                await rules.OfAsync(owner, GrantSubjectKind.Role, roleId, cancellationToken).ConfigureAwait(false))));
        }

        return new GrantView(now, user, roles);
    }
}
