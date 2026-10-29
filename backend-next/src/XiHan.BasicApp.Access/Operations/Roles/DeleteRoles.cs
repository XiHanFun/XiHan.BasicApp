// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Domain.Services;
using XiHan.BasicApp.Access.Infrastructure;
using XiHan.BasicApp.Contracts.Auditing;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Operations.Roles;

/// <summary>
/// 批量删除角色。
/// </summary>
/// <remarks>
/// <c>DELETE /api/roles?ids=...</c>。内建角色不能删；连同成员指派与授予一起清掉，逐项结果。
/// </remarks>
/// <param name="roles">角色仓储。</param>
/// <param name="members">指派仓储。</param>
/// <param name="service">角色域服务。</param>
/// <param name="scopes">角色数据范围服务（连同删自定义部门行）。</param>
/// <param name="grants">角色授予。</param>
/// <param name="rules">授予规则（连同删 Deny 与有效期）。</param>
/// <param name="cache">指派缓存。</param>
/// <param name="fieldRules">字段安全规则（连同删以该角色为目标的规则）。</param>
/// <param name="hierarchy">继承写入（仍被继承的不能删，连同删它作为下级的边）。</param>
/// <param name="delegations">委托（连同删委托该角色的记录）。</param>
/// <param name="conditions">权限条件（绑定没了连同删除）。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.RolesDelete)]
public sealed class DeleteRoles(IRepository<SysRole, Guid> roles, IRepository<SysUserRole, Guid> members, RoleService service, RoleGrants grants, RoleDataScopeService scopes, PermissionRuleWriter rules, RoleAssignments cache, FieldSecurityService fieldRules, RoleHierarchyService hierarchy, DelegationService delegations, PermissionConditionService conditions) : IOperation<RoleIdsRequest, BatchResult>
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

            // 仍被别的角色继承的不能删：删了会悄悄拿走下级持有人的授予
            var detached = await hierarchy.DetachAsync(role, context.CancellationToken).ConfigureAwait(false);
            if (!detached.TryGetValue(out var free))
            {
                return detached.Error!;
            }

            if (!free)
            {
                failed.Add(new BatchFailure(id, AccessErrors.RoleHasChildren));
                continue;
            }

            var removed = await RemoveAsync(role, context.CancellationToken).ConfigureAwait(false);
            if (removed.IsFailure)
            {
                return removed.Error!;
            }

            context.AddAuditLog(new PermissionChangeLogEntry(PermissionChangeType.RoleDelete) { RoleId = role.Id, RoleName = role.Name });
            succeeded.Add(id);
        }

        return new BatchResult(succeeded, failed);
    }

    private async Task<Result> RemoveAsync(SysRole role, CancellationToken cancellationToken)
    {
        var roleId = role.Id;
        var scopeRows = await scopes.ReplaceAsync(role, [], cancellationToken).ConfigureAwait(false);
        if (scopeRows.IsFailure)
        {
            return scopeRows;
        }

        var delegated = await delegations.RemoveForRoleAsync(role, cancellationToken).ConfigureAwait(false);
        if (delegated.IsFailure)
        {
            return delegated;
        }

        var fieldRows = await fieldRules.RemoveForTargetAsync(role.OwnerTenantId, FieldSecurityTargetKind.Role, roleId, cancellationToken).ConfigureAwait(false);
        if (fieldRows.IsFailure)
        {
            return fieldRows;
        }

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

            foreach (var assignment in assignments)
            {
                await cache.InvalidateAsync(assignment.OwnerTenantId, assignment.UserId, cancellationToken).ConfigureAwait(false);
            }
        }

        var conditionRows = await conditions.RemoveBindingsAsync(role.OwnerTenantId, GrantSubjectKind.Role, role.Id, null, null, cancellationToken).ConfigureAwait(false);
        if (conditionRows.IsFailure)
        {
            return conditionRows;
        }

        var ruleRows = await rules.RemoveAsync(role.OwnerTenantId, GrantSubjectKind.Role, role.Id, null, cancellationToken).ConfigureAwait(false);
        if (ruleRows.IsFailure)
        {
            return ruleRows.Error!;
        }

        var revoked = await grants.RevokeAllAsync(role, cancellationToken).ConfigureAwait(false);
        return revoked.IsFailure ? revoked : await roles.RemoveAsync(role, cancellationToken).ConfigureAwait(false);
    }
}
