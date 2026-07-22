// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 任务领域服务
/// </summary>
public interface ITaskDomainService
{
    /// <summary>
    /// 创建任务
    /// </summary>
    Task<TaskCommandResult> CreateTaskAsync(TaskCreateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除任务
    /// </summary>
    Task<TaskCommandResult> DeleteTaskAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新任务
    /// </summary>
    Task<TaskCommandResult> UpdateTaskAsync(TaskUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新任务运行状态
    /// </summary>
    Task<TaskCommandResult> UpdateTaskRunStatusAsync(TaskRunStatusChangeCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新任务启停状态
    /// </summary>
    Task<TaskCommandResult> UpdateTaskStatusAsync(TaskStatusChangeCommand command, CancellationToken cancellationToken = default);
}
