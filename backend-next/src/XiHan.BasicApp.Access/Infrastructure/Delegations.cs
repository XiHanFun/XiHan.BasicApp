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
/// 被委托人名下一条未撤销的委托（判定所需的部分）。
/// </summary>
/// <param name="DelegatorId">委托人。</param>
/// <param name="Permission">委托的权限码。</param>
/// <param name="RoleId">委托的角色。</param>
/// <param name="EffectiveAt">生效时间。</param>
/// <param name="ExpiresAt">到期时间。</param>
public sealed record DelegationView(Guid DelegatorId, string? Permission, Guid? RoleId, DateTimeOffset? EffectiveAt, DateTimeOffset ExpiresAt)
{
    /// <summary>
    /// 某一时刻是否在有效期内。
    /// </summary>
    public bool IsEffectiveAt(DateTimeOffset now) => (EffectiveAt is null || EffectiveAt <= now) && ExpiresAt > now;
}

/// <summary>
/// 被委托人名下未撤销、未到期的委托（单例）：按（侧、被委托人）放在共享缓存里 60 秒，新建、撤销与删除时失效；有效期在判定时比较。
/// </summary>
/// <param name="cache">共享缓存。</param>
/// <param name="scopes">作用域工厂（缓存未命中时用只读工作单元查库）。</param>
/// <param name="clock">时间源。</param>
public sealed class Delegations(ISharedCache cache, IServiceScopeFactory scopes, TimeProvider clock)
{
    private static readonly TimeSpan _timeToLive = TimeSpan.FromSeconds(60);

    /// <summary>
    /// 被委托人名下的委托。
    /// </summary>
    public async Task<IReadOnlyList<DelegationView>> OfAsync(string owner, Guid delegateeId, CancellationToken cancellationToken)
    {
        var raw = await cache.GetOrCreateAsync(KeyOf(owner, delegateeId), ct => LoadAsync(owner, delegateeId, ct), CacheEntryOptions.Expires(_timeToLive), cancellationToken).ConfigureAwait(false);
        return raw.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(static line => line.Split('\t')).Select(static parts => new DelegationView(
            Guid.ParseExact(parts[0], "N"),
            parts[1].Length == 0 ? null : parts[1],
            parts[2].Length == 0 ? null : Guid.ParseExact(parts[2], "N"),
            parts[3].Length == 0 ? null : FromTicks(parts[3]),
            FromTicks(parts[4]))).ToList();
    }

    /// <summary>
    /// 被委托人的委托失效。
    /// </summary>
    public async Task InvalidateAsync(string owner, Guid delegateeId, CancellationToken cancellationToken)
        => await cache.RemoveAsync(KeyOf(owner, delegateeId), cancellationToken).ConfigureAwait(false);

    private static CacheKey KeyOf(string owner, Guid delegateeId) => new($"access:delegations:{owner}:{delegateeId:N}");

    private async ValueTask<string> LoadAsync(string owner, Guid delegateeId, CancellationToken cancellationToken)
        => await StoreScope.ReadAsync(scopes, async (services, ct) =>
        {
            var now = clock.GetUtcNow();
            var rows = await services.GetRequiredService<IRepository<SysPermissionDelegation, Guid>>()
                .ListAsync(EntityQuery.All<SysPermissionDelegation>().Where(new Specification<SysPermissionDelegation>(d => d.OwnerTenantId == owner && d.DelegateeId == delegateeId && !d.RevokedAt.HasValue && d.ExpiresAt > now)), ct)
                .ConfigureAwait(false);
            return string.Join('\n', rows.Select(static d => string.Join('\t',
                d.DelegatorId.ToString("N"),
                d.Permission ?? string.Empty,
                d.RoleId is { } roleId ? roleId.ToString("N") : string.Empty,
                d.EffectiveAt is { } from ? Ticks(from) : string.Empty,
                Ticks(d.ExpiresAt))));
        }, cancellationToken).ConfigureAwait(false);

    private static string Ticks(DateTimeOffset value) => value.UtcTicks.ToString(CultureInfo.InvariantCulture);

    private static DateTimeOffset FromTicks(string value) => new(long.Parse(value, NumberStyles.None, CultureInfo.InvariantCulture), TimeSpan.Zero);
}
