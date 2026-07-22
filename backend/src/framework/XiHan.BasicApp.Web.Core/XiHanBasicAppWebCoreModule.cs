// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
    typeof(XiHanBasicAppCoreModule),
    typeof(XiHanWebCoreModule),
    typeof(XiHanWebApiModule),
    typeof(XiHanWebDocsModule),
    typeof(XiHanWebRealTimeModule),
    typeof(XiHanWebGatewayModule)
)]
public class XiHanBasicAppWebCoreModule : XiHanModule
{
}
