// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.CodeGeneration.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Infrastructure.Seeders;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.CodeGeneration.Infrastructure.Seeders;

/// <summary>
/// 代码生成权限目录：代码生成资源的读取、创建、更新、删除、导入表结构、执行生成
/// </summary>
/// <remarks>代码生成是开发工具，只在平台可用（平台侧权限）。</remarks>
public sealed class CodeGenPermissionCatalogSeeder(
    ISqlSugarClientResolver clientResolver,
    ILogger<CodeGenPermissionCatalogSeeder> logger,
    IServiceProvider serviceProvider)
    : PermissionCatalogSeederBase(clientResolver, logger, serviceProvider)
{
    private static readonly ResourceSeed CodeGen = new(CodeGenPermissionCodes.Resource, "代码生成", "/api/codegen", "按数据表生成前后端代码的接口", 100);

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => SeedOrders.PermissionCatalog + 10;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[CodeGeneration]权限目录";

    /// <summary>
    /// 模块编码
    /// </summary>
    public override string ModuleCode => CodeGenPermissionCodes.Module;

    /// <summary>
    /// 本模块的资源
    /// </summary>
    public override IReadOnlyList<ResourceSeed> Resources { get; } = [CodeGen];

    /// <summary>
    /// 本模块的权限
    /// </summary>
    public override IReadOnlyList<PermissionSeed> Permissions { get; } =
    [
        .. PermissionSeed.Of(CodeGen, PermissionSide.Platform, 3000,
            OperationSeeds.Read, OperationSeeds.Create, OperationSeeds.Update, OperationSeeds.Delete, OperationSeeds.Import, OperationSeeds.Execute),
    ];
}
