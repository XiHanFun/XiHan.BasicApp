#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:XiHanBasicAppWebCoreModule
// Guid:ebce03ed-9f7b-4886-9da4-269908e9eca7
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/6/3 0:31:13
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.Framework.Core.Modularity;
using XiHan.Framework.Web.Api;
using XiHan.Framework.Web.Core;
using XiHan.Framework.Web.Docs;
using XiHan.Framework.Web.Gateway;
using XiHan.Framework.Web.RealTime;

namespace XiHan.BasicApp.Web.Core;

/// <summary>
/// XiHanBasicAppWebCoreModule
/// </summary>
[DependsOn(
    typeof(XiHanWebCoreModule),
    typeof(XiHanWebApiModule),
    typeof(XiHanWebDocsModule),
    typeof(XiHanWebRealTimeModule),
    typeof(XiHanWebGatewayModule),
    typeof(XiHanBasicAppCoreModule)
)]
public class XiHanBasicAppWebCoreModule : XiHanModule
{
    /// <summary>
    /// 服务配置
    /// </summary>
    /// <param name="context"></param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
    }
}
