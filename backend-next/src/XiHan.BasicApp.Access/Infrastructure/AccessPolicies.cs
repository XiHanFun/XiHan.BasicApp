// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Domain.Services;
using XiHan.BasicApp.Contracts.Auditing;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Caching.Abstractions.Caches;
using XiHan.Framework.Caching.Abstractions.Stores;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Security.Abstractions.Conditions;

namespace XiHan.BasicApp.Access.Infrastructure;

/// <summary>
/// 一条访问策略条件（判定所需的部分）。
/// </summary>
/// <param name="GroupNo">组号。</param>
/// <param name="AttributeName">属性名。</param>
/// <param name="Operator">运算符。</param>
/// <param name="IsNegated">是否取反。</param>
/// <param name="Value">条件值。</param>
public sealed record PolicyClauseView(int GroupNo, string AttributeName, ConditionOperator Operator, bool IsNegated, string? Value);

/// <summary>
/// 一条启用的访问策略（判定所需的部分）。
/// </summary>
/// <param name="Id">策略 ID。</param>
/// <param name="Code">编码。</param>
/// <param name="Name">名称。</param>
/// <param name="Action">动作。</param>
/// <param name="Priority">优先级。</param>
/// <param name="EffectiveAt">生效时间。</param>
/// <param name="ExpiresAt">失效时间。</param>
/// <param name="Conditions">条件。</param>
public sealed record AccessPolicyView(Guid Id, string Code, string Name, AccessPolicyAction Action, int Priority, DateTimeOffset? EffectiveAt, DateTimeOffset? ExpiresAt, List<PolicyClauseView> Conditions)
{
    /// <summary>
    /// 某一时刻是否在有效期内。
    /// </summary>
    public bool IsEffectiveAt(DateTimeOffset now) => (EffectiveAt is null || EffectiveAt <= now) && (ExpiresAt is null || ExpiresAt > now);

    /// <summary>
    /// 条件转成条件引擎的条件集：按组号分组、组内按写入顺序。
    /// </summary>
    public ConditionSet ToSet()
        => new([.. Conditions.GroupBy(static c => c.GroupNo).OrderBy(static g => g.Key).Select(static g => new ConditionGroup(
            [.. g.Select(static c => new ConditionClause(c.AttributeName, c.Operator, c.Value, c.IsNegated))],
            g.Key.ToString(System.Globalization.CultureInfo.InvariantCulture)))]);
}

/// <summary>
/// 一侧启用的访问策略（单例）：放在共享缓存里 60 秒，写入即失效；按优先级（越小越先）与编码排列，有效期在判定时比较。
/// </summary>
/// <param name="cache">共享缓存。</param>
/// <param name="scopes">作用域工厂（缓存未命中时用只读工作单元查库）。</param>
public sealed class AccessPolicies(ISharedCache cache, IServiceScopeFactory scopes)
{
    private static readonly TimeSpan _timeToLive = TimeSpan.FromSeconds(60);

    /// <summary>
    /// 一侧启用的策略。
    /// </summary>
    public async Task<IReadOnlyList<AccessPolicyView>> OfAsync(string owner, CancellationToken cancellationToken)
    {
        var raw = await cache.GetOrCreateAsync(KeyOf(owner), ct => LoadAsync(owner, ct), CacheEntryOptions.Expires(_timeToLive), cancellationToken).ConfigureAwait(false);
        return raw.Length == 0 ? [] : JsonSerializer.Deserialize(raw, AccessJsonContext.Default.ListAccessPolicyView) ?? [];
    }

    /// <summary>
    /// 一侧的策略失效。
    /// </summary>
    public async Task InvalidateAsync(string owner, CancellationToken cancellationToken)
        => await cache.RemoveAsync(KeyOf(owner), cancellationToken).ConfigureAwait(false);

    private static CacheKey KeyOf(string owner) => new($"access:policies:{owner}");

    private async ValueTask<string> LoadAsync(string owner, CancellationToken cancellationToken)
        => await StoreScope.ReadAsync(scopes, async (services, ct) =>
        {
            var policies = await services.GetRequiredService<IRepository<SysAccessPolicy, Guid>>()
                .ListAsync(EntityQuery.All<SysAccessPolicy>().Where(new Specification<SysAccessPolicy>(p => p.OwnerTenantId == owner && p.Status == AccessPolicyStatus.Active)), ct)
                .ConfigureAwait(false);
            if (policies.Count == 0)
            {
                return string.Empty;
            }

            var ids = policies.Select(static p => p.Id).ToList();
            var conditions = (await services.GetRequiredService<IRepository<SysAccessPolicyCondition, Guid>>()
                    .ListAsync(EntityQuery.All<SysAccessPolicyCondition>().Where(new Specification<SysAccessPolicyCondition>(c => c.OwnerTenantId == owner && ids.Contains(c.PolicyId))), ct)
                    .ConfigureAwait(false))
                .ToLookup(static c => c.PolicyId);
            var views = policies
                .OrderBy(static p => p.Priority).ThenBy(static p => p.Code, StringComparer.Ordinal)
                .Select(p => new AccessPolicyView(p.Id, p.Code, p.Name, p.Action, p.Priority, p.EffectiveAt, p.ExpiresAt, [.. conditions[p.Id].OrderBy(static c => c.Ordinal).Select(static c => new PolicyClauseView(c.GroupNo, c.AttributeName, c.Operator, c.IsNegated, c.Value))]))
                .ToList();
            return JsonSerializer.Serialize(views, AccessJsonContext.Default.ListAccessPolicyView);
        }, cancellationToken).ConfigureAwait(false);
}

