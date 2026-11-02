// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Infrastructure;
using XiHan.BasicApp.Access.Operations.Roles;
using XiHan.BasicApp.Contracts.Auditing;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Domain.Services;

/// <summary>
/// 角色指派。
/// </summary>
/// <remarks>
/// 「给角色指派成员」的规则（同侧、约束规则、权限变更日志、指派缓存失效）集中在这里：
/// <c>roles.assign</c> 与审批通过后的授予（授权申请）都经它，规则只有一份。
/// </remarks>
/// <param name="service">角色域服务。</param>
/// <param name="users">用户仓储。</param>
/// <param name="members">指派仓储。</param>
/// <param name="ids">标识生成器。</param>
/// <param name="assignments">指派缓存（新成员的判定立即看到）。</param>
/// <param name="enforcer">约束规则求值。</param>
public sealed class RoleAssignmentService(RoleService service, IRepository<SysUser, Guid> users, IRepository<SysUserRole, Guid> members, IIdGenerator<Guid> ids, RoleAssignments assignments, ConstraintEnforcer enforcer)
{
    /// <summary>
    /// 给角色指派成员。
    /// </summary>
    /// <param name="request">角色与成员、有效期。</param>
    /// <param name="context">调用上下文。</param>
    /// <returns>逐个成员的结果；角色不存在或有效期不合法时整体失败。</returns>
    public async Task<Result<BatchResult>> AssignAsync(RoleMembersRequest request, InvocationContext context)
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
