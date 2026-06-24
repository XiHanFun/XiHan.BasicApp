#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:XiHanBasicAppSaasModule
// Guid:9b39d543-6e3f-46b8-a288-40076def6e6a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2024/12/07 06:24:50
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Core;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Hubs;
using XiHan.BasicApp.Saas.Infrastructure.OAuth;
using XiHan.BasicApp.Saas.Infrastructure.Tasks;
using XiHan.BasicApp.Saas.Extensions;
using XiHan.BasicApp.Web.Core;
using XiHan.Framework.Core.Application;
using XiHan.Framework.Core.Modularity;
using XiHan.Framework.Tasks.ScheduledJobs.Abstractions;
using XiHan.Framework.Tasks.ScheduledJobs.Extensions;
using XiHan.Framework.Web.Core.Extensions;
using XiHan.Framework.Web.RealTime.Constants;
using XiHan.Framework.Web.RealTime.Extensions;

namespace XiHan.BasicApp.Saas;

/// <summary>
/// 曦寒基础应用 SaaS 模块
/// </summary>
[DependsOn(
    typeof(XiHanBasicAppCoreModule),
    typeof(XiHanBasicAppWebCoreModule)
)]
public class XiHanBasicAppSaasModule : XiHanModule
{
    /// <summary>
    /// 服务配置
    /// </summary>
    /// <param name="context">服务配置上下文</param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;

        // 注册 SaaS 模块种子数据
        services.AddSaasDataSeeders();

        // 注册 SaaS 领域服务
        services.AddSaasDomainServices();

        // 注册 SaaS 应用层内部服务
        services.AddSaasApplicationServices();

        // 注册 SaaS 领域事件处理器（ILocalEventHandler<T> 由事件总线框架自动发现并订阅）
        services.AddSaasEventHandlers();

        // 注册日志写入器
        services.AddSaasLogWriters();

        // 注册 SaaS 认证基础设施（用户存储、第三方登录存储、密码历史存储）
        services.AddSaasAuthStores();

        // 注册 SaaS 消息发送器（Email / SMS）
        services.AddSaasMessageSenders();

        // 注册任务调度基础设施（替换 InMemoryJobStore 为数据库持久化，注册 IJobWorker 实现）
        services.AddSaasJobInfrastructure();

        // 注册导出中心基础设施（导出引擎 + Provider + 后台执行 worker）
        services.AddSaasExportInfrastructure();
    }

    /// <summary>
    /// 应用初始化后：自动发现声明式任务并同步数据库中的活跃 SysTask 到调度器
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    public override void OnPostApplicationInitialization(ApplicationInitializationContext context)
    {
        var scheduler = context.ServiceProvider.GetRequiredService<IJobScheduler>();
        var logger = context.ServiceProvider.GetRequiredService<ILogger<XiHanBasicAppSaasModule>>();

        // 1. 扫描当前程序集中带 [JobName] 特性的 IJobWorker 实现（声明式任务）。
        //    注意：DynamicJobWorker 是反射桥接器、UserStatisticsAggregationTask 是反射目标类，
        //    均不带 [JobName]，故本步对当前程序集是空操作；保留以支持未来的声明式 Worker。
        var jobAssembly = typeof(DynamicJobWorker).Assembly;
        scheduler.RegisterJobsFromAssembly(jobAssembly);

        // 2. 同步数据库中所有启用的 SysTask 记录到调度器（含崩溃残留 Running 状态复位）
        using var scope = context.ServiceProvider.CreateScope();
        var schedulerSyncService = scope.ServiceProvider.GetRequiredService<ITaskSchedulerSyncService>();
        schedulerSyncService.SyncAllActiveJobsAsync().GetAwaiter().GetResult();
        logger.LogInformation("数据库中的活跃 SysTask 记录已同步到调度器");
    }

    /// <summary>
    /// 应用初始化
    /// </summary>
    /// <param name="context"></param>
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();

        app.UseEndpoints(endpoints =>
        {
            // 映射 SignalR Hub 端点
            endpoints.MapXiHanHub<BasicAppNotificationHub>(SignalRConstants.HubPaths.Notification);
            endpoints.MapXiHanHub<BasicAppChatHub>(SignalRConstants.HubPaths.Chat);

            // 第三方登录（OAuth）端点：/api/OAuth/ExternalLogin（发起）+ /api/OAuth/Callback（回调）
            endpoints.MapOAuthEndpoints();
        });
    }
}
