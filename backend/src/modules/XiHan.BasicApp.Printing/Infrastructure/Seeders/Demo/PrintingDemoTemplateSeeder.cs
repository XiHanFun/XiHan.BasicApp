// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Printing.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Infrastructure.Seeders;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Printing.Infrastructure.Seeders;

/// <summary>
/// 打印演示模板种子：六张平台全局模板（TenantId = 0，对租户开放），演示开关打开时才写
/// </summary>
/// <remarks>
/// 按模板编码先查后写（含已删除的行），已有的不改、删掉的不补回；模板内容见 <see cref="PrintingDemoCatalog"/>。
/// </remarks>
public sealed class PrintingDemoTemplateSeeder(
    ISqlSugarClientResolver clientResolver,
    ILogger<PrintingDemoTemplateSeeder> logger,
    IServiceProvider serviceProvider)
    : DemoDataSeederBase(clientResolver, logger, serviceProvider)
{
    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => SeedOrders.Demo + 50;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[Printing]演示模板";

    /// <summary>
    /// 写入演示模板
    /// </summary>
    protected override async Task SeedDemoAsync()
    {
        var codes = PrintingDemoCatalog.Templates.Select(static template => template.Code).ToList();
        var existing = (await DbClientFor<SysPrintTemplate>().Queryable<SysPrintTemplate>()
                .IncludingDeleted()
                .Where(template => template.TenantId == 0 && codes.Contains(template.TemplateCode))
                .Select(template => template.TemplateCode)
                .ToListAsync())
            .ToHashSet(StringComparer.Ordinal);

        var added = 0;
        foreach (var (demo, index) in PrintingDemoCatalog.Templates.Select((template, index) => (template, index)))
        {
            if (existing.Contains(demo.Code))
            {
                continue;
            }

            _ = await DbClientFor<SysPrintTemplate>().Insertable(new SysPrintTemplate
            {
                TenantId = 0,
                TemplateCode = demo.Code,
                TemplateName = demo.Name,
                DataSourceCode = demo.DataSourceCode,
                TemplateJson = PrintingDemoCatalog.LoadTemplateJson(demo.Code),
                AllowTenantUse = true,
                Status = EnableStatus.Enabled,
                Sort = (index + 1) * 10,
                Remark = demo.Remark
            }).ExecuteCommandAsync();
            added++;
        }

        Logger.LogInformation("{Seeder}：新增 {Added} 张演示模板，已有 {Existing} 张", Name, added, existing.Count);
    }
}
