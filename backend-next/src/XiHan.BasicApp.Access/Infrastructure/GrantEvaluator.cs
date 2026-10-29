// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Contracts;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Security.Abstractions;
using XiHan.Framework.Security.Abstractions.Permissions;

namespace XiHan.BasicApp.Access.Infrastructure;

/// <summary>
/// 一个授予来源（用户本身或一个角色）在判定那一刻的授予、规则与条件。
/// </summary>
/// <param name="Kind">主体类别。</param>
/// <param name="SubjectId">主体 ID。</param>
/// <param name="Grants">框架授予存储里的权限码（可含 <c>*</c>）。</param>
/// <param name="Rules">授予规则（Deny 与有效期）。</param>
/// <param name="Conditions">启用的条件子句（按绑定：权限码 × 授予或拒绝）。</param>
public sealed record GrantSource(GrantSubjectKind Kind, Guid SubjectId, IReadOnlyCollection<string> Grants, IReadOnlyList<PermissionRuleView> Rules, IReadOnlyList<ConditionClauseView> Conditions)
{
    /// <summary>
    /// 某一时刻拒绝权限码的绑定（同码或 <c>*</c> 上在有效期内的 Deny），返回绑定的权限码。
    /// </summary>
    public IEnumerable<string> DenyBindings(string permission, DateTimeOffset now)
    {
        foreach (var rule in Rules)
        {
            if (rule.Action == PermissionAction.Deny && Covers(rule.Permission, permission) && rule.IsEffectiveAt(now))
            {
                yield return rule.Permission;
            }
        }
    }

    /// <summary>
    /// 某一时刻授予权限码的绑定（同码或 <c>*</c> 的授予，有 Grant 规则行时还要在它的有效期内），返回绑定的权限码。
    /// </summary>
    public IEnumerable<string> GrantBindings(string permission, DateTimeOffset now)
    {
        foreach (var code in new[] { permission, PermissionGrant.All })
        {
            if (Grants.Contains(code) && InWindow(code, now))
            {
                yield return code;
            }
        }
    }

    /// <summary>
    /// 一个绑定上启用的条件子句；没有即无条件。
    /// </summary>
    public IReadOnlyList<ConditionClauseView> ConditionsOf(string binding, PermissionAction action)
        => Conditions.Count == 0 ? [] : [.. Conditions.Where(c => c.Action == action && string.Equals(c.Permission, binding, StringComparison.Ordinal))];

    /// <summary>
    /// 某一时刻是否拒绝权限码。条件生效时只算无条件的拒绝（有条件的由调用时按请求判定），条件不生效时有条件的拒绝也算。
    /// </summary>
    public bool DeniesAt(string permission, DateTimeOffset now, bool conditionsApply)
        => DenyBindings(permission, now).Any(b => !conditionsApply || ConditionsOf(b, PermissionAction.Deny).Count == 0);

    /// <summary>
    /// 某一时刻是否授予权限码（有条件的授予也算：调用时再按请求判定）。
    /// </summary>
    public bool GrantsAt(string permission, DateTimeOffset now) => GrantBindings(permission, now).Any();

    /// <summary>
    /// 某一时刻这个来源在权限码上有没有挂条件的绑定。
    /// </summary>
    public bool HasConditions(string permission, DateTimeOffset now)
        => Conditions.Count > 0
            && (DenyBindings(permission, now).Any(b => ConditionsOf(b, PermissionAction.Deny).Count > 0)
                || GrantBindings(permission, now).Any(b => ConditionsOf(b, PermissionAction.Grant).Count > 0));

    private static bool Covers(string binding, string permission)
        => string.Equals(binding, permission, StringComparison.Ordinal) || string.Equals(binding, PermissionGrant.All, StringComparison.Ordinal);

    // 没有规则行的授予就是普通授予；有 Grant 规则行时看它的有效期
    private bool InWindow(string permission, DateTimeOffset now)
    {
        foreach (var rule in Rules)
        {
            if (rule.Action == PermissionAction.Grant && string.Equals(rule.Permission, permission, StringComparison.Ordinal))
            {
                return rule.IsEffectiveAt(now);
            }
        }

        return true;
    }
}

