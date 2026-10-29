// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Scalar.AspNetCore;
using XiHan.BasicApp.Access;
using XiHan.BasicApp.Auditing;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Files;
using XiHan.BasicApp.Host.Endpoints;
using XiHan.BasicApp.Identity;
using XiHan.BasicApp.Identity.Infrastructure.External;
using XiHan.BasicApp.Notification;
using XiHan.BasicApp.Notification.Infrastructure.Telegram;
using XiHan.BasicApp.OpenPlatform;
using XiHan.BasicApp.OpenPlatform.Infrastructure;
using XiHan.BasicApp.OpenPlatform.Infrastructure.OAuth;
using XiHan.BasicApp.Runtime;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.BasicApp.Organization;
using XiHan.BasicApp.Realtime;
using XiHan.BasicApp.Settings;
using XiHan.BasicApp.Tenancy;
using XiHan.BasicApp.Workbench;
using XiHan.Framework.Auditing.Database;
using XiHan.Framework.Hosting.AspNetCore;
using XiHan.Framework.Hosting.AspNetCore.Endpoints;
using XiHan.Framework.Messaging.Abstractions.Outbox;
using XiHan.Framework.MultiTenancy.Database;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Profiles.Full;
using XiHan.Framework.Protocols.Grpc.Services;
using XiHan.Framework.Protocols.Http.OpenApi;
using XiHan.Framework.Protocols.Http.Routing;
using XiHan.Framework.Protocols.Mcp.Transports;
using XiHan.Framework.Security.Database;
using XiHan.Framework.Security.Jwt;

namespace XiHan.BasicApp.Host;

/// <summary>
/// 宿主装配（方案 5.13）：Full Profile + 显式 Provider + 全部能力包，可选模块按 <see cref="HostModules"/> 的开关装配；端点按固定顺序映射。测试工程经 <see cref="Create"/> 复用同一份装配。
/// </summary>
public static class BasicAppHost
{
    /// <summary>
    /// 建应用：装配框架与能力包、映射端点；不启动。<paramref name="configure"/> 在 <c>Build()</c> 之前执行，供测试换成 TestServer 并注入配置；
    /// <paramref name="configureApp"/> 在 <c>UseXiHan()</c> 之前执行，供要排在内核桥接之前的中间件（静态文件、ASP.NET Core 认证）接入。
    /// </summary>
    public static WebApplication Create(string[] args, Action<WebApplicationBuilder>? configure = null, Action<WebApplication>? configureApp = null)
    {
        ArgumentNullException.ThrowIfNull(args);
        var builder = WebApplication.CreateBuilder(args);
        // 宿主自己的端点返回类型走源生成序列化；操作的请求 / 响应由各包的 JsonContext 经登记表处理，不经这里
        builder.Services.ConfigureHttpJsonOptions(static options => options.SerializerOptions.TypeInfoResolverChain.Insert(0, HostJsonContext.Default));
        // 先让调用方改配置与服务：AddXiHan 在这一步就读配置、装配特性
        configure?.Invoke(builder);

        // 可选模块的开关在调用方改完配置之后读（测试可以关掉模块）
        var modules = HostModules.FromConfiguration(builder.Configuration);
        builder.AddXiHan(xihan =>
        {
            xihan
            .UseFullProfile()
            // 方式一：回调设库类型、连接串、AopEvents（操作人、差异日志）与 snake_case 命名；协议约定由框架随后施加。全审计实体的写入带差异日志（ADR-0044）
            .UseFeature(new SqlSugarPersistenceFeature(SqlSugarRuntime.Configure) { DiffLog = DiffLogBuffer.IsLogged })
            .UseFeature<JwtSecurityFeature>()
            .UseFeature<DatabaseMultiTenancyFeature>()
            .UseFeature<DatabaseSecurityFeature>()
            .UseFeature<DatabaseAuditingFeature>()
            // 发件箱必装：领域事件与业务写入同事务派发
            .UseFeature<OutboxFeature>()
            .UseFeature<RuntimeFeature>()
            .UseFeature<IdentityFeature>()
            .UseFeature<TenancyFeature>()
            .UseFeature<OrganizationFeature>()
            .UseFeature<AccessFeature>()
            .UseFeature<SettingsFeature>()
            .UseFeature<FilesFeature>()
            .UseFeature<NotificationFeature>()
            .UseFeature<AuditingFeature>()
            .UseFeature<RealtimeFeature>()
            .UseFeature<OpenPlatformFeature>();
            if (modules.Workbench)
            {
                xihan.UseFeature<WorkbenchFeature>();
            }
        });

        var app = builder.Build();
        configureApp?.Invoke(app);
        Map(app);
        return app;
    }

    /// <summary>
    /// 映射全部端点：探针、操作（<c>/api/**</c>）、接口文档（按 <c>BasicApp:ApiDocs:Enabled</c>）、MCP、gRPC 与 <c>.proto</c>、实时 Hub、普通端点。
    /// ASP.NET Core 的认证与授权排在 <c>UseXiHan()</c> 之前：只有实时 Hub 的端点带授权策略（它的 JWT 方案不设为缺省），操作照旧由内核认证。
    /// </summary>
    public static void Map(WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.UseAuthentication();
        app.UseAuthorization();
        // 开放接口的请求体原文要在框架绑定载荷之前留下（验签要原文，加密的要先解密）
        app.UseOpenApiRequestBuffer();
        app.UseXiHan();
        app.MapXiHanHealth();
        app.MapXiHanOperations();

        var apiDocs = ApiDocsOptions.FromConfiguration(app.Configuration, app.Environment);
        if (apiDocs.Enabled)
        {
            // 一份 openapi.json、一个生成器、一份 Schema：操作 + 带元数据的普通端点；/scalar 读同一份
            app.MapXiHanOpenApi(new OpenApiOptions { Title = AppInfo.Name, Version = AppInfo.Version });
            PipelineBypassMetadata.BypassXiHanPipeline(app.MapScalarApiReference(static options => options.WithOpenApiRoutePattern(OpenApiEndpointRouteBuilderExtensions.DefaultPattern).WithTitle(AppInfo.Name)));
        }

        app.MapXiHanMcp();
        app.MapXiHanGrpc();
        app.MapXiHanProto();
        app.MapBasicAppEndpoints();
    }

    /// <summary>
    /// 不能用操作表达的普通端点：根路径的应用信息、文件的上传与下载、实时 Hub；OAuth 重定向、Webhook 随各包补入。
    /// </summary>
    public static IEndpointRouteBuilder MapBasicAppEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        var environment = endpoints.ServiceProvider.GetRequiredService<IHostEnvironment>();
        var info = new HostInfo(AppInfo.Name, AppInfo.Version, environment.EnvironmentName);
        // 根路径只是应用自述，不是业务契约：排除出接口文档，否则生成器会要求它的 JSON 引用某个操作的 Schema
        PipelineBypassMetadata.BypassXiHanPipeline(endpoints.MapGet("/", () => info).ExcludeFromDescription());
        endpoints.MapFileEndpoints();
        endpoints.MapRealtimeHubs();
        endpoints.MapTelegramWebhook();
        endpoints.MapOAuthServer();
        endpoints.MapExternalLogin();
        return endpoints;
    }
}
