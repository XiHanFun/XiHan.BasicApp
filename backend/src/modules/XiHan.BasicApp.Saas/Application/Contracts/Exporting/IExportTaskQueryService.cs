// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
