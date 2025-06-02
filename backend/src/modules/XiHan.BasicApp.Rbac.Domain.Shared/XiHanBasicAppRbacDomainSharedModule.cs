﻿#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:XiHanBasicAppRbacDomainSharedModule
// Guid:e8ce7154-a48c-4934-b548-96673cd857e0
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2024/12/7 6:26:04
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.AspNetCore;
using XiHan.Framework.Core.Modularity;
using XiHan.Framework.Ddd.Domain.Shared;

namespace XiHan.BasicApp.Rbac.Domain.Shared;

/// <summary>
/// 曦寒基础应用角色控制领域共享模块
/// </summary>
[DependsOn(
    typeof(XiHanDddDomainSharedModule),
    typeof(XiHanBasicAppAspNetCoreModule)
)]
public class XiHanBasicAppRbacDomainSharedModule : XiHanModule
{
    /// <summary>
    /// 服务配置
    /// </summary>
    /// <param name="context"></param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
    }
}
