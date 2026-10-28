// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Domain.Services;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Operations.Roles;

/// <summary>
/// 某个用户在当前作用侧的角色：<c>GET /api/roles:by-user?userId=…</c>（声明只读，ADR-0041）（用户页的角色入口用它回显，再按差异 <c>roles.assign / unassign</c>）。
/// </summary>
/// <param name="members">指派仓储。</param>
/// <param name="service">角色域服务。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = AccessPermissions.RolesRead)]
public sealed class RolesByUser(IRepository<SysUserRole, Guid> members, RoleService service) : IOperation<RolesByUserRequest, RolesByUserResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "roles.by-user";

    /// <inheritdoc />
    public async ValueTask<Result<RolesByUserResponse>> HandleAsync(RolesByUserRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var owner = RoleService.OwnerOf(context);
        var userId = request.UserId;
        var assignments = await members.ListAsync(EntityQuery.All<SysUserRole>().Where(new Specification<SysUserRole>(m => m.UserId == userId && m.OwnerTenantId == owner)), context.CancellationToken).ConfigureAwait(false);
        if (assignments.Count == 0)
        {
            return new RolesByUserResponse([]);
        }

        var found = await service.FindManyAsync(assignments.Select(static a => a.RoleId).Distinct().ToList(), owner, context.CancellationToken).ConfigureAwait(false);
        return new RolesByUserResponse(found.OrderBy(static r => r.Order).ThenBy(static r => r.Code, StringComparer.Ordinal).Select(RoleResponse.From).ToList());
    }
}

/// <summary>
/// <c>roles.by-user</c> 的请求。
/// </summary>
/// <param name="UserId">用户主键。</param>
public sealed record RolesByUserRequest(Guid UserId);

/// <summary>
/// <c>roles.by-user</c> 的响应。
/// </summary>
/// <param name="Items">角色，按排序号与角色码。</param>
public sealed record RolesByUserResponse(IReadOnlyList<RoleResponse> Items);
