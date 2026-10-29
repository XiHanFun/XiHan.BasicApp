// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Organization.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Organization.Operations.Departments;

/// <summary>
/// 移动部门。
/// </summary>
/// <remarks>
/// <c>POST /api/departments/{id}:move</c>。新上级必须存在且不是自己或自己的子孙；整棵子树的闭包行重算。
/// </remarks>
/// <param name="service">部门域服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = OrganizationPermissions.DepartmentsUpdate)]
public sealed class MoveDepartment(DepartmentService service) : IOperation<MoveDepartmentRequest, DepartmentResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "departments.move";

    /// <inheritdoc />
    public async ValueTask<Result<DepartmentResponse>> HandleAsync(MoveDepartmentRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var scope = OrganizationScope.RequireTenant(context);
        if (scope.IsFailure)
        {
            return scope.Error!;
        }

        if (!(await service.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var department))
        {
            return Error.NotFound(OrganizationErrors.DepartmentNotFound, OrganizationMessages.DepartmentNotFound, MessageArguments.Of(("id", request.Id)));
        }

        if (request.ParentId is { } parentId && !(await service.FindAsync(parentId, context.CancellationToken).ConfigureAwait(false)).HasValue)
        {
            return Error.NotFound(OrganizationErrors.DepartmentNotFound, OrganizationMessages.DepartmentNotFound, MessageArguments.Of(("id", parentId)));
        }

        var moved = await service.MoveAsync(department, request.ParentId, context.CancellationToken).ConfigureAwait(false);
        return moved.IsFailure ? moved.Error! : DepartmentResponse.From(department);
    }
}
