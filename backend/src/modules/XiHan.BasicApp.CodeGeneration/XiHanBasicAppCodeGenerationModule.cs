#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:XiHanBasicAppCodeGenerationModule
// Guid:706325c3-33e8-4710-8128-f1ee449ffc27
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2024/12/07 06:36:28
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac;
using XiHan.Framework.Core.Extensions.DependencyInjection;
using XiHan.Framework.Core.Modularity;

namespace XiHan.BasicApp.CodeGeneration;

/// <summary>
/// 曦寒基础应用代码生成应用模块
/// </summary>
[DependsOn(
    typeof(XiHanBasicAppRbacModule)
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
        var config = services.GetConfiguration();
    }
}