/// <summary>
/// 一条角色链：一个角色与它此刻传得到授予的上级（方案 5.9 角色继承，4.x 同）。链上任一角色的 Deny 只拿走这条链，不影响别的角色。
/// </summary>
/// <param name="RoleId">链首的角色（持有或受委托的那个）。</param>
/// <param name="Members">链上的来源：首个是角色本身，其后是上级。</param>
public sealed record RoleChain(Guid RoleId, IReadOnlyList<GrantSource> Members)
{
    /// <summary>
    /// 这条链某一时刻是否给出权限码（有条件的授予算给出；条件生效时有条件的拒绝不算拿走）。
    /// </summary>
    public bool Allows(string permission, DateTimeOffset now, bool conditionsApply)
        => !Members.Any(m => m.DeniesAt(permission, now, conditionsApply)) && Members.Any(m => m.GrantsAt(permission, now));
}

/// <summary>
/// 一个主体此刻的授予全貌：用户本身、此刻算数的角色链（令牌声明 ∩ 有效指派）、受委托的权限码与角色链，以及判定所用的时间。
/// 合并次序沿用 4.x：用户 Deny &gt; 用户 Grant ≈ 委托的权限码 &gt; 角色链 Deny（只拿走本链）&gt; 角色链 Grant；不在有效期内的 Grant 与 Deny 都不算数。
/// 权限条件生效时这里是乐观的结论（有条件的授予算持有、有条件的拒绝不拿走），调用时由 <see cref="ConditionDecider"/> 按请求属性给出精确结论。
/// </summary>
/// <param name="Now">判定时间。</param>
/// <param name="User">用户本身。</param>
/// <param name="Roles">此刻算数的角色链。</param>
/// <param name="Delegated">此刻受委托的权限码。</param>
/// <param name="DelegatedRoles">此刻受委托的角色链。</param>
/// <param name="ConditionsApply">权限条件是否生效（模式不是 Disabled）。</param>
public sealed record GrantView(DateTimeOffset Now, GrantSource User, IReadOnlyList<RoleChain> Roles, IReadOnlySet<string> Delegated, IReadOnlyList<RoleChain> DelegatedRoles, bool ConditionsApply = true)
{
    /// <summary>
    /// 是否持有权限码。
    /// </summary>
    public bool Allows(string permission)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);
        if (User.DeniesAt(permission, Now, ConditionsApply))
        {
            return false;
        }

        return User.GrantsAt(permission, Now)
            || Delegated.Contains(permission)
            || Roles.Any(c => c.Allows(permission, Now, ConditionsApply))
            || DelegatedRoles.Any(c => c.Allows(permission, Now, ConditionsApply));
    }

    /// <summary>
    /// 持有的权限码上有没有要按请求判定的条件（条件不生效时一律没有）。
    /// </summary>
    public bool RequiresConditions(string permission)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);
        return ConditionsApply
            && (User.HasConditions(permission, Now)
                || Roles.Concat(DelegatedRoles).Any(c => c.Members.Any(m => m.HasConditions(permission, Now))));
    }

    /// <summary>
    /// 授予存储之外的授予：上级角色与委托给出的权限码（交给框架的 <see cref="IPermissionGrantSource"/>，命中后仍由 <see cref="Allows"/> 精确判定）。
    /// </summary>
    public IReadOnlyCollection<string> ExtraGrants()
    {
        var extra = new HashSet<string>(Delegated, StringComparer.Ordinal);
        foreach (var chain in Roles)
        {
            foreach (var member in chain.Members.Skip(1))
            {
                extra.UnionWith(member.Grants);
            }
        }

        foreach (var chain in DelegatedRoles)
        {
            foreach (var member in chain.Members)
            {
                extra.UnionWith(member.Grants);
            }
        }

        return extra;
    }
}

