#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:XiHanBasicAppWebHostModule
// Guid:c9bf348b-8c2f-4e2a-9f36-cc2edafe551e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2024/12/10 5:34:12
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.CodeGeneration.Application;
using XiHan.BasicApp.Rbac.Application;
using XiHan.Framework.Core.Application;
using XiHan.Framework.Core.Modularity;

namespace XiHan.BasicApp.WebHost;

/// <summary>
/// 曦寒基础应用 Web 主机
/// </summary>
[DependsOn(
    typeof(XiHanBasicAppRbacApplicationModule),
    typeof(XiHanBasicAppCodeGenerationApplicationModule)
)]
public class XiHanBasicAppWebHostModule : XiHanModule
{
    /// <summary>
    /// 服务配置
    /// </summary>
    /// <param name="context"></param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        //var services = context.Services;

        //_ = services.AddControllers();
        //_ = services.AddOpenApi();

        //_ = services.AddCors(options =>
        //{
        //    options.AddDefaultPolicy(builder =>
        //    {
        //        _ = builder.AllowAnyOrigin()
        //            .AllowAnyMethod()
        //            .AllowAnyHeader();
        //    });
        //});
    }

    /// <summary>
    /// 应用初始化
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        //_ = context.ServiceProvider;
        //_ = context.GetEnvironment();
        //var app = context.GetApplicationBuilder();

        //_ = app.UseRouting();
        //_ = app.UseCors();
        //_ = app.UseEndpoints(endpoints =>
        //{
        //    // 不对约定路由做任何假设，也就是不使用约定路由，依赖用户的特性路由
        //    _ = endpoints.MapControllers();
        //    _ = endpoints.MapOpenApi();
        //    _ = endpoints.MapScalarApiReference();
        //});
    }
}
