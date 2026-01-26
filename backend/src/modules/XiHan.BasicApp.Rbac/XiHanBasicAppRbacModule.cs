#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:XiHanBasicAppRbacModule
// Guid:9b39d543-6e3f-46b8-a288-40076def6e6a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2024/12/7 6:24:50
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Extensions;
using XiHan.BasicApp.Web.Core;
using XiHan.Framework.Authentication;
using XiHan.Framework.Authorization;
using XiHan.Framework.Core.Application;
using XiHan.Framework.Core.Extensions.DependencyInjection;
using XiHan.Framework.Core.Modularity;
using XiHan.Framework.Core.Threading;
using XiHan.Framework.Data.SqlSugar.Options;
using XiHan.Framework.Web.Core.Extensions;

namespace XiHan.BasicApp.Rbac;

/// <summary>
/// 曦寒基础应用角色控制应用模块
/// </summary>
[DependsOn(
    typeof(XiHanBasicAppCoreModule),
    typeof(XiHanBasicAppWebCoreModule),
    typeof(XiHanAuthenticationModule),
    typeof(XiHanAuthorizationModule)
)]
public class XiHanBasicAppRbacModule : XiHanModule
{
    /// <summary>
    /// 服务配置
    /// </summary>
    /// <param name="context"></param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;
        var config = services.GetConfiguration();

        // 配置SqlSugar选项
        var configSqlSugar = config.GetSection("XiHan:Data:SqlSugarCore");

        // 从配置文件绑定基础配置
        Configure<XiHanSqlSugarCoreOptions>(configSqlSugar);

        // 1. 注册基础设施（Repositories）
        services.AddRbacRepositories();

        // 2. 注册领域服务（Domain Layer）
        services.AddRbacDomainServices();

        // 3. 注册应用服务（Application Layer）
        services.AddRbacApplicationServices();

        // 4. 注册基础设施适配器（Infrastructure Layer）
        services.AddRbacInfrastructureAdapters();

        // 5. 注册数据种子提供者
        services.AddRbacDataSeeders();
    }

    /// <summary>
    /// 应用初始化
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        AsyncHelper.RunSync(async () =>
        {
            // 进行数据库初始化
            await app.UseDbInitializerAsync(initialize: true);
        });
    }
}