/// <summary>
/// 组装主体此刻的授予全貌（单例）：权限过滤器、授予来源与 <c>auth.permissions</c> 同源。
/// 授予取框架授予存储（带缓存），规则、条件、指派、继承图与委托取 <see cref="PermissionRules"/>、<see cref="PermissionConditions"/>、<see cref="RoleAssignments"/>、<see cref="RoleHierarchy"/> 与 <see cref="Delegations"/>；
/// 主体 ID 不是本应用用户（外部签发的令牌）时没有全貌，不按本包规则收窄。
/// </summary>
/// <param name="store">框架授予存储。</param>
/// <param name="rules">授予规则。</param>
/// <param name="assignments">角色指派。</param>
/// <param name="hierarchy">角色继承图。</param>
/// <param name="delegations">委托。</param>
/// <param name="conditions">权限条件。</param>
/// <param name="accessOptions">授权包配置（条件生效模式）。</param>
/// <param name="options">安全选项（角色声明名）。</param>
/// <param name="clock">时间源。</param>
public sealed class GrantEvaluator(IPermissionGrantStore store, PermissionRules rules, RoleAssignments assignments, RoleHierarchy hierarchy, Delegations delegations, PermissionConditions conditions, AccessOptions accessOptions, SecurityOptions options, TimeProvider clock)
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
        var holding = await HoldingAsync(owner, userId, now, cancellationToken).ConfigureAwait(false);

        // 令牌声明 ∩ 有效指派：解除与到期立即生效，令牌里多出来的角色不算数
        var held = new List<Guid>();
        foreach (var claim in security.Principal.ClaimValues(options.RoleClaim))
        {
            if (Guid.TryParse(claim, out var roleId) && holding.Remove(roleId))
            {
                held.Add(roleId);
            }
        }

        return await BuildAsync(owner, tenant, userId, held, now, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 按用户此刻的有效指派组装全貌（不看令牌）：条件试算与影响试算用。
    /// </summary>
    /// <param name="owner">所属侧。</param>
    /// <param name="userId">用户。</param>
    /// <param name="now">判定时刻（试算可以给别的时刻）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task<GrantView> ViewOfAsync(string owner, Guid userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        var tenant = string.Equals(owner, TenantIds.Platform, StringComparison.Ordinal) ? null : owner;
        var holding = await HoldingAsync(owner, userId, now, cancellationToken).ConfigureAwait(false);
        return await BuildAsync(owner, tenant, userId, [.. holding], now, cancellationToken).ConfigureAwait(false);
    }

    private async Task<GrantView> BuildAsync(string owner, string? tenant, Guid userId, List<Guid> held, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var context = new ViewBuilder(store, rules, conditions, owner, tenant, now, await hierarchy.GraphOfAsync(owner, cancellationToken).ConfigureAwait(false));
        var user = await context.SourceAsync(GrantSubjectKind.User, userId, cancellationToken).ConfigureAwait(false);
        var roles = await context.ChainsAsync(held, cancellationToken).ConfigureAwait(false);
        var (delegated, delegatedRoles) = await DelegatedAsync(context, userId, cancellationToken).ConfigureAwait(false);
        return new GrantView(now, user, roles, delegated, delegatedRoles, accessOptions.PermissionConditionMode != PermissionConditionMode.Disabled);
    }

    /// <summary>
    /// 此刻持有的角色（有效指派且角色启用）。
    /// </summary>
    private async Task<HashSet<Guid>> HoldingAsync(string owner, Guid userId, DateTimeOffset now, CancellationToken cancellationToken)
        => (await assignments.OfAsync(owner, userId, cancellationToken).ConfigureAwait(false)).Where(a => a.HoldsAt(now)).Select(static a => a.RoleId).ToHashSet();

    /// <summary>
    /// 此刻算数的委托：在有效期内，且委托人此刻仍持有所委托的权限码（按委托人的指派判定，不再顺着委托人的委托传下去）或角色。
    /// </summary>
    private async Task<(IReadOnlySet<string> Permissions, IReadOnlyList<RoleChain> Roles)> DelegatedAsync(ViewBuilder context, Guid userId, CancellationToken cancellationToken)
    {
        var active = (await delegations.OfAsync(context.Owner, userId, cancellationToken).ConfigureAwait(false)).Where(d => d.IsEffectiveAt(context.Now)).ToList();
        if (active.Count == 0)
        {
            return (new HashSet<string>(StringComparer.Ordinal), []);
        }

        var permissions = new HashSet<string>(StringComparer.Ordinal);
        var roleIds = new List<Guid>();
        foreach (var group in active.GroupBy(static d => d.DelegatorId))
        {
            var holding = await HoldingAsync(context.Owner, group.Key, context.Now, cancellationToken).ConfigureAwait(false);
            GrantView? delegator = null;
            foreach (var delegation in group)
            {
                if (delegation.RoleId is { } roleId)
                {
                    if (holding.Contains(roleId))
                    {
                        roleIds.Add(roleId);
                    }
                }
                else if (delegation.Permission is { } permission)
                {
                    delegator ??= await OwnViewAsync(context, group.Key, holding, cancellationToken).ConfigureAwait(false);
                    if (delegator.Allows(permission))
                    {
                        permissions.Add(permission);
                    }
                }
            }
        }

        return (permissions, await context.ChainsAsync(roleIds.Distinct().ToList(), cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// 用户此刻（按指派，不看令牌、不算委托）是否持有权限码或角色：委托的新建校验与判定共用。
    /// </summary>
    /// <param name="owner">所属侧。</param>
    /// <param name="userId">用户。</param>
    /// <param name="permission">权限码（与 <paramref name="roleId"/> 二选一）。</param>
    /// <param name="roleId">角色。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task<bool> HoldsAsync(string owner, Guid userId, string? permission, Guid? roleId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        var now = clock.GetUtcNow();
        var holding = await HoldingAsync(owner, userId, now, cancellationToken).ConfigureAwait(false);
        if (roleId is { } role)
        {
            return holding.Contains(role);
        }

        if (string.IsNullOrWhiteSpace(permission))
        {
            return false;
        }

        var tenant = string.Equals(owner, TenantIds.Platform, StringComparison.Ordinal) ? null : owner;
        var context = new ViewBuilder(store, rules, conditions, owner, tenant, now, await hierarchy.GraphOfAsync(owner, cancellationToken).ConfigureAwait(false));
        return (await OwnViewAsync(context, userId, holding, cancellationToken).ConfigureAwait(false)).Allows(permission);
    }

    /// <summary>
    /// 用户自己的授予（本身与持有角色的链），不含委托：委托不再顺着委托人的委托传下去。
    /// </summary>
    private static async Task<GrantView> OwnViewAsync(ViewBuilder context, Guid userId, HashSet<Guid> holding, CancellationToken cancellationToken)
        => new(
            context.Now,
            await context.SourceAsync(GrantSubjectKind.User, userId, cancellationToken).ConfigureAwait(false),
            await context.ChainsAsync([.. holding], cancellationToken).ConfigureAwait(false),
            new HashSet<string>(StringComparer.Ordinal),
            []);

    /// <summary>
    /// 一次组装的上下文：同一侧、同一时刻、同一份继承图。
    /// </summary>
    private sealed class ViewBuilder(IPermissionGrantStore store, PermissionRules rules, PermissionConditions conditions, string owner, string? tenant, DateTimeOffset now, RoleGraph graph)
    {
        public string Owner => owner;

        public DateTimeOffset Now => now;

        public async Task<GrantSource> SourceAsync(GrantSubjectKind kind, Guid subjectId, CancellationToken cancellationToken)
        {
            var subject = kind == GrantSubjectKind.User ? GrantSubject.ForUser(subjectId.ToString("D")) : GrantSubject.ForRole(subjectId.ToString("D"));
            return new GrantSource(
                kind,
                subjectId,
                await store.GetGrantsAsync(subject, tenant, cancellationToken).ConfigureAwait(false),
                await rules.OfAsync(owner, kind, subjectId, cancellationToken).ConfigureAwait(false),
                await conditions.OfAsync(owner, kind, subjectId, cancellationToken).ConfigureAwait(false));
        }

        public async Task<IReadOnlyList<RoleChain>> ChainsAsync(List<Guid> roleIds, CancellationToken cancellationToken)
        {
            var chains = new List<RoleChain>(roleIds.Count);
            foreach (var roleId in roleIds)
            {
                var members = new List<GrantSource> { await SourceAsync(GrantSubjectKind.Role, roleId, cancellationToken).ConfigureAwait(false) };
                foreach (var (ancestor, _) in graph.EffectiveAncestorsOf(roleId))
                {
                    members.Add(await SourceAsync(GrantSubjectKind.Role, ancestor, cancellationToken).ConfigureAwait(false));
                }

                chains.Add(new RoleChain(roleId, members));
            }

            return chains;
        }
    }
}
