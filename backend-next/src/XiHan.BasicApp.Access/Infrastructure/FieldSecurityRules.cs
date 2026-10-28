// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Caching.Abstractions.Caches;
using XiHan.Framework.Caching.Abstractions.Stores;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;

namespace XiHan.BasicApp.Access.Infrastructure;

/// <summary>
/// 一条启用的字段安全规则的判定视图。
/// </summary>
/// <param name="TargetKind">目标类型。</param>
/// <param name="TargetId">目标 ID。</param>
/// <param name="Field">字段名。</param>
/// <param name="Strategy">读取方式。</param>
/// <param name="KeepHead">部分脱敏保留前几位。</param>
/// <param name="KeepTail">部分脱敏保留后几位。</param>
/// <param name="Replacement">固定文本。</param>
/// <param name="IsEditable">能否改写。</param>
public sealed record FieldSecurityRuleView(FieldSecurityTargetKind TargetKind, Guid TargetId, string Field, FieldMaskStrategy Strategy, int? KeepHead, int? KeepTail, string? Replacement, bool IsEditable)
{
    /// <summary>
    /// 转成契约里的单条规则（合并前）。
    /// </summary>
    public FieldRule ToRule() => new(Field, Strategy, KeepHead, KeepTail, Replacement, IsEditable);
}

/// <summary>
/// 字段安全规则的读取（单例）：按（侧、实体）缓存启用的规则，60 秒，写入后由写入方失效。缓存存成 JSON 文本（固定文本可含任意字符），按创建先后排好（合并时固定文本取第一条）。
/// </summary>
/// <param name="cache">共享缓存。</param>
/// <param name="scopes">作用域工厂（在独立的只读作用域里读仓储）。</param>
public sealed class FieldSecurityRules(ISharedCache cache, IServiceScopeFactory scopes)
{
    private static readonly TimeSpan _timeToLive = TimeSpan.FromSeconds(60);

    /// <summary>
    /// 一侧在一个实体上的全部启用规则。
    /// </summary>
    public async Task<IReadOnlyList<FieldSecurityRuleView>> OfAsync(string owner, string entity, CancellationToken cancellationToken)
    {
        var raw = await cache.GetOrCreateAsync(KeyOf(owner, entity), ct => LoadAsync(owner, entity, ct), CacheEntryOptions.Expires(_timeToLive), cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize(raw, AccessJsonContext.Default.ListFieldSecurityRuleView) ?? [];
    }

    /// <summary>
    /// 一侧在一个实体上的规则有变：失效缓存。
    /// </summary>
    public async Task InvalidateAsync(string owner, string entity, CancellationToken cancellationToken)
        => await cache.RemoveAsync(KeyOf(owner, entity), cancellationToken).ConfigureAwait(false);

    private static CacheKey KeyOf(string owner, string entity) => new($"access:field-rules:{owner}:{entity}");

    private async ValueTask<string> LoadAsync(string owner, string entity, CancellationToken cancellationToken)
        => await StoreScope.ReadAsync(scopes, async (services, ct) =>
        {
            var rows = await services.GetRequiredService<IRepository<SysFieldSecurityRule, Guid>>()
                .ListAsync(EntityQuery.All<SysFieldSecurityRule>().Where(new Specification<SysFieldSecurityRule>(r => r.OwnerTenantId == owner && r.Entity == entity && r.Status == FieldRuleStatus.Active)), ct)
                .ConfigureAwait(false);
            var views = rows.OrderBy(static r => r.CreatedAt).ThenBy(static r => r.Id)
                .Select(static r => new FieldSecurityRuleView(r.TargetKind, r.TargetId, r.Field, r.Strategy, r.KeepHead, r.KeepTail, r.Replacement, r.IsEditable))
                .ToList();
            return JsonSerializer.Serialize(views, AccessJsonContext.Default.ListFieldSecurityRuleView);
        }, cancellationToken).ConfigureAwait(false);
}
