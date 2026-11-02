// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Pages;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Runtime;
using XiHan.BasicApp.Runtime.Seeding;
using XiHan.BasicApp.Workflow.Domain.Services;
using XiHan.BasicApp.Workflow.Infrastructure;
using XiHan.BasicApp.Workflow.Seeds;
using XiHan.Framework.Kernel;
using XiHan.Framework.Kernel.Hosting;
using XiHan.Framework.Localization.Abstractions;
using XiHan.Framework.Persistence.Abstractions;
using XiHan.Framework.Primitives;
using XiHan.Framework.Primitives.Manifests;
using XiHan.Framework.Security.Abstractions;
using XiHan.Framework.Workflow.Abstractions;
using EngineCapabilities = XiHan.Framework.Workflow.Abstractions.WorkflowCapabilities;

namespace XiHan.BasicApp.Workflow;

/// <summary>
/// 工作流模块（Tier 3，可选）。
/// </summary>
/// <remarks>
/// 引擎、定义目录与存储是框架的数据库工作流（宿主恒装，能力 <c>workflow.execution@1</c>）；本包登记组织类受理人（角色、部门、部门负责人、岗位、发起人主管与主管链）、
/// 审批业务登记表（各包经 Contracts 的 <c>AddWorkflowBusiness</c> 登记，同一业务码两次即启动失败）、定义与实例的管理操作、选人操作、权限目录与页面，
/// 以及演示数据打开时在演示租户里建四个演示流程。宿主按 <c>BasicApp:Modules:Workflow:Enabled</c> 装配。
/// </remarks>
public sealed class WorkflowFeature : IFeature
{
    /// <inheritdoc />
    public Manifest Manifest { get; } = new()
    {
        Id = AppInfo.ManifestIdPrefix + "workflow",
        Package = "XiHan.BasicApp.Workflow",
        Version = AppInfo.Version,
        Primitives = 1,
        Stability = Stability.Preview,
        Kind = ManifestKind.Capability,
        Provides = [new CapabilityDeclaration(WorkflowCapabilities.Workflow, WorkflowCapabilities.Version)],
        Requires =
        [
            new CapabilityRequirement(KernelCapabilities.Hosting, KernelCapabilities.Version),
            new CapabilityRequirement(PersistenceCapabilities.Relational, PersistenceCapabilities.Version),
            new CapabilityRequirement(SecurityCapabilities.Authorization, SecurityCapabilities.Version),
            new CapabilityRequirement(RuntimeCapabilities.Runtime, RuntimeCapabilities.Version),
            new CapabilityRequirement(EngineCapabilities.Execution, EngineCapabilities.Version),
        ],
        Contributes = new Contributions
        {
            Permissions = WorkflowPermissions.All.Select(static p => p.Code).ToList(),
        },
        Entry = typeof(WorkflowFeature).FullName,
    };

    /// <inheritdoc />
    public void Configure(FeatureContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Services.AddMessageCatalog(WorkflowMessages.Catalog);
        context.Services.AddScoped<WorkflowBusinessRegistry>();

        // 构造一次登记表：同一业务码登记两次在启动阶段就失败（照 4.x 的「同编码多个即抛错」，提前到装配）
        context.AddStartupCheck(static services =>
        {
            using var scope = services.CreateScope();
            _ = scope.ServiceProvider.GetRequiredService<WorkflowBusinessRegistry>();
        });
        context.Services.AddAssigneeProvider<RoleAssigneeProvider>();
        context.Services.AddAssigneeProvider<DepartmentAssigneeProvider>();
        context.Services.AddAssigneeProvider<DepartmentLeaderAssigneeProvider>();
        context.Services.AddAssigneeProvider<PositionAssigneeProvider>();
        context.Services.AddAssigneeProvider<StarterLeaderAssigneeProvider>();
        context.Services.AddAssigneeProvider<StarterLeaderChainAssigneeProvider>();
        context.Services.AddPermissionCatalog("workflow", WorkflowPermissions.All);

        // 字段安全：定义与实例只登记读的部分（引擎表没有应用实体，取不到写入的原值）
        WorkflowFieldSecurity.Register(context.Services);
        context.Services.AddPageCatalog("workflow", WorkflowPages.All);
        foreach (var permission in WorkflowPermissions.All)
        {
            context.AddPermission(permission.ToDefinition());
        }

        context.Services.AddSeeder<WorkflowDemoSeeder>();
        context.AddGeneratedOperations();
    }
}
