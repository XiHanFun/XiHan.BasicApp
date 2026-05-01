#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ITaskLogQueryService
// Guid:d9f8ee64-43da-496f-b56a-176b4d650c49
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 任务日志查询应用服务接口
/// </summary>
public interface ITaskLogQueryService : IApplicationService
{
    /// <summary>
    /// 获取任务日志分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务日志分页列表</returns>
    Task<PageResultDtoBase<TaskLogListItemDto>> GetTaskLogPageAsync(TaskLogPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取任务日志详情
    /// </summary>
    /// <param name="id">任务日志主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务日志详情</returns>
    Task<TaskLogDetailDto?> GetTaskLogDetailAsync(long id, CancellationToken cancellationToken = default);
}
