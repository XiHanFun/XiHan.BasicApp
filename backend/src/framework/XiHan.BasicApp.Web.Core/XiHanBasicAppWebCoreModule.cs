// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection.Extensions;
using XiHan.BasicApp.Core;
using XiHan.BasicApp.Web.Core.Upgrade;
using XiHan.Framework.Core.Application;
using XiHan.Framework.Core.Modularity;
using XiHan.Framework.Upgrade.Abstractions;
using XiHan.Framework.Web.Api;
using XiHan.Framework.Web.Core;
using XiHan.Framework.Web.Core.Extensions;
using XiHan.Framework.Web.Docs;
using XiHan.Framework.Web.Gateway;
using XiHan.Framework.Web.Mcp;
using XiHan.Framework.Web.RealTime;

namespace XiHan.BasicApp.Web.Core;

/// <summary>
/// XiHanBasicAppWebCoreModule
/// </summary>
[DependsOn(
    typeof(XiHanBasicAppCoreModule),
    typeof(XiHanWebCoreModule),
    typeof(XiHanWebApiModule),
    typeof(XiHanWebDocsModule),
    typeof(XiHanWebRealTimeModule),
    typeof(XiHanWebGatewayModule),
    typeof(XiHanWebMcpModule)
)]
public class XiHanBasicAppWebCoreModule : XiHanModule
{
    /// <summary>
    /// 服务配置
    /// </summary>
    /// <param name="context">服务配置上下文</param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;

        // 维护模式：升级引擎置位、中间件据此拦截业务请求
        services.TryAddSingleton<MaintenanceModeState>();
        services.Replace(ServiceDescriptor.Singleton<IUpgradeMaintenanceModeManager, BasicAppUpgradeMaintenanceModeManager>());
    }

    /// <summary>
    /// 应用初始化
    /// </summary>
    /// <param name="context">应用初始化上下文</param>
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        // 尽量靠前：维护期间的请求不必再走后续管线
        _ = context.GetApplicationBuilder().UseMaintenanceMode();
    }
}
