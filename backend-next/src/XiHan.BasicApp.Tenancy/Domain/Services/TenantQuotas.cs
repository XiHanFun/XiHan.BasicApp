// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Files;
using XiHan.BasicApp.Contracts.MultiTenancy;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Tenancy.Domain.Entities;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Tenancy.Domain.Services;

/// <summary>
/// 一个租户的配额与用量。
/// </summary>
/// <param name="Tenant">租户。</param>
/// <param name="Edition">套餐；没有为空。</param>
/// <param name="UserLimit">有效的席位上限（租户自己的，没有取套餐的）；为空不限。</param>
/// <param name="UsedSeats">已用席位（注册地是该租户的用户与租户成员）。</param>
/// <param name="StorageLimitMb">有效的存储上限（MB）；为空不限。</param>
/// <param name="UsedStorageBytes">已用存储（字节）。</param>
public sealed record TenantQuotaView(SysTenant Tenant, SysEdition? Edition, int? UserLimit, long UsedSeats, long? StorageLimitMb, long UsedStorageBytes)
{
    /// <summary>
    /// 每 MB 的字节数。
    /// </summary>
    public const long BytesPerMb = 1024L * 1024;

    /// <summary>
    /// 席位是否超额。
    /// </summary>
    public bool IsOverSeats => UserLimit is { } limit && UsedSeats > limit;

    /// <summary>
    /// 存储是否超额。
    /// </summary>
    public bool IsOverStorage => StorageLimitMb is { } limit && UsedStorageBytes > limit * BytesPerMb;
}

/// <summary>
/// 租户配额（4.x 同）：有效上限 = 租户自己的 → 套餐的 → 不限；席位按注册地是该租户的用户（未删除）加租户成员计，存储按 Files 报的用量计（没装 Files 即 0）。
/// 只约束租户主体的调用，平台主体代租户办事不受限；已经超额的租户只在审计里报告。
/// </summary>
/// <param name="tenants">租户仓储。</param>
/// <param name="editions">套餐仓储。</param>
/// <param name="unitOfWork">工作单元（分组计数）。</param>
/// <param name="usage">存储用量（Files 实现）。</param>
public sealed class TenantQuotas(IRepository<SysTenant, Guid> tenants, IRepository<SysEdition, Guid> editions, SqlSugarUnitOfWork unitOfWork, IEnumerable<IStorageUsage> usage) : ITenantQuota
{
    /// <inheritdoc />
    public async Task<Result> EnsureStorageAsync(InvocationContext context, string tenantId, long additionalBytes)
    {
        ArgumentNullException.ThrowIfNull(context);
        var view = await ConstrainedAsync(context, tenantId).ConfigureAwait(false);
        if (view is not { StorageLimitMb: { } limit } || view.UsedStorageBytes + additionalBytes <= limit * TenantQuotaView.BytesPerMb)
        {
            return Result.Success();
        }

        return Error.Conflict(TenancyErrors.StorageQuotaExceeded, TenancyMessages.StorageQuotaExceeded, MessageArguments.Of(
            ("used", MbOf(view.UsedStorageBytes)), ("limit", limit), ("needed", MbOf(additionalBytes))));
    }

    /// <inheritdoc />
    public async Task<Result> EnsureSeatsAsync(InvocationContext context, string tenantId, int additionalSeats)
    {
        ArgumentNullException.ThrowIfNull(context);
        var view = await ConstrainedAsync(context, tenantId).ConfigureAwait(false);
        if (view is not { UserLimit: { } limit } || view.UsedSeats + additionalSeats <= limit)
        {
            return Result.Success();
        }

        return Error.Conflict(TenancyErrors.SeatQuotaExceeded, TenancyMessages.SeatQuotaExceeded, MessageArguments.Of(("used", view.UsedSeats), ("limit", limit)));
    }

    /// <summary>
    /// 一批租户的配额与用量（顺序同入参）。
    /// </summary>
    /// <param name="list">租户。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>配额与用量。</returns>
    public async Task<IReadOnlyList<TenantQuotaView>> ViewsAsync(IReadOnlyList<SysTenant> list, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(list);
        if (list.Count == 0)
        {
            return [];
        }

        var ids = list.Select(static t => t.TenantId).ToList();
        var editionIds = list.Where(static t => t.EditionId.HasValue).Select(static t => t.EditionId!.Value).Distinct().ToList();
        var known = editionIds.Count == 0
            ? []
            : (await editions.ListAsync(EntityQuery.All<SysEdition>().Where(new Specification<SysEdition>(e => editionIds.Contains(e.Id))), cancellationToken).ConfigureAwait(false)).ToDictionary(static e => e.Id);
        var users = await unitOfWork.ConnectionFor(typeof(SysUser)).Queryable<SysUser>()
            .Where(u => ids.Contains(u.HomeTenantId) && !u.IsDeleted)
            .GroupBy(u => u.HomeTenantId)
            .Select(u => new { Tenant = u.HomeTenantId, Count = SqlFunc.AggregateCount(u.Id) })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var members = await unitOfWork.ConnectionFor(typeof(SysTenantMember)).Queryable<SysTenantMember>()
            .Where(m => ids.Contains(m.TenantId) && !m.IsDeleted)
            .GroupBy(m => m.TenantId)
            .Select(m => new { Tenant = m.TenantId, Count = SqlFunc.AggregateCount(m.Id) })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var seats = users.Concat(members).GroupBy(static c => c.Tenant).ToDictionary(static g => g.Key, static g => g.Sum(static c => (long)c.Count), StringComparer.Ordinal);
        var storage = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (var source in usage)
        {
            foreach (var (tenant, bytes) in await source.UsedBytesAsync(ids, cancellationToken).ConfigureAwait(false))
            {
                storage[tenant] = storage.GetValueOrDefault(tenant) + bytes;
            }
        }

        return [.. list.Select(t =>
        {
            var edition = t.EditionId is { } id ? known.GetValueOrDefault(id) : null;
            return new TenantQuotaView(t, edition, t.UserLimit ?? edition?.UserLimit, seats.GetValueOrDefault(t.TenantId), t.StorageLimitMb ?? edition?.StorageLimitMb, storage.GetValueOrDefault(t.TenantId));
        })];
    }

    private static double MbOf(long bytes) => Math.Round(bytes / (double)TenantQuotaView.BytesPerMb, 2);

    /// <summary>
    /// 要约束的租户的配额：平台主体的调用、平台一侧与目录里没有的租户为空（不约束）。
    /// </summary>
    private async Task<TenantQuotaView?> ConstrainedAsync(InvocationContext context, string tenantId)
    {
        if (context.GetSecurity().Tenant is null || string.Equals(tenantId, TenantIds.Platform, StringComparison.Ordinal))
        {
            return null;
        }

        var found = await tenants.ListAsync(EntityQuery.All<SysTenant>().Where(new Specification<SysTenant>(t => t.TenantId == tenantId)).Page(0, 1), context.CancellationToken).ConfigureAwait(false);
        return found.Count == 0 ? null : (await ViewsAsync(found, context.CancellationToken).ConfigureAwait(false))[0];
    }
}
