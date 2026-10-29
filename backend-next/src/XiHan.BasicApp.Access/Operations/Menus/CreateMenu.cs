// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Operations.Menus;

/// <summary>
/// 新建运营节点。
/// </summary>
/// <remarks>
/// <c>POST /api/menus</c>（平台侧）。只能建目录或外链（页面 <c>ACCESS_VALIDATION_008</c>）；编码存为 <c>custom.&lt;名&gt;</c> 且唯一（<c>ACCESS_CONFLICT_003</c>）；
/// 目录只在顶层、外链可挂目录（<c>ACCESS_VALIDATION_006</c>）；外链地址须是 http / https 绝对地址（<c>ACCESS_VALIDATION_007</c>）；权限码须在目录里（<c>ACCESS_VALIDATION_001</c>）。
/// </remarks>
/// <param name="menus">菜单仓储。</param>
/// <param name="service">菜单域服务。</param>
/// <param name="catalog">权限目录。</param>
/// <param name="ids">标识生成器。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.MenusCreate)]
public sealed class CreateMenu(IRepository<SysMenu, Guid> menus, MenuService service, PermissionCatalog catalog, IIdGenerator<Guid> ids) : IOperation<CreateMenuRequest, MenuResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "menus.create";

    /// <inheritdoc />
    public async ValueTask<Result<MenuResponse>> HandleAsync(CreateMenuRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (MenuService.RequirePlatform(context) is { } denied)
        {
            return denied;
        }

        if (request.Kind == MenuKind.Page)
        {
            return Error.Validation(AccessErrors.MenuPageManaged, AccessMessages.MenuPageManaged);
        }

        var code = MenuService.CustomCode(request.Code.Trim());
        var rows = await service.ListAsync(context.CancellationToken).ConfigureAwait(false);
        if (rows.Any(r => string.Equals(r.Code, code, StringComparison.Ordinal)))
        {
            return Error.Conflict(AccessErrors.MenuCodeTaken, AccessMessages.MenuCodeTaken, MessageArguments.Of(("code", code)));
        }

        if (MenuService.CheckParent(request.Kind, registeredChild: false, currentParentId: null, request.ParentId, rows) is { } parentError)
        {
            return parentError;
        }

        if (MenuRules.CheckUrlAndPermission(request.Kind, request.ExternalUrl, request.Permission, catalog) is { } invalid)
        {
            return invalid;
        }

        var row = new SysMenu
        {
            Id = ids.NewId(),
            Code = code,
            Kind = request.Kind,
            Package = MenuService.CustomPackage,
            ParentId = request.ParentId,
            Title = request.Title.Trim(),
            Icon = string.IsNullOrWhiteSpace(request.Icon) ? null : request.Icon.Trim(),
            Order = request.Order,
            Permission = string.IsNullOrWhiteSpace(request.Permission) ? null : request.Permission.Trim(),
            ExternalUrl = request.Kind == MenuKind.Link ? request.ExternalUrl!.Trim() : null,
            IsVisible = request.IsVisible,
        };
        var added = await menus.AddAsync(row, context.CancellationToken).ConfigureAwait(false);
        return added.IsFailure ? added.Error! : MenuResponse.From(row);
    }
}

/// <summary>
/// 运营节点的外链与权限规则（新建与修改共用）。
/// </summary>
internal static class MenuRules
{
    /// <summary>
    /// 校验外链地址与权限码。
    /// </summary>
    /// <remarks>
    /// 外链必须有合规地址、目录不能有地址；权限码可空，非空须在目录里。
    /// </remarks>
    public static Error? CheckUrlAndPermission(MenuKind kind, string? url, string? permission, PermissionCatalog catalog)
    {
        if (kind == MenuKind.Link ? !MenuService.IsValidUrl(url) : !string.IsNullOrWhiteSpace(url))
        {
            return Error.Validation(AccessErrors.MenuUrlInvalid, AccessMessages.MenuUrlInvalid);
        }

        if (!string.IsNullOrWhiteSpace(permission) && catalog.Find(permission.Trim()) is null)
        {
            return Error.Validation(AccessErrors.PermissionUnknown, AccessMessages.PermissionUnknown, MessageArguments.Of(("code", permission.Trim())));
        }

        return null;
    }
}
