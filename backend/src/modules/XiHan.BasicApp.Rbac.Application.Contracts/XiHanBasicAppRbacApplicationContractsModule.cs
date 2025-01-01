#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:XiHanBasicAppRbacApplicationContractsModule
// Guid:7d811c9d-0bfd-40ae-80b9-c87bb69dd759
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2024/12/7 6:25:20
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.Framework.Core.Modularity;
using XiHan.Framework.Ddd.Application.Contracts;

namespace XiHan.BasicApp.Rbac.Application.Contracts;

/// <summary>
/// XiHanBasicAppRbacApplicationContractsModule
/// </summary>
[DependsOn(
    typeof(XiHanDddApplicationContractsModule)
)]
public class XiHanBasicAppRbacApplicationContractsModule : XiHanModule
{
    /// <summary>
    /// 服务配置
    /// </summary>
    /// <param name="context"></param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
    }
}
