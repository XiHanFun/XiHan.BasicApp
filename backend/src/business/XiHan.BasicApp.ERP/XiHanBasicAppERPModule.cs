// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas;
using XiHan.Framework.Core.Modularity;

namespace XiHan.BasicApp.ERP;

/// <summary>
/// 曦寒基础应用企业资源计划应用模块
/// </summary>
[DependsOn(
    typeof(XiHanBasicAppSaasModule)
)]
public class XiHanBasicAppERPModule : XiHanModule
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
