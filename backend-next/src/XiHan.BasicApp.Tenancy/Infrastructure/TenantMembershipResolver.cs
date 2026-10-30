// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.MultiTenancy;
using XiHan.BasicApp.Tenancy.Domain.Entities;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;

namespace XiHan.BasicApp.Tenancy.Infrastructure;

/// <summary>
/// 成员关系查询（作用域，用调用方的工作单元）。
/// </summary>
/// <remarks>
/// 给 Identity 的切换租户、模仿登录与收件人判断用户能否进入、是否算租户内的用户，以及谁是所有者（<see cref="SysTenant.AdminUserId"/>）。
/// </remarks>
/// <param name="members">成员仓储。</param>
/// <param name="tenants">租户仓储。</param>
/// <param name="clock">时间源。</param>
public sealed class TenantMembershipResolver(IRepository<SysTenantMember, Guid> members, IRepository<SysTenant, Guid> tenants, TimeProvider clock) : ITenantMembershipResolver
{
    /// <inheritdoc />
    public async Task<bool> CanEnterAsync(string tenantId, Guid userId, CancellationToken cancellationToken)
    {
        var rows = await members.ListAsync(EntityQuery.All<SysTenantMember>().Where(new Specification<SysTenantMember>(m => m.TenantId == tenantId && m.UserId == userId)), cancellationToken).ConfigureAwait(false);
        var now = clock.GetUtcNow();
        return rows.Any(m => m.IsInEffect(now));
    }

    /// <inheritdoc />
    public async Task<bool> IsMemberAsync(string tenantId, Guid userId, CancellationToken cancellationToken)
    {
        var rows = await members.ListAsync(EntityQuery.All<SysTenantMember>().Where(new Specification<SysTenantMember>(m => m.TenantId == tenantId && m.UserId == userId && m.MemberType == TenantMemberType.Member)), cancellationToken).ConfigureAwait(false);
        var now = clock.GetUtcNow();
        return rows.Any(m => m.IsInEffect(now));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> MembersOfAsync(string tenantId, CancellationToken cancellationToken)
    {
        var rows = await members.ListAsync(EntityQuery.All<SysTenantMember>().Where(new Specification<SysTenantMember>(m => m.TenantId == tenantId && m.MemberType == TenantMemberType.Member)), cancellationToken).ConfigureAwait(false);
        var now = clock.GetUtcNow();
        return rows.Where(m => m.IsInEffect(now)).Select(static m => m.UserId).Distinct().ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlySet<Guid>> OwnersAmongAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userIds);
        if (userIds.Count == 0)
        {
            return new HashSet<Guid>();
        }

        var ids = userIds.Distinct().ToList();
        var owned = await tenants.ListAsync(EntityQuery.All<SysTenant>().Where(new Specification<SysTenant>(t => t.AdminUserId != null && ids.Contains(t.AdminUserId.Value))), cancellationToken).ConfigureAwait(false);
        return owned.Select(static t => t.AdminUserId!.Value).ToHashSet();
    }
}
