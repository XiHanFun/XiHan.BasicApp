#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IWorkbenchQueryService
// Guid:73d4214d-2269-4ff9-ac41-88259f331e3f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/07 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
