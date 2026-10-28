// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Domain.Services;
using XiHan.BasicApp.Access.Infrastructure;
using XiHan.BasicApp.Contracts.Auditing;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Operations.Roles;

/// <summary>
/// 撤销角色的权限：<c>POST /api/roles/{id}:revoke</c>。撤掉授予，连同这些权限码上的规则（有效期、原因、Deny）；
/// 只有 Deny 没有授予的权限码撤销即解除拒绝，算成功；既没有授予也没有规则的是 NotFound。
/// </summary>
/// <param name="service">角色域服务。</param>
/// <param name="catalog">权限目录。</param>
/// <param name="grants">角色授予。</param>
/// <param name="rules">授予规则。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.RolesGrant)]
public sealed class RevokeRole(RoleService service, PermissionCatalog catalog, RoleGrants grants, PermissionRuleWriter rules) : IOperation<RolePermissionsRequest, PermissionBatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "roles.revoke";

    /// <inheritdoc />
    public ValueTask<Result<PermissionBatchResult>> HandleAsync(RolePermissionsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var owner = RoleService.OwnerOf(context);
        return RolePermissionBatch.RunAsync(request, context, service, catalog, PermissionChangeType.RoleRevoke, async (role, permissions, ct) =>
        {
            var outcomes = await grants.RevokeAsync(role, permissions, ct).ConfigureAwait(false);
            var removed = await rules.RemoveAsync(owner, GrantSubjectKind.Role, role.Id, permissions, ct).ConfigureAwait(false);
            var cleared = removed.ValueOrThrow();
            return outcomes.Select(o => o.Outcome.IsFailure && o.Outcome.Error!.Kind == "NOTFOUND" && cleared.Contains(o.Grant.Permission) ? o with { Outcome = Result.Success() } : o).ToList();
        });
    }
}
