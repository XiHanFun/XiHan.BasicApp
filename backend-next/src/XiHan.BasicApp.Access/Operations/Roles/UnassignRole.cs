// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Domain.Services;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Operations.Roles;

/// <summary>
/// 解除角色成员：<c>POST /api/roles/{id}:unassign</c>。不是成员的算成功（幂等）。解除最迟在用户下一次刷新令牌时生效。
/// </summary>
/// <param name="service">角色域服务。</param>
/// <param name="members">指派仓储。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.RolesAssign)]
public sealed class UnassignRole(RoleService service, IRepository<SysUserRole, Guid> members) : IOperation<RoleMembersRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "roles.unassign";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(RoleMembersRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (!(await service.FindAsync(request.Id, RoleService.OwnerOf(context), context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var role))
        {
            return Error.NotFound(AccessErrors.RoleNotFound, AccessMessages.RoleNotFound, MessageArguments.Of(("id", request.Id)));
        }

        var roleId = role.Id;
        var wanted = request.UserIds.ToHashSet();
        var rows = (await members.ListAsync(EntityQuery.All<SysUserRole>().Where(new Specification<SysUserRole>(m => m.RoleId == roleId)), context.CancellationToken).ConfigureAwait(false)).Where(m => wanted.Contains(m.UserId)).ToList();
        if (rows.Count > 0)
        {
            foreach (var outcome in await members.RemoveRangeAsync(rows, context.CancellationToken).ConfigureAwait(false))
            {
                if (outcome.IsFailure)
                {
                    return outcome.Error!;
                }
            }
        }

        return BatchResult.AllSucceeded(request.UserIds.Distinct().ToList());
    }
}
