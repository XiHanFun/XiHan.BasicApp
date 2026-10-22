// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.AccessPolicies;

/// <summary>
/// 两层合起来的裁决：平台的拒绝租户放行不了；同层放行压过拒绝
/// </summary>
public sealed class AccessPolicyDecision
{
    private AccessPolicyDecision(IReadOnlyList<CompiledAccessPolicy> matched, CompiledAccessPolicy? blockedBy)
    {
        Matched = matched;
        BlockedBy = blockedBy;
    }

    /// <summary>
    /// 两层命中的全部策略
    /// </summary>
    public IReadOnlyList<CompiledAccessPolicy> Matched { get; }

    /// <summary>
    /// 拦下请求的策略；放行时为空
    /// </summary>
    public CompiledAccessPolicy? BlockedBy { get; }

    /// <summary>
    /// 是否拦下
    /// </summary>
    public bool IsBlocked => BlockedBy is not null;

    /// <summary>
    /// 合并两层
    /// </summary>
    /// <param name="platform">平台层结果</param>
    /// <param name="platformAllowed">平台层在更早的时机已命中放行策略</param>
    /// <param name="tenant">租户层结果；平台已拒绝或不涉及租户时为空</param>
    /// <returns>裁决</returns>
    public static AccessPolicyDecision Combine(AccessPolicyLayerResult platform, bool platformAllowed, AccessPolicyLayerResult? tenant)
    {
        ArgumentNullException.ThrowIfNull(platform);
        var matched = tenant is null ? platform.Matched : [.. platform.Matched, .. tenant.Matched];
        if (!platformAllowed && platform.Deny is not null)
        {
            return new AccessPolicyDecision(platform.Matched, platform.Deny);
        }

        return new AccessPolicyDecision(matched, tenant?.Deny);
    }

    /// <summary>
    /// 某条命中的策略是否算作拦下了请求：与生效的拒绝策略同层的拒绝策略
    /// </summary>
    public bool IsBlocking(CompiledAccessPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        return BlockedBy is not null && policy.Action == AccessPolicyAction.Deny && policy.TenantId == BlockedBy.TenantId;
    }
}
