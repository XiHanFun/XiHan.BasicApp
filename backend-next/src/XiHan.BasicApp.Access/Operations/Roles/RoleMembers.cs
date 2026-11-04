// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Domain.Services;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Operations.Roles;

/// <summary>
/// 列出角色成员
/// </summary>
/// <remarks>
/// <c>GET /api/roles/{id}:members</c>（声明只读，ADR-0041），带用户摘要，标出外部成员
/// 只列还在这一侧的人：外部成员的成员关系到期之后不再显示（指派留着，续上即恢复；被移出的指派已解除）
/// </remarks>
/// <param name="service">角色域服务</param>
/// <param name="members">指派仓储</param>
/// <param name="users">这一侧的用户</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = AccessPermissions.RolesRead)]
public sealed class RoleMembers(RoleService service, IRepository<SysUserRole, Guid> members, SideUsers users) : IOperation<RoleMembersQueryRequest, RoleMembersResponse>
{
    private const int MaxItems = 500;

    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "roles.members";

    /// <inheritdoc />
    public async ValueTask<Result<RoleMembersResponse>> HandleAsync(RoleMembersQueryRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var owner = RoleService.OwnerOf(context);
        if (!(await service.FindAsync(request.Id, owner, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var role))
        {
            return Error.NotFound(AccessErrors.RoleNotFound, AccessMessages.RoleNotFound, MessageArguments.Of(("id", request.Id)));
        }

        var roleId = role.Id;
        var query = EntityQuery.All<SysUserRole>().Where(new Specification<SysUserRole>(m => m.RoleId == roleId));
        var total = await members.CountAsync(query, context.CancellationToken).ConfigureAwait(false);
        var rows = await members.ListAsync(query.OrderBy(static m => m.CreatedAt).Page(0, MaxItems), context.CancellationToken).ConfigureAwait(false);
        if (rows.Count == 0)
        {
            return new RoleMembersResponse([], total);
        }

        var assignments = rows.ToDictionary(static m => m.UserId);
        var found = await users.FindManyAsync(rows.Select(static m => m.UserId).ToList(), owner, context.CancellationToken).ConfigureAwait(false);
        var items = found
            .Select(u => new RoleMemberResponse(u.Id, u.UserName, u.DisplayName, u.Status, assignments[u.Id].CreatedAt, assignments[u.Id].EffectiveAt, assignments[u.Id].ExpiresAt, !string.Equals(u.HomeTenantId, owner, StringComparison.Ordinal)))
            .OrderBy(static m => m.UserName, StringComparer.Ordinal)
            .ToList();
        return new RoleMembersResponse(items, total - (rows.Count - items.Count));
    }
}
