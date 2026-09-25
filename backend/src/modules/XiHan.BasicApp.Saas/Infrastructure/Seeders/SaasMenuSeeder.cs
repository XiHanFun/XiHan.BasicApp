// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Application.Pages;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Seeders;

/// <summary>
/// SaaS 菜单：<see cref="PageRegistry"/> 登记的页面与按钮
/// </summary>
public sealed class SaasMenuSeeder(
    ISqlSugarClientResolver clientResolver,
    ILogger<SaasMenuSeeder> logger,
    IServiceProvider serviceProvider)
    : PageRegistryMenuSeederBase(clientResolver, logger, serviceProvider)
{
    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => SeedOrders.Menus;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[SaaS]菜单";

    /// <summary>
    /// 本模块登记的页面（父目录须排在子项之前，按顺序解析 ParentId）
    /// </summary>
    protected override IReadOnlyList<PageDescriptor> Pages => PageRegistry.All;

    /// <summary>
    /// 本模块登记的页面内按钮
    /// </summary>
    protected override IReadOnlyList<ButtonDescriptor> Buttons => PageRegistry.Buttons;
}
