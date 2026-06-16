#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:XiHanBasicAppWebHostModule
// Guid:c3d5fae5-17b2-44f8-aaa2-1ce1b868f8e6
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2024/12/10 05:34:12
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.CodeGeneration;
using XiHan.BasicApp.Saas;
using XiHan.Framework.Core.Modularity;

namespace XiHan.BasicApp.WebHost;

/// <summary>
/// 曦寒基础应用 Web 主机
/// </summary>
[DependsOn(
    // 应用模块依赖
    typeof(XiHanBasicAppSaasModule),
    typeof(XiHanBasicAppCodeGenerationModule)
)]
public class XiHanBasicAppWebHostModule : XiHanModule
{
}
