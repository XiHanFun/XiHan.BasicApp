// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.MultiTenancy;
using XiHan.BasicApp.Contracts.Users;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.Framework.Persistence.SqlSugar;

namespace XiHan.BasicApp.Identity.Infrastructure;

/// <summary>
/// <see cref="IUserDirectory"/> 的实现。
/// </summary>
/// <remarks>
/// 在调用的工作单元里直接查用户表；按 ID 取摘要含已删除的，按一侧取在用账号的不含。
/// 租户一侧的普通成员来自 <see cref="ITenantMembershipResolver"/>（Tenancy 实现，没装即只有注册在该租户的账号）。
/// </remarks>
/// <param name="unitOfWork">工作单元。</param>
/// <param name="memberships">租户成员。</param>
public sealed class UserDirectory(SqlSugarUnitOfWork unitOfWork, IEnumerable<ITenantMembershipResolver> memberships) : IUserDirectory
{
    /// <summary>
    /// 搜索一次最多返回的账号数。
    /// </summary>
    public const int MaxSearchLimit = 200;

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, UserSummary>> FindAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userIds);
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, UserSummary>();
        }

        var rows = await unitOfWork.ConnectionFor(typeof(SysUser)).Queryable<SysUser>().Where(u => ids.Contains(u.Id)).ToListAsync(cancellationToken).ConfigureAwait(false);
        return rows.ToDictionary(static u => u.Id, static u => new UserSummary(u.Id, u.UserName, u.DisplayName, u.Email, u.PhoneNumber));
    }

    /// <inheritdoc />
    public async Task<IReadOnlySet<Guid>> ActiveInSideAsync(string side, IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(side);
        ArgumentNullException.ThrowIfNull(userIds);
        var ids = userIds.Where(static id => id != Guid.Empty).Distinct().ToList();
        if (ids.Count == 0)
        {
            return new HashSet<Guid>();
        }

        var members = await MembersAsync(side, cancellationToken).ConfigureAwait(false);
        var rows = await unitOfWork.ConnectionFor(typeof(SysUser)).Queryable<SysUser>()
            .Where(u => ids.Contains(u.Id) && !u.IsDeleted && u.Status == UserStatus.Active)
            .Select(static u => new { u.Id, u.HomeTenantId })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return rows.Where(u => string.Equals(u.HomeTenantId, side, StringComparison.Ordinal) || members.Contains(u.Id)).Select(static u => u.Id).ToHashSet();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserSummary>> SearchActiveInSideAsync(string side, string? keyword, int limit, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(side);
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(limit, MaxSearchLimit);
        var members = (await MembersAsync(side, cancellationToken).ConfigureAwait(false)).ToList();
        var query = unitOfWork.ConnectionFor(typeof(SysUser)).Queryable<SysUser>().Where(u => !u.IsDeleted && u.Status == UserStatus.Active);
        query = members.Count == 0 ? query.Where(u => u.HomeTenantId == side) : query.Where(u => u.HomeTenantId == side || members.Contains(u.Id));
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var term = keyword.Trim();
            query = query.Where(u => u.UserName.Contains(term) || u.DisplayName.Contains(term) || (u.Email != null && u.Email.Contains(term)));
        }

        var rows = await query.OrderBy(static u => u.UserName).Take(limit).ToListAsync(cancellationToken).ConfigureAwait(false);
        return rows.Select(static u => new UserSummary(u.Id, u.UserName, u.DisplayName, u.Email, u.PhoneNumber)).ToList();
    }

    private async Task<HashSet<Guid>> MembersAsync(string side, CancellationToken cancellationToken)
    {
        var members = new HashSet<Guid>();
        if (string.Equals(side, TenantIds.Platform, StringComparison.Ordinal))
        {
            return members;
        }

        foreach (var resolver in memberships)
        {
            members.UnionWith(await resolver.MembersOfAsync(side, cancellationToken).ConfigureAwait(false));
        }

        return members;
    }
}
