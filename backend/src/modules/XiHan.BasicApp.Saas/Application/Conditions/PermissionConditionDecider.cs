// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.Framework.Authorization.Conditions;

namespace XiHan.BasicApp.Saas.Application.Conditions;

/// <summary>
/// 按权限条件判定一个权限码的结果
/// </summary>
public enum PermissionConditionOutcome
{
    /// <summary>
    /// 授予
    /// </summary>
    Granted = 0,

    /// <summary>
    /// 条件不满足，或有条件的禁止命中
    /// </summary>
    Denied = 1,

    /// <summary>
    /// 还差数据行属性，模型绑定之后补算
    /// </summary>
    Pending = 2
}

/// <summary>
/// 求值判定与说明：结果，以及每个条件集的求值说明（快照引用到却找不到的条件集为空）
/// </summary>
public sealed record PermissionConditionExplanation(
    PermissionConditionOutcome Outcome,
    IReadOnlyDictionary<string, ConditionEvaluationResult?> Sets);

/// <summary>
/// 权限条件判定：在授权快照的条件上求值一个权限码
/// </summary>
/// <remarks>
/// 先看用户有条件的禁止：任一命中即拒绝。再看授予路径：
/// 路径成立 = （有无条件的授予行，或任一授予条件集命中）且 禁止条件集都不命中，任一路径成立即授予。
/// 还差数据行属性的条件集算「待定」：有路径可能成立、又没有路径确定成立时，结果是待定。
/// 快照里引用到却找不到的条件集，授予侧当不命中、禁止侧当命中。
/// </remarks>
public sealed class PermissionConditionDecider
{
    private readonly SaasConditionSetCompiler _compiler;
    private readonly IConditionEvaluator _evaluator;

    /// <summary>
    /// 构造函数
    /// </summary>
    public PermissionConditionDecider(SaasConditionSetCompiler compiler, IConditionEvaluator evaluator)
    {
        _compiler = compiler;
        _evaluator = evaluator;
    }

    /// <summary>
    /// 不求值的判定（<see cref="PermissionConditionMode.Disabled"/>）：授予行一律算授予、禁止行一律算禁止
    /// </summary>
    public static PermissionConditionOutcome DecideIgnoringConditions(AuthorizationConditions conditions, string permissionCode)
    {
        ArgumentNullException.ThrowIfNull(conditions);
        if (conditions.Denies.ContainsKey(permissionCode))
        {
            return PermissionConditionOutcome.Denied;
        }

        return !conditions.Grants.TryGetValue(permissionCode, out var paths) || paths.Any(path => path.DenySetKeys.Count == 0)
            ? PermissionConditionOutcome.Granted
            : PermissionConditionOutcome.Denied;
    }

    /// <summary>
    /// 求值判定
    /// </summary>
    public async Task<PermissionConditionOutcome> DecideAsync(
        AuthorizationConditions conditions,
        string permissionCode,
        IConditionAttributeSource source,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(conditions);
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionCode);
        ArgumentNullException.ThrowIfNull(source);

        var outcomes = new Dictionary<string, ConditionOutcome?>(StringComparer.Ordinal);
        return await DecideCoreAsync(conditions, permissionCode, async key =>
        {
            if (!outcomes.TryGetValue(key, out var outcome))
            {
                outcome = conditions.Sets.TryGetValue(key, out var set)
                    ? (await _evaluator.EvaluateAsync(_compiler.Compile(set), source, cancellationToken: cancellationToken)).Outcome
                    : null;
                outcomes[key] = outcome;
            }

            return outcome;
        });
    }

    /// <summary>
    /// 求值判定并给出说明：该权限码引用到的每个条件集都完整求值（不短路），附带每组每个子句的取值与结果，试算用
    /// </summary>
    public async Task<PermissionConditionExplanation> ExplainAsync(
        AuthorizationConditions conditions,
        string permissionCode,
        IConditionAttributeSource source,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(conditions);
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionCode);
        ArgumentNullException.ThrowIfNull(source);

        var keys = (conditions.Grants.GetValueOrDefault(permissionCode) ?? [])
            .SelectMany(path => path.GrantSetKeys.Concat(path.DenySetKeys))
            .Concat(conditions.Denies.GetValueOrDefault(permissionCode) ?? [])
            .Distinct(StringComparer.Ordinal);
        var results = new Dictionary<string, ConditionEvaluationResult?>(StringComparer.Ordinal);
        foreach (var key in keys)
        {
            results[key] = conditions.Sets.TryGetValue(key, out var set)
                ? await _evaluator.EvaluateAsync(_compiler.Compile(set), source, explain: true, cancellationToken)
                : null;
        }

        var outcome = await DecideCoreAsync(conditions, permissionCode, key => ValueTask.FromResult(results.GetValueOrDefault(key)?.Outcome));
        return new PermissionConditionExplanation(outcome, results);
    }

    private static async Task<PermissionConditionOutcome> DecideCoreAsync(
        AuthorizationConditions conditions,
        string permissionCode,
        Func<string, ValueTask<ConditionOutcome?>> setOutcomeAsync)
    {
        var pending = false;
        if (conditions.Denies.TryGetValue(permissionCode, out var denyKeys))
        {
            foreach (var key in denyKeys)
            {
                switch (await setOutcomeAsync(key))
                {
                    case null or ConditionOutcome.Matched:
                        return PermissionConditionOutcome.Denied;
                    case ConditionOutcome.Pending:
                        pending = true;
                        break;
                }
            }
        }

        if (!conditions.Grants.TryGetValue(permissionCode, out var paths))
        {
            return pending ? PermissionConditionOutcome.Pending : PermissionConditionOutcome.Granted;
        }

        var possible = false;
        foreach (var path in paths)
        {
            var grant = path.HasUnconditionalGrant ? ConditionOutcome.Matched : ConditionOutcome.NotMatched;
            foreach (var key in path.GrantSetKeys)
            {
                if (grant == ConditionOutcome.Matched)
                {
                    break;
                }

                var outcome = await setOutcomeAsync(key);
                if (outcome is ConditionOutcome.Matched or ConditionOutcome.Pending)
                {
                    grant = outcome.Value;
                }
            }

            if (grant == ConditionOutcome.NotMatched)
            {
                continue;
            }

            var denied = false;
            var denyPending = false;
            foreach (var key in path.DenySetKeys)
            {
                var outcome = await setOutcomeAsync(key);
                if (outcome is null or ConditionOutcome.Matched)
                {
                    denied = true;
                    break;
                }

                denyPending |= outcome == ConditionOutcome.Pending;
            }

            if (denied)
            {
                continue;
            }

            if (grant == ConditionOutcome.Matched && !denyPending)
            {
                return pending ? PermissionConditionOutcome.Pending : PermissionConditionOutcome.Granted;
            }

            possible = true;
        }

        return possible ? PermissionConditionOutcome.Pending : PermissionConditionOutcome.Denied;
    }
}
