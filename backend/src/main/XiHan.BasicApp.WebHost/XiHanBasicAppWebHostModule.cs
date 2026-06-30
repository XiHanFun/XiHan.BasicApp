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

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Linq;
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
    /// 本模块为依赖图根，初始化在框架 Web 管道（UseRouting/UseAuthentication/UseEndpoints）之后；
    /// 未匹配控制器的 /health 请求落到此终结中间件，匿名短路返回。
    /// 若启用了开放接口签名中间件（默认关闭），需把 /health 加入其忽略路径。
    /// </remarks>
    /// <param name="context">应用初始化上下文</param>
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        app.UseHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = WriteMinimalHealthResponseAsync
        });
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
