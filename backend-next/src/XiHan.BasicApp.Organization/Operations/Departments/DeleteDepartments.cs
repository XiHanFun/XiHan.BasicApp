// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Organization.Domain.Entities;
using XiHan.BasicApp.Organization.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Organization.Operations.Departments;

/// <summary>
/// 批量删除部门。
/// </summary>
/// <remarks>
/// <c>DELETE /api/departments?ids=...</c>。还有子部门或成员的逐项 <c>ORGANIZATION_CONFLICT_002</c>；删除即软删并摘除闭包行。
/// </remarks>
/// <param name="departments">部门仓储。</param>
/// <param name="service">部门域服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = OrganizationPermissions.DepartmentsDelete)]
public sealed class DeleteDepartments(IRepository<SysDepartment, Guid> departments, DepartmentService service) : IOperation<DepartmentIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "departments.delete";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(DepartmentIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var scope = OrganizationScope.RequireTenant(context);
        if (scope.IsFailure)
        {
            return scope.Error!;
        }

        var found = (await departments.FindManyAsync(request.Ids, context.CancellationToken).ConfigureAwait(false)).ToDictionary(static d => d.Id);
        var succeeded = new List<Guid>();
        var failed = new List<BatchFailure>();
        foreach (var id in request.Ids.Distinct())
        {
            if (!found.TryGetValue(id, out var department))
            {
                failed.Add(new BatchFailure(id, OrganizationErrors.DepartmentNotFound));
                continue;
            }

            if (await service.ChildCountAsync(id, context.CancellationToken).ConfigureAwait(false) > 0 || await service.MemberCountAsync(id, context.CancellationToken).ConfigureAwait(false) > 0)
            {
                failed.Add(new BatchFailure(id, OrganizationErrors.DepartmentInUse));
                continue;
            }

            var detached = await service.DetachAsync(department, context.CancellationToken).ConfigureAwait(false);
            if (detached.IsFailure)
            {
                return detached.Error!;
            }

            succeeded.Add(id);
        }

        return new BatchResult(succeeded, failed);
    }
}
