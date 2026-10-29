// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Infrastructure;
using XiHan.BasicApp.Access.Operations.PermissionConditions;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;
using XiHan.Framework.Security.Abstractions.Conditions;

namespace XiHan.BasicApp.Access.Domain.Services;

/// <summary>
/// 权限条件的试算、影响试算与盘点。
/// </summary>
/// <remarks>
/// 对应 4.x 的 PermissionConditionTrialService 与 InventoryService。
/// 试算按用户此刻的有效指派与委托组装授予全貌、
/// 按给定的请求情形逐组逐子句说明；影响试算列出一个绑定影响到的人（角色即它及其下级角色的有效成员，最多 200 人按登录名）；盘点给出这一侧每条子句的诊断与所在组的结论。
/// </remarks>
/// <param name="evaluator">授予全貌。</param>
/// <param name="decider">条件判定。</param>
/// <param name="conditions">条件写入规则（绑定）。</param>
/// <param name="hierarchy">继承图。</param>
/// <param name="users">用户仓储。</param>
/// <param name="members">角色指派仓储。</param>
/// <param name="conditionRows">条件仓储。</param>
/// <param name="roles">角色查找。</param>
/// <param name="catalog">权限目录。</param>
/// <param name="registry">条件属性登记表。</param>
/// <param name="compiler">条件编译器。</param>
/// <param name="options">授权包配置。</param>
/// <param name="services">服务提供器（属性来源查库）。</param>
/// <param name="clock">时间源。</param>
public sealed class ConditionInsightService(
    GrantEvaluator evaluator,
    ConditionDecider decider,
    PermissionConditionService conditions,
    RoleHierarchyService hierarchy,
    IRepository<SysUser, Guid> users,
    IRepository<SysUserRole, Guid> members,
    IRepository<SysPermissionCondition, Guid> conditionRows,
    RoleService roles,
    PermissionCatalog catalog,
    IConditionAttributeRegistry registry,
    IConditionCompiler compiler,
    AccessOptions options,
    IServiceProvider services,
    TimeProvider clock)
{
    /// <summary>
    /// 影响试算最多列几人。
    /// </summary>
    /// <remarks>
    /// 4.x 同。
    /// </remarks>
    public const int BatchLimit = 200;

    /// <summary>
    /// 单人试算。
    /// </summary>
    public async Task<Result<ConditionTrialResponse>> TrialAsync(string owner, ConditionTrialRequest request, Func<Error, string> localize, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(localize);
        var checkedRequest = Check(request.Permission, request.Ip);
        if (checkedRequest.IsFailure)
        {
            return checkedRequest.Error!;
        }

        if (!(await users.FindAsync(request.UserId, cancellationToken).ConfigureAwait(false)).TryGetValue(out var user) || !string.Equals(user.HomeTenantId, owner, StringComparison.Ordinal))
        {
            return Error.NotFound(AccessErrors.MemberNotFound, AccessMessages.MemberNotFound, MessageArguments.Of(("id", request.UserId)));
        }

        var time = request.Time ?? clock.GetUtcNow();
        var view = await evaluator.ViewOfAsync(owner, user.Id, time, cancellationToken).ConfigureAwait(false);
        var source = ConditionAttributeSource.Create(RequestOf(owner, user, view, request.Permission, request.Ip, request.UserAgent, request.Path, request.Method, request.IsImpersonating, time), services, options);
        var decision = await decider.DecideAsync(view, request.Permission, source, explain: true, cancellationToken).ConfigureAwait(false);
        var names = await NamesAsync(owner, decision, cancellationToken).ConfigureAwait(false);
        return new ConditionTrialResponse(
            decision.Verdict,
            user.Id,
            user.UserName,
            request.Permission,
            request.Ip,
            time,
            options.ConditionTimeZone.Id,
            [.. decision.Denies.Select(d => SetOf(d, names, localize))],
            [.. decision.Paths.Select(p => new ConditionTrialPathResponse(
                p.Source,
                p.RoleId,
                p.RoleId is { } roleId ? names.GetValueOrDefault(roleId) : null,
                p.HasUnconditionalGrant,
                [.. p.GrantSets.Select(s => SetOf(s, names, localize))],
                [.. p.DenySets.Select(s => SetOf(s, names, localize))],
                p.Holds))]);
    }

    /// <summary>
    /// 影响试算：一个绑定影响到的人此刻各自的结论。
    /// </summary>
    public async Task<Result<ConditionBatchTrialResponse>> BatchTrialAsync(string owner, ConditionBatchTrialRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var checkedRequest = Check(request.Permission, request.Ip);
        if (checkedRequest.IsFailure)
        {
            return checkedRequest.Error!;
        }

        var bindings = await conditions.BindingsAsync(owner, request.SubjectKind, request.SubjectId, cancellationToken).ConfigureAwait(false);
        if (bindings.IsFailure)
        {
            return bindings.Error!;
        }

        var time = request.Time ?? clock.GetUtcNow();
        var holders = await HoldersAsync(owner, request.SubjectKind, request.SubjectId, time, cancellationToken).ConfigureAwait(false);
        var people = (await users.FindManyAsync(holders, cancellationToken).ConfigureAwait(false))
            .Where(u => string.Equals(u.HomeTenantId, owner, StringComparison.Ordinal))
            .OrderBy(static u => u.UserName, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var items = new List<ConditionBatchTrialItemResponse>();
        foreach (var user in people.Take(BatchLimit))
        {
            var view = await evaluator.ViewOfAsync(owner, user.Id, time, cancellationToken).ConfigureAwait(false);
            var source = ConditionAttributeSource.Create(RequestOf(owner, user, view, request.Permission, request.Ip, request.UserAgent, request.Path, request.Method, false, time), services, options);
            var decision = await decider.DecideAsync(view, request.Permission, source, explain: false, cancellationToken).ConfigureAwait(false);
            items.Add(new ConditionBatchTrialItemResponse(user.Id, user.UserName, decision.Verdict));
        }

        return new ConditionBatchTrialResponse(request.Permission, people.Count, people.Count > BatchLimit, items);
    }

    /// <summary>
    /// 盘点：这一侧每条子句的诊断与所在组的结论。
    /// </summary>
    public async Task<ConditionInventoryResponse> InventoryAsync(string owner, Func<Error, string> localize, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(localize);
        var rows = await conditionRows.ListAsync(EntityQuery.All<SysPermissionCondition>().Where(new Specification<SysPermissionCondition>(c => c.OwnerTenantId == owner)), cancellationToken).ConfigureAwait(false);
        var active = new Dictionary<(GrantSubjectKind, Guid), HashSet<(string, PermissionAction)>>();
        var names = new Dictionary<Guid, string>();
        foreach (var subject in rows.Select(static c => (c.SubjectKind, c.SubjectId)).Distinct())
        {
            var bindings = await conditions.BindingsAsync(owner, subject.SubjectKind, subject.SubjectId, cancellationToken).ConfigureAwait(false);
            active[subject] = bindings.TryGetValue(out var current) ? [.. current.Select(static b => (b.Permission, b.Action))] : [];
        }

        foreach (var role in await roles.FindManyAsync([.. rows.Where(static c => c.SubjectKind == GrantSubjectKind.Role).Select(static c => c.SubjectId).Distinct()], owner, cancellationToken).ConfigureAwait(false))
        {
            names[role.Id] = role.Name;
        }

        foreach (var user in await users.FindManyAsync([.. rows.Where(static c => c.SubjectKind == GrantSubjectKind.User).Select(static c => c.SubjectId).Distinct()], cancellationToken).ConfigureAwait(false))
        {
            names[user.Id] = $"{user.DisplayName} (@{user.UserName})";
        }

        var diagnosed = rows.Select(c =>
        {
            var diagnostics = new List<ConditionDiagnostic>();
            string? message = null;
            if (!registry.TryResolve(c.AttributeName, out var resolution))
            {
                diagnostics.Add(ConditionDiagnostic.UnregisteredAttribute);
            }
            else if (resolution.IsAlias)
            {
                diagnostics.Add(ConditionDiagnostic.LegacyName);
            }

            var compiled = compiler.Compile(new ConditionSet([new ConditionGroup([new ConditionClause(c.AttributeName, c.Operator, c.Value, c.IsNegated)])]), new ConditionCompileOptions { TimeZone = options.ConditionTimeZone });
            if (compiled.Errors.Count > 0 && resolution is not null)
            {
                diagnostics.Add(ConditionDiagnostic.InvalidCondition);
                message = localize(compiled.Errors[0].Error);
            }

            if (!active.GetValueOrDefault((c.SubjectKind, c.SubjectId), []).Contains((c.Permission, c.Action)))
            {
                diagnostics.Add(ConditionDiagnostic.BindingInactive);
            }

            return (Row: c, Diagnostics: diagnostics, Message: message);
        }).ToList();

        // 组的结论：绑定不在 > 全部停用 > 有用不了的启用子句 > 正常
        var conclusions = diagnosed
            .GroupBy(static d => (d.Row.SubjectKind, d.Row.SubjectId, d.Row.Permission, d.Row.Action, d.Row.GroupNo))
            .ToDictionary(static g => g.Key, static g =>
                g.Any(static d => d.Diagnostics.Contains(ConditionDiagnostic.BindingInactive)) ? ConditionGroupConclusion.BindingInactive
                : g.All(static d => d.Row.Status != ConditionStatus.Active) ? ConditionGroupConclusion.Disabled
                : g.Any(static d => d.Row.Status == ConditionStatus.Active && (d.Diagnostics.Contains(ConditionDiagnostic.UnregisteredAttribute) || d.Diagnostics.Contains(ConditionDiagnostic.InvalidCondition))) ? ConditionGroupConclusion.NeverMatches
                : ConditionGroupConclusion.Evaluable);
        var items = diagnosed
            .OrderBy(static d => d.Row.SubjectKind).ThenBy(static d => d.Row.SubjectId).ThenBy(static d => d.Row.Permission, StringComparer.Ordinal).ThenBy(static d => d.Row.Action).ThenBy(static d => d.Row.GroupNo).ThenBy(static d => d.Row.CreatedAt)
            .Select(d => new ConditionInventoryItemResponse(
                d.Row.Id, d.Row.SubjectKind, d.Row.SubjectId, names.GetValueOrDefault(d.Row.SubjectId), d.Row.Permission, d.Row.Action, d.Row.GroupNo, d.Row.AttributeName, d.Row.Operator, d.Row.IsNegated, d.Row.Value, d.Row.Status,
                d.Diagnostics, d.Message, conclusions[(d.Row.SubjectKind, d.Row.SubjectId, d.Row.Permission, d.Row.Action, d.Row.GroupNo)]))
            .ToList();
        return new ConditionInventoryResponse(clock.GetUtcNow(), rows.Count, rows.Select(static c => (c.SubjectKind, c.SubjectId, c.Permission, c.Action)).Distinct().Count(), items);
    }

    private Result Check(string permission, string? ip)
    {
        if (catalog.Find(permission.Trim()) is null)
        {
            return Error.Validation(AccessErrors.PermissionUnknown, AccessMessages.PermissionUnknown, MessageArguments.Of(("code", permission)));
        }

        return string.IsNullOrWhiteSpace(ip) || IPAddress.TryParse(ip.Trim(), out _)
            ? Result.Success()
            : Error.Validation(AccessErrors.TrialIpInvalid, AccessMessages.TrialIpInvalid, MessageArguments.Of(("ip", ip)));
    }

    private static ConditionRequest RequestOf(string owner, SysUser user, GrantView view, string permission, string? ip, string? userAgent, string? path, string? method, bool impersonating, DateTimeOffset time)
        => new(owner, user.Id, user.UserName, impersonating, [.. view.Roles.Select(static r => r.RoleId)], [permission], path, method, null, ip?.Trim(), userAgent, "http", time);

    // 角色：它与它的全部下级角色此刻的有效成员；用户：本人
    private async Task<IReadOnlyList<Guid>> HoldersAsync(string owner, GrantSubjectKind kind, Guid subjectId, DateTimeOffset time, CancellationToken cancellationToken)
    {
        if (kind == GrantSubjectKind.User)
        {
            return [subjectId];
        }

        var graph = await hierarchy.GraphOfAsync(owner, cancellationToken).ConfigureAwait(false);
        var roleIds = graph.DescendantsOf(subjectId).Select(static d => d.RoleId).Append(subjectId).ToList();
        var rows = await members.ListAsync(EntityQuery.All<SysUserRole>().Where(new Specification<SysUserRole>(m => m.OwnerTenantId == owner && roleIds.Contains(m.RoleId))), cancellationToken).ConfigureAwait(false);
        return [.. rows.Where(m => m.IsEffectiveAt(time)).Select(static m => m.UserId).Distinct()];
    }

    private async Task<Dictionary<Guid, string>> NamesAsync(string owner, ConditionDecision decision, CancellationToken cancellationToken)
    {
        var sets = decision.Denies.Concat(decision.Paths.SelectMany(static p => p.GrantSets.Concat(p.DenySets))).ToList();
        var roleIds = sets.Where(static s => s.Kind == GrantSubjectKind.Role).Select(static s => s.SubjectId).Concat(decision.Paths.Where(static p => p.RoleId.HasValue).Select(static p => p.RoleId!.Value)).Distinct().ToList();
        var names = (await roles.FindManyAsync(roleIds, owner, cancellationToken).ConfigureAwait(false)).ToDictionary(static r => r.Id, static r => r.Name);
        foreach (var user in await users.FindManyAsync([.. sets.Where(static s => s.Kind == GrantSubjectKind.User).Select(static s => s.SubjectId).Distinct()], cancellationToken).ConfigureAwait(false))
        {
            names[user.Id] = $"{user.DisplayName} (@{user.UserName})";
        }

        return names;
    }

    private static ConditionTrialSetResponse SetOf(ConditionSetResult set, Dictionary<Guid, string> names, Func<Error, string> localize)
        => new(
            set.Kind,
            set.SubjectId,
            names.GetValueOrDefault(set.SubjectId),
            set.Binding,
            set.Action,
            set.Applies,
            set.Evaluation?.Outcome,
            [.. (set.Evaluation?.Groups ?? []).Select(g => new ConditionTrialGroupResponse(
                g.Key,
                g.Outcome,
                [.. g.Clauses.Select(c => new ConditionTrialClauseResponse(c.Key, c.AttributeName, c.Operator, c.IsNegated, c.Value, c.Outcome, c.AttributeValues, c.Reason is { } reason ? localize(reason) : null))]))],
            [.. set.Errors.Select(e => localize(e.Error))]);
}
