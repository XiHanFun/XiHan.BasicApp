// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Domain.Services;
using XiHan.BasicApp.Access.Infrastructure;
using XiHan.BasicApp.Contracts;

namespace XiHan.BasicApp.Access.Operations.Policies;

/// <summary>
/// 访问策略的响应投影：带条件、此刻是否有效与时机（按条件引用的属性推出）。
/// </summary>
/// <param name="service">策略服务（取条件）。</param>
/// <param name="evaluator">策略求值（编译推出时机）。</param>
/// <param name="clock">时间源。</param>
public sealed class AccessPolicyProjection(AccessPolicyService service, AccessPolicyEvaluator evaluator, TimeProvider clock)
{
    /// <summary>
    /// 投影一条。
    /// </summary>
    public async Task<AccessPolicyResponse> ToAsync(SysAccessPolicy policy, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(policy);
        return (await ToAsync([policy], cancellationToken).ConfigureAwait(false))[0];
    }

    /// <summary>
    /// 投影一批（同一侧）。
    /// </summary>
    public async Task<IReadOnlyList<AccessPolicyResponse>> ToAsync(IReadOnlyList<SysAccessPolicy> policies, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(policies);
        if (policies.Count == 0)
        {
            return [];
        }

        var owner = policies[0].OwnerTenantId;
        var platform = string.Equals(owner, TenantIds.Platform, StringComparison.Ordinal);
        var conditions = await service.ConditionsOfAsync(owner, [.. policies.Select(static p => p.Id)], cancellationToken).ConfigureAwait(false);
        var now = clock.GetUtcNow();
        return [.. policies.Select(p =>
        {
            var rows = conditions[p.Id].ToList();
            var definitions = rows.Select(static c => new PolicyConditionDefinition(c.GroupNo, c.AttributeName, c.Operator, c.IsNegated, c.Value, c.Description)).ToList();
            var compiled = evaluator.Compile(AccessPolicyService.ViewOf(p.Id, p.Code, p.Name, p.Action, p.Priority, p.EffectiveAt, p.ExpiresAt, definitions));
            return new AccessPolicyResponse(
                p.Id, p.Code, p.Name, p.Description, p.Action, p.Priority, p.Status, p.EffectiveAt, p.ExpiresAt, p.Status == AccessPolicyStatus.Active && p.IsEffectiveAt(now),
                AccessPolicyEvaluator.PhaseOf(compiled, platform), p.TemplateCode, p.Remark,
                [.. rows.Select(AccessPolicyConditionResponse.From)], p.CreatedAt, p.ModifiedAt);
        })];
    }
}
