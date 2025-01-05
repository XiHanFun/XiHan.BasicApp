#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:XiHanBasicAppCodeGenerationDomainModule
// Guid:a9bb3380-d8cd-41e0-b2db-e8aed20213b5
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2024/12/7 6:37:16
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.Framework.Core.Modularity;
using XiHan.Framework.Ddd.Domain;

namespace XiHan.BasicApp.CodeGeneration.Domain;

/// <summary>
/// 曦寒基础应用代码生成领域模块
/// </summary>
[DependsOn(
    typeof(XiHanDddDomainModule)
)]
public class XiHanBasicAppCodeGenerationDomainModule : XiHanModule
{
    /// <summary>
    /// 服务配置
    /// </summary>
    /// <param name="context"></param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
    }
}
