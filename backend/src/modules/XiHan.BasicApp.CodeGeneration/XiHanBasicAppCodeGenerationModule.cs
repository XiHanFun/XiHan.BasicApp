// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.CodeGeneration.Extensions;

using XiHan.BasicApp.Saas;
using XiHan.Framework.Core.Modularity;

namespace XiHan.BasicApp.CodeGeneration;

/// <summary>
/// 曦寒基础应用代码生成应用模块
/// </summary>
[DependsOn(
    typeof(XiHanBasicAppSaasModule)
)]
public class XiHanBasicAppCodeGenerationModule : XiHanModule
{
    /// <summary>
    /// 服务配置
    /// </summary>
    /// <param name="context"></param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;

        // 注册代码生成模块种子数据
        services.AddCodeGenerationDataSeeders();

        // 注册代码生成引擎（渲染器/类型映射/扫描/打包/编排）
        services.AddCodeGenerationEngine();

        // 注册代码生成领域服务（未携带 DI 标记接口，需显式登记）
        services.AddCodeGenerationDomainServices();
    }
}
