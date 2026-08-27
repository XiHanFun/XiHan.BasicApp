// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 租户成员仓储实现
/// </summary>
public sealed class TenantUserRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysTenantUser>(clientResolver), ITenantUserRepository
{
    /// <summary>
    /// 获取用户可进入的租户成员关系
    /// </summary>
    public async Task<IReadOnlyList<SysTenantUser>> GetActiveByUserIdAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateNoTenantQueryable()
            .Where(user => user.UserId == userId)
            .Where(user => user.InviteStatus == TenantMemberInviteStatus.Accepted)
            .Where(user => user.Status == ValidityStatus.Valid)
            .Where(user => user.EffectiveTime == null || user.EffectiveTime <= now)
            .Where(user => user.ExpirationTime == null || user.ExpirationTime > now)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取指定租户成员关系
    /// </summary>
    public async Task<SysTenantUser?> GetMembershipAsync(long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(user => user.UserId == userId)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 获取指定租户成员关系
    /// </summary>
    public async Task<SysTenantUser?> GetMembershipAsync(long tenantId, long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 清租户过滤后按 TenantId 精确匹配：平台态查某个租户的成员，读共享过滤器会把 TenantId=0 的行一并放行
        return await CreateNoTenantQueryable()
            .Where(user => user.TenantId == tenantId)
            .Where(user => user.UserId == userId)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 统计指定租户已占用的席位数
    /// </summary>
    public async Task<IReadOnlyDictionary<long, long>> CountActiveMembersByTenantIdsAsync(IReadOnlyCollection<long> tenantIds, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenantIds);
        cancellationToken.ThrowIfCancellationRequested();

        if (tenantIds.Count == 0)
        {
            return new Dictionary<long, long>();
        }

        var ids = tenantIds.Distinct().ToList();

        // 清租户过滤后按 TenantId 精确匹配：读共享过滤器会放行 TenantId=0 的平台级成员，
        // 依赖它会把平台账号计进每个租户的席位。生效期口径与 GetActiveByUserIdAsync 的鉴权口径保持一致。
        var rows = await CreateNoTenantQueryable()
            .Where(user => ids.Contains(user.TenantId))
            .Where(user => user.InviteStatus == TenantMemberInviteStatus.Accepted)
            .Where(user => user.Status == ValidityStatus.Valid)
            .Where(user => user.MemberType != TenantMemberType.PlatformAdmin)
            .Where(user => user.EffectiveTime == null || user.EffectiveTime <= now)
            .Where(user => user.ExpirationTime == null || user.ExpirationTime > now)
            .GroupBy(user => user.TenantId)
            .Select(user => new TenantUsageRow { TenantId = user.TenantId, Value = SqlFunc.AggregateCount(user.UserId) })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(row => row.TenantId, row => row.Value);
    }
}
