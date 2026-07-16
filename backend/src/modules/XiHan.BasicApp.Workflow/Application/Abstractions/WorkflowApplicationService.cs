#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:WorkflowApplicationService
// Guid:c07e94d1-58a3-4f26-b7e0-92d15c86f3a4
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/17 10:20:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Workflow.Application;

/// <summary>
/// 工作流应用服务基类
/// </summary>
/// <remarks>
/// 与 Saas 同构：类级 <see cref="AuthorizeAttribute"/> + <see cref="DynamicApiAttribute"/> 自动暴露为 HTTP API，
/// 归属独立的 DynamicApi 分组。工作流写操作不挂 UnitOfWork：引擎存储按操作独立作用域持久化，
/// 与请求事务无关（引擎不集成工作单元，见框架工作流库说明）。
/// </remarks>
[Authorize]
[DynamicApi(Group = "BasicApp.Workflow", GroupName = "工作流服务")]
public abstract class WorkflowApplicationService : ApplicationServiceBase
{
}
