// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Domain.Services;
using XiHan.BasicApp.Access.Infrastructure;
using XiHan.BasicApp.Contracts.Auditing;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Operations.Roles;

/// <summary>
/// 修改角色：<c>PUT /api/roles/{id}</c>。
/// </summary>
/// <param name="roles">角色仓储。</param>
/// <param name="service">角色域服务。</param>
/// <param name="scopes">角色数据范围服务。</param>
/// <param name="hierarchy">继承图缓存（启停后失效）。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.RolesUpdate)]
public sealed class UpdateRole(IRepository<SysRole, Guid> roles, RoleService service, RoleDataScopeService scopes, RoleHierarchy hierarchy) : IOperation<UpdateRoleRequest, RoleResponse>
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

        var previousScope = role.DataScope;
        var previousStatus = role.Status;
        role.Name = request.Name.Trim();
        role.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        role.Order = request.Order;
        role.Status = request.Status;
        var scope = request.DataScope ?? role.DataScope;
        var customDepartments = request.CustomDepartments ?? (scope == DataScope.Custom ? (await scopes.ListAsync(role.Id, context.CancellationToken).ConfigureAwait(false)).Select(static s => new DataScopeDepartmentRequest(s.DepartmentId, s.IncludeChildren)).ToList() : []);
        var validated = await scopes.ValidateAsync(role, scope, customDepartments, context.CancellationToken).ConfigureAwait(false);
        if (validated.IsFailure)
        {
            return validated.Error!;
        }

        role.DataScope = scope;
        var updated = await roles.UpdateAsync(role, context.CancellationToken).ConfigureAwait(false);
        if (updated.IsFailure)
        {
            return updated.Error!;
        }

        var replaced = await scopes.ReplaceAsync(role, customDepartments, context.CancellationToken).ConfigureAwait(false);
        if (replaced.IsFailure)
        {
            return replaced.Error!;
        }

        // 启停改变继承链：停用的上级不再传下授予
        if (previousStatus != role.Status)
        {
            await hierarchy.InvalidateAsync(role.OwnerTenantId, context.CancellationToken).ConfigureAwait(false);
        }

        // 数据范围变了（或重新给了自定义部门）记一条权限变更日志：说明里是新旧范围与自定义部门数
        if (previousScope != scope || request.CustomDepartments is not null)
        {
            context.AddAuditLog(new PermissionChangeLogEntry(PermissionChangeType.RoleDataScope) { RoleId = role.Id, RoleName = role.Name, Detail = scope == DataScope.Custom ? $"{previousScope} -> {scope} ({customDepartments.Count})" : $"{previousScope} -> {scope}" });
        }

        return RoleResponse.From(role);
    }
}
