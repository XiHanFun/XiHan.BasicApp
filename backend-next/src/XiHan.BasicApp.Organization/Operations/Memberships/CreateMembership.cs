// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Organization.Domain.Entities;
using XiHan.BasicApp.Organization.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Organization.Operations.Memberships;

/// <summary>
/// 把用户加进部门：<c>POST /api/memberships</c>。用户注册地必须是当前租户；同一用户同一部门只能有一条；用户的第一条归属自动成为主部门，显式设主时其他归属取消主标记。
/// </summary>
/// <param name="memberships">归属仓储。</param>
/// <param name="departments">部门域服务。</param>
/// <param name="positions">岗位仓储。</param>
/// <param name="service">归属域服务。</param>
/// <param name="ids">标识生成器。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = OrganizationPermissions.MembershipsCreate)]
public sealed class CreateMembership(
    IRepository<SysUserDepartment, Guid> memberships,
    DepartmentService departments,
    IRepository<SysPosition, Guid> positions,
    MembershipService service,
    IIdGenerator<Guid> ids) : IOperation<CreateMembershipRequest, MembershipResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "memberships.create";

    /// <inheritdoc />
    public async ValueTask<Result<MembershipResponse>> HandleAsync(CreateMembershipRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (!OrganizationScope.RequireTenant(context).TryGetValue(out var tenantId))
        {
            return OrganizationScope.RequireTenant(context).Error!;
        }

        if (!(await service.FindUserInTenantAsync(request.UserId, tenantId, context.CancellationToken).ConfigureAwait(false)).HasValue)
        {
            return Error.Validation(OrganizationErrors.UserNotInTenant, OrganizationMessages.UserNotInTenant, MessageArguments.Of(("userId", request.UserId)));
        }

        if (!(await departments.FindAsync(request.DepartmentId, context.CancellationToken).ConfigureAwait(false)).HasValue)
        {
            return Error.NotFound(OrganizationErrors.DepartmentNotFound, OrganizationMessages.DepartmentNotFound, MessageArguments.Of(("id", request.DepartmentId)));
        }

        if (request.PositionId is { } positionId && !(await positions.FindAsync(positionId, context.CancellationToken).ConfigureAwait(false)).HasValue)
        {
            return Error.NotFound(OrganizationErrors.PositionNotFound, OrganizationMessages.PositionNotFound, MessageArguments.Of(("id", positionId)));
        }

        if ((await service.FindAsync(request.UserId, request.DepartmentId, context.CancellationToken).ConfigureAwait(false)).HasValue)
        {
            return Error.Conflict(OrganizationErrors.MembershipExists, OrganizationMessages.MembershipExists, MessageArguments.Of(("userId", request.UserId), ("departmentId", request.DepartmentId)));
        }

        var membership = new SysUserDepartment
        {
            Id = ids.NewId(),
            UserId = request.UserId,
            DepartmentId = request.DepartmentId,
            PositionId = request.PositionId,
            EmployeeNumber = string.IsNullOrWhiteSpace(request.EmployeeNumber) ? null : request.EmployeeNumber.Trim(),
            JobGrade = string.IsNullOrWhiteSpace(request.JobGrade) ? null : request.JobGrade.Trim(),
            HiredAt = request.HiredAt,
            Order = request.Order,
        };
        var existing = await service.OfUserAsync(request.UserId, context.CancellationToken).ConfigureAwait(false);
        if (request.IsPrimary || existing.Count == 0)
        {
            var made = await service.MakePrimaryAsync(membership, context.CancellationToken).ConfigureAwait(false);
            if (made.IsFailure)
            {
                return made.Error!;
            }
        }

        var added = await memberships.AddAsync(membership, context.CancellationToken).ConfigureAwait(false);
        if (added.IsFailure)
        {
            return added.Error!;
        }

        return (await service.ComposeAsync([membership], context.CancellationToken).ConfigureAwait(false))[0];
    }
}
