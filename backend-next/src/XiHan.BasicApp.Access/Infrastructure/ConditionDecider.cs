// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.Framework.Security.Abstractions.Conditions;

namespace XiHan.BasicApp.Access.Infrastructure;

/// <summary>
/// 一个主体在一个权限码上按条件判定的结论（4.x 的五种去掉待定：6.0 不登记数据行属性）。
/// </summary>
public enum ConditionVerdict
{
    /// <summary>
    /// 不持有（没有授予，或被无条件拒绝）。
    /// </summary>
    NotGranted = 0,

    /// <summary>
    /// 持有且不涉及条件。
    /// </summary>
    Unconditional = 1,

    /// <summary>
    /// 按条件判定为授予。
    /// </summary>
    Granted = 2,

    /// <summary>
    /// 按条件判定为拒绝（有条件的拒绝命中，或没有一条授予路径成立）。
    /// </summary>
    Denied = 3,
}

/// <summary>
/// 一个绑定上的条件集求值结果。
/// </summary>
/// <param name="Kind">主体类别。</param>
/// <param name="SubjectId">主体 ID。</param>
/// <param name="Binding">绑定的权限码（可能是 <c>*</c>）。</param>
/// <param name="Action">授予或拒绝。</param>
/// <param name="Applies">这个条件集是否生效：授予侧为命中，拒绝侧为命中或无法编译（从严）。</param>
/// <param name="Evaluation">求值结果（无法编译时为空）。</param>
/// <param name="Errors">编译错误。</param>
public sealed record ConditionSetResult(GrantSubjectKind Kind, Guid SubjectId, string Binding, PermissionAction Action, bool Applies, ConditionEvaluationResult? Evaluation, IReadOnlyList<ConditionCompileError> Errors);

/// <summary>
/// 一条授予路径的判定（用户本身、委托的权限码、持有或受委托的角色链）。
/// </summary>
/// <param name="Source">来源：<c>user</c>、<c>delegation</c>、<c>role</c> 或 <c>delegation-role</c>。</param>
/// <param name="RoleId">角色链的链首角色。</param>
/// <param name="HasUnconditionalGrant">路径上有没有无条件的授予。</param>
/// <param name="GrantSets">路径上有条件的授予。</param>
/// <param name="DenySets">路径上有条件的拒绝（只有角色链有：用户本身的拒绝作用于全部路径）。</param>
/// <param name="Holds">这条路径是否成立。</param>
public sealed record ConditionPathResult(string Source, Guid? RoleId, bool HasUnconditionalGrant, IReadOnlyList<ConditionSetResult> GrantSets, IReadOnlyList<ConditionSetResult> DenySets, bool Holds);

/// <summary>
/// 按条件判定的结论与说明。
/// </summary>
/// <param name="Verdict">结论。</param>
/// <param name="Denies">用户本身有条件的拒绝。</param>
/// <param name="Paths">授予路径（说明模式下逐条给出；否则判定成立即停）。</param>
public sealed record ConditionDecision(ConditionVerdict Verdict, IReadOnlyList<ConditionSetResult> Denies, IReadOnlyList<ConditionPathResult> Paths);

/// <summary>
/// 权限条件的判定（4.x 的 PermissionConditionDecider）：先看用户本身有条件的拒绝，命中即拒绝；再看授予路径——
/// 路径成立 = 有无条件授予或某个授予条件集命中，且路径上的拒绝条件集都不命中。授予侧无法编译按不命中，拒绝侧按命中（从严）；
/// 取不到值（无法判定）在组内按不命中（条件引擎的语义）。
/// </summary>
/// <param name="compiler">条件编译器。</param>
/// <param name="evaluator">条件求值器。</param>
/// <param name="options">授权包配置（时区）。</param>
public sealed class ConditionDecider(IConditionCompiler compiler, IConditionEvaluator evaluator, AccessOptions options)
{
    /// <summary>
    /// 判定。
    /// </summary>
    /// <param name="view">授予全貌。</param>
    /// <param name="permission">权限码。</param>
    /// <param name="source">请求属性。</param>
    /// <param name="explain">是否给出完整说明（试算用；判定时为 <see langword="false"/>，成立即停）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task<ConditionDecision> DecideAsync(GrantView view, string permission, IConditionAttributeSource source, bool explain, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);
        ArgumentNullException.ThrowIfNull(source);
        if (!view.Allows(permission))
        {
            return new ConditionDecision(ConditionVerdict.NotGranted, [], []);
        }

        if (!view.RequiresConditions(permission))
        {
            return new ConditionDecision(ConditionVerdict.Unconditional, [], []);
        }

        var now = view.Now;
        var denies = new List<ConditionSetResult>();
        foreach (var binding in view.User.DenyBindings(permission, now))
        {
            var result = await EvaluateAsync(view.User, binding, PermissionAction.Deny, source, explain, cancellationToken).ConfigureAwait(false);
            denies.Add(result);
            if (result.Applies && !explain)
            {
                return new ConditionDecision(ConditionVerdict.Denied, denies, []);
            }
        }

