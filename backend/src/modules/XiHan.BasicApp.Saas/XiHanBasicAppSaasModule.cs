// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Core;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Hubs;
using XiHan.BasicApp.Saas.Infrastructure.OAuth;
using XiHan.BasicApp.Saas.Infrastructure.Tasks;
using XiHan.BasicApp.Saas.Extensions;
using XiHan.BasicApp.Web.Core;
using XiHan.Framework.Core.Application;
using XiHan.Framework.Core.Extensions.DependencyInjection;
using XiHan.Framework.Core.Modularity;
using XiHan.Framework.Data.SqlSugar.Options;
using XiHan.Framework.MultiTenancy;
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

        // XiHan.Framework 3.10.1 NuGet 包早于 IStrictMultiTenantEntity 发布，无法使用后来加入框架的标记接口。
        // 对四张聊天表按具体实体注册等价过滤器，确保租户态只见本租户、平台态只见 TenantId=0；
        // 该过滤同样参与仓储预读及自动 UPDATE/DELETE 过滤，保持聊天数据的读写租户边界一致。
        services.Configure<XiHanSqlSugarCoreOptions>(options =>
        {
            options.AddGlobalFilter<SysChatConversation>(entity => entity.TenantId == ResolveStrictChatTenantScopeId());
            options.AddGlobalFilter<SysChatConversationMember>(entity => entity.TenantId == ResolveStrictChatTenantScopeId());
            options.AddGlobalFilter<SysChatMessage>(entity => entity.TenantId == ResolveStrictChatTenantScopeId());
            options.AddGlobalFilter<SysChatMessageReaction>(entity => entity.TenantId == ResolveStrictChatTenantScopeId());
        });

        // 注册 SaaS 模块种子数据（系统基线始终播种；演示数据由 Saas:Seed:EnableDemoData 控制）
        services.AddSaasDataSeeders();
        services.AddSaasDemoDataSeeders();

        // 注册 SaaS 领域服务
        services.AddSaasDomainServices();

        // 注册 SaaS 应用层内部服务
        services.AddSaasApplicationServices();

        // 修正 .NET 配置对 List<T> 的「追加而非替换」语义：框架 XiHanOpenApiSecurityOptions 的默认
        // ProtectedPathPrefixes=["/api"] 不会被 appsettings 的值替换，而是被追加，导致最终为 ["/api", 配置值...]，
        // 把整个第一方前端 /api/* 全纳入强制签名（登录等匿名调用直接 401「缺少安全请求头」）。
        // 此处以 appsettings 配置值为准重建这两个路径列表（.Get 返回全新列表，无默认污染）；PostConfigure 在框架绑定之后执行，最终生效。
        services.AddOptions<XiHan.Framework.Web.Api.Security.OpenApi.XiHanOpenApiSecurityOptions>()
            .PostConfigure<IConfiguration>((options, configuration) =>
            {
                var sectionName = XiHan.Framework.Web.Api.Security.OpenApi.XiHanOpenApiSecurityOptions.SectionName;

                var protectedPrefixes = configuration.GetSection($"{sectionName}:ProtectedPathPrefixes").Get<List<string>>();
                if (protectedPrefixes is not null)
                {
                    options.ProtectedPathPrefixes = protectedPrefixes;
                }

                var ignoredPrefixes = configuration.GetSection($"{sectionName}:IgnoredPathPrefixes").Get<List<string>>();
                if (ignoredPrefixes is not null)
                {
                    options.IgnoredPathPrefixes = ignoredPrefixes;
                }
            });

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
    /// 解析聊天实体严格隔离所使用的租户标识；无租户上下文时使用平台租户 0。
    /// </summary>
    /// <remarks>
    /// 过滤表达式只调用本方法取得 <see cref="long"/> 标量，避免把复杂租户对象交给 SqlSugar 参数翻译器。
    /// <see cref="AsyncLocalCurrentTenantAccessor"/> 随当前异步执行流保存上下文，因此无需共享可变锁；
    /// 每次查询重新求值，可安全支持同一进程内的并发租户请求。
    /// </remarks>
    /// <returns>当前租户标识；平台态返回 0。</returns>
    private static long ResolveStrictChatTenantScopeId()
    {
        return AsyncLocalCurrentTenantAccessor.Instance.Current?.TenantId ?? 0;
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

            // OAuth2 授权服务端标准端点：/connect/authorize（跳同意页）+ /connect/token（令牌）+ /connect/revoke（撤销）
            endpoints.MapOAuthConnectEndpoints();

            // OIDC 端点：发现文档 + JWKS + 用户信息（未启用时不注册任何路由）
            endpoints.MapOidcEndpoints();
        });
    }
}
