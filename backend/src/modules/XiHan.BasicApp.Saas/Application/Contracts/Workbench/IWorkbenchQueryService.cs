// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 工作台查询应用服务接口
/// </summary>
public interface IWorkbenchQueryService : IApplicationService
{
    /// <summary>
    /// 获取工作台仪表盘摘要
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>工作台仪表盘摘要</returns>
    Task<WorkbenchDashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default);
}
