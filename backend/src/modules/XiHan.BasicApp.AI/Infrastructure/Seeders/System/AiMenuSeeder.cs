// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Application.Pages;
using XiHan.BasicApp.Saas.Infrastructure.Seeders.System;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.MultiTenancy.Abstractions;
using AiPageRegistry = XiHan.BasicApp.AI.Application.Pages.PageRegistry;

namespace XiHan.BasicApp.AI.Infrastructure.Seeders.System;

/// <summary>
/// AI 模块菜单种子数据
/// </summary>
/// <remarks>置于本模块全部权限种子之后：菜单建即绑权限，权限缺失会被跳过。</remarks>
public sealed class AiMenuSeeder(
    ISqlSugarClientResolver clientResolver,
    ILogger<AiMenuSeeder> logger,
    IServiceProvider serviceProvider,
    ICurrentTenant currentTenant)
    : PageRegistryMenuSeederBase(clientResolver, logger, serviceProvider, currentTenant)
{
    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => 217;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[Ai]模块菜单种子数据";

    /// <summary>
    /// 日志中标识模块的名称
    /// </summary>
    protected override string ModuleName => "AI";

    /// <summary>
    /// 本模块登记的页面（父目录须排在子项之前，按顺序解析 ParentId）
    /// </summary>
    protected override IReadOnlyList<PageDescriptor> Pages => AiPageRegistry.All;

    /// <summary>
    /// 本模块登记的页面内按钮
    /// </summary>
    protected override IReadOnlyList<ButtonDescriptor> Buttons => AiPageRegistry.Buttons;
}
