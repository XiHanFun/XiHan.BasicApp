// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Services;
using XiHan.BasicApp.Access.Infrastructure;
using XiHan.BasicApp.Contracts.Auditing;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Operations.Roles;

/// <summary>
/// 给角色授予权限：<c>POST /api/roles/{id}:grant</c>。权限码须在目录里且作用侧相符；已有的授予算成功。
/// </summary>
/// <param name="service">角色域服务。</param>
/// <param name="catalog">权限目录。</param>
/// <param name="grants">角色授予。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.RolesGrant)]
public sealed class GrantRole(RoleService service, PermissionCatalog catalog, RoleGrants grants) : IOperation<RolePermissionsRequest, PermissionBatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "roles.grant";

    /// <inheritdoc />
    public ValueTask<Result<PermissionBatchResult>> HandleAsync(RolePermissionsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return RolePermissionBatch.RunAsync(request, context, service, catalog, PermissionChangeType.RoleGrant, grants.GrantAsync);
    }
}
