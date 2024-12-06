﻿#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2024 ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:XiHanCodeGenerationDomainSharedModule
// Guid:c78b6eca-f93e-4b78-8084-560b5db2a85d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2024/12/7 6:37:38
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.Framework.Core.Modularity;
using XiHan.Framework.Ddd.Domain.Shared;

namespace XiHan.App.CodeGeneration.Domain.Shared;

/// <summary>
/// XiHanCodeGenerationDomainSharedModule
/// </summary>
[DependsOn(
    typeof(XiHanDddDomainSharedModule)
    )]
public class XiHanCodeGenerationDomainSharedModule : XiHanModule
{
    /// <summary>
    /// 服务配置
    /// </summary>
    /// <param name="context"></param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
    }
}
