#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:XiHanBasicAppMESModule
// Guid:9b67bf1a-ab1b-4736-a06d-a84d734fc1e6
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/16 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas;
using XiHan.Framework.Core.Modularity;

namespace XiHan.BasicApp.MES;

/// <summary>
/// 曦寒基础应用制造执行应用模块
/// </summary>
[DependsOn(
    typeof(XiHanBasicAppSaasModule)
)]
public class XiHanBasicAppMESModule : XiHanModule
{
    /// <summary>
    /// 服务配置
    /// </summary>
    /// <param name="context"></param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // 业务服务注册（种子 / 领域服务 / 权限菜单）在此登记
    }
}
