// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Organization.Domain.Entities;
using XiHan.BasicApp.Organization.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Organization.Operations.Memberships;

/// <summary>
/// 把用户移出部门。
/// </summary>
/// <remarks>
/// <c>DELETE /api/memberships?ids=...</c>。移出的是主部门时，该用户剩下的第一条归属自动成为主部门。
/// </remarks>
/// <param name="memberships">归属仓储。</param>
/// <param name="service">归属域服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = OrganizationPermissions.MembershipsDelete)]
public sealed class DeleteMemberships(IRepository<SysUserDepartment, Guid> memberships, MembershipService service) : IOperation<MembershipIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "memberships.delete";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(MembershipIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var scope = OrganizationScope.RequireTenant(context);
        if (scope.IsFailure)
        {
            return scope.Error!;
        }

        var found = (await memberships.FindManyAsync(request.Ids, context.CancellationToken).ConfigureAwait(false)).ToDictionary(static m => m.Id);
        var succeeded = new List<Guid>();
        var failed = new List<BatchFailure>();
        var removedIds = new HashSet<Guid>();
        foreach (var id in request.Ids.Distinct())
        {
            if (!found.TryGetValue(id, out var membership))
            {
                failed.Add(new BatchFailure(id, OrganizationErrors.MembershipNotFound));
                continue;
            }

            var removed = await memberships.RemoveAsync(membership, context.CancellationToken).ConfigureAwait(false);
            if (removed.IsFailure)
            {
                return removed.Error!;
            }

            removedIds.Add(id);
            succeeded.Add(id);
            if (membership.IsPrimary)
            {
                var remaining = (await service.OfUserAsync(membership.UserId, context.CancellationToken).ConfigureAwait(false)).Where(m => !removedIds.Contains(m.Id)).OrderBy(static m => m.Order).ThenBy(static m => m.CreatedAt).ToList();
                if (remaining.Count > 0)
                {
                    remaining[0].IsPrimary = true;
                    var promoted = await memberships.UpdateAsync(remaining[0], context.CancellationToken).ConfigureAwait(false);
                    if (promoted.IsFailure)
                    {
                        return promoted.Error!;
                    }
                }
            }
        }

        return new BatchResult(succeeded, failed);
    }
}
