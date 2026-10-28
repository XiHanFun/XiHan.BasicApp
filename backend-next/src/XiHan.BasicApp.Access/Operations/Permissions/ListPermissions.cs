// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Services;
using XiHan.BasicApp.Access.Infrastructure;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Operations.Permissions;

/// <summary>
/// 权限目录：<c>GET /api/permissions</c>，列某一侧可授予的权限（平台侧 Platform / Both，租户侧 Tenant / Both）。缺省是调用方所在的一侧；
/// 平台主体可用 <c>side=tenant</c> 看租户侧（编辑套餐白名单），租户主体要看平台侧即 <c>ACCESS_DENIED_001</c>。租户主体的目录再按其套餐白名单收窄（授予给角色也不会生效的权限不列出）。
/// 只读目录类操作，同时作为 MCP / AI 工具暴露（5.4）。
/// </summary>
/// <param name="catalog">权限目录。</param>
/// <param name="effective">有效权限（套餐收窄）。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http | Expose.Mcp | Expose.Ai, Permission = AccessPermissions.PermissionsRead)]
public sealed class ListPermissions(PermissionCatalog catalog, EffectivePermissions effective) : IOperation<ListPermissionsRequest, PermissionCatalogResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "permissions.list";

    /// <inheritdoc />
    public async ValueTask<Result<PermissionCatalogResponse>> HandleAsync(ListPermissionsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var tenant = context.GetSecurity().Tenant?.Id;
        if (tenant is not null && string.Equals(request.Side, "platform", StringComparison.Ordinal))
        {
            return Error.Denied(AccessErrors.PlatformSideDenied, AccessMessages.PlatformSideDenied);
        }

        var platformSide = request.Side is null ? tenant is null : string.Equals(request.Side, "platform", StringComparison.Ordinal);
        var items = catalog.ForSide(platformSide);
        if (tenant is not null)
        {
            var allowed = await effective.NarrowAsync(items.Select(static p => p.Code).ToHashSet(StringComparer.Ordinal), tenant, context.CancellationToken).ConfigureAwait(false);
            items = items.Where(p => allowed.Contains(p.Code)).ToList();
        }

        return new PermissionCatalogResponse(items.Select(PermissionResponse.From).ToList());
    }
}
