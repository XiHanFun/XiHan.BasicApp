// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.Globalization;
using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Domain.Services;
using XiHan.BasicApp.Access.Infrastructure;
using XiHan.BasicApp.Contracts.Auditing;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Operations.Roles;

/// <summary>
/// <c>roles.update-member</c> 的请求：改一条指派的有效期。角色标识由路由值 <c>{id}</c> 绑定。
/// </summary>
/// <param name="Id">角色 ID。</param>
/// <param name="UserId">成员的用户 ID。</param>
/// <param name="EffectiveAt">生效时间；为空即立即生效。</param>
/// <param name="ExpiresAt">到期时间（不含）；为空即长期有效。</param>
public sealed record RoleMemberWindowRequest(Guid Id, [Required] Guid UserId, DateTimeOffset? EffectiveAt = null, DateTimeOffset? ExpiresAt = null);

/// <summary>
/// 改成员指派的有效期：<c>POST /api/roles/{id}:update-member</c>。到期或改到未来生效的指派立即从判定里拿掉（令牌里的角色声明由权限过滤器按指派求交）；
/// 改成此刻有效的要等下次刷新令牌才进令牌。已到期的指派改回有效等于重新持有这个角色：过静态职责分离、基数与条件（4.x 的单条指派生效同样判定）。
/// </summary>
/// <param name="service">角色域服务。</param>
/// <param name="members">指派仓储。</param>
/// <param name="users">用户仓储。</param>
/// <param name="assignments">指派缓存。</param>
/// <param name="enforcer">约束规则求值。</param>
/// <param name="clock">时间源。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.RolesAssign)]
public sealed class UpdateRoleMember(RoleService service, IRepository<SysUserRole, Guid> members, IRepository<SysUser, Guid> users, RoleAssignments assignments, ConstraintEnforcer enforcer, TimeProvider clock) : IOperation<RoleMemberWindowRequest, RoleMemberResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "roles.update-member";

    /// <inheritdoc />
    public async ValueTask<Result<RoleMemberResponse>> HandleAsync(RoleMemberWindowRequest request, InvocationContext context)
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
        var userId = request.UserId;
        var rows = await members.ListAsync(EntityQuery.All<SysUserRole>().Where(new Specification<SysUserRole>(m => m.RoleId == roleId && m.UserId == userId)), context.CancellationToken).ConfigureAwait(false);
        if (rows.Count == 0 || !(await users.FindAsync(userId, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var user))
        {
            return Error.NotFound(AccessErrors.MemberNotFound, AccessMessages.MemberNotFound, MessageArguments.Of(("id", userId)));
        }

        var row = rows[0];
        var now = clock.GetUtcNow();
        if (row.ExpiresAt is { } expired && expired <= now && (request.ExpiresAt is null || request.ExpiresAt > now))
        {
            var held = await enforcer.HeldRolesAsync(owner, userId, context.CancellationToken).ConfigureAwait(false);
            held.Add(roleId);
            var violations = new List<ConstraintViolation>(await enforcer.CheckUserRolesAsync(owner, userId, user.UserName, held, [roleId], context).ConfigureAwait(false));
            violations.AddRange(await enforcer.CheckRoleMembersAsync(owner, roleId, await enforcer.MemberCountAsync(owner, roleId, context.CancellationToken).ConfigureAwait(false) + 1, context.CancellationToken).ConfigureAwait(false));
            if (ConstraintEnforcer.Settle(context, violations, roleId, userId) is { } blocked)
            {
                return blocked;
            }
        }

        row.EffectiveAt = request.EffectiveAt;
        row.ExpiresAt = request.ExpiresAt;
        var updated = await members.UpdateAsync(row, context.CancellationToken).ConfigureAwait(false);
        if (updated.IsFailure)
        {
            return updated.Error!;
        }

        await assignments.InvalidateAsync(owner, userId, context.CancellationToken).ConfigureAwait(false);
        context.AddAuditLog(new PermissionChangeLogEntry(PermissionChangeType.RoleAssignmentWindow)
        {
            RoleId = roleId,
            RoleName = role.Name,
            TargetUserId = userId,
            TargetUserName = user.UserName,
            Detail = string.Create(CultureInfo.InvariantCulture, $"{request.EffectiveAt:O} -> {request.ExpiresAt:O}"),
        });
        return new RoleMemberResponse(user.Id, user.UserName, user.DisplayName, user.Status, row.CreatedAt, row.EffectiveAt, row.ExpiresAt);
    }
}
