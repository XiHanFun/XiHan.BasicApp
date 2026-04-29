#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:XiHanBasicAppRbacModule
// Guid:9b39d543-6e3f-46b8-a288-40076def6e6a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2024/12/07 06:24:50
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Saas.Infrastructure.Extensions;
using XiHan.BasicApp.Web.Core;
using XiHan.Framework.Core.Modularity;

namespace XiHan.BasicApp.Saas;

/// <summary>
/// 曦寒基础应用 SaaS 权限模块
/// </summary>
[DependsOn(
    typeof(XiHanBasicAppCoreModule),
    typeof(XiHanBasicAppWebCoreModule)
)]
public class XiHanBasicAppRbacModule : XiHanModule
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
    }
}
