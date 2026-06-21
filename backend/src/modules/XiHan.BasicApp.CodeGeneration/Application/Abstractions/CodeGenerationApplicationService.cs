#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:CodeGenerationApplicationService
// Guid:c0de9e00-0500-4a00-9000-000000000500
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/20 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.CodeGeneration.Application;

/// <summary>
/// 代码生成应用服务基类
/// </summary>
/// <remarks>
/// 与 Saas 同构：类级 <see cref="AuthorizeAttribute"/> + <see cref="DynamicApiAttribute"/> 自动暴露为 HTTP API，
/// 但归属独立的 DynamicApi 分组，与系统 SaaS 服务区隔。
/// </remarks>
[Authorize]
[DynamicApi(Group = "BasicApp.CodeGen", GroupName = "代码生成服务")]
public abstract class CodeGenerationApplicationService : ApplicationServiceBase
{
}
