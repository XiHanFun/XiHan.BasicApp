#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2024 ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:XiHanBasicAppApplicationContractsModule
// Guid:b4b1d28b-8ec6-4968-bd22-7e75f3192260
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2024/12/10 5:37:27
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.CodeGeneration.Application.Contracts;
using XiHan.BasicApp.Domain.Shared;
using XiHan.BasicApp.Rbac.Application.Contracts;
using XiHan.Framework.Core.Modularity;
using XiHan.Framework.Ddd.Application.Contracts;

namespace XiHan.BasicApp.Application.Contracts;

/// <summary>
/// XiHanBasicAppApplicationContractsModule
/// </summary>
[DependsOn(
    typeof(XiHanDddApplicationContractsModule),
    typeof(XiHanBasicAppRbacApplicationContractsModule),
    typeof(XiHanBasicAppCodeGenerationApplicationContractsModule),
    typeof(XiHanBasicAppDomainSharedModule)
)]
public class XiHanBasicAppApplicationContractsModule : XiHanModule
{
    /// <summary>
    /// 服务配置
    /// </summary>
    /// <param name="context"></param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
    }
}
