#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ITaskAppService
// Guid:5e7c0453-f396-4e80-bd69-409f6db73121
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/06 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 系统任务命令应用服务接口
/// </summary>
public interface ITaskAppService : IApplicationService
{
    /// <summary>
    /// 创建系统任务
    /// </summary>
    Task<TaskDetailDto> CreateTaskAsync(TaskCreateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新系统任务
    /// </summary>
    Task<TaskDetailDto> UpdateTaskAsync(TaskUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新系统任务启停状态
    /// </summary>
    Task<TaskDetailDto> UpdateTaskStatusAsync(TaskStatusUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新系统任务运行状态
    /// </summary>
    Task<TaskDetailDto> UpdateTaskRunStatusAsync(TaskRunStatusUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 立即执行一次系统任务（不影响既有调度计划）
    /// </summary>
    Task<TaskRunResultDto> RunTaskAsync(TaskRunDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除系统任务
    /// </summary>
    Task DeleteTaskAsync(long id, CancellationToken cancellationToken = default);
}
