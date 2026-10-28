// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Operations.Menus;

/// <summary>
/// 菜单树：<c>GET /api/menus</c>（平台侧），含隐藏节点、运营节点与定制标记。
/// </summary>
/// <param name="service">菜单域服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.MenusRead)]
public sealed class ListMenus(MenuService service) : IOperation<ListMenusRequest, MenusResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "menus.list";

    /// <inheritdoc />
    public async ValueTask<Result<MenusResponse>> HandleAsync(ListMenusRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (MenuService.RequirePlatform(context) is { } denied)
        {
            return denied;
        }

        return MenusResponse.Tree(await service.ListAsync(context.CancellationToken).ConfigureAwait(false));
    }
}
