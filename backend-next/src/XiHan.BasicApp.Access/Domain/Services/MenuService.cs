// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Contracts.Pages;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Domain.Services;

/// <summary>
/// 菜单域服务：只在平台侧管理、页面登记的查找（恢复默认用）、上级节点的规则、外链地址的规则。
/// 规则：目录只在顶层；外链与顶层页面可在顶层或目录下；子页面钉在登记的父页面下。
/// </summary>
/// <param name="menus">菜单仓储。</param>
/// <param name="sections">各包的页面登记。</param>
public sealed class MenuService(IRepository<SysMenu, Guid> menus, IEnumerable<PageCatalogSection> sections)
{
    /// <summary>
    /// 运营节点的包名与编码前缀。
    /// </summary>
    public const string CustomPackage = "custom";

    /// <summary>
    /// 菜单只在平台侧管理（权限是平台侧的；不受套餐限制的租户持有 <c>*</c> 也在这里拒绝）。
    /// </summary>
    public static Error? RequirePlatform(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.GetSecurity().Tenant is null ? null : Error.Denied(AccessErrors.MenuPlatformOnly, AccessMessages.MenuPlatformOnly);
    }

    /// <summary>
    /// 运营节点的编码。
    /// </summary>
    public static string CustomCode(string name) => $"{CustomPackage}.{name}";

    /// <summary>
    /// 全部菜单行。
    /// </summary>
    public Task<IReadOnlyList<SysMenu>> ListAsync(CancellationToken cancellationToken) => menus.ListAsync(EntityQuery.All<SysMenu>(), cancellationToken);

    /// <summary>
    /// 在页面登记里找页面：返回登记与父页面码（顶层为空）；登记里没有即空。
    /// </summary>
    public (PageDescriptor Page, string? ParentCode)? FindRegistered(string code)
    {
        foreach (var section in sections)
        {
            if (Find(section.Pages, null) is { } found)
            {
                return found;
            }
        }

        return null;

        (PageDescriptor, string?)? Find(IReadOnlyList<PageDescriptor> pages, string? parent)
        {
            foreach (var page in pages)
            {
                if (string.Equals(page.Code, code, StringComparison.Ordinal))
                {
                    return (page, parent);
                }

                if (Find(page.Children, page.Code) is { } child)
                {
                    return child;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// 校验上级节点：种类为 <paramref name="kind"/> 的节点改挂到 <paramref name="parentId"/> 是否合规（见类说明）；<paramref name="registeredChild"/> 为真时只能留在 <paramref name="currentParentId"/>。
    /// </summary>
    public static Error? CheckParent(MenuKind kind, bool registeredChild, Guid? currentParentId, Guid? parentId, IReadOnlyList<SysMenu> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        if (registeredChild)
        {
            return parentId == currentParentId ? null : Error.Validation(AccessErrors.MenuParentInvalid, AccessMessages.MenuParentInvalid);
        }

        if (parentId is null)
        {
            return null;
        }

        if (kind == MenuKind.Directory)
        {
            return Error.Validation(AccessErrors.MenuParentInvalid, AccessMessages.MenuParentInvalid);
        }

        var parent = rows.FirstOrDefault(r => r.Id == parentId.Value);
        return parent is { Kind: MenuKind.Directory } ? null : Error.Validation(AccessErrors.MenuParentInvalid, AccessMessages.MenuParentInvalid);
    }

    /// <summary>
    /// 外链地址须是 http / https 的绝对地址。
    /// </summary>
    public static bool IsValidUrl(string? url)
        => !string.IsNullOrWhiteSpace(url)
           && Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
           && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    /// <summary>
    /// 把页面节点的展示字段恢复为登记的值并清掉定制标记。
    /// </summary>
    public static void ResetPresentation(SysMenu row, PageDescriptor page, Guid? parentId)
    {
        ArgumentNullException.ThrowIfNull(row);
        ArgumentNullException.ThrowIfNull(page);
        row.Title = null;
        row.Icon = page.Icon;
        row.Order = page.Order;
        row.ParentId = parentId;
        row.IsVisible = true;
        row.KeepAlive = false;
        row.IsAffix = false;
        row.IsCustomized = false;
    }
}
