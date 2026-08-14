// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Application.Pages;
using XiHan.BasicApp.Saas.Infrastructure.Seeders.System;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.MultiTenancy.Abstractions;
using CodeGenPageRegistry = XiHan.BasicApp.CodeGeneration.Application.Pages.PageRegistry;

namespace XiHan.BasicApp.CodeGeneration.Infrastructure.Seeders.System;

/// <summary>
/// 代码生成模块菜单种子数据
/// </summary>
/// <remarks>置于本模块权限种子之后：菜单建即绑权限，权限缺失会被跳过。</remarks>
public sealed class CodeGenerationMenuSeeder(
    ISqlSugarClientResolver clientResolver,
    ILogger<CodeGenerationMenuSeeder> logger,
    IServiceProvider serviceProvider,
    ICurrentTenant currentTenant)
    : PageRegistryMenuSeederBase(clientResolver, logger, serviceProvider, currentTenant)
{
    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => 103;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[CodeGeneration]模块菜单种子数据";

    /// <summary>
    /// 日志中标识模块的名称
    /// </summary>
    protected override string ModuleName => "代码生成";

    /// <summary>
    /// 本模块登记的页面（父目录须排在子项之前，按顺序解析 ParentId）
    /// </summary>
    protected override IReadOnlyList<PageDescriptor> Pages => CodeGenPageRegistry.All;

    /// <summary>
    /// 本模块登记的页面内按钮
    /// </summary>
    protected override IReadOnlyList<ButtonDescriptor> Buttons => CodeGenPageRegistry.Buttons;
}
