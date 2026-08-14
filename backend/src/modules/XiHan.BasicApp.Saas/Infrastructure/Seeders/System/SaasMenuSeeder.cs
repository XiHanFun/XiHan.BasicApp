// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Application.Pages;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Infrastructure.Seeders.System;

/// <summary>
/// SaaS 菜单种子数据
/// </summary>
public sealed class SaasMenuSeeder(
    ISqlSugarClientResolver clientResolver,
    ILogger<SaasMenuSeeder> logger,
    IServiceProvider serviceProvider,
    ICurrentTenant currentTenant)
    : PageRegistryMenuSeederBase(clientResolver, logger, serviceProvider, currentTenant)
{
    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => 25;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[SaaS]系统菜单种子数据";

    /// <summary>
    /// 日志中标识模块的名称
    /// </summary>
    protected override string ModuleName => "SaaS";

    /// <summary>
    /// 本模块登记的页面（父目录须排在子项之前，按顺序解析 ParentId）
    /// </summary>
    protected override IReadOnlyList<PageDescriptor> Pages => PageRegistry.All;

    /// <summary>
    /// 本模块登记的页面内按钮
    /// </summary>
    protected override IReadOnlyList<ButtonDescriptor> Buttons => PageRegistry.Buttons;
}
