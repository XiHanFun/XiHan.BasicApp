// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Domain.Services;
using XiHan.BasicApp.Access.Infrastructure;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Operations.Roles;

/// <summary>
/// 角色详情：<c>GET /api/roles/{id}</c>，带当前授予的权限码与成员数。
/// </summary>
/// <param name="service">角色域服务。</param>
/// <param name="grants">角色授予。</param>
/// <param name="scopes">角色数据范围服务。</param>
/// <param name="members">指派仓储。</param>
/// <param name="rules">授予规则。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.RolesRead)]
public sealed class GetRole(RoleService service, RoleGrants grants, IRepository<SysUserRole, Guid> members, RoleDataScopeService scopes, PermissionRuleWriter rules) : IOperation<GetRoleRequest, RoleDetailResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "roles.get";

    /// <inheritdoc />
    public async ValueTask<Result<RoleDetailResponse>> HandleAsync(GetRoleRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (!(await service.FindAsync(request.Id, RoleService.OwnerOf(context), context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var role))
        {
            return Error.NotFound(AccessErrors.RoleNotFound, AccessMessages.RoleNotFound, MessageArguments.Of(("id", request.Id)));
        }

        var permissions = await grants.ListAsync(role, context.CancellationToken).ConfigureAwait(false);
        var roleId = role.Id;
        var count = await members.CountAsync(EntityQuery.All<SysUserRole>().Where(new Specification<SysUserRole>(m => m.RoleId == roleId)), context.CancellationToken).ConfigureAwait(false);
        var custom = role.DataScope == DataScope.Custom
            ? (await scopes.ListAsync(role.Id, context.CancellationToken).ConfigureAwait(false)).Select(static s => new DataScopeDepartmentRequest(s.DepartmentId, s.IncludeChildren)).ToList()
            : [];
        var ruleRows = await rules.ListAsync(role.OwnerTenantId, GrantSubjectKind.Role, role.Id, context.CancellationToken).ConfigureAwait(false);
        return new RoleDetailResponse(RoleResponse.From(role), permissions, count, custom, ruleRows.OrderBy(static r => r.Permission, StringComparer.Ordinal).Select(PermissionRuleResponse.From).ToList());
    }
}