        var denied = denies.Any(static d => d.Applies);
        var paths = new List<ConditionPathResult>();
        var user = await UserPathAsync(view.User, permission, now, source, explain, cancellationToken).ConfigureAwait(false);
        if (user is not null)
        {
            paths.Add(user);
        }

        if (view.Delegated.Contains(permission))
        {
            paths.Add(new ConditionPathResult("delegation", null, true, [], [], true));
        }

        if (!denied && !explain && paths.Any(static p => p.Holds))
        {
            return new ConditionDecision(ConditionVerdict.Granted, denies, paths);
        }

        foreach (var (chain, label) in view.Roles.Select(static c => (c, "role")).Concat(view.DelegatedRoles.Select(static c => (c, "delegation-role"))))
        {
            var path = await ChainPathAsync(chain, label, permission, now, source, explain, cancellationToken).ConfigureAwait(false);
            if (path is null)
            {
                continue;
            }

            paths.Add(path);
            if (!denied && !explain && path.Holds)
            {
                return new ConditionDecision(ConditionVerdict.Granted, denies, paths);
            }
        }

        return new ConditionDecision(!denied && paths.Any(static p => p.Holds) ? ConditionVerdict.Granted : ConditionVerdict.Denied, denies, paths);
    }

    private async Task<ConditionPathResult?> UserPathAsync(GrantSource user, string permission, DateTimeOffset now, IConditionAttributeSource source, bool explain, CancellationToken cancellationToken)
    {
        var bindings = user.GrantBindings(permission, now).ToList();
        if (bindings.Count == 0)
        {
            return null;
        }

        var unconditional = bindings.Any(b => user.ConditionsOf(b, PermissionAction.Grant).Count == 0);
        var grants = new List<ConditionSetResult>();
        if (!unconditional || explain)
        {
            foreach (var binding in bindings.Where(b => user.ConditionsOf(b, PermissionAction.Grant).Count > 0))
            {
                grants.Add(await EvaluateAsync(user, binding, PermissionAction.Grant, source, explain, cancellationToken).ConfigureAwait(false));
            }
        }

        return new ConditionPathResult("user", null, unconditional, grants, [], unconditional || grants.Any(static g => g.Applies));
    }

    private async Task<ConditionPathResult?> ChainPathAsync(RoleChain chain, string label, string permission, DateTimeOffset now, IConditionAttributeSource source, bool explain, CancellationToken cancellationToken)
    {
        // 链上有无条件的拒绝即整条链不给出；没有任何授予也不是一条路径
        if (chain.Members.Any(m => m.DeniesAt(permission, now, conditionsApply: true)) || !chain.Members.Any(m => m.GrantsAt(permission, now)))
        {
            return null;
        }

        var unconditional = chain.Members.Any(m => m.GrantBindings(permission, now).Any(b => m.ConditionsOf(b, PermissionAction.Grant).Count == 0));
        var grants = new List<ConditionSetResult>();
        if (!unconditional || explain)
        {
            foreach (var member in chain.Members)
            {
                foreach (var binding in member.GrantBindings(permission, now).Where(b => member.ConditionsOf(b, PermissionAction.Grant).Count > 0))
                {
                    grants.Add(await EvaluateAsync(member, binding, PermissionAction.Grant, source, explain, cancellationToken).ConfigureAwait(false));
                }
            }
        }

        var granted = unconditional || grants.Any(static g => g.Applies);
        var denies = new List<ConditionSetResult>();
        if (granted || explain)
        {
            foreach (var member in chain.Members)
            {
                foreach (var binding in member.DenyBindings(permission, now))
                {
                    denies.Add(await EvaluateAsync(member, binding, PermissionAction.Deny, source, explain, cancellationToken).ConfigureAwait(false));
                }
            }
        }

        return new ConditionPathResult(label, chain.RoleId, unconditional, grants, denies, granted && !denies.Any(static d => d.Applies));
    }

    private async Task<ConditionSetResult> EvaluateAsync(GrantSource subject, string binding, PermissionAction action, IConditionAttributeSource source, bool explain, CancellationToken cancellationToken)
    {
        var compiled = compiler.Compile(ConditionClauseView.ToSet(subject.ConditionsOf(binding, action)), new ConditionCompileOptions { TimeZone = options.ConditionTimeZone });
        if (compiled.Errors.Count > 0)
        {
            return new ConditionSetResult(subject.Kind, subject.SubjectId, binding, action, action == PermissionAction.Deny, null, compiled.Errors);
        }

        var evaluation = await evaluator.EvaluateAsync(compiled, source, explain, cancellationToken).ConfigureAwait(false);
        return new ConditionSetResult(subject.Kind, subject.SubjectId, binding, action, evaluation.IsMatched, evaluation, []);
    }
}
