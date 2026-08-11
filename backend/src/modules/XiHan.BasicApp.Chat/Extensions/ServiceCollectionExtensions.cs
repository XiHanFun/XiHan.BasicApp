// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Chat.Application.EventHandlers;
using XiHan.BasicApp.Chat.Domain.DomainServices;
using XiHan.BasicApp.Chat.Infrastructure.Seeders.System;
using XiHan.Framework.Data.Extensions.DependencyInjection;
using XiHan.Framework.EventBus.Local;
using XiHan.Framework.Utils.Collections;

namespace XiHan.BasicApp.Chat.Extensions;

/// <summary>
/// 聊天模块服务注册扩展
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加聊天模块种子数据（权限 → 菜单 → 角色授权 → 任务 → 配置）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddChatDataSeeders(this IServiceCollection services)
    {
        _ = services.AddDataSeeder<ChatPermissionSeeder>();       // 400
        _ = services.AddDataSeeder<ChatMenuSeeder>();             // 401
        _ = services.AddDataSeeder<ChatRolePermissionSeeder>();   // 402
        _ = services.AddDataSeeder<ChatTaskSeeder>();             // 403
        _ = services.AddDataSeeder<ChatConfigurationSeeder>();    // 404
        return services;
    }

    /// <summary>
    /// 添加聊天模块领域服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddChatDomainServices(this IServiceCollection services)
    {
        services.AddScoped<IChatDomainService, ChatDomainService>();
        return services;
    }

    /// <summary>
    /// 添加聊天模块领域事件处理器（须显式加入本地事件总线订阅列表，裸注册不会被订阅）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddChatEventHandlers(this IServiceCollection services)
    {
        // 部门归属变更 → 部门群成员同步（入部门进群/移出踢群）
        services.AddTransient<ChatDepartmentMemberSyncEventHandler>();
        services.Configure<XiHanLocalEventBusOptions>(options => options.Handlers.AddIfNotContains(typeof(ChatDepartmentMemberSyncEventHandler)));
        return services;
    }
}