/// <summary>
/// 一条命中的策略与它的时机。
/// </summary>
/// <param name="Policy">策略。</param>
/// <param name="Phase">时机：平台策略只引用认证前就取得到的属性即认证前，否则认证后。</param>
public sealed record AccessPolicyHit(AccessPolicyView Policy, ConditionHitPhase Phase);

/// <summary>
/// 一层（平台或租户）的判定。
/// </summary>
/// <param name="Hits">命中的策略（全部记命中）。</param>
/// <param name="Allowed">是否有放行命中（压过同层的拒绝）。</param>
/// <param name="BlockedBy">拦下这次调用的第一条拒绝策略；没有即不拦。</param>
public sealed record AccessPolicyLayer(IReadOnlyList<AccessPolicyHit> Hits, bool Allowed, AccessPolicyView? BlockedBy);

/// <summary>
/// 访问策略的求值（单例，4.x 的 AccessPolicyEvaluator）：逐条求值一层里此刻有效的策略，命中全部记下；同层有放行命中即不拒绝，否则第一条命中的拒绝拦下。
/// 编译结果按条件内容记住（策略改了内容就是新的一份）；编译不过的策略不命中。
/// </summary>
/// <param name="compiler">条件编译器。</param>
/// <param name="evaluator">条件求值器。</param>
/// <param name="options">授权包配置（时区）。</param>
public sealed class AccessPolicyEvaluator(IConditionCompiler compiler, IConditionEvaluator evaluator, AccessOptions options)
{
    private const int MemoLimit = 1000;
    private readonly ConcurrentDictionary<string, CompiledConditionSet> _compiled = new(StringComparer.Ordinal);

    /// <summary>
    /// 求值一层。
    /// </summary>
    /// <param name="policies">这一层启用的策略（已排好）。</param>
    /// <param name="source">请求属性。</param>
    /// <param name="now">判定时刻。</param>
    /// <param name="platform">是不是平台层（只有平台层分认证前时机）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task<AccessPolicyLayer> EvaluateAsync(IReadOnlyList<AccessPolicyView> policies, IConditionAttributeSource source, DateTimeOffset now, bool platform, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(policies);
        ArgumentNullException.ThrowIfNull(source);
        var hits = new List<AccessPolicyHit>();
        foreach (var policy in policies)
        {
            if (!policy.IsEffectiveAt(now))
            {
                continue;
            }

            var compiled = Compile(policy);
            if (compiled.Errors.Count > 0 || compiled.IsEmpty)
            {
                continue;
            }

            var result = await evaluator.EvaluateAsync(compiled, source, explain: false, cancellationToken).ConfigureAwait(false);
            if (result.IsMatched)
            {
                hits.Add(new AccessPolicyHit(policy, PhaseOf(compiled, platform)));
            }
        }

        var allowed = hits.Any(static h => h.Policy.Action == AccessPolicyAction.Allow);
        var blockedBy = allowed ? null : hits.FirstOrDefault(static h => h.Policy.Action == AccessPolicyAction.Deny)?.Policy;
        return new AccessPolicyLayer(hits, allowed, blockedBy);
    }

    /// <summary>
    /// 编译一条策略（按内容记住）。
    /// </summary>
    public CompiledConditionSet Compile(AccessPolicyView policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        var signature = JsonSerializer.Serialize(policy.Conditions, AccessJsonContext.Default.ListPolicyClauseView);
        if (_compiled.Count > MemoLimit)
        {
            _compiled.Clear();
        }

        return _compiled.GetOrAdd(signature, _ => compiler.Compile(policy.ToSet(), new ConditionCompileOptions { TimeZone = options.ConditionTimeZone }));
    }

    /// <summary>
    /// 时机：平台策略只引用认证前就取得到的属性即认证前，否则认证后；租户策略一律认证后。
    /// </summary>
    public static ConditionHitPhase PhaseOf(CompiledConditionSet compiled, bool platform)
    {
        ArgumentNullException.ThrowIfNull(compiled);
        return platform && compiled.ReferencedAttributes.All(static a => AccessConditionAttributes.BeforeAuthentication.Contains(a))
            ? ConditionHitPhase.BeforeAuthentication
            : ConditionHitPhase.AfterAuthentication;
    }
}
