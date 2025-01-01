#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:XiHanBasicAppApplicationModule
// Guid:6c2edf44-7109-4565-8faa-6a1d402c19b3
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2024/12/10 5:37:01
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Application.Contracts;
using XiHan.BasicApp.CodeGeneration.Application;
using XiHan.BasicApp.Domain;
using XiHan.BasicApp.Rbac.Application;
using XiHan.Framework.Core.Modularity;
using XiHan.Framework.Ddd.Application;

namespace XiHan.BasicApp.Application;

/// <summary>
/// XiHanBasicAppApplicationModule
/// </summary>
[DependsOn(
    typeof(XiHanDddApplicationModule),
    typeof(XiHanBasicAppRbacApplicationModule),
    typeof(XiHanBasicAppCodeGenerationApplicationModule),
    typeof(XiHanBasicAppApplicationContractsModule),
    typeof(XiHanBasicAppDomainModule)
)]
public class XiHanBasicAppApplicationModule : XiHanModule
{
    /// <summary>
    /// 服务配置
    /// </summary>
    /// <param name="context"></param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
    }
}
