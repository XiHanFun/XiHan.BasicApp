// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Domain.Services;
using XiHan.BasicApp.Access.Infrastructure;
using XiHan.BasicApp.Contracts.Auditing;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;
using XiHan.Framework.Security.Abstractions.Permissions;
using XiHan.Framework.Security.Database.Grants;

namespace XiHan.BasicApp.Access.Operations.Roles;

/// <summary>
/// 让角色拒绝权限。
/// </summary>
/// <remarks>
/// <c>POST /api/roles/{id}:deny</c>。同一角色在一个权限码上只能是授予或拒绝之一：原有的授予撤掉、换成 Deny 规则（可带有效期与原因）。
/// 角色上的 Deny 只拿走这个角色自己的授予；同一用户经别的角色或直接授权拿到的同一权限码不受影响（用户级 Deny 才压过一切）。
/// </remarks>
/// <param name="service">角色域服务。</param>
/// <param name="catalog">权限目录。</param>
/// <param name="grants">角色授予。</param>
/// <param name="rules">授予规则。</param>
/// <param name="conditions">权限条件（绑定没了连同删除）。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.RolesGrant)]
public sealed class DenyRole(RoleService service, PermissionCatalog catalog, RoleGrants grants, PermissionRuleWriter rules, PermissionConditionService conditions) : IOperation<RolePermissionsRequest, PermissionBatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "roles.deny";

    /// <inheritdoc />
    public async ValueTask<Result<PermissionBatchResult>> HandleAsync(RolePermissionsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var window = GrantWindow.Validate(request.EffectiveAt, request.ExpiresAt);
        if (window.IsFailure)
        {
            return window.Error!;
        }

        var owner = RoleService.OwnerOf(context);
        return await RolePermissionBatch.RunAsync(request, context, service, catalog, PermissionChangeType.RoleDeny, async (role, permissions, ct) =>
        {
            // 撤掉同码的授予（没有授予不算失败），再写 Deny
            await grants.RevokeAsync(role, permissions, ct).ConfigureAwait(false);
            (await rules.SetAsync(owner, GrantSubjectKind.Role, role.Id, permissions, PermissionAction.Deny, request.EffectiveAt, request.ExpiresAt, request.Reason, ct).ConfigureAwait(false)).ThrowIfFailure();
            (await conditions.RemoveBindingsAsync(owner, GrantSubjectKind.Role, role.Id, permissions, PermissionAction.Grant, ct).ConfigureAwait(false)).ThrowIfFailure();
            return permissions.Select(p => new PermissionGrantResult(new PermissionGrant(RoleService.SubjectOf(role), p, RoleService.GrantTenantOf(role)), Result.Success())).ToList();
        }).ConfigureAwait(false);
    }
}
