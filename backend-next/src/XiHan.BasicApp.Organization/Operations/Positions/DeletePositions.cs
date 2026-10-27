// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Organization.Domain.Entities;
using XiHan.BasicApp.Organization.Domain.Services;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Organization.Operations.Positions;

/// <summary>
/// 批量删除岗位：<c>DELETE /api/positions?ids=...</c>。还被归属引用的逐项 <c>ORGANIZATION_CONFLICT_004</c>。
/// </summary>
/// <param name="positions">岗位仓储。</param>
/// <param name="memberships">归属仓储。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = OrganizationPermissions.PositionsDelete)]
public sealed class DeletePositions(IRepository<SysPosition, Guid> positions, IRepository<SysUserDepartment, Guid> memberships) : IOperation<PositionIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "positions.delete";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(PositionIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var scope = OrganizationScope.RequireTenant(context);
        if (scope.IsFailure)
        {
            return scope.Error!;
        }

        var found = (await positions.FindManyAsync(request.Ids, context.CancellationToken).ConfigureAwait(false)).ToDictionary(static p => p.Id);
        var succeeded = new List<Guid>();
        var failed = new List<BatchFailure>();
        foreach (var id in request.Ids.Distinct())
        {
            if (!found.TryGetValue(id, out var position))
            {
                failed.Add(new BatchFailure(id, OrganizationErrors.PositionNotFound));
                continue;
            }

            var positionId = position.Id;
            if (await memberships.CountAsync(EntityQuery.All<SysUserDepartment>().Where(new Specification<SysUserDepartment>(m => m.PositionId == positionId)), context.CancellationToken).ConfigureAwait(false) > 0)
            {
                failed.Add(new BatchFailure(id, OrganizationErrors.PositionInUse));
                continue;
            }

            var removed = await positions.RemoveAsync(position, context.CancellationToken).ConfigureAwait(false);
            if (removed.IsFailure)
            {
                return removed.Error!;
            }

            succeeded.Add(id);
        }

        return new BatchResult(succeeded, failed);
    }
}
