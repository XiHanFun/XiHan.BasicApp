// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Contracts.Pages;
using XiHan.BasicApp.Contracts.Seeding;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Seeds;

/// <summary>
/// 菜单种子。
/// </summary>
/// <remarks>
/// 号段 400，目录类每次对齐元数据。
/// 各包的页面登记 → 页面节点。按页面码匹配：有则对齐，没有则新增，登记里已不存在的页面节点删除；子页面挂到父页面。
/// 登记的字段（包、路由、组件、标题键、权限、子页面的上级）每次对齐；展示字段（图标、排序、顶层页面的上级等）只在节点没被运营定制时对齐。
/// 运营建的目录与外链节点不碰。
/// </remarks>
/// <param name="menus">菜单仓储。</param>
/// <param name="sections">各包的页面登记。</param>
/// <param name="ids">标识生成器。</param>
public sealed class MenuSeeder(IRepository<SysMenu, Guid> menus, IEnumerable<PageCatalogSection> sections, IIdGenerator<Guid> ids) : ISeeder
{
    /// <inheritdoc />
    public string Name => "access.menus";

    /// <inheritdoc />
    public int Order => SeedOrders.Menus;

    /// <inheritdoc />
    public async ValueTask<Result> SeedAsync(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var existing = (await menus.ListAsync(EntityQuery.All<SysMenu>(), context.CancellationToken).ConfigureAwait(false)).ToDictionary(static m => m.Code, StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var section in sections.OrderBy(static s => s.Package, StringComparer.Ordinal))
        {
            var aligned = await AlignAsync(section.Package, section.Pages, parentId: null, existing, seen, context.CancellationToken).ConfigureAwait(false);
            if (aligned.IsFailure)
            {
                return aligned;
            }
        }

        var stale = existing.Values.Where(m => m.Kind == MenuKind.Page && !seen.Contains(m.Code)).ToList();
        if (stale.Count > 0)
        {
            foreach (var outcome in await menus.RemoveRangeAsync(stale, context.CancellationToken).ConfigureAwait(false))
            {
                if (outcome.IsFailure)
                {
                    return outcome;
                }
            }
        }

        return Result.Success();
    }

    private async Task<Result> AlignAsync(string package, IReadOnlyList<PageDescriptor> pages, Guid? parentId, Dictionary<string, SysMenu> existing, HashSet<string> seen, CancellationToken cancellationToken)
    {
        foreach (var page in pages)
        {
            seen.Add(page.Code);
            Result written;
            if (existing.TryGetValue(page.Code, out var row))
            {
                if (Differs(row, page, package, parentId))
                {
                    Apply(row, page, package, parentId);
                    written = await menus.UpdateAsync(row, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    written = Result.Success();
                }
            }
            else
            {
                row = new SysMenu { Id = ids.NewId(), Code = page.Code, Kind = MenuKind.Page };
                Apply(row, page, package, parentId);
                written = await menus.AddAsync(row, cancellationToken).ConfigureAwait(false);
                existing[page.Code] = row;
            }

            if (written.IsFailure)
            {
                return written;
            }

            var children = await AlignAsync(package, page.Children, row.Id, existing, seen, cancellationToken).ConfigureAwait(false);
            if (children.IsFailure)
            {
                return children;
            }
        }

        return Result.Success();
    }

    // 子页面的上级归登记；顶层页面的上级是展示字段（可挂到运营目录下）
    private static bool Differs(SysMenu row, PageDescriptor page, string package, Guid? parentId)
        => row.Kind != MenuKind.Page
           || !string.Equals(row.Package, package, StringComparison.Ordinal)
           || !string.Equals(row.Route, page.Route, StringComparison.Ordinal)
           || !string.Equals(row.Component, page.Component, StringComparison.Ordinal)
           || !string.Equals(row.TitleKey, page.TitleKey, StringComparison.Ordinal)
           || !string.Equals(row.Permission, page.Permission, StringComparison.Ordinal)
           || row.ExternalUrl is not null
           || (parentId is not null && row.ParentId != parentId)
           || (!row.IsCustomized && (row.ParentId != parentId || !string.Equals(row.Icon, page.Icon, StringComparison.Ordinal) || row.Order != page.Order));

    private static void Apply(SysMenu row, PageDescriptor page, string package, Guid? parentId)
    {
        row.Kind = MenuKind.Page;
        row.Package = package;
        row.Route = page.Route;
        row.Component = page.Component;
        row.TitleKey = page.TitleKey;
        row.Permission = page.Permission;
        row.ExternalUrl = null;
        if (parentId is not null)
        {
            row.ParentId = parentId;
        }

        if (!row.IsCustomized)
        {
            row.ParentId = parentId;
            row.Icon = page.Icon;
            row.Order = page.Order;
        }
    }
}
