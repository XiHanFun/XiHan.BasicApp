// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Chat.Application;

/// <summary>
/// 聊天应用服务基类
/// </summary>
/// <remarks>
/// 与 Saas/AI/代码生成同构：类级 <see cref="AuthorizeAttribute"/> + <see cref="DynamicApiAttribute"/> 自动暴露为 HTTP API，
/// 归属独立的 DynamicApi 分组 BasicApp.Chat，与其它模块服务区隔。
/// </remarks>
[Authorize]
[DynamicApi(Group = "BasicApp.Chat", GroupName = "聊天服务")]
public abstract class ChatApplicationService : ApplicationServiceBase
{
}
