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
/// 给 Identity 的切换租户与模仿登录判断用户能否进入某个租户。
/// </remarks>
/// <param name="members">成员仓储。</param>
public sealed class TenantMembershipResolver(IRepository<SysTenantMember, Guid> members) : ITenantMembershipResolver
{
    /// <inheritdoc />
    public async Task<bool> IsMemberAsync(string tenantId, Guid userId, CancellationToken cancellationToken)
    {
        var count = await members.CountAsync(EntityQuery.All<SysTenantMember>().Where(new Specification<SysTenantMember>(m => m.TenantId == tenantId && m.UserId == userId)), cancellationToken).ConfigureAwait(false);
        return count > 0;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> MembersOfAsync(string tenantId, CancellationToken cancellationToken)
    {
        var rows = await members.ListAsync(EntityQuery.All<SysTenantMember>().Where(new Specification<SysTenantMember>(m => m.TenantId == tenantId)), cancellationToken).ConfigureAwait(false);
        return rows.Select(static m => m.UserId).Distinct().ToList();
    }
}
