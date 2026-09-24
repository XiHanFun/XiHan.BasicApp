// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Extensions;

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

        // 清租户过滤后按 TenantId 精确匹配：要查的租户未必是当前上下文
        return await CreateNoTenantQueryable()
            .Where(user => user.TenantId == tenantId)
            .Where(user => user.UserId == userId)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 账号在所有租户的成员关系（任意状态，跨租户读取）
    /// </summary>
    public async Task<IReadOnlyList<SysTenantUser>> GetAllByUserIdIgnoreTenantAsync(long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateNoTenantQueryable()
            .Where(member => member.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 按关键字检索租户成员，返回命中成员的用户主键
    /// </summary>
    public async Task<IReadOnlyList<long>> SearchMemberUserIdsAsync(long tenantId, string keyword, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyword);
        cancellationToken.ThrowIfCancellationRequested();

        var text = keyword.Trim();
        var members = await CreateNoTenantQueryable()
            .Where(member => member.TenantId == tenantId)
            .Select(member => new { member.UserId, member.DisplayName, member.InviteRemark, member.Remark })
            .ToListAsync(cancellationToken);
        if (members.Count == 0)
        {
            return [];
        }

        // 账号属于来源租户（外部成员不在本租户），跨租户按主键取
        var userIds = members.Select(member => member.UserId).Distinct().ToList();
        var accounts = await DbClient.Queryable<SysUser>()
            .ClearTenantFilter()
            .Where(user => userIds.Contains(user.BasicId))
            .Select(user => new { user.BasicId, user.UserName, user.NickName, user.RealName, user.Email })
            .ToListAsync(cancellationToken);

        var matched = members
            .Where(member => Matches(text, member.DisplayName, member.InviteRemark, member.Remark))
            .Select(member => member.UserId)
            .Concat(accounts
                .Where(account => Matches(text, account.UserName, account.NickName, account.RealName, account.Email))
                .Select(account => account.BasicId));
        return [.. matched.Distinct()];
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

        // 清租户过滤后按 TenantId 精确匹配：一次统计多个租户，不依赖当前上下文。
        // 生效期口径与 GetActiveByUserIdAsync 的鉴权口径保持一致。
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

    /// <summary>
    /// 任一文本包含关键字（忽略大小写）
    /// </summary>
    private static bool Matches(string keyword, params string?[] values)
    {
        return values.Any(value => value?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true);
    }
}
