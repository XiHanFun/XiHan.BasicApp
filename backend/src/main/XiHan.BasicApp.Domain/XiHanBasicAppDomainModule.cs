#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:XiHanBasicAppDomainModule
// Guid:23a6a269-ac1a-4d1b-95ca-bdf0d8cf40e1
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2024/12/10 5:37:45
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.CodeGeneration.Domain;
using XiHan.BasicApp.Domain.Shared;
using XiHan.BasicApp.Rbac.Domain;
using XiHan.Framework.Core.Modularity;
using XiHan.Framework.Ddd.Domain;

namespace XiHan.BasicApp.Domain;

/// <summary>
/// XiHanBasicAppDomainModule
/// </summary>
[DependsOn(
    typeof(XiHanDddDomainModule),
    typeof(XiHanBasicAppRbacDomainModule),
    typeof(XiHanBasicAppCodeGenerationDomainModule),
    typeof(XiHanBasicAppDomainSharedModule)
)]
public class XiHanBasicAppDomainModule : XiHanModule
{
    /// <summary>
    /// 服务配置
    /// </summary>
    /// <param name="context"></param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
    }
}
