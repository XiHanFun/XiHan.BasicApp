// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.Framework.Application.Attributes;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 工作台查询应用服务
/// </summary>
/// <remarks>
/// 仪表盘业务数据的接口入口，目前没有方法，留给接入方按自己的业务添加。
/// 每个方法用 [PermissionAuthorize] 绑定它所读数据的查看权限，只查当前上下文（平台或租户）的数据；
/// 前端小组件需要按权限出现时，在 PageRegistry 的仪表盘页面下登记按钮，并把按钮码写进小组件登记表的 permission。
/// </remarks>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "工作台")]
public sealed class WorkbenchQueryService
    : SaasApplicationService, IWorkbenchQueryService
{
}
