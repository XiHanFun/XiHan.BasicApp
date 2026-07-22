// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 系统任务查询应用服务接口
/// </summary>
public interface ITaskQueryService : IApplicationService
{
    /// <summary>
    /// 获取系统任务分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统任务分页列表</returns>
    Task<PageResultDtoBase<TaskListItemDto>> GetTaskPageAsync(TaskPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取系统任务详情
    /// </summary>
    /// <param name="id">系统任务主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统任务详情</returns>
    Task<TaskDetailDto?> GetTaskDetailAsync(long id, CancellationToken cancellationToken = default);
}
