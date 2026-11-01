// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Seeding;
using XiHan.BasicApp.Printing.Domain.Entities;
using XiHan.BasicApp.Printing.Domain.Services;
using XiHan.BasicApp.Runtime.Seeding;
using XiHan.Framework.Caching.Abstractions.Caches;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Printing.Seeds;

/// <summary>
/// 打印演示模板的种子。
/// </summary>
/// <remarks>
/// 号段 900 + 50（照 4.x 的 <c>Demo + 50</c>），<c>BasicApp:Seed:EnableDemoData</c> 打开才写：六张平台全局模板，向租户开放、启用，排序 10～60。
/// 按编码先查后写（含已删除的行）：已有的不改，删掉的不补回（只首写）。模板内容见 <see cref="PrintingDemoCatalog"/>。
/// </remarks>
/// <param name="templates">模板仓储。</param>
/// <param name="ids">标识生成器。</param>
/// <param name="options">种子配置。</param>
/// <param name="cache">共享缓存（失效此前缓存的「没有」）。</param>
public sealed class DemoPrintTemplateSeeder(IRepository<SysPrintTemplate, Guid> templates, IIdGenerator<Guid> ids, SeedOptions options, ISharedCache cache) : ISeeder
{
    /// <inheritdoc />
    public string Name => "printing.demo";

    /// <inheritdoc />
    public int Order => SeedOrders.Demo + (SeedOrders.ModuleOffset * 5);

    /// <inheritdoc />
    public async ValueTask<Result> SeedAsync(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!options.EnableDemoData)
        {
            return Result.Success();
        }

        const string platform = TenantIds.Platform;
        var codes = PrintingDemoCatalog.Templates.Select(static t => t.Code).ToList();
        var query = EntityQuery.All<SysPrintTemplate>().Where(new Specification<SysPrintTemplate>(t => t.OwnerTenantId == platform && codes.Contains(t.TemplateCode))) with { IncludeDeleted = true };
        var existing = (await templates.ListAsync(query, context.CancellationToken).ConfigureAwait(false)).Select(static t => t.TemplateCode).ToHashSet(StringComparer.Ordinal);
        foreach (var (demo, index) in PrintingDemoCatalog.Templates.Select(static (t, i) => (t, i)))
        {
            if (existing.Contains(demo.Code))
            {
                continue;
            }

            var template = new SysPrintTemplate
            {
                Id = ids.NewId(),
                OwnerTenantId = platform,
                TemplateCode = demo.Code,
                TemplateName = demo.Name,
                DataSourceCode = demo.DataSourceCode,
                TemplateJson = PrintingDemoCatalog.LoadTemplateJson(demo.Code),
                AllowTenantUse = true,
                Status = PrintTemplateStatus.Active,
                Order = (index + 1) * 10,
                Remark = demo.Remark,
            };
            var added = await templates.AddAsync(template, context.CancellationToken).ConfigureAwait(false);
            if (added.IsFailure)
            {
                return added;
            }

            await PrintTemplateResolver.InvalidateAsync(cache, template, context.CancellationToken).ConfigureAwait(false);
        }

        return Result.Success();
    }
}
