// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
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
/// 一条授予规则的判定视图。
/// </summary>
/// <param name="Permission">权限码（或 <c>*</c>）。</param>
/// <param name="Action">授予还是拒绝。</param>
/// <param name="EffectiveAt">生效时间。</param>
/// <param name="ExpiresAt">到期时间（不含）。</param>
public sealed record PermissionRuleView(string Permission, PermissionAction Action, DateTimeOffset? EffectiveAt, DateTimeOffset? ExpiresAt)
{
    /// <summary>
    /// 此刻是否在有效期内。
    /// </summary>
    public bool IsEffectiveAt(DateTimeOffset now) => (EffectiveAt is null || EffectiveAt <= now) && (ExpiresAt is null || ExpiresAt > now);
}

/// <summary>
/// 授予规则的读取（单例）：按（侧、主体）缓存在共享缓存里，写入后由写入方失效；有效期在判定时按当前时间比较，到点生效与到期不必重建缓存。
/// 缓存存成文本行，不依赖缓存的对象序列化。
/// </summary>
/// <param name="cache">共享缓存。</param>
/// <param name="scopes">作用域工厂（在独立的只读作用域里读仓储）。</param>
public sealed class PermissionRules(ISharedCache cache, IServiceScopeFactory scopes)
{
    private static readonly TimeSpan _timeToLive = TimeSpan.FromSeconds(60);

    /// <summary>
    /// 一个主体在一侧的全部规则。
    /// </summary>
    public async Task<IReadOnlyList<PermissionRuleView>> OfAsync(string owner, GrantSubjectKind kind, Guid subjectId, CancellationToken cancellationToken)
    {
        var raw = await cache.GetOrCreateAsync(KeyOf(owner, kind, subjectId), ct => LoadAsync(owner, kind, subjectId, ct), CacheEntryOptions.Expires(_timeToLive), cancellationToken).ConfigureAwait(false);
        return Decode(raw);
    }

    /// <summary>
    /// 主体的规则有变：失效缓存。
    /// </summary>
    public async Task InvalidateAsync(string owner, GrantSubjectKind kind, Guid subjectId, CancellationToken cancellationToken)
        => await cache.RemoveAsync(KeyOf(owner, kind, subjectId), cancellationToken).ConfigureAwait(false);

    private static CacheKey KeyOf(string owner, GrantSubjectKind kind, Guid subjectId) => new($"access:rules:{owner}:{kind}:{subjectId:N}");

    private async ValueTask<string> LoadAsync(string owner, GrantSubjectKind kind, Guid subjectId, CancellationToken cancellationToken)
        => await StoreScope.ReadAsync(scopes, async (services, ct) =>
        {
            var rows = await services.GetRequiredService<IRepository<SysPermissionRule, Guid>>()
                .ListAsync(EntityQuery.All<SysPermissionRule>().Where(new Specification<SysPermissionRule>(r => r.OwnerTenantId == owner && r.SubjectKind == kind && r.SubjectId == subjectId)), ct)
                .ConfigureAwait(false);
            return string.Join('\n', rows.Select(static r => string.Join('\t', r.Permission, ((int)r.Action).ToString(CultureInfo.InvariantCulture), Encode(r.EffectiveAt), Encode(r.ExpiresAt))));
        }, cancellationToken).ConfigureAwait(false);

    private static string Encode(DateTimeOffset? value) => value is { } at ? at.UtcTicks.ToString(CultureInfo.InvariantCulture) : string.Empty;

    private static DateTimeOffset? DecodeTime(string value)
        => value.Length == 0 ? null : new DateTimeOffset(long.Parse(value, NumberStyles.None, CultureInfo.InvariantCulture), TimeSpan.Zero);

    private static List<PermissionRuleView> Decode(string raw)
    {
        if (raw.Length == 0)
        {
            return [];
        }

        return raw.Split('\n').Select(static line => line.Split('\t')).Select(static parts => new PermissionRuleView(
            parts[0],
            (PermissionAction)int.Parse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture),
            DecodeTime(parts[2]),
            DecodeTime(parts[3]))).ToList();
    }
}
