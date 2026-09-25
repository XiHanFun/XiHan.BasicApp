// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Application.Pages;
using XiHan.BasicApp.Saas.Infrastructure.Seeders;
using XiHan.Framework.Data.SqlSugar.Clients;
using AiPageRegistry = XiHan.BasicApp.AI.Application.Pages.PageRegistry;

namespace XiHan.BasicApp.AI.Infrastructure.Seeders;

/// <summary>
/// AI 菜单：本模块页面登记表里的页面与按钮
/// </summary>
public sealed class AiMenuSeeder(
    ISqlSugarClientResolver clientResolver,
    ILogger<AiMenuSeeder> logger,
    IServiceProvider serviceProvider)
    : PageRegistryMenuSeederBase(clientResolver, logger, serviceProvider)
{
    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => SeedOrders.Menus + 20;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[AI]菜单";

    /// <summary>
    /// 本模块登记的页面（父目录须排在子项之前，按顺序解析 ParentId）
    /// </summary>
    protected override IReadOnlyList<PageDescriptor> Pages => AiPageRegistry.All;

    /// <summary>
    /// 本模块登记的页面内按钮
    /// </summary>
    protected override IReadOnlyList<ButtonDescriptor> Buttons => AiPageRegistry.Buttons;
}
