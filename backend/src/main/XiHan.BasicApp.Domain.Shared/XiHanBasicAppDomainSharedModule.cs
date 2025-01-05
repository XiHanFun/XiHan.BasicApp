#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:XiHanBasicAppDomainSharedModule
// Guid:e1b6e1b9-7ad8-4bae-a219-5c73126e560e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2024/12/10 5:38:03
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.CodeGeneration.Domain.Shared;
using XiHan.BasicApp.Rbac.Domain.Shared;
using XiHan.Framework.Core.Modularity;
using XiHan.Framework.Ddd.Domain.Shared;

namespace XiHan.BasicApp.Domain.Shared;

/// <summary>
/// 曦寒基础应用领域共享模块
/// </summary>
[DependsOn(
    typeof(XiHanDddDomainSharedModule),
    typeof(XiHanBasicAppRbacDomainSharedModule),
    typeof(XiHanBasicAppCodeGenerationDomainSharedModule)
)]
public class XiHanBasicAppDomainSharedModule : XiHanModule
{
    /// <summary>
    /// 服务配置
    /// </summary>
    /// <param name="context"></param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
    }
}
