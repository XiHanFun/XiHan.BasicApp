// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.BasicApp.Tenancy.Domain.Entities;
using XiHan.Framework.Caching.Abstractions.Caches;
using XiHan.Framework.Caching.Abstractions.Stores;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;

namespace XiHan.BasicApp.Tenancy.Infrastructure;

/// <summary>
/// 套餐门控（单例，与框架权限检查器同寿命）：租户 → 套餐 → 白名单。在权限判定的热路径上，所以结果进共享缓存（<see cref="CacheTimeToLive"/>），
/// 套餐白名单、租户换套餐、删套餐时由写路径主动失效，跨实例靠 TTL 收敛。读库经 <see cref="StoreScope"/> 在独立作用域里只读（判定时可能还没有工作单元）。
/// 语义：没有业务租户行（不经 Tenancy 开通的目录租户）或未绑定套餐即不受限；绑定的套餐不存在（悬空引用）按空白名单处理，宁可拒绝。
/// </summary>
public sealed class EditionGate : IEditionGate
{
    /// <summary>
    /// 缓存存活时间。
    /// </summary>
    public static readonly TimeSpan CacheTimeToLive = TimeSpan.FromSeconds(30);

    // 缓存值：不受限记成这个标记（不是合法权限码），否则是空格分隔的权限码（空串即空白名单）
    private const string Unrestricted = "*";

    private readonly ISharedCache _cache;
    private readonly IServiceScopeFactory _scopes;

    /// <summary>
    /// 创建门控。
    /// </summary>
    /// <param name="cache">共享缓存。</param>
    /// <param name="scopes">作用域工厂（只读加载用）。</param>
    public EditionGate(ISharedCache cache, IServiceScopeFactory scopes)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlySet<string>?> AllowedAsync(string tenantId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        var value = await _cache.GetOrCreateAsync(KeyOf(tenantId), ct => LoadAsync(tenantId, ct), CacheEntryOptions.Expires(CacheTimeToLive), cancellationToken).ConfigureAwait(false);
        return string.Equals(value, Unrestricted, StringComparison.Ordinal)
            ? null
            : value.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>
    /// 失效这些租户的门控缓存（换套餐、改白名单、删套餐之后调用）。
    /// </summary>
    public async ValueTask InvalidateAsync(IEnumerable<string> tenantIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tenantIds);
        var keys = tenantIds.Distinct(StringComparer.Ordinal).Select(KeyOf).ToList();
        if (keys.Count > 0)
        {
            await _cache.RemoveManyAsync(keys, cancellationToken).ConfigureAwait(false);
        }
    }

    private static CacheKey KeyOf(string tenantId) => new($"tenancy:edition-gate:{tenantId}");

    private async ValueTask<string> LoadAsync(string tenantId, CancellationToken cancellationToken)
    {
        return await StoreScope.ReadAsync(_scopes, async (services, ct) =>
        {
            var tenants = services.GetRequiredService<IRepository<SysTenant, Guid>>();
            var rows = await tenants.ListAsync(EntityQuery.All<SysTenant>().Where(new Specification<SysTenant>(t => t.TenantId == tenantId)).Page(0, 1), ct).ConfigureAwait(false);
            if (rows.Count == 0 || rows[0].EditionId is not { } editionId)
            {
                return Unrestricted;
            }

            if (!(await services.GetRequiredService<IRepository<SysEdition, Guid>>().FindAsync(editionId, ct).ConfigureAwait(false)).HasValue)
            {
                return string.Empty;
            }

            var whitelist = await services.GetRequiredService<IRepository<SysEditionGrant, Guid>>()
                .ListAsync(EntityQuery.All<SysEditionGrant>().Where(new Specification<SysEditionGrant>(p => p.EditionId == editionId)), ct).ConfigureAwait(false);
            return string.Join(' ', whitelist.Select(static p => p.PermissionCode).Order(StringComparer.Ordinal));
        }, cancellationToken).ConfigureAwait(false);
    }
}
