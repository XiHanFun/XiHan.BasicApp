// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Application.Pages;
using XiHan.BasicApp.Saas.Infrastructure.Seeders.System;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.MultiTenancy.Abstractions;
using PrintingPageRegistry = XiHan.BasicApp.Printing.Application.Pages.PageRegistry;

namespace XiHan.BasicApp.Printing.Infrastructure.Seeders.System;

/// <summary>
/// 打印模块菜单种子数据
/// </summary>
/// <remarks>置于本模块权限种子之后：菜单建即绑权限，权限缺失会被跳过。页面挂靠 Saas 的 setting 目录。</remarks>
public sealed class PrintingMenuSeeder(
    ISqlSugarClientResolver clientResolver,
    ILogger<PrintingMenuSeeder> logger,
    IServiceProvider serviceProvider,
    ICurrentTenant currentTenant)
    : PageRegistryMenuSeederBase(clientResolver, logger, serviceProvider, currentTenant)
{
    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => 501;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[Printing]模块菜单种子数据";

    /// <inheritdoc />
    protected override string ModuleName => "Printing";

    /// <inheritdoc />
    protected override IReadOnlyList<PageDescriptor> Pages => PrintingPageRegistry.All;

    /// <inheritdoc />
    protected override IReadOnlyList<ButtonDescriptor> Buttons => PrintingPageRegistry.Buttons;
}
