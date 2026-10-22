// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Workflow.Application.Business;
using XiHan.BasicApp.Workflow.Application.EventHandlers;
using XiHan.BasicApp.Workflow.Infrastructure.Assignees;
using XiHan.BasicApp.Workflow.Infrastructure.Seeders;
using XiHan.BasicApp.Workflow.Infrastructure.Stores;
using XiHan.Framework.Data.Extensions.DependencyInjection;
using XiHan.Framework.EventBus.Local;
using XiHan.Framework.Utils.Collections;
using XiHan.Framework.Workflow.Abstractions.Outcomes;
using XiHan.Framework.Workflow.Abstractions.Stores;
using XiHan.Framework.Workflow.Extensions.DependencyInjection;
using XiHan.BasicApp.Workflow.Domain.Entities;
using XiHan.BasicApp.Saas.Extensions;

namespace XiHan.BasicApp.Workflow.Extensions;

/// <summary>
/// 工作流模块服务注册扩展
/// </summary>
/// <remarks>
/// 自动注册边界：仓储（SaasRepository 链含 IScopedDependency）与应用服务（IApplicationService）由框架约定扫描注册，勿手动登记；
/// 存储替换须用 Replace（框架 AddXiHanWorkflow 已 TryAddSingleton 内存默认实现，TryAdd 会被静默忽略）；
/// 本地事件处理器须加入 XiHanLocalEventBusOptions.Handlers（裸 AddTransient 不会被订阅）。
/// </remarks>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 替换框架工作流存储为 SqlSugar 持久化实现（定义/实例/书签落库，获得崩溃恢复能力）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddWorkflowStores(this IServiceCollection services)
    {
        services.Replace(ServiceDescriptor.Singleton<IWorkflowDefinitionStore, SqlSugarWorkflowDefinitionStore>());
        services.Replace(ServiceDescriptor.Singleton<IWorkflowInstanceStore, SqlSugarWorkflowInstanceStore>());
        services.Replace(ServiceDescriptor.Singleton<IWorkflowBookmarkStore, SqlSugarWorkflowBookmarkStore>());
        return services;
    }

    /// <summary>
    /// 注册组织架构受理人解析器（角色、部门、部门负责人、岗位、发起人主管）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddWorkflowAssigneeProviders(this IServiceCollection services)
    {
        services.TryAddScoped<WorkflowOrganizationDirectory>();
        services.AddXiHanWorkflowAssigneeProvider<RoleAssigneeProvider>();
        services.AddXiHanWorkflowAssigneeProvider<DepartmentAssigneeProvider>();
        services.AddXiHanWorkflowAssigneeProvider<DepartmentLeaderAssigneeProvider>();
        services.AddXiHanWorkflowAssigneeProvider<PositionAssigneeProvider>();
        services.AddXiHanWorkflowAssigneeProvider<StarterLeaderAssigneeProvider>();
        services.AddXiHanWorkflowAssigneeProvider<StarterLeaderChainAssigneeProvider>();
        return services;
    }

    /// <summary>
    /// 注册审批业务接入（业务登记表、发起/撤回/进度服务、结论分发、中止回写、发布校验、审批记录）
    /// </summary>
    /// <remarks>
    /// Saas 先以「工作流未启用」实现占位 <see cref="IWorkflowBusinessService"/>，这里 Replace 为真实实现。
    /// </remarks>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddWorkflowBusinessIntegration(this IServiceCollection services)
    {
        services.TryAddScoped<WorkflowBusinessRegistry>();
        services.TryAddScoped<WorkflowUserNameResolver>();
        services.TryAddScoped<WorkflowTaskRecordWriter>();
        services.TryAddScoped<WorkflowBusinessAbortNotifier>();
        services.TryAddScoped<WorkflowBusinessDefinitionGuard>();
        services.Replace(ServiceDescriptor.Scoped<IWorkflowBusinessService, WorkflowBusinessService>());
        services.TryAddEnumerable(ServiceDescriptor.Transient<IWorkflowOutcomeHandler, WorkflowBusinessOutcomeHandler>());
        return services;
    }

    /// <summary>
    /// 注册工作流种子：权限目录、菜单
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddWorkflowDataSeeders(this IServiceCollection services)
    {
        services.AddDataSeeder<WorkflowPermissionCatalogSeeder>();
        services.AddDataSeeder<WorkflowMenuSeeder>();
        return services;
    }

    /// <summary>
    /// 注册工作流演示流程种子（种子自己按演示开关跳过；创建与发布经流程定义管理器，需要工作流引擎与持久化存储）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddWorkflowDemoData(this IServiceCollection services)
    {
        services.AddDataSeeder<WorkflowDemoDefinitionSeeder>();
        return services;
    }

    /// <summary>
    /// 注册工作流本地事件处理器（待办创建/转办/实例故障 → 站内通知）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddWorkflowEventHandlers(this IServiceCollection services)
    {
        services.AddWorkflowLocalEventHandler<WorkflowUserTaskCreatedNotificationHandler>();
        services.AddWorkflowLocalEventHandler<WorkflowUserTaskTransferredNotificationHandler>();
        services.AddWorkflowLocalEventHandler<WorkflowInstanceFaultedNotificationHandler>();
        services.AddWorkflowLocalEventHandler<WorkflowCarbonCopyRecordHandler>();
        return services;
    }

    /// <summary>
    /// 注册本地事件处理器（AddTransient + 加入本地事件总线处理器列表）
    /// </summary>
    private static IServiceCollection AddWorkflowLocalEventHandler<THandler>(this IServiceCollection services)
        where THandler : class
    {
        services.AddTransient<THandler>();
        services.Configure<XiHanLocalEventBusOptions>(options => options.Handlers.AddIfNotContains(typeof(THandler)));
        return services;
    }

    /// <summary>
    /// 登记工作流模块可配置字段安全的实体
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddWorkflowFieldSecurityEntities(this IServiceCollection services)
    {
        return services.AddFieldSecurityEntities(entities => entities
            .Add<SysWorkflowDefinition>()
            .Add<SysWorkflowInstance>());
    }
}
