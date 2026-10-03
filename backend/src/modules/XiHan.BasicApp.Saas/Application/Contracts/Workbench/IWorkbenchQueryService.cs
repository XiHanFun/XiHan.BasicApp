// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 工作台查询应用服务接口
/// </summary>
/// <remarks>
/// 仪表盘的业务数据接口加在这里：方法名以 Async 结尾、末参是带默认值的 <see cref="CancellationToken"/>，
/// DTO 放在 Application/Dtos/Workbench，前端在 workbenchApi 里用 createDynamicApiClient('WorkbenchQuery') 调用。
/// 自带的仪表盘图表用的是前端示例数据，不经过本服务。
/// </remarks>
public interface IWorkbenchQueryService : IApplicationService
{
}
