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
using XiHan.Framework.Security.Abstractions.Conditions;

namespace XiHan.BasicApp.Access.Infrastructure;

/// <summary>
/// 一个主体上一条启用的条件子句（判定所需的部分）。
/// </summary>
/// <param name="Id">子句 ID。</param>
/// <param name="Permission">权限码。</param>
/// <param name="Action">挂在授予还是拒绝上。</param>
/// <param name="GroupNo">组号。</param>
/// <param name="AttributeName">属性名。</param>
/// <param name="Operator">运算符。</param>
/// <param name="IsNegated">是否取反。</param>
/// <param name="Value">条件值。</param>
public sealed record ConditionClauseView(Guid Id, string Permission, PermissionAction Action, int GroupNo, string AttributeName, ConditionOperator Operator, bool IsNegated, string? Value)
{
    /// <summary>
    /// 把一组子句（同一绑定）转成条件引擎的条件集。
    /// </summary>
    /// <remarks>
    /// 按组号分组、组内按写入顺序，子句键为子句 ID。
    /// </remarks>
    public static ConditionSet ToSet(IEnumerable<ConditionClauseView> clauses)
        => new([.. clauses.GroupBy(static c => c.GroupNo).OrderBy(static g => g.Key).Select(static g => new ConditionGroup(
            [.. g.Select(static c => new ConditionClause(c.AttributeName, c.Operator, c.Value, c.IsNegated, c.Id.ToString("N")))],
            g.Key.ToString(System.Globalization.CultureInfo.InvariantCulture)))]);
}

/// <summary>
/// 主体上启用的条件子句（单例）。
/// </summary>
/// <remarks>
/// 按（侧、主体类别、主体）放在共享缓存里 60 秒，写入即失效。
/// </remarks>
/// <param name="cache">共享缓存。</param>
/// <param name="scopes">作用域工厂（缓存未命中时用只读工作单元查库）。</param>
public sealed class PermissionConditions(ISharedCache cache, IServiceScopeFactory scopes)
{
    private static readonly TimeSpan _timeToLive = TimeSpan.FromSeconds(60);

    /// <summary>
    /// 主体上启用的子句。
    /// </summary>
    public async Task<IReadOnlyList<ConditionClauseView>> OfAsync(string owner, GrantSubjectKind kind, Guid subjectId, CancellationToken cancellationToken)
    {
        var raw = await cache.GetOrCreateAsync(KeyOf(owner, kind, subjectId), ct => LoadAsync(owner, kind, subjectId, ct), CacheEntryOptions.Expires(_timeToLive), cancellationToken).ConfigureAwait(false);
        return raw.Length == 0 ? [] : JsonSerializer.Deserialize(raw, AccessJsonContext.Default.ListConditionClauseView) ?? [];
    }

    /// <summary>
    /// 主体上的子句失效。
    /// </summary>
    public async Task InvalidateAsync(string owner, GrantSubjectKind kind, Guid subjectId, CancellationToken cancellationToken)
        => await cache.RemoveAsync(KeyOf(owner, kind, subjectId), cancellationToken).ConfigureAwait(false);

    private static CacheKey KeyOf(string owner, GrantSubjectKind kind, Guid subjectId) => new($"access:conditions:{owner}:{kind}:{subjectId:N}");

    private async ValueTask<string> LoadAsync(string owner, GrantSubjectKind kind, Guid subjectId, CancellationToken cancellationToken)
        => await StoreScope.ReadAsync(scopes, async (services, ct) =>
        {
            var rows = await services.GetRequiredService<IRepository<SysPermissionCondition, Guid>>()
                .ListAsync(EntityQuery.All<SysPermissionCondition>().Where(new Specification<SysPermissionCondition>(c => c.OwnerTenantId == owner && c.SubjectKind == kind && c.SubjectId == subjectId && c.Status == ConditionStatus.Active)), ct)
                .ConfigureAwait(false);
            if (rows.Count == 0)
            {
                return string.Empty;
            }

            var views = rows.OrderBy(static c => c.CreatedAt).Select(static c => new ConditionClauseView(c.Id, c.Permission, c.Action, c.GroupNo, c.AttributeName, c.Operator, c.IsNegated, c.Value)).ToList();
            return JsonSerializer.Serialize(views, AccessJsonContext.Default.ListConditionClauseView);
        }, cancellationToken).ConfigureAwait(false);
}
