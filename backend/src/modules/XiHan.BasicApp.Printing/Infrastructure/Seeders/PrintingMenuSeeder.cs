// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Application.Pages;
using XiHan.BasicApp.Saas.Infrastructure.Seeders;
using XiHan.Framework.Data.SqlSugar.Clients;
using PrintingPageRegistry = XiHan.BasicApp.Printing.Application.Pages.PageRegistry;

namespace XiHan.BasicApp.Printing.Infrastructure.Seeders;

/// <summary>
/// 打印菜单：本模块页面登记表里的页面与按钮
/// </summary>
public sealed class PrintingMenuSeeder(
    ISqlSugarClientResolver clientResolver,
    ILogger<PrintingMenuSeeder> logger,
    IServiceProvider serviceProvider)
    : PageRegistryMenuSeederBase(clientResolver, logger, serviceProvider)
{
    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => SeedOrders.Menus + 50;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[Printing]菜单";

    /// <summary>
    /// 本模块登记的页面（父目录须排在子项之前，按顺序解析 ParentId）
    /// </summary>
    protected override IReadOnlyList<PageDescriptor> Pages => PrintingPageRegistry.All;

    /// <summary>
    /// 本模块登记的页面内按钮
    /// </summary>
    protected override IReadOnlyList<ButtonDescriptor> Buttons => PrintingPageRegistry.Buttons;
}
