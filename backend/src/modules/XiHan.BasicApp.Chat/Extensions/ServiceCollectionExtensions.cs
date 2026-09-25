// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Chat.Application.EventHandlers;
using XiHan.BasicApp.Chat.Domain.DomainServices;
using XiHan.BasicApp.Chat.Infrastructure.Seeders;
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
    /// 添加聊天模块种子：权限目录、菜单、参数配置、内建定时任务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddChatDataSeeders(this IServiceCollection services)
    {
        _ = services.AddDataSeeder<ChatPermissionCatalogSeeder>();
        _ = services.AddDataSeeder<ChatMenuSeeder>();
        _ = services.AddDataSeeder<ChatSettingSeeder>();
        _ = services.AddDataSeeder<ChatTaskSeeder>();
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
