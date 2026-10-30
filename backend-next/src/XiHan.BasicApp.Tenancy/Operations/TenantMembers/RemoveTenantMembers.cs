// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Tenancy.Domain.Entities;
using XiHan.BasicApp.Tenancy.Domain.Services;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Tenancy.Operations.TenantMembers;

/// <summary>
/// 批量移除成员。
/// </summary>
/// <remarks>
/// <c>tenant-members.delete</c>（平台侧）。移除后该用户不能再切换进该租户；留在该租户里的会话到续签时被拒（<c>IDENTITY_DENIED_006</c>）。
/// 租户的所有者逐项拒绝（<c>TENANCY_CONFLICT_012</c>），要先转移所有权。
/// </remarks>
/// <param name="members">成员仓储。</param>
/// <param name="tenants">租户仓储（所有者不能移出）。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = TenancyPermissions.TenantMembersDelete)]
public sealed class RemoveTenantMembers(IRepository<SysTenantMember, Guid> members, IRepository<SysTenant, Guid> tenants) : IOperation<TenantMemberIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "tenant-members.delete";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(TenantMemberIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (TenantService.RequirePlatform(context) is { } denied)
        {
            return denied;
        }

        var found = (await members.FindManyAsync(request.Ids, context.CancellationToken).ConfigureAwait(false)).ToDictionary(static m => m.Id);
        // 所有者要先转移所有权才能移出
        var tenantIds = found.Values.Select(static m => m.TenantId).Distinct().ToList();
        var owners = (await tenants.ListAsync(EntityQuery.All<SysTenant>().Where(new Specification<SysTenant>(t => tenantIds.Contains(t.TenantId))), context.CancellationToken).ConfigureAwait(false))
            .Where(static t => t.AdminUserId is not null).ToDictionary(static t => t.TenantId, static t => t.AdminUserId!.Value, StringComparer.Ordinal);
        var succeeded = new List<Guid>();
        var failed = new List<BatchFailure>();
        foreach (var id in request.Ids.Distinct())
        {
            if (!found.TryGetValue(id, out var member))
            {
                failed.Add(new BatchFailure(id, TenancyErrors.MemberNotFound));
                continue;
            }

            if (owners.TryGetValue(member.TenantId, out var ownerId) && ownerId == member.UserId)
            {
                failed.Add(new BatchFailure(id, TenancyErrors.OwnerRemoval));
                continue;
            }

            var removed = await members.RemoveAsync(member, context.CancellationToken).ConfigureAwait(false);
            if (removed.IsFailure)
            {
                return removed.Error!;
            }

            succeeded.Add(id);
        }

        return new BatchResult(succeeded, failed);
    }
}
