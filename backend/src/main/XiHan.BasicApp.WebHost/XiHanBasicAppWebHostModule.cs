// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using XiHan.BasicApp.AI;
using XiHan.BasicApp.Chat;
using XiHan.BasicApp.CodeGeneration;
using XiHan.BasicApp.Printing;
using XiHan.BasicApp.Saas;
using XiHan.BasicApp.Workflow;
using XiHan.BasicApp.WebHost.HealthChecks;
using XiHan.Framework.Bot.Telegram.Webhook;
using XiHan.Framework.Core.Application;
using XiHan.Framework.Core.Modularity;
using XiHan.Framework.Web.Core.Extensions;

namespace XiHan.BasicApp.WebHost;

/// <summary>
/// 曦寒基础应用 Web 主机
/// </summary>
[DependsOn(
    // 应用模块依赖
    typeof(XiHanBasicAppSaasModule),
    typeof(XiHanBasicAppCodeGenerationModule),
    typeof(XiHanBasicAppAIModule),
    typeof(XiHanBasicAppWorkflowModule),
    typeof(XiHanBasicAppPrintingModule),
    typeof(XiHanBasicAppChatModule)
)]
public class XiHanBasicAppWebHostModule : XiHanModule
{
    /// <summary>
    /// 服务配置：注册数据库 / Redis / 向量库健康检查
    /// </summary>
    /// <param name="context">服务配置上下文</param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database")
            .AddCheck<RedisHealthCheck>("redis")
            .AddCheck<QdrantHealthCheck>("qdrant");
    }

    /// <summary>
    /// 应用初始化前：注册 Telegram Bot Webhook 接收中间件，确保在认证/授权中间件之前处理 Webhook 请求
    /// </summary>
    /// <remarks>
    /// 模块预初始化先于框架 WebApi 模块的管线注册执行（镜像 Swagger UI 的 OnPreApplicationInitialization 写法），
    /// 故中间件位于鉴权之前。中间件仅匹配 POST {前缀}/{机器人名}，自带 secret_token 强校验（fail-closed），
    /// 非 Webhook 路径请求原样放行；仅 Webhook 传输模式（webhook-base-url 配置非空）会实际收到请求。
    /// </remarks>
    /// <param name="context">应用初始化上下文</param>
    public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        _ = app.UseTelegramBotWebhook();
    }

    /// <summary>
    /// 应用初始化：执行版本升级脚本 + 暴露匿名 /health 端点（仅返回总状态 + 各项名，不外泄连接串/异常细节）
    /// </summary>
    /// <remarks>
    /// 自动版本更新：本模块是依赖图的根，其初始化晚于框架 <c>XiHanDataModule</c> 的数据库初始化
    /// （建表 + 种子），因此在此处执行 <c>UpdateScripts/*.sql</c> 时表结构与基线数据均已就绪。
    /// 仅主节点（<c>SnowflakeId:WorkerId == 1</c>）执行；全新部署（无 version.txt）视为最新版本、
    /// 只落版本号不跑脚本，故不会在空库上误执行历史脚本。
    /// <para>
    /// 健康检查：框架启用了鉴权 FallbackPolicy：无端点/非匿名端点的请求会被授权中间件 401。
    /// 因此把 /health 注册为端点并 <c>AllowAnonymous()</c>（与框架 <c>MapOpenApi().AllowAnonymous()</c> 一致），
    /// 使授权中间件放行。端点数据源在请求期动态读取，故本模块（依赖图根）后置注册仍可被路由匹配。
    /// 若启用了开放接口签名中间件（默认关闭），需把 /health 加入其忽略路径。
    /// </para>
    /// </remarks>
    /// <param name="context">应用初始化上下文</param>
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();

        var options = new HealthCheckOptions
        {
            ResponseWriter = WriteMinimalHealthResponseAsync
        };

        if (app is IEndpointRouteBuilder endpoints)
        {
            _ = endpoints.MapHealthChecks("/health", options).AllowAnonymous();
        }
        else
        {
            // 兜底：非端点路由构建器时退化为中间件（可能受 FallbackPolicy 影响）
            _ = app.UseHealthChecks("/health", options);
        }
    }

    /// <summary>
    /// 最小化健康响应：仅总状态 + 各检查项名称与状态，不含连接串/异常/描述等敏感细节
    /// </summary>
    private static Task WriteMinimalHealthResponseAsync(HttpContext httpContext, HealthReport report)
    {
        httpContext.Response.ContentType = "application/json; charset=utf-8";
        var payload = new
        {
            status = report.Status.ToString(),
            totalDurationMs = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString()
            })
        };
        return httpContext.Response.WriteAsJsonAsync(payload);
    }
}
