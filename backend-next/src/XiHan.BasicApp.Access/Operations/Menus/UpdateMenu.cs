// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Operations.Menus;

/// <summary>
/// 修改菜单节点。
/// </summary>
/// <remarks>
/// <c>PUT /api/menus/{id}</c>（平台侧）。
/// 页面节点只改展示并标为已定制（种子此后不覆盖展示字段，<c>menus.reset</c> 恢复）；外链与权限来自页面登记，请求里给了即 <c>ACCESS_VALIDATION_008</c>；子页面不能离开登记的父页面。
/// 运营节点标题必填（<c>ACCESS_VALIDATION_009</c>），上级、外链与权限的规则同新建。
/// </remarks>
/// <param name="menus">菜单仓储。</param>
/// <param name="service">菜单域服务。</param>
/// <param name="catalog">权限目录。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.MenusUpdate)]
public sealed class UpdateMenu(IRepository<SysMenu, Guid> menus, MenuService service, PermissionCatalog catalog) : IOperation<UpdateMenuRequest, MenuResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "menus.update";

    /// <inheritdoc />
    public async ValueTask<Result<MenuResponse>> HandleAsync(UpdateMenuRequest request, InvocationContext context)
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

        var rows = await service.ListAsync(context.CancellationToken).ConfigureAwait(false);
        var title = string.IsNullOrWhiteSpace(request.Title) ? null : request.Title.Trim();
        if (row.Kind == MenuKind.Page)
        {
            if (!string.IsNullOrWhiteSpace(request.ExternalUrl) || !string.IsNullOrWhiteSpace(request.Permission))
            {
                return Error.Validation(AccessErrors.MenuPageManaged, AccessMessages.MenuPageManaged);
            }

            var registeredChild = service.FindRegistered(row.Code)?.ParentCode is not null;
            if (MenuService.CheckParent(row.Kind, registeredChild, row.ParentId, request.ParentId, rows) is { } parentError)
            {
                return parentError;
            }

            row.IsCustomized = true;
        }
        else
        {
            if (title is null)
            {
                return Error.Validation(AccessErrors.MenuTitleRequired, AccessMessages.MenuTitleRequired);
            }

            if (MenuService.CheckParent(row.Kind, registeredChild: false, row.ParentId, request.ParentId, rows) is { } parentError)
            {
                return parentError;
            }

            if (MenuRules.CheckUrlAndPermission(row.Kind, request.ExternalUrl, request.Permission, catalog) is { } invalid)
            {
                return invalid;
            }

            row.Permission = string.IsNullOrWhiteSpace(request.Permission) ? null : request.Permission.Trim();
            row.ExternalUrl = row.Kind == MenuKind.Link ? request.ExternalUrl!.Trim() : null;
        }

        row.Title = title;
        row.Icon = string.IsNullOrWhiteSpace(request.Icon) ? null : request.Icon.Trim();
        row.Order = request.Order;
        row.ParentId = request.ParentId;
        row.IsVisible = request.IsVisible;
        row.KeepAlive = request.KeepAlive;
        row.IsAffix = request.IsAffix;
        var updated = await menus.UpdateAsync(row, context.CancellationToken).ConfigureAwait(false);
        return updated.IsFailure ? updated.Error! : MenuResponse.From(row);
    }
}
