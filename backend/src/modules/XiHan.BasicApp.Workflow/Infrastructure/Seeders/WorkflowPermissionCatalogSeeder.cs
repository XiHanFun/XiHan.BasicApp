// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Infrastructure.Seeders;
using XiHan.BasicApp.Workflow.Domain.Permissions;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Workflow.Infrastructure.Seeders;

/// <summary>
/// 工作流权限目录：工作流资源的读取、创建、更新、删除、执行
/// </summary>
/// <remarks>工作流是租户业务能力，平台用不到（租户侧权限）。</remarks>
public sealed class WorkflowPermissionCatalogSeeder(
    ISqlSugarClientResolver clientResolver,
    ILogger<WorkflowPermissionCatalogSeeder> logger,
    IServiceProvider serviceProvider)
    : PermissionCatalogSeederBase(clientResolver, logger, serviceProvider)
{
    private static readonly ResourceSeed Workflow = new(WorkflowPermissionCodes.Resource, "工作流", "/api/workflow", "流程定义、实例与待办接口", 300);

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => SeedOrders.PermissionCatalog + 30;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[Workflow]权限目录";

    /// <summary>
    /// 模块编码
    /// </summary>
    public override string ModuleCode => WorkflowPermissionCodes.Module;

    /// <summary>
    /// 本模块的资源
    /// </summary>
    public override IReadOnlyList<ResourceSeed> Resources { get; } = [Workflow];

    /// <summary>
    /// 本模块的权限
    /// </summary>
    public override IReadOnlyList<PermissionSeed> Permissions { get; } =
    [
        .. PermissionSeed.Of(Workflow, PermissionSide.Tenant, 3200,
            OperationSeeds.Read, OperationSeeds.Create, OperationSeeds.Update, OperationSeeds.Delete, OperationSeeds.Execute),
    ];
}
