#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:XiHanBasicAppWebHostModule
// Guid:c3d5fae5-17b2-44f8-aaa2-1ce1b868f8e6
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2024/12/10 05:34:12
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using XiHan.BasicApp.CodeGeneration;
using XiHan.BasicApp.Saas;
using XiHan.BasicApp.WebHost.HealthChecks;
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
    typeof(XiHanBasicAppCodeGenerationModule)
)]
public class XiHanBasicAppWebHostModule : XiHanModule
{
    /// <summary>
    /// 服务配置：注册数据库 / Redis 健康检查
    /// </summary>
    /// <param name="context">服务配置上下文</param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database")
            .AddCheck<RedisHealthCheck>("redis");
    }

    /// <summary>
    /// 应用初始化：暴露匿名 /health 端点（仅返回总状态 + 各项名，不外泄连接串/异常细节）
    /// </summary>
    /// <remarks>
    /// 框架启用了鉴权 FallbackPolicy：无端点/非匿名端点的请求会被授权中间件 401。
    /// 因此把 /health 注册为端点并 <c>AllowAnonymous()</c>（与框架 <c>MapOpenApi().AllowAnonymous()</c> 一致），
    /// 使授权中间件放行。端点数据源在请求期动态读取，故本模块（依赖图根）后置注册仍可被路由匹配。
    /// 若启用了开放接口签名中间件（默认关闭），需把 /health 加入其忽略路径。
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
