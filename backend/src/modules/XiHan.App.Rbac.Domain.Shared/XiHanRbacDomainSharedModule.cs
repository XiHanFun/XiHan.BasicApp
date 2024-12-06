#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2024 ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:XiHanRbacDomainSharedModule
// Guid:e8ce7154-a48c-4934-b548-96673cd857e0
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2024/12/7 6:26:04
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.Framework.Core.Modularity;
using XiHan.Framework.Ddd.Domain.Shared;

namespace XiHan.App.Rbac.Domain.Shared;

/// <summary>
/// XiHanRbacDomainSharedModule
/// </summary>
[DependsOn(
    typeof(XiHanDddDomainSharedModule)
    )]
public class XiHanRbacDomainSharedModule : XiHanModule
{
    /// <summary>
    /// 服务配置
    /// </summary>
    /// <param name="context"></param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
    }
}
