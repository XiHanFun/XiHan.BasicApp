#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2024 ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:XiHanBasicAppCodeGenerationApplicationContractsModule
// Guid:a71de7c4-4e42-4bf1-a226-8918d7f94b02
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2024/12/7 6:36:53
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.Framework.Core.Modularity;
using XiHan.Framework.Ddd.Application.Contracts;

namespace XiHan.BasicApp.CodeGeneration.Application.Contracts;

/// <summary>
/// XiHanBasicAppCodeGenerationApplicationContractsModule
/// </summary>
[DependsOn(
    typeof(XiHanDddApplicationContractsModule)
    )]
public class XiHanBasicAppCodeGenerationApplicationContractsModule : XiHanModule
{
    /// <summary>
    /// 服务配置
    /// </summary>
    /// <param name="context"></param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
    }
}
