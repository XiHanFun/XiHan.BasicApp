// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Operations.Roles;

/// <summary>
/// 修改角色：<c>PUT /api/roles/{id}</c>。
/// </summary>
/// <param name="roles">角色仓储。</param>
/// <param name="service">角色域服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.RolesUpdate)]
public sealed class UpdateRole(IRepository<SysRole, Guid> roles, RoleService service) : IOperation<UpdateRoleRequest, RoleResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "roles.update";

    /// <inheritdoc />
    public async ValueTask<Result<RoleResponse>> HandleAsync(UpdateRoleRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (!(await service.FindAsync(request.Id, RoleService.OwnerOf(context), context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var role))
        {
            return Error.NotFound(AccessErrors.RoleNotFound, AccessMessages.RoleNotFound, MessageArguments.Of(("id", request.Id)));
        }

        role.Name = request.Name.Trim();
        role.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        role.Order = request.Order;
        role.Status = request.Status;
        var updated = await roles.UpdateAsync(role, context.CancellationToken).ConfigureAwait(false);
        return updated.IsFailure ? updated.Error! : RoleResponse.From(role);
    }
}
