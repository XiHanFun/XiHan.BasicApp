// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Domain.Services;
using XiHan.BasicApp.Access.Operations.Policies;

namespace XiHan.BasicApp.Access.Operations.ConstraintRules;

/// <summary>
/// 约束规则的响应投影：带规则项（目标的显示名与编码）、条件与此刻是否生效。
/// </summary>
/// <param name="service">规则服务。</param>
/// <param name="clock">时间源。</param>
public sealed class ConstraintRuleProjection(ConstraintRuleService service, TimeProvider clock)
{
    /// <summary>
    /// 投影一条。
    /// </summary>
    public async Task<ConstraintRuleResponse> ToAsync(SysConstraintRule rule, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(rule);
        return (await ToAsync([rule], cancellationToken).ConfigureAwait(false))[0];
    }

    /// <summary>
    /// 投影一批（同一侧）。
    /// </summary>
    public async Task<IReadOnlyList<ConstraintRuleResponse>> ToAsync(IReadOnlyList<SysConstraintRule> rules, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(rules);
        if (rules.Count == 0)
        {
            return [];
        }

        var owner = rules[0].OwnerTenantId;
        var items = await service.ItemsOfAsync(owner, [.. rules.Select(static r => r.Id)], cancellationToken).ConfigureAwait(false);
        var names = await service.TargetNamesAsync(owner, rules.SelectMany(r => items[r.Id].Select(i => (r.TargetType, i.TargetId))), cancellationToken).ConfigureAwait(false);
        var now = clock.GetUtcNow();
        return [.. rules.Select(r => new ConstraintRuleResponse(
            r.Id, r.Code, r.Name, r.Type, r.TargetType, r.MaxAllowed,
            [.. Infrastructure.ConstraintRules.ConditionsOf(r.Conditions).Select(static c => new AccessPolicyConditionResponse(c.GroupNo, c.AttributeName, c.Operator, c.IsNegated, c.Value, null))],
            r.ViolationAction, r.Priority, r.Status, r.EffectiveAt, r.ExpiresAt, r.IsEffectiveAt(now), r.Description, r.Remark,
            [.. items[r.Id].Select(i => names.TryGetValue((r.TargetType, i.TargetId), out var name)
                ? new ConstraintItemResponse(i.TargetId, name.Name, name.Code, i.GroupNo, i.Remark)
                : new ConstraintItemResponse(i.TargetId, null, null, i.GroupNo, i.Remark))],
            r.CreatedAt, r.ModifiedAt))];
    }
}
