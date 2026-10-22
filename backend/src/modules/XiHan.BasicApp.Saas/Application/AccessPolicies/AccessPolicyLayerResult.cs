// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.AccessPolicies;

/// <summary>
/// 一层（平台或某租户）策略的求值结果：命中的策略按优先级排列，同层放行压过拒绝
/// </summary>
public sealed class AccessPolicyLayerResult
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="matched">命中的策略，按优先级排列</param>
    public AccessPolicyLayerResult(IReadOnlyList<CompiledAccessPolicy> matched)
    {
        ArgumentNullException.ThrowIfNull(matched);
        Matched = matched;
        Allow = matched.FirstOrDefault(policy => policy.Action == AccessPolicyAction.Allow);
        Deny = Allow is null ? matched.FirstOrDefault(policy => policy.Action == AccessPolicyAction.Deny) : null;
    }

    /// <summary>
    /// 没有命中任何策略
    /// </summary>
    public static AccessPolicyLayerResult None { get; } = new([]);

    /// <summary>
    /// 命中的策略，按优先级排列
    /// </summary>
    public IReadOnlyList<CompiledAccessPolicy> Matched { get; }

    /// <summary>
    /// 命中的第一条放行策略
    /// </summary>
    public CompiledAccessPolicy? Allow { get; }

    /// <summary>
    /// 生效的拒绝策略：没有放行时命中的第一条拒绝策略
    /// </summary>
    public CompiledAccessPolicy? Deny { get; }
}
