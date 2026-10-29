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
/// 一条指派的判定视图。
/// </summary>
/// <param name="RoleId">角色。</param>
/// <param name="EffectiveAt">生效时间。</param>
/// <param name="ExpiresAt">到期时间（不含）。</param>
/// <param name="RoleActive">角色是否启用。</param>
public sealed record RoleAssignmentView(Guid RoleId, DateTimeOffset? EffectiveAt, DateTimeOffset? ExpiresAt, bool RoleActive)
{
    /// <summary>
    /// 此刻这条指派是否算数：角色启用且在有效期内。
    /// </summary>
    public bool HoldsAt(DateTimeOffset now) => RoleActive && (EffectiveAt is null || EffectiveAt <= now) && (ExpiresAt is null || ExpiresAt > now);
}

/// <summary>
/// 用户在一侧的角色指派（单例）。
/// </summary>
/// <remarks>
/// 按（侧、用户）缓存在共享缓存里，指派变化时由写入方失效；角色停用经缓存存活期（一分钟）传到判定。
/// 令牌里的角色声明是签发那一刻的快照：权限过滤器拿它与这里的指派求交，到期、被解除或停用的角色立即不再算数。
/// </remarks>
/// <param name="cache">共享缓存。</param>
/// <param name="scopes">作用域工厂。</param>
public sealed class RoleAssignments(ISharedCache cache, IServiceScopeFactory scopes)
{
    private static readonly TimeSpan _timeToLive = TimeSpan.FromSeconds(60);

    /// <summary>
    /// 用户在一侧的全部指派。
    /// </summary>
    public async Task<IReadOnlyList<RoleAssignmentView>> OfAsync(string owner, Guid userId, CancellationToken cancellationToken)
    {
        var raw = await cache.GetOrCreateAsync(KeyOf(owner, userId), ct => LoadAsync(owner, userId, ct), CacheEntryOptions.Expires(_timeToLive), cancellationToken).ConfigureAwait(false);
        if (raw.Length == 0)
        {
            return [];
        }

        return raw.Split('\n').Select(static line => line.Split('\t')).Select(static parts => new RoleAssignmentView(
            Guid.ParseExact(parts[0], "N"),
            DecodeTime(parts[1]),
            DecodeTime(parts[2]),
            parts[3] == "1")).ToList();
    }

    /// <summary>
    /// 用户的指派有变：失效缓存。
    /// </summary>
    public async Task InvalidateAsync(string owner, Guid userId, CancellationToken cancellationToken)
        => await cache.RemoveAsync(KeyOf(owner, userId), cancellationToken).ConfigureAwait(false);

    private static CacheKey KeyOf(string owner, Guid userId) => new($"access:assignments:{owner}:{userId:N}");

    private async ValueTask<string> LoadAsync(string owner, Guid userId, CancellationToken cancellationToken)
        => await StoreScope.ReadAsync(scopes, async (services, ct) =>
        {
            var rows = await services.GetRequiredService<IRepository<SysUserRole, Guid>>()
                .ListAsync(EntityQuery.All<SysUserRole>().Where(new Specification<SysUserRole>(a => a.UserId == userId && a.OwnerTenantId == owner)), ct)
                .ConfigureAwait(false);
            if (rows.Count == 0)
            {
                return string.Empty;
            }

            var active = (await services.GetRequiredService<IRepository<SysRole, Guid>>().FindManyAsync(rows.Select(static a => a.RoleId).Distinct().ToList(), ct).ConfigureAwait(false))
                .Where(static r => r.Status == RoleStatus.Active)
                .Select(static r => r.Id)
                .ToHashSet();
            return string.Join('\n', rows.Select(a => string.Join('\t', a.RoleId.ToString("N"), Encode(a.EffectiveAt), Encode(a.ExpiresAt), active.Contains(a.RoleId) ? "1" : "0")));
        }, cancellationToken).ConfigureAwait(false);

    private static string Encode(DateTimeOffset? value) => value is { } at ? at.UtcTicks.ToString(CultureInfo.InvariantCulture) : string.Empty;

    private static DateTimeOffset? DecodeTime(string value)
        => value.Length == 0 ? null : new DateTimeOffset(long.Parse(value, NumberStyles.None, CultureInfo.InvariantCulture), TimeSpan.Zero);
}
