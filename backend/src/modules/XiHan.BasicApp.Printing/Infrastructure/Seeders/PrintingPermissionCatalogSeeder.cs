// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Printing.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Infrastructure.Seeders;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Printing.Infrastructure.Seeders;

/// <summary>
/// 打印权限目录：打印模板的查看、创建、编辑、启停、删除、使用与全局管理
/// </summary>
/// <remarks>模板在平台与租户都能维护与使用（两侧生效）；全局模板及其对租户的开放只在平台管理（平台侧）。</remarks>
public sealed class PrintingPermissionCatalogSeeder(
    ISqlSugarClientResolver clientResolver,
    ILogger<PrintingPermissionCatalogSeeder> logger,
    IServiceProvider serviceProvider)
    : PermissionCatalogSeederBase(clientResolver, logger, serviceProvider)
{
    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => SeedOrders.PermissionCatalog + 50;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[Printing]权限目录";

    /// <summary>
    /// 模块编码
    /// </summary>
    public override string ModuleCode => PrintingPermissionCodes.Module;

    /// <summary>
    /// 本模块的权限
    /// </summary>
    public override IReadOnlyList<PermissionSeed> Permissions { get; } =
    [
        new(PrintingPermissionCodes.Read, "打印模板查看", "查看当前作用域打印模板列表与详情", PrintingPermissionCodes.Module, PermissionSide.Both, false, 3400),
        new(PrintingPermissionCodes.Create, "打印模板创建", "创建当前作用域打印模板", PrintingPermissionCodes.Module, PermissionSide.Both, true, 3401),
        new(PrintingPermissionCodes.Update, "打印模板编辑", "编辑打印模板元数据和 hiprint 设计 JSON", PrintingPermissionCodes.Module, PermissionSide.Both, true, 3402),
        new(PrintingPermissionCodes.Status, "打印模板启停", "启用或停用打印模板", PrintingPermissionCodes.Module, PermissionSide.Both, true, 3403),
        new(PrintingPermissionCodes.Delete, "打印模板删除", "删除已经停用的打印模板", PrintingPermissionCodes.Module, PermissionSide.Both, true, 3404),
        new(PrintingPermissionCodes.Use, "打印模板使用", "按编码解析模板并执行预览或直接打印", PrintingPermissionCodes.Module, PermissionSide.Both, true, 3405),
        new(PrintingPermissionCodes.GlobalManage, "全局打印模板管理", "管理平台全局打印模板及租户开放状态", PrintingPermissionCodes.Module, PermissionSide.Platform, true, 3406),
    ];
}
