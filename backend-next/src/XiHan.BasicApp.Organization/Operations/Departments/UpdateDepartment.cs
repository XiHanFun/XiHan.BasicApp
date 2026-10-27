// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Organization.Domain.Entities;
using XiHan.BasicApp.Organization.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Organization.Operations.Departments;

/// <summary>
/// 修改部门：<c>PUT /api/departments/{id}</c>（名称、负责人、排序、说明、状态；上级走 <c>departments.move</c>）。
/// </summary>
/// <param name="departments">部门仓储。</param>
/// <param name="service">部门域服务。</param>
/// <param name="members">成员域服务（核对负责人）。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = OrganizationPermissions.DepartmentsUpdate)]
public sealed class UpdateDepartment(IRepository<SysDepartment, Guid> departments, DepartmentService service, MembershipService members) : IOperation<UpdateDepartmentRequest, DepartmentResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "departments.update";

    /// <inheritdoc />
    public async ValueTask<Result<DepartmentResponse>> HandleAsync(UpdateDepartmentRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (!OrganizationScope.RequireTenant(context).TryGetValue(out var tenantId))
        {
            return OrganizationScope.RequireTenant(context).Error!;
        }

        if (!(await service.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var department))
        {
            return Error.NotFound(OrganizationErrors.DepartmentNotFound, OrganizationMessages.DepartmentNotFound, MessageArguments.Of(("id", request.Id)));
        }

        if (request.LeaderUserId is { } leaderId && !(await members.FindUserInTenantAsync(leaderId, tenantId, context.CancellationToken).ConfigureAwait(false)).HasValue)
        {
            return Error.Validation(OrganizationErrors.UserNotInTenant, OrganizationMessages.UserNotInTenant, MessageArguments.Of(("userId", leaderId)));
        }

        department.Name = request.Name.Trim();
        department.LeaderUserId = request.LeaderUserId;
        department.Order = request.Order;
        department.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        department.Status = request.Status;
        var updated = await departments.UpdateAsync(department, context.CancellationToken).ConfigureAwait(false);
        return updated.IsFailure ? updated.Error! : DepartmentResponse.From(department);
    }
}
