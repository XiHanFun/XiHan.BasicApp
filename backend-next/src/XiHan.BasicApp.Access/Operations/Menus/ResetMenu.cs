// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Operations.Menus;

/// <summary>
/// 恢复页面节点的默认展示。
/// </summary>
/// <remarks>
/// <c>POST /api/menus/{id}:reset</c>（平台侧，<c>access.menus.update</c>）。
/// 标题覆盖、图标、排序、上级、可见、缓存、固定标签回到页面登记的值并清掉定制标记，种子此后照常对齐；运营节点没有登记，<c>ACCESS_VALIDATION_010</c>。
/// </remarks>
/// <param name="menus">菜单仓储。</param>
/// <param name="service">菜单域服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.MenusUpdate)]
public sealed class ResetMenu(IRepository<SysMenu, Guid> menus, MenuService service) : IOperation<ResetMenuRequest, MenuResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "menus.reset";

    /// <inheritdoc />
    public async ValueTask<Result<MenuResponse>> HandleAsync(ResetMenuRequest request, InvocationContext context)
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

        if (row.Kind != MenuKind.Page)
        {
            return Error.Validation(AccessErrors.MenuNotPage, AccessMessages.MenuNotPage, MessageArguments.Of(("code", row.Code)));
        }

        // 登记里已经没有这个页面（种子下次会删掉它）：当作不存在
        if (service.FindRegistered(row.Code) is not { } registered)
        {
            return Error.NotFound(AccessErrors.MenuNotFound, AccessMessages.MenuNotFound, MessageArguments.Of(("id", request.Id)));
        }

        Guid? parentId = null;
        if (registered.ParentCode is { } parentCode)
        {
            parentId = (await service.ListAsync(context.CancellationToken).ConfigureAwait(false)).FirstOrDefault(r => string.Equals(r.Code, parentCode, StringComparison.Ordinal))?.Id;
        }

        MenuService.ResetPresentation(row, registered.Page, parentId);
        var updated = await menus.UpdateAsync(row, context.CancellationToken).ConfigureAwait(false);
        return updated.IsFailure ? updated.Error! : MenuResponse.From(row);
    }
}
