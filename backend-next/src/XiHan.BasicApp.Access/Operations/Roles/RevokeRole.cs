// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Services;
using XiHan.BasicApp.Access.Infrastructure;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Operations.Roles;

/// <summary>
/// 撤销角色的权限：<c>POST /api/roles/{id}:revoke</c>。没有授予过的权限码在逐项结果里是 NotFound。
/// </summary>
/// <param name="service">角色域服务。</param>
/// <param name="catalog">权限目录。</param>
/// <param name="grants">角色授予。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.RolesGrant)]
public sealed class RevokeRole(RoleService service, PermissionCatalog catalog, RoleGrants grants) : IOperation<RolePermissionsRequest, PermissionBatchResult>
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
        return RolePermissionBatch.RunAsync(request, context, service, catalog, grants.RevokeAsync);
    }
}
