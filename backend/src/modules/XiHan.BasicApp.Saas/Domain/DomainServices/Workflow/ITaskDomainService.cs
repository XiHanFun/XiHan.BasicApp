#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ITaskDomainService
// Guid:6d0a32f5-7f60-4d9d-99d7-3b71e648d49f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
