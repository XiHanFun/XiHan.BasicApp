// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
