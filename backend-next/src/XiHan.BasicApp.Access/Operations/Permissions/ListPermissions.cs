// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Operations.Permissions;

/// <summary>
/// 权限目录：<c>GET /api/permissions</c>，只列调用方作用侧可授予的权限（平台侧 Platform / Both，租户侧 Tenant / Both）。
/// </summary>
/// <param name="catalog">权限目录。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.PermissionsRead)]
public sealed class ListPermissions(PermissionCatalog catalog) : IOperation<ListPermissionsRequest, PermissionCatalogResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "permissions.list";

    /// <inheritdoc />
    public ValueTask<Result<PermissionCatalogResponse>> HandleAsync(ListPermissionsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var platformSide = context.GetSecurity().Tenant is null;
        return ValueTask.FromResult(Result.Success(new PermissionCatalogResponse(catalog.ForSide(platformSide).Select(PermissionResponse.From).ToList())));
    }
}
