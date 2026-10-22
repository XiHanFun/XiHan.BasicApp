// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Conditions;

namespace XiHan.BasicApp.Saas.Application.AccessPolicies;

/// <summary>
/// 认证之后（请求闸门与登录时）的两层求值：平台级要等认证才能求的策略，加上主体所在租户的策略
/// </summary>
/// <remarks>
/// 平台只引用请求属性的策略在认证前那一段求，这里不再重复；那一段命中的平台放行照样压过这里的平台拒绝。
/// 租户级策略只作用于有用户的请求。
/// </remarks>
public sealed class AccessPolicyAuthenticatedEvaluator
{
    private readonly IAccessPolicySnapshotProvider _snapshots;
    private readonly AccessPolicyEvaluator _evaluator;
    private readonly ISaasConditionAttributeSourceFactory _sources;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AccessPolicyAuthenticatedEvaluator(
        IAccessPolicySnapshotProvider snapshots,
        AccessPolicyEvaluator evaluator,
        ISaasConditionAttributeSourceFactory sources)
    {
        _snapshots = snapshots;
        _evaluator = evaluator;
        _sources = sources;
    }

    /// <summary>
    /// 求值
    /// </summary>
    /// <param name="context">主体、租户与请求</param>
    /// <param name="platformAllowed">认证前已命中平台的放行策略</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>裁决；两层都没有可求的策略时为空</returns>
    public async Task<AccessPolicyDecision?> EvaluateAsync(SaasConditionContext context, bool platformAllowed, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        var snapshot = await _snapshots.GetAsync(cancellationToken);
        var tenantPolicies = context.UserId is > 0 && context.TenantId > 0 ? snapshot.GetTenant(context.TenantId) : [];
        if (snapshot.PlatformAfterAuthentication.Count == 0 && tenantPolicies.Count == 0)
        {
            return null;
        }

        var source = _sources.Create(context);
        var platform = await _evaluator.EvaluateAsync(snapshot.PlatformAfterAuthentication, source, context.Now, cancellationToken);
        var platformBlocks = !platformAllowed && platform.Deny is not null;
        var tenant = platformBlocks ? null : await _evaluator.EvaluateAsync(tenantPolicies, source, context.Now, cancellationToken);
        return AccessPolicyDecision.Combine(platform, platformAllowed, tenant);
    }
}
