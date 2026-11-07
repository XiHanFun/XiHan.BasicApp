// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Operations.Menus;

/// <summary>
/// 启用或停用菜单节点
/// </summary>
/// <remarks>
/// <c>POST /api/menus/{id}:set-status</c>（平台侧，<c>access.menus.update</c>）
/// 停用的节点连同子树不进 <c>auth.permissions</c>，页面没有路由；状态是展示字段，页面节点改了即标为已定制（种子不覆盖，<c>menus.reset</c> 恢复启用）；状态没变不写库
/// </remarks>
/// <param name="menus">菜单仓储</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.MenusUpdate)]
public sealed class SetMenuStatus(IRepository<SysMenu, Guid> menus) : IOperation<SetMenuStatusRequest, MenuResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "menus.set-status";

    /// <inheritdoc />
    public async ValueTask<Result<MenuResponse>> HandleAsync(SetMenuStatusRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (MenuService.RequirePlatform(context) is { } denied)
        {
            return denied;
        }

        if (!(await menus.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var row))
        {
            return Error.NotFound(AccessErrors.MenuNotFound, AccessMessages.MenuNotFound, MessageArguments.Of(("id", request.Id)));
        }

        if (row.Status == request.Status)
        {
            return MenuResponse.From(row);
        }

        row.Status = request.Status;
        if (row.Kind == MenuKind.Page)
        {
            row.IsCustomized = true;
        }

        var updated = await menus.UpdateAsync(row, context.CancellationToken).ConfigureAwait(false);
        return updated.IsFailure ? updated.Error! : MenuResponse.From(row);
    }
}
