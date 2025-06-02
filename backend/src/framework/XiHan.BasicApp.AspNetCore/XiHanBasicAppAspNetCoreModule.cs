#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:XiHanBasicAppAspNetCoreModule
// Guid:ebce03ed-9f7b-4886-9da4-269908e9eca7
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/6/3 0:31:13
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.Framework.AspNetCore;
using XiHan.Framework.AspNetCore.Authentication.JwtBearer;
using XiHan.Framework.AspNetCore.Authentication.OAuth;
using XiHan.Framework.AspNetCore.Mvc;
using XiHan.Framework.AspNetCore.Refit;
using XiHan.Framework.AspNetCore.Scalar;
using XiHan.Framework.AspNetCore.Serilog;
using XiHan.Framework.AspNetCore.SignalR;
using XiHan.Framework.AspNetCore.Swagger;
using XiHan.Framework.Core.Modularity;

namespace XiHan.BasicApp.AspNetCore;

/// <summary>
/// XiHanBasicAppAspNetCoreModule
/// </summary>
[DependsOn(
    typeof(XiHanAspNetCoreModule),
    typeof(XiHanAspNetCoreMvcModule),
    typeof(XiHanAspNetCoreSerilogModule),
    typeof(XiHanAspNetCoreSignalRModule),
    typeof(XiHanAspNetCoreRefitModule),
    typeof(XiHanAspNetCoreAuthenticationJwtBearerModule),
    typeof(XiHanAspNetCoreAuthenticationOAuthModule),
    typeof(XiHanAspNetCoreScalarModule),
    typeof(XiHanAspNetCoreSwaggerModule),

    typeof(XiHanBasicAppCoreModule)
)]
public class XiHanBasicAppAspNetCoreModule : XiHanModule
{
    /// <summary>
    /// 服务配置
    /// </summary>
    /// <param name="context"></param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
    }
}
