#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AiApplicationService
// Guid:a11c0de0-4000-4a10-9a00-00000000ai40
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/05 14:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.AI.Application;

/// <summary>
/// AI 应用服务基类
/// </summary>
/// <remarks>
/// 与 Saas/代码生成同构：类级 <see cref="AuthorizeAttribute"/> + <see cref="DynamicApiAttribute"/> 自动暴露为 HTTP API，
/// 归属独立的 DynamicApi 分组 BasicApp.AI，与系统 SaaS/代码生成服务区隔。
/// </remarks>
[Authorize]
[DynamicApi(Group = "BasicApp.AI", GroupName = "AI 服务")]
public abstract class AiApplicationService : ApplicationServiceBase
{
}
