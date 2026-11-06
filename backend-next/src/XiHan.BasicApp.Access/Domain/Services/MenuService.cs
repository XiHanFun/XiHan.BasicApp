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
/// 菜单域服务
/// </summary>
/// <remarks>
/// 只在平台侧管理、页面登记的查找（恢复默认用）、上级节点的规则、外链地址的规则
/// 规则：目录、外链与顶层页面可在顶层或某个目录下，目录可以一层层套下去，但不能挂到自己或自己的下级目录下；子页面钉在登记的父页面下
/// </remarks>
/// <param name="menus">菜单仓储</param>
/// <param name="sections">各包的页面登记</param>
public sealed class MenuService(IRepository<SysMenu, Guid> menus, IEnumerable<PageCatalogSection> sections)
{
    /// <summary>
    /// 运营节点的包名与编码前缀
    /// </summary>
    public const string CustomPackage = "custom";

    /// <summary>
    /// 要求菜单只在平台侧管理
    /// </summary>
    /// <remarks>
    /// 权限是平台侧的；不受套餐限制的租户持有 <c>*</c> 也在这里拒绝
    /// </remarks>
    public static Error? RequirePlatform(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.GetSecurity().Tenant is null ? null : Error.Denied(AccessErrors.MenuPlatformOnly, AccessMessages.MenuPlatformOnly);
    }

    /// <summary>
    /// 运营节点的编码
    /// </summary>
    public static string CustomCode(string name) => $"{CustomPackage}.{name}";

    /// <summary>
    /// 全部菜单行
    /// </summary>
    public Task<IReadOnlyList<SysMenu>> ListAsync(CancellationToken cancellationToken) => menus.ListAsync(EntityQuery.All<SysMenu>(), cancellationToken);

    /// <summary>
    /// 在页面登记里找页面
    /// </summary>
    /// <returns>登记与父页面码（顶层为空）；登记里没有即空</returns>
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
    /// 校验上级节点
    /// </summary>
    /// <remarks>
    /// 种类为 <paramref name="kind"/> 的节点改挂到 <paramref name="parentId"/> 是否合规（见类说明）；<paramref name="registeredChild"/> 为真时只能留在 <paramref name="currentParentId"/>
    /// <paramref name="selfId"/> 是已有节点的主键（新建为空）：目录沿上级链往上走，碰到自己即成环
    /// </remarks>
    public static Error? CheckParent(MenuKind kind, bool registeredChild, Guid? currentParentId, Guid? parentId, IReadOnlyList<SysMenu> rows, Guid? selfId = null)
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

        var byId = rows.ToDictionary(static r => r.Id);
        if (!byId.TryGetValue(parentId.Value, out var parent) || parent.Kind != MenuKind.Directory)
        {
            return Error.Validation(AccessErrors.MenuParentInvalid, AccessMessages.MenuParentInvalid);
        }

        if (kind != MenuKind.Directory || selfId is null)
        {
            return null;
        }

        // 只有目录能当上级，所以环只会出在目录之间：新上级是自己，或在自己的子树里
        // 走过的节点记下来：库里万一已有环（绕过操作写进去的）也不会转不出来
        var seen = new HashSet<Guid>();
        var cursor = parent;
        while (cursor is not null && seen.Add(cursor.Id))
        {
            if (cursor.Id == selfId.Value)
            {
                return Error.Validation(AccessErrors.MenuParentInvalid, AccessMessages.MenuParentInvalid);
            }

            cursor = cursor.ParentId is { } next && byId.TryGetValue(next, out var up) ? up : null;
        }

        return null;
    }

    /// <summary>
    /// 外链地址须是 http / https 的绝对地址
    /// </summary>
    public static bool IsValidUrl(string? url)
        => !string.IsNullOrWhiteSpace(url)
           && Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
           && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    /// <summary>
    /// 顶层页面的缺省上级：登记的目录
    /// </summary>
    /// <returns>目录节点的主键；页面没登记目录或目录不在（运营删了）即空</returns>
    public static Guid? DirectoryOf(PageDescriptor page, IEnumerable<SysMenu> rows)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(rows);
        return page.Directory is null
            ? null
            : rows.FirstOrDefault(r => r.Kind == MenuKind.Directory && string.Equals(r.Code, page.Directory, StringComparison.Ordinal))?.Id;
    }

    /// <summary>
    /// 把页面节点的展示字段恢复为登记的值并清掉定制标记
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
        row.Status = MenuStatus.Active;
        row.Badge = null;
        row.BadgeType = null;
        row.BadgeDot = false;
        row.IsCustomized = false;
    }
}
