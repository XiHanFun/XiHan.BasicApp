// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Organization.Domain.Entities;
using XiHan.BasicApp.Organization.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Organization.Operations.Departments;

/// <summary>
/// 创建部门。
/// </summary>
/// <remarks>
/// <c>POST /api/departments</c>。编码租户内唯一；上级必须存在；负责人必须是本租户用户；写入后挂进闭包表。
/// </remarks>
/// <param name="departments">部门仓储。</param>
/// <param name="service">部门域服务。</param>
/// <param name="members">成员域服务（核对负责人）。</param>
/// <param name="ids">标识生成器。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = OrganizationPermissions.DepartmentsCreate)]
public sealed class CreateDepartment(IRepository<SysDepartment, Guid> departments, DepartmentService service, MembershipService members, IIdGenerator<Guid> ids) : IOperation<CreateDepartmentRequest, DepartmentResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "departments.create";

    /// <inheritdoc />
    public async ValueTask<Result<DepartmentResponse>> HandleAsync(CreateDepartmentRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (!OrganizationScope.RequireTenant(context).TryGetValue(out var tenantId))
        {
            return OrganizationScope.RequireTenant(context).Error!;
        }

        var code = request.Code.Trim();
        if (await service.IsCodeTakenAsync(code, context.CancellationToken).ConfigureAwait(false))
        {
            return Error.Conflict(OrganizationErrors.DepartmentCodeTaken, OrganizationMessages.DepartmentCodeTaken, MessageArguments.Of(("code", code)));
        }

        if (request.ParentId is { } parentId && !(await service.FindAsync(parentId, context.CancellationToken).ConfigureAwait(false)).HasValue)
        {
            return Error.NotFound(OrganizationErrors.DepartmentNotFound, OrganizationMessages.DepartmentNotFound, MessageArguments.Of(("id", parentId)));
        }

        if (request.LeaderUserId is { } leaderId && !(await members.FindUserInTenantAsync(leaderId, tenantId, context.CancellationToken).ConfigureAwait(false)).HasValue)
        {
            return Error.Validation(OrganizationErrors.UserNotInTenant, OrganizationMessages.UserNotInTenant, MessageArguments.Of(("userId", leaderId)));
        }

        var department = new SysDepartment
        {
            Id = ids.NewId(),
            Code = code,
            Name = request.Name.Trim(),
            ParentId = request.ParentId,
            LeaderUserId = request.LeaderUserId,
            Order = request.Order,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
        };
        var added = await departments.AddAsync(department, context.CancellationToken).ConfigureAwait(false);
        if (added.IsFailure)
        {
            return added.Error!;
        }

        var attached = await service.AttachAsync(department, context.CancellationToken).ConfigureAwait(false);
        return attached.IsFailure ? attached.Error! : DepartmentResponse.From(department);
    }
}
