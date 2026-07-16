#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ServiceCollectionExtensions
// Guid:1c96e0d5-84b7-4f42-a3c8-50e17d92b6f4
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/17 10:37:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using XiHan.BasicApp.Workflow.Application.EventHandlers;
using XiHan.BasicApp.Workflow.Infrastructure.Seeders.System;
using XiHan.BasicApp.Workflow.Infrastructure.Stores;
using XiHan.Framework.Data.Extensions.DependencyInjection;
using XiHan.Framework.EventBus.Local;
using XiHan.Framework.Utils.Collections;
using XiHan.Framework.Workflow.Abstractions.Stores;

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
    /// 注册工作流种子数据（Order 300+ 独立段，链内顺序：操作 → 资源 → 权限 → 菜单 → 角色授权）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddWorkflowDataSeeders(this IServiceCollection services)
    {
        services.AddDataSeeder<SysOperationSeeder>();       // Order = 300（操作字典，权限派生前置）
        services.AddDataSeeder<SysResourceSeeder>();        // Order = 301（资源，权限派生前置）
        services.AddDataSeeder<SysPermissionSeeder>();      // Order = 302（资源 × 操作 → workflow:* 权限）
        services.AddDataSeeder<SysMenuSeeder>();            // Order = 303（菜单，建即绑 workflow:read）
        services.AddDataSeeder<SysRolePermissionSeeder>();  // Order = 304（默认仅授超管）
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
}
