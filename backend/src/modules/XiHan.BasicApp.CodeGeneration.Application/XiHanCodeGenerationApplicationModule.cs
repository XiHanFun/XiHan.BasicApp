#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2024 ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:XiHanCodeGenerationApplicationModule
// Guid:706325c3-33e8-4710-8128-f1ee449ffc27
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2024/12/7 6:36:28
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.Framework.Core.Modularity;
using XiHan.Framework.Ddd.Application;

namespace XiHan.BasicApp.CodeGeneration.Application;

/// <summary>
/// XiHanCodeGenerationApplicationModule
/// </summary>
[DependsOn(
    typeof(XiHanDddApplicationModule)
    )]
public class XiHanCodeGenerationApplicationModule : XiHanModule
{
    /// <summary>
    /// 服务配置
    /// </summary>
    /// <param name="context"></param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
    }
}
