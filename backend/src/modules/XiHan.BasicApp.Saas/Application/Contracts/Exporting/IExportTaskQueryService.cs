#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IExportTaskQueryService
// Guid:d3c0e8a6-1b4f-4a9c-85d7-7f60819203a4
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/14 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 导出任务查询应用服务接口（读侧：当前用户的导出任务列表 / 详情，供导出中心展示与轮询）
/// </summary>
public interface IExportTaskQueryService : IApplicationService
{
    /// <summary>
    /// 获取当前用户的导出任务分页（按创建时间倒序）
    /// </summary>
    Task<PageResultDtoBase<ExportTaskDto>> GetMineAsync(int pageIndex = 1, int pageSize = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取当前用户的导出任务详情（自鉴权；不存在返回 null）
    /// </summary>
    Task<ExportTaskDto?> GetDetailAsync(long id, CancellationToken cancellationToken = default);
}
