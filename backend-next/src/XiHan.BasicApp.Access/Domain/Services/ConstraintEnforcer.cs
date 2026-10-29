// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Infrastructure;
using XiHan.BasicApp.Contracts.Auditing;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;
using XiHan.Framework.Security.Abstractions.Conditions;

namespace XiHan.BasicApp.Access.Domain.Services;

/// <summary>
/// 一次违规：哪条规则、哪一组（按组判定的类型）、涉及哪些目标。
/// </summary>
/// <param name="Rule">规则。</param>
/// <param name="GroupNo">组号；基数与条件类为空。</param>
/// <param name="Targets">涉及的目标（角色 ID、权限码或用户 ID）。</param>
public sealed record ConstraintViolation(ConstraintRuleView Rule, int? GroupNo, IReadOnlyList<string> Targets);

/// <summary>
/// 约束规则的求值（4.x 的 ConstraintRuleEnforcementDomainService，补齐非 SSD 类型）：
/// <list type="bullet">
/// <item>静态职责分离：用户持有的角色（没过期的指派与没过期的角色委托，按继承展开上级）里，同一组超过上限即违规；</item>
/// <item>动态职责分离：会话激活的角色（同样按继承展开）同理；</item>
/// <item>权限互斥：角色或用户直接持有的权限码里，同一组超过上限即违规；</item>
/// <item>基数：规则项里的角色成员数、或规则项里的用户持有的角色数超过上限即违规；</item>
/// <item>条件：指派规则项里的角色时，按条件引擎判定被指派的用户（主体）与发起这次指派的请求（环境），不成立即违规。</item>
/// </list>
/// 只报这次变更带进来的违规（变更之前已有的不拦，4.x 是整组重判）；静态的判定不看角色启停，按结构判定（4.x 同）。
/// 违规按规则的处理方式落地（<see cref="Settle"/>）：拒绝与需审批的拦下变更，警告与只记录的放行并记权限变更日志。
/// </summary>
/// <param name="rules">规则缓存。</param>
/// <param name="hierarchy">继承图（按本次工作单元读，继承刚改过也看得到）。</param>
/// <param name="members">指派仓储。</param>
/// <param name="delegations">委托缓存。</param>
/// <param name="policies">策略求值（条件编译）。</param>
/// <param name="conditions">条件求值器。</param>
/// <param name="options">授权包配置。</param>
/// <param name="services">服务提供器（属性来源查库）。</param>
/// <param name="clock">时间源。</param>
public sealed class ConstraintEnforcer(
    ConstraintRules rules,
    RoleHierarchyService hierarchy,
    IRepository<SysUserRole, Guid> members,
    Delegations delegations,
    AccessPolicyEvaluator policies,
    IConditionEvaluator conditions,
    AccessOptions options,
    IServiceProvider services,
    TimeProvider clock)
{
    /// <summary>
    /// 用户在这一侧持有的角色（静态职责分离与基数用）：没过期的指派（还没生效的也算）与没过期的角色委托。
    /// </summary>
    public async Task<HashSet<Guid>> HeldRolesAsync(string owner, Guid userId, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var rows = await members.ListAsync(EntityQuery.All<SysUserRole>().Where(new Specification<SysUserRole>(m => m.OwnerTenantId == owner && m.UserId == userId)), cancellationToken).ConfigureAwait(false);
        var held = rows.Where(m => m.ExpiresAt is null || m.ExpiresAt > now).Select(static m => m.RoleId).ToHashSet();
        foreach (var delegated in await delegations.OfAsync(owner, userId, cancellationToken).ConfigureAwait(false))
        {
            if (delegated.RoleId is { } roleId && delegated.ExpiresAt > now)
            {
                held.Add(roleId);
            }
        }

        return held;
    }

    /// <summary>
    /// 角色在这一侧没过期的成员数（基数用）。
    /// </summary>
    public async Task<int> MemberCountAsync(string owner, Guid roleId, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var rows = await members.ListAsync(EntityQuery.All<SysUserRole>().Where(new Specification<SysUserRole>(m => m.OwnerTenantId == owner && m.RoleId == roleId)), cancellationToken).ConfigureAwait(false);
        return rows.Where(m => m.ExpiresAt is null || m.ExpiresAt > now).Select(static m => m.UserId).Distinct().Count();
    }

    /// <summary>
    /// 用户要多持有一些角色：静态职责分离、用户的角色数基数与条件。
    /// </summary>
    /// <param name="owner">所属侧。</param>
    /// <param name="userId">用户。</param>
    /// <param name="userName">登录名（条件的主体属性）。</param>
    /// <param name="heldAfter">变更之后持有的角色（含新增的）。</param>
    /// <param name="adding">这次新增的角色。</param>
    /// <param name="context">调用上下文（条件的环境属性取这次请求）。</param>
    public async Task<IReadOnlyList<ConstraintViolation>> CheckUserRolesAsync(string owner, Guid userId, string? userName, IReadOnlyCollection<Guid> heldAfter, IReadOnlyCollection<Guid> adding, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(heldAfter);
        ArgumentNullException.ThrowIfNull(adding);
        ArgumentNullException.ThrowIfNull(context);
        var now = clock.GetUtcNow();
        var effective = await EffectiveAsync(owner, now, context.CancellationToken).ConfigureAwait(false);
        if (effective.Count == 0 || adding.Count == 0)
        {
            return [];
        }

        var violations = new List<ConstraintViolation>();
        var separation = effective.Where(static r => r.Type == ConstraintType.Ssd).ToList();
        if (separation.Count > 0)
        {
            var graph = await hierarchy.GraphOfAsync(owner, context.CancellationToken).ConfigureAwait(false);
            violations.AddRange(GroupViolations(separation, Expand(graph, heldAfter), Expand(graph, adding)));
        }

        var user = userId.ToString("D");
        violations.AddRange(effective
            .Where(r => r.Type == ConstraintType.Cardinality && r.TargetType == ConstraintTargetType.User && heldAfter.Count > r.MaxAllowed && r.Items.Any(i => string.Equals(i.TargetId, user, StringComparison.OrdinalIgnoreCase)))
            .Select(r => new ConstraintViolation(r, null, [user])));

        var conditional = effective.Where(static r => r.Type == ConstraintType.Condition).ToList();
        if (conditional.Count > 0)
        {
            IConditionAttributeSource? source = null;
            foreach (var rule in conditional)
            {
                var targets = adding.Select(static r => r.ToString("D")).Where(r => rule.Items.Any(i => string.Equals(i.TargetId, r, StringComparison.OrdinalIgnoreCase))).ToList();
                if (targets.Count == 0)
                {
                    continue;
                }

                source ??= ConditionAttributeSource.Create(AccessRequests.ForAssignment(context, owner, userId, userName, heldAfter, now), services, options);
                var compiled = policies.Compile(new AccessPolicyView(rule.Id, rule.Code, rule.Name, AccessPolicyAction.LogOnly, 0, null, null, rule.Conditions));
                var met = compiled.Errors.Count == 0 && !compiled.IsEmpty && (await conditions.EvaluateAsync(compiled, source, explain: false, context.CancellationToken).ConfigureAwait(false)).IsMatched;
                if (!met)
                {
                    violations.Add(new ConstraintViolation(rule, null, targets));
                }
            }
        }

        return violations;
    }

    /// <summary>
    /// 角色要多出一些成员：角色的成员数基数。
    /// </summary>
    public async Task<IReadOnlyList<ConstraintViolation>> CheckRoleMembersAsync(string owner, Guid roleId, int membersAfter, CancellationToken cancellationToken)
    {
        var role = roleId.ToString("D");
        return [.. (await EffectiveAsync(owner, clock.GetUtcNow(), cancellationToken).ConfigureAwait(false))
            .Where(r => r.Type == ConstraintType.Cardinality && r.TargetType == ConstraintTargetType.Role && membersAfter > r.MaxAllowed && r.Items.Any(i => string.Equals(i.TargetId, role, StringComparison.OrdinalIgnoreCase)))
            .Select(r => new ConstraintViolation(r, null, [role]))];
    }

    /// <summary>
    /// 角色或用户要多直接持有一些权限码：权限互斥。
    /// </summary>
    public async Task<IReadOnlyList<ConstraintViolation>> CheckPermissionsAsync(string owner, IReadOnlyCollection<string> heldAfter, IReadOnlyCollection<string> adding, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(heldAfter);
        ArgumentNullException.ThrowIfNull(adding);
        var exclusive = (await EffectiveAsync(owner, clock.GetUtcNow(), cancellationToken).ConfigureAwait(false)).Where(static r => r.Type == ConstraintType.MutualExclusion).ToList();
        return exclusive.Count == 0 || adding.Count == 0
            ? []
            : [.. GroupViolations(exclusive, heldAfter.ToHashSet(StringComparer.Ordinal), adding.ToHashSet(StringComparer.Ordinal))];
    }

    /// <summary>
    /// 一批要直接授予的权限码逐个过权限互斥（前面放行的算已持有）：拦截类违规的挑出来，警告与只记录的放行并记日志。
    /// </summary>
    /// <param name="owner">所属侧。</param>
    /// <param name="held">主体此刻直接持有的权限码。</param>
    /// <param name="candidates">要授予的权限码。</param>
    /// <param name="context">调用上下文。</param>
    /// <param name="roleId">主体是角色时的角色。</param>
    /// <param name="userId">主体是用户时的用户。</param>
    /// <returns>被拦下的权限码。</returns>
    public async Task<IReadOnlyList<string>> ScreenPermissionsAsync(string owner, IReadOnlyCollection<string> held, IReadOnlyList<string> candidates, InvocationContext context, Guid? roleId = null, Guid? userId = null)
    {
        ArgumentNullException.ThrowIfNull(held);
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(context);
        var current = held.ToHashSet(StringComparer.Ordinal);
        var blocked = new List<string>();
        foreach (var code in candidates)
        {
            if (current.Contains(code))
            {
                continue;
            }

            var violations = await CheckPermissionsAsync(owner, [.. current, code], [code], context.CancellationToken).ConfigureAwait(false);
            if (Settle(context, violations, roleId, userId) is not null)
            {
                blocked.Add(code);
                continue;
            }

            current.Add(code);
        }

        return blocked;
    }

    /// <summary>
    /// 一组角色的上级（含它们自己）要多出一些上级：静态职责分离（继承变更后，持有这个角色或它的下级的用户）。
    /// </summary>
    /// <param name="owner">所属侧。</param>
    /// <param name="roleId">改了上级的角色。</param>
    /// <param name="addedParents">新加的直接上级。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task<IReadOnlyList<(Guid UserId, ConstraintViolation Violation)>> CheckInheritanceAsync(string owner, Guid roleId, IReadOnlyCollection<Guid> addedParents, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(addedParents);
        var now = clock.GetUtcNow();
        var separation = (await EffectiveAsync(owner, now, cancellationToken).ConfigureAwait(false)).Where(static r => r.Type == ConstraintType.Ssd).ToList();
        if (separation.Count == 0 || addedParents.Count == 0)
        {
            return [];
        }

        var graph = await hierarchy.GraphOfAsync(owner, cancellationToken).ConfigureAwait(false);
        var contributed = Expand(graph, addedParents);
        var affected = graph.DescendantsOf(roleId).Select(static d => d.RoleId).Append(roleId).ToList();
        var holders = (await members.ListAsync(EntityQuery.All<SysUserRole>().Where(new Specification<SysUserRole>(m => m.OwnerTenantId == owner && affected.Contains(m.RoleId))), cancellationToken).ConfigureAwait(false))
            .Where(m => m.ExpiresAt is null || m.ExpiresAt > now)
            .Select(static m => m.UserId)
            .Distinct()
            .ToList();
        var violations = new List<(Guid, ConstraintViolation)>();
        foreach (var userId in holders)
        {
            var held = await HeldRolesAsync(owner, userId, cancellationToken).ConfigureAwait(false);
            violations.AddRange(GroupViolations(separation, Expand(graph, held), contributed).Select(v => (userId, v)));
        }

        return violations;
    }

    /// <summary>
    /// 动态职责分离：按顺序逐个尝试激活，加上它会违反拦截类 DSD 规则的不激活（同一组只留排序最前的）；警告与只记录类的违规照样激活并交回记录。
    /// </summary>
    /// <param name="owner">会话的一侧。</param>
    /// <param name="ordered">候选角色，按优先顺序。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task<(IReadOnlyList<Guid> Activated, IReadOnlyList<(Guid RoleId, ConstraintViolation Violation)> Dropped, IReadOnlyList<ConstraintViolation> Tolerated)> SelectActivationAsync(string owner, IReadOnlyList<Guid> ordered, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ordered);
        var dynamic = (await EffectiveAsync(owner, clock.GetUtcNow(), cancellationToken).ConfigureAwait(false)).Where(static r => r.Type == ConstraintType.Dsd).ToList();
        if (dynamic.Count == 0)
        {
            return (ordered, [], []);
        }

        var graph = await hierarchy.GraphOfAsync(owner, cancellationToken).ConfigureAwait(false);
        var activated = new List<Guid>();
        var dropped = new List<(Guid, ConstraintViolation)>();
        var tolerated = new List<ConstraintViolation>();
        foreach (var roleId in ordered)
        {
            var found = GroupViolations(dynamic, Expand(graph, [.. activated, roleId]), Expand(graph, [roleId])).ToList();
            if (found.FirstOrDefault(static v => v.Rule.IsBlocking) is { } blocking)
            {
                dropped.Add((roleId, blocking));
                continue;
            }

            activated.Add(roleId);
            tolerated.AddRange(found);
        }

        return (activated, dropped, tolerated);
    }

    /// <summary>
    /// 动态职责分离：一个指定的激活集（本人换激活集）。
    /// </summary>
    public async Task<IReadOnlyList<ConstraintViolation>> CheckActivationAsync(string owner, IReadOnlyCollection<Guid> activated, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(activated);
        var dynamic = (await EffectiveAsync(owner, clock.GetUtcNow(), cancellationToken).ConfigureAwait(false)).Where(static r => r.Type == ConstraintType.Dsd).ToList();
        if (dynamic.Count == 0)
        {
            return [];
        }

        var graph = await hierarchy.GraphOfAsync(owner, cancellationToken).ConfigureAwait(false);
        var expanded = Expand(graph, activated);
        return [.. GroupViolations(dynamic, expanded, expanded)];
    }

    /// <summary>
    /// 按处理方式落地：有拦截类的违规即返回错误（这次变更不做，警告也不记）；否则警告与只记录的各记一条权限变更日志。
    /// </summary>
    /// <param name="context">调用上下文。</param>
    /// <param name="violations">违规。</param>
    /// <param name="roleId">变更涉及的角色。</param>
    /// <param name="userId">变更涉及的用户。</param>
    public static Error? Settle(InvocationContext context, IReadOnlyList<ConstraintViolation> violations, Guid? roleId = null, Guid? userId = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(violations);
        if (violations.FirstOrDefault(static v => v.Rule.IsBlocking) is { } blocking)
        {
            return Error.Conflict(AccessErrors.ConstraintViolated, AccessMessages.ConstraintViolated, MessageArguments.Of(("code", blocking.Rule.Code), ("name", blocking.Rule.Name)));
        }

        foreach (var violation in violations)
        {
            Record(context, violation, roleId, userId);
        }

        return null;
    }

    /// <summary>
    /// 记一条违规（警告与只记录类）。
    /// </summary>
    public static void Record(InvocationContext context, ConstraintViolation violation, Guid? roleId = null, Guid? userId = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(violation);
        context.AddAuditLog(new PermissionChangeLogEntry(PermissionChangeType.ConstraintViolation)
        {
            RoleId = roleId,
            TargetUserId = userId,
            PermissionCodes = violation.Rule.TargetType == ConstraintTargetType.Permission ? violation.Targets : [],
            Detail = $"{violation.Rule.Code} {violation.Rule.Type} {violation.Rule.Action}{(violation.GroupNo is { } group ? $" group:{group}" : string.Empty)} targets:{string.Join(',', violation.Targets)}",
        });
    }

    private async Task<IReadOnlyList<ConstraintRuleView>> EffectiveAsync(string owner, DateTimeOffset now, CancellationToken cancellationToken)
        => [.. (await rules.OfAsync(owner, cancellationToken).ConfigureAwait(false)).Where(r => r.IsEffectiveAt(now))];

    // 角色连同它经继承可达的全部上级（不看启停）
    private static HashSet<string> Expand(RoleGraph graph, IEnumerable<Guid> roleIds)
    {
        var expanded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var roleId in roleIds)
        {
            expanded.Add(roleId.ToString("D"));
            foreach (var ancestor in graph.AncestorsOf(roleId))
            {
                expanded.Add(ancestor.RoleId.ToString("D"));
            }
        }

        return expanded;
    }

    // 按组判定：同一组里持有的项超过上限、且其中有这次带进来的，即一次违规
    private static IEnumerable<ConstraintViolation> GroupViolations(IEnumerable<ConstraintRuleView> applicable, HashSet<string> held, HashSet<string> contributed)
    {
        foreach (var rule in applicable)
        {
            foreach (var group in rule.Items.GroupBy(static i => i.GroupNo))
            {
                var matched = group.Select(static i => i.TargetId).Where(held.Contains).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                if (matched.Count > rule.MaxAllowed && matched.Any(contributed.Contains))
                {
                    yield return new ConstraintViolation(rule, group.Key, matched);
                }
            }
        }
    }
}
