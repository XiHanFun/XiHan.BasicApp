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
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using XiHan.BasicApp.CodeGeneration;
using XiHan.BasicApp.Saas;
using XiHan.BasicApp.Saas.Hubs;
using XiHan.Framework.Core.Application;
using XiHan.Framework.Core.Modularity;
using XiHan.Framework.Web.Core.Extensions;
using XiHan.Framework.Web.RealTime.Constants;
using XiHan.Framework.Web.RealTime.Extensions;

namespace XiHan.BasicApp.WebHost;

/// <summary>
/// 曦寒基础应用 Web 主机
/// </summary>
[DependsOn(
    // 应用模块依赖
    typeof(XiHanBasicAppRbacModule),
    typeof(XiHanBasicAppCodeGenerationModule)
)]
public class XiHanBasicAppWebHostModule : XiHanModule
{
    /// <summary>
    /// 服务配置
    /// </summary>
    /// <param name="context"></param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
    }

    /// <summary>
    /// 应用初始化
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();

        // 本地存储静态文件服务：将本地存储根目录（默认 uploads）暴露到 /uploads 路径，
        // 使本地存储 Provider 返回的静态 URL（头像、公开文件等）可经 <img>/直链访问。
        UseLocalStorageStaticFiles(app);

        // 映射 SignalR Hub 端点
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapXiHanHub<BasicAppNotificationHub>(SignalRConstants.HubPaths.Notification);
            endpoints.MapXiHanHub<BasicAppChatHub>(SignalRConstants.HubPaths.Chat);
        });
    }

    /// <summary>
    /// 启用本地存储静态文件服务（与 LocalStorageOptions 的 RootPath/UrlPrefix 对齐）。
    /// </summary>
    private static void UseLocalStorageStaticFiles(IApplicationBuilder app)
    {
        var environment = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();

        // 与 LocalStorageOptions 默认值对齐：RootPath="uploads"、UrlPrefix="/uploads"
        const string rootPath = "uploads";
        const string requestPath = "/uploads";

        var physicalRoot = Path.IsPathRooted(rootPath)
            ? rootPath
            : Path.Combine(environment.ContentRootPath, rootPath);

        if (!Directory.Exists(physicalRoot))
        {
            Directory.CreateDirectory(physicalRoot);
        }

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(physicalRoot),
            RequestPath = requestPath,
            ServeUnknownFileTypes = false
        });
    }
}
