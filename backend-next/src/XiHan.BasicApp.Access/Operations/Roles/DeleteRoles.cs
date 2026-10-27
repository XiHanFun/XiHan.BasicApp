// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Domain.Services;
using XiHan.BasicApp.Access.Infrastructure;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Operations.Roles;

/// <summary>
/// 批量删除角色：<c>DELETE /api/roles?ids=...</c>。内建角色不能删；连同成员指派与授予一起清掉，逐项结果。
/// </summary>
/// <param name="roles">角色仓储。</param>
/// <param name="members">指派仓储。</param>
/// <param name="service">角色域服务。</param>
/// <param name="grants">角色授予。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.RolesDelete)]
public sealed class DeleteRoles(IRepository<SysRole, Guid> roles, IRepository<SysUserRole, Guid> members, RoleService service, RoleGrants grants) : IOperation<RoleIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "roles.delete";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(RoleIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var owner = RoleService.OwnerOf(context);
        var found = (await service.FindManyAsync(request.Ids, owner, context.CancellationToken).ConfigureAwait(false)).ToDictionary(static r => r.Id);
        var succeeded = new List<Guid>();
        var failed = new List<BatchFailure>();
        foreach (var id in request.Ids.Distinct())
        {
            if (!found.TryGetValue(id, out var role))
            {
                failed.Add(new BatchFailure(id, AccessErrors.RoleNotFound));
                continue;
            }

            if (role.IsBuiltIn)
            {
                failed.Add(new BatchFailure(id, AccessErrors.BuiltInRole));
                continue;
            }

            var removed = await RemoveAsync(role, context.CancellationToken).ConfigureAwait(false);
            if (removed.IsFailure)
            {
                return removed.Error!;
            }

            succeeded.Add(id);
        }

        return new BatchResult(succeeded, failed);
    }

    private async Task<Result> RemoveAsync(SysRole role, CancellationToken cancellationToken)
    {
        var roleId = role.Id;
        var assignments = await members.ListAsync(EntityQuery.All<SysUserRole>().Where(new Specification<SysUserRole>(m => m.RoleId == roleId)), cancellationToken).ConfigureAwait(false);
        if (assignments.Count > 0)
        {
            foreach (var outcome in await members.RemoveRangeAsync(assignments, cancellationToken).ConfigureAwait(false))
            {
                if (outcome.IsFailure)
                {
                    return outcome;
                }
            }
        }

        var revoked = await grants.RevokeAllAsync(role, cancellationToken).ConfigureAwait(false);
        return revoked.IsFailure ? revoked : await roles.RemoveAsync(role, cancellationToken).ConfigureAwait(false);
    }
}
