// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Caching.Abstractions.Caches;
using XiHan.Framework.Caching.Abstractions.Stores;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;

namespace XiHan.BasicApp.Access.Infrastructure;

/// <summary>
/// 一个规则项（判定所需的部分）。
/// </summary>
/// <param name="TargetId">目标：角色 ID、权限码或用户 ID。</param>
/// <param name="GroupNo">组号。</param>
public sealed record ConstraintItemView(string TargetId, int GroupNo);

/// <summary>
/// 一条启用的约束规则（判定所需的部分）。
/// </summary>
/// <param name="Id">规则 ID。</param>
/// <param name="Code">编码。</param>
/// <param name="Name">名称。</param>
/// <param name="Type">约束类型。</param>
/// <param name="TargetType">目标类型。</param>
/// <param name="MaxAllowed">数量上限。</param>
/// <param name="Action">违规处理方式。</param>
/// <param name="EffectiveAt">生效时间。</param>
/// <param name="ExpiresAt">失效时间。</param>
/// <param name="Conditions">条件类的条件。</param>
/// <param name="Items">规则项。</param>
public sealed record ConstraintRuleView(Guid Id, string Code, string Name, ConstraintType Type, ConstraintTargetType TargetType, int MaxAllowed, ConstraintViolationAction Action, DateTimeOffset? EffectiveAt, DateTimeOffset? ExpiresAt, List<PolicyClauseView> Conditions, List<ConstraintItemView> Items)
{
    /// <summary>
    /// 某一时刻是否在有效期内。
    /// </summary>
    public bool IsEffectiveAt(DateTimeOffset now) => (EffectiveAt is null || EffectiveAt <= now) && (ExpiresAt is null || ExpiresAt > now);

    /// <summary>
    /// 违规是否拦下变更（拒绝与需审批；审批未接入，按拒绝）。
    /// </summary>
    public bool IsBlocking => Action is ConstraintViolationAction.Deny or ConstraintViolationAction.RequireApproval;
}

/// <summary>
/// 一侧启用的约束规则（单例）：放在共享缓存里 60 秒，写入即失效；按优先级从大到小、再按编码排列，有效期在判定时比较。
/// </summary>
/// <param name="cache">共享缓存。</param>
/// <param name="scopes">作用域工厂（缓存未命中时用只读工作单元查库）。</param>
public sealed class ConstraintRules(ISharedCache cache, IServiceScopeFactory scopes)
{
    private static readonly TimeSpan _timeToLive = TimeSpan.FromSeconds(60);

    /// <summary>
    /// 一侧启用的规则。
    /// </summary>
    public async Task<IReadOnlyList<ConstraintRuleView>> OfAsync(string owner, CancellationToken cancellationToken)
    {
        var raw = await cache.GetOrCreateAsync(KeyOf(owner), ct => LoadAsync(owner, ct), CacheEntryOptions.Expires(_timeToLive), cancellationToken).ConfigureAwait(false);
        return raw.Length == 0 ? [] : JsonSerializer.Deserialize(raw, AccessJsonContext.Default.ListConstraintRuleView) ?? [];
    }

    /// <summary>
    /// 一侧的规则失效。
    /// </summary>
    public async Task InvalidateAsync(string owner, CancellationToken cancellationToken)
        => await cache.RemoveAsync(KeyOf(owner), cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// 条件类的条件从 JSON 读回；为空即没有。
    /// </summary>
    public static List<PolicyClauseView> ConditionsOf(string? json)
        => string.IsNullOrWhiteSpace(json) ? [] : JsonSerializer.Deserialize(json, AccessJsonContext.Default.ListPolicyClauseView) ?? [];

    private static CacheKey KeyOf(string owner) => new($"access:constraints:{owner}");

    private async ValueTask<string> LoadAsync(string owner, CancellationToken cancellationToken)
        => await StoreScope.ReadAsync(scopes, async (services, ct) =>
        {
            var rules = await services.GetRequiredService<IRepository<SysConstraintRule, Guid>>()
                .ListAsync(EntityQuery.All<SysConstraintRule>().Where(new Specification<SysConstraintRule>(r => r.OwnerTenantId == owner && r.Status == ConstraintRuleStatus.Active)), ct)
                .ConfigureAwait(false);
            if (rules.Count == 0)
            {
                return string.Empty;
            }

            var ids = rules.Select(static r => r.Id).ToList();
            var items = (await services.GetRequiredService<IRepository<SysConstraintRuleItem, Guid>>()
                    .ListAsync(EntityQuery.All<SysConstraintRuleItem>().Where(new Specification<SysConstraintRuleItem>(i => i.OwnerTenantId == owner && ids.Contains(i.RuleId))), ct)
                    .ConfigureAwait(false))
                .ToLookup(static i => i.RuleId);
            var views = rules
                .OrderByDescending(static r => r.Priority).ThenBy(static r => r.Code, StringComparer.Ordinal)
                .Select(r => new ConstraintRuleView(r.Id, r.Code, r.Name, r.Type, r.TargetType, r.MaxAllowed, r.ViolationAction, r.EffectiveAt, r.ExpiresAt, ConditionsOf(r.Conditions), [.. items[r.Id].Select(static i => new ConstraintItemView(i.TargetId, i.GroupNo))]))
                .ToList();
            return JsonSerializer.Serialize(views, AccessJsonContext.Default.ListConstraintRuleView);
        }, cancellationToken).ConfigureAwait(false);
}
