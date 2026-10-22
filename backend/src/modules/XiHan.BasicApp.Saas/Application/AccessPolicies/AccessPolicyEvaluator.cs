// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Authorization.Conditions;

namespace XiHan.BasicApp.Saas.Application.AccessPolicies;

/// <summary>
/// 访问策略求值：逐条求一层策略的条件集，生效期外的跳过，待定按不命中
/// </summary>
public sealed class AccessPolicyEvaluator
{
    private readonly IConditionEvaluator _evaluator;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AccessPolicyEvaluator(IConditionEvaluator evaluator)
    {
        _evaluator = evaluator;
    }

    /// <summary>
    /// 求一层策略
    /// </summary>
    /// <param name="policies">这一层的策略，按优先级排列</param>
    /// <param name="source">属性来源</param>
    /// <param name="now">求值时刻（判断生效期）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>这一层的求值结果</returns>
    public async ValueTask<AccessPolicyLayerResult> EvaluateAsync(
        IReadOnlyList<CompiledAccessPolicy> policies,
        IConditionAttributeSource source,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(policies);
        ArgumentNullException.ThrowIfNull(source);
        if (policies.Count == 0)
        {
            return AccessPolicyLayerResult.None;
        }

        var matched = new List<CompiledAccessPolicy>();
        foreach (var policy in policies)
        {
            if (!policy.IsEffectiveAt(now))
            {
                continue;
            }

            var result = await _evaluator.EvaluateAsync(policy.Conditions, source, explain: false, cancellationToken);
            if (result.IsMatched)
            {
                matched.Add(policy);
            }
        }

        return matched.Count == 0 ? AccessPolicyLayerResult.None : new AccessPolicyLayerResult(matched);
    }
}
