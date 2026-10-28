// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Operations.Menus;

/// <summary>
/// 菜单节点：<c>GET /api/menus/{id}</c>（平台侧）。
/// </summary>
/// <param name="menus">菜单仓储。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.MenusRead)]
public sealed class GetMenu(IRepository<SysMenu, Guid> menus) : IOperation<GetMenuRequest, MenuResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "menus.get";

    /// <inheritdoc />
    public async ValueTask<Result<MenuResponse>> HandleAsync(GetMenuRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (MenuService.RequirePlatform(context) is { } denied)
        {
            return denied;
        }

        return (await menus.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var row)
            ? MenuResponse.From(row)
            : Error.NotFound(AccessErrors.MenuNotFound, AccessMessages.MenuNotFound, MessageArguments.Of(("id", request.Id)));
    }
}
