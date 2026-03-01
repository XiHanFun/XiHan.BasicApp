#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:XiHanBasicAppUpgradeModule
// Guid:8b8d1d7b-6d3b-41c9-9d20-8f9cbd3b4f2b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/01 16:33:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Upgrade.Extensions;
using XiHan.Framework.Core.Modularity;
using XiHan.Framework.Upgrade;

namespace XiHan.BasicApp.Upgrade;

/// <summary>
/// 曦寒基础应用升级模块
/// </summary>
[DependsOn(
    typeof(XiHanUpgradeModule)
)]
public class XiHanBasicAppUpgradeModule : XiHanModule
{
    /// <summary>
    /// 服务配置
    /// </summary>
    /// <param name="context"></param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;
        services.AddUpgradeInfrastructureAdapters();
        services.AddUpgradeApplicationServices();
    }
}
