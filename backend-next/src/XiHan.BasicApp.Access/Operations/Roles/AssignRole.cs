// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Domain.Services;
using XiHan.BasicApp.Access.Infrastructure;
using XiHan.BasicApp.Contracts.Auditing;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Operations.Roles;

/// <summary>
/// 给角色指派成员：<c>POST /api/roles/{id}:assign</c>。用户须与角色同一侧（注册地 = 角色所属侧）；已是成员的算成功。
/// 指派最迟在用户下一次刷新令牌时生效（角色进 <c>role</c> 声明）。新成员逐个过约束规则（静态职责分离、基数与条件），拦截类的违规记为这一人失败（<c>ACCESS_CONFLICT_012</c>）。
/// </summary>
/// <param name="service">角色域服务。</param>
/// <param name="users">用户仓储。</param>
/// <param name="members">指派仓储。</param>
/// <param name="ids">标识生成器。</param>
/// <param name="assignments">指派缓存（新成员的判定立即看到）。</param>
/// <param name="enforcer">约束规则求值。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.RolesAssign)]
public sealed class AssignRole(RoleService service, IRepository<SysUser, Guid> users, IRepository<SysUserRole, Guid> members, IIdGenerator<Guid> ids, RoleAssignments assignments, ConstraintEnforcer enforcer) : IOperation<RoleMembersRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "roles.assign";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(RoleMembersRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var window = GrantWindow.Validate(request.EffectiveAt, request.ExpiresAt);
        if (window.IsFailure)
        {
            return window.Error!;
        }

        var owner = RoleService.OwnerOf(context);
        if (!(await service.FindAsync(request.Id, owner, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var role))
        {
            return Error.NotFound(AccessErrors.RoleNotFound, AccessMessages.RoleNotFound, MessageArguments.Of(("id", request.Id)));
        }

        var roleId = role.Id;
        var found = (await users.FindManyAsync(request.UserIds, context.CancellationToken).ConfigureAwait(false)).ToDictionary(static u => u.Id);
        var existing = (await members.ListAsync(EntityQuery.All<SysUserRole>().Where(new Specification<SysUserRole>(m => m.RoleId == roleId)), context.CancellationToken).ConfigureAwait(false)).Select(static m => m.UserId).ToHashSet();
        var succeeded = new List<Guid>();
        var failed = new List<BatchFailure>();
        var additions = new List<SysUserRole>();
        var memberCount = await enforcer.MemberCountAsync(owner, roleId, context.CancellationToken).ConfigureAwait(false);
        foreach (var userId in request.UserIds.Distinct())
        {
            if (!found.TryGetValue(userId, out var user) || !string.Equals(user.HomeTenantId, owner, StringComparison.Ordinal))
            {
                failed.Add(new BatchFailure(userId, AccessErrors.MemberNotFound));
                continue;
            }

            if (!existing.Contains(userId))
            {
                var held = await enforcer.HeldRolesAsync(owner, userId, context.CancellationToken).ConfigureAwait(false);
                held.Add(roleId);
                var violations = new List<ConstraintViolation>(await enforcer.CheckUserRolesAsync(owner, userId, user.UserName, held, [roleId], context).ConfigureAwait(false));
                violations.AddRange(await enforcer.CheckRoleMembersAsync(owner, roleId, memberCount + additions.Count + 1, context.CancellationToken).ConfigureAwait(false));
                if (ConstraintEnforcer.Settle(context, violations, roleId, userId) is not null)
                {
                    failed.Add(new BatchFailure(userId, AccessErrors.ConstraintViolated));
                    continue;
                }

                additions.Add(new SysUserRole { Id = ids.NewId(), OwnerTenantId = owner, UserId = userId, RoleId = roleId, EffectiveAt = request.EffectiveAt, ExpiresAt = request.ExpiresAt });
            }

            succeeded.Add(userId);
        }

        if (additions.Count > 0)
        {
            foreach (var outcome in await members.AddRangeAsync(additions, context.CancellationToken).ConfigureAwait(false))
            {
                if (outcome.IsFailure)
                {
                    return outcome.Error!;
                }
            }
        }

        // 已经是成员的不算变更；新加入的每人一条权限变更日志
        foreach (var addition in additions)
        {
            await assignments.InvalidateAsync(owner, addition.UserId, context.CancellationToken).ConfigureAwait(false);
            context.AddAuditLog(new PermissionChangeLogEntry(PermissionChangeType.RoleAssign) { RoleId = roleId, RoleName = role.Name, TargetUserId = addition.UserId, TargetUserName = found[addition.UserId].UserName });
        }

        return new BatchResult(succeeded, failed);
    }
}
