// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Organization.Domain.Entities;
using XiHan.BasicApp.Organization.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Organization.Operations.Memberships;

/// <summary>
/// 修改归属。
/// </summary>
/// <remarks>
/// <c>PUT /api/memberships/{id}</c>（岗位、主部门、工号、职级、入职时间、排序）。
/// </remarks>
/// <param name="memberships">归属仓储。</param>
/// <param name="positions">岗位仓储。</param>
/// <param name="service">归属域服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = OrganizationPermissions.MembershipsUpdate)]
public sealed class UpdateMembership(IRepository<SysUserDepartment, Guid> memberships, IRepository<SysPosition, Guid> positions, MembershipService service) : IOperation<UpdateMembershipRequest, MembershipResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "memberships.update";

    /// <inheritdoc />
    public async ValueTask<Result<MembershipResponse>> HandleAsync(UpdateMembershipRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var scope = OrganizationScope.RequireTenant(context);
        if (scope.IsFailure)
        {
            return scope.Error!;
        }

        if (!(await memberships.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var membership))
        {
            return Error.NotFound(OrganizationErrors.MembershipNotFound, OrganizationMessages.MembershipNotFound, MessageArguments.Of(("id", request.Id)));
        }

        if (request.PositionId is { } positionId && !(await positions.FindAsync(positionId, context.CancellationToken).ConfigureAwait(false)).HasValue)
        {
            return Error.NotFound(OrganizationErrors.PositionNotFound, OrganizationMessages.PositionNotFound, MessageArguments.Of(("id", positionId)));
        }

        membership.PositionId = request.PositionId;
        membership.EmployeeNumber = string.IsNullOrWhiteSpace(request.EmployeeNumber) ? null : request.EmployeeNumber.Trim();
        membership.JobGrade = string.IsNullOrWhiteSpace(request.JobGrade) ? null : request.JobGrade.Trim();
        membership.HiredAt = request.HiredAt;
        membership.Order = request.Order;
        if (request.IsPrimary && !membership.IsPrimary)
        {
            var made = await service.MakePrimaryAsync(membership, context.CancellationToken).ConfigureAwait(false);
            if (made.IsFailure)
            {
                return made.Error!;
            }
        }

        var updated = await memberships.UpdateAsync(membership, context.CancellationToken).ConfigureAwait(false);
        if (updated.IsFailure)
        {
            return updated.Error!;
        }

        return (await service.ComposeAsync([membership], context.CancellationToken).ConfigureAwait(false))[0];
    }
}
