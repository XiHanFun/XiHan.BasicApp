// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 系统任务应用层映射器
/// </summary>
public static class TaskApplicationMapper
{
    /// <summary>
    /// 映射系统任务创建命令
    /// </summary>
    public static TaskCreateCommand ToCreateCommand(TaskCreateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new TaskCreateCommand(
            input.TaskCode,
            input.TaskName,
            input.TaskDescription,
            input.TaskGroup,
            input.TaskClass,
            input.TaskMethod,
            input.TaskParams,
            input.TriggerType,
            input.CronExpression,
            input.StartTime,
            input.EndTime,
            input.NextRunTime,
            input.LastRunTime,
            input.IntervalSeconds,
            input.RepeatCount,
            input.ExecutedCount,
            input.TimeoutSeconds,
            input.RunTaskStatus,
            input.Priority,
            input.AllowConcurrent,
            input.RetryCount,
            input.MaxRetryCount,
            input.Status,
            input.Remark);
    }

    /// <summary>
    /// 映射系统任务列表项
    /// </summary>
    /// <param name="task">系统任务实体</param>
    /// <returns>系统任务列表项 DTO</returns>
    public static TaskListItemDto ToListItemDto(SysTask task)
    {
        ArgumentNullException.ThrowIfNull(task);

        return new TaskListItemDto
        {
            BasicId = task.BasicId,
            TaskCode = task.TaskCode,
            TaskName = task.TaskName,
            TaskDescription = task.TaskDescription,
            TaskGroup = task.TaskGroup,
            TriggerType = task.TriggerType,
            CronExpression = task.CronExpression,
            StartTime = task.StartTime,
            EndTime = task.EndTime,
            NextRunTime = task.NextRunTime,
            LastRunTime = task.LastRunTime,
            IntervalSeconds = task.IntervalSeconds,
            RepeatCount = task.RepeatCount,
            ExecutedCount = task.ExecutedCount,
            TimeoutSeconds = task.TimeoutSeconds,
            RunTaskStatus = task.RunTaskStatus,
            Priority = task.Priority,
            AllowConcurrent = task.AllowConcurrent,
            RetryCount = task.RetryCount,
            MaxRetryCount = task.MaxRetryCount,
            Status = task.Status,
            CreatedTime = task.CreatedTime,
            ModifiedTime = task.ModifiedTime
        };
    }

    /// <summary>
    /// 映射系统任务详情
    /// </summary>
    /// <param name="task">系统任务实体</param>
    /// <returns>系统任务详情 DTO</returns>
    public static TaskDetailDto ToDetailDto(SysTask task)
    {
        ArgumentNullException.ThrowIfNull(task);

        var item = ToListItemDto(task);
        return new TaskDetailDto
        {
            BasicId = item.BasicId,
            TaskCode = item.TaskCode,
            TaskName = item.TaskName,
            TaskDescription = item.TaskDescription,
            TaskGroup = item.TaskGroup,
            TaskClass = task.TaskClass,
            TaskMethod = task.TaskMethod,
            TaskParams = task.TaskParams,
            TriggerType = item.TriggerType,
            CronExpression = item.CronExpression,
            StartTime = item.StartTime,
            EndTime = item.EndTime,
            NextRunTime = item.NextRunTime,
            LastRunTime = item.LastRunTime,
            IntervalSeconds = item.IntervalSeconds,
            RepeatCount = item.RepeatCount,
            ExecutedCount = item.ExecutedCount,
            TimeoutSeconds = item.TimeoutSeconds,
            RunTaskStatus = item.RunTaskStatus,
            Priority = item.Priority,
            AllowConcurrent = item.AllowConcurrent,
            RetryCount = item.RetryCount,
            MaxRetryCount = item.MaxRetryCount,
            Status = item.Status,
            CreatedTime = item.CreatedTime,
            CreatedId = task.CreatedId,
            CreatedBy = task.CreatedBy,
            ModifiedTime = item.ModifiedTime,
            ModifiedId = task.ModifiedId,
            ModifiedBy = task.ModifiedBy,
            Remark = task.Remark
        };
    }

    /// <summary>
    /// 映射系统任务运行状态变更命令
    /// </summary>
    public static TaskRunStatusChangeCommand ToRunStatusCommand(TaskRunStatusUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new TaskRunStatusChangeCommand(
            input.BasicId,
            input.RunTaskStatus,
            input.NextRunTime,
            input.LastRunTime,
            input.ExecutedCount,
            input.RetryCount,
            input.Remark);
    }

    /// <summary>
    /// 映射系统任务启停状态变更命令
    /// </summary>
    public static TaskStatusChangeCommand ToStatusCommand(TaskStatusUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return new TaskStatusChangeCommand(input.BasicId, input.Status, input.Remark);
    }

    /// <summary>
    /// 映射系统任务更新命令
    /// </summary>
    public static TaskUpdateCommand ToUpdateCommand(TaskUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new TaskUpdateCommand(
            input.BasicId,
            input.TaskName,
            input.TaskDescription,
            input.TaskGroup,
            input.TaskClass,
            input.TaskMethod,
            input.TaskParams,
            input.TriggerType,
            input.CronExpression,
            input.StartTime,
            input.EndTime,
            input.NextRunTime,
            input.LastRunTime,
            input.IntervalSeconds,
            input.RepeatCount,
            input.ExecutedCount,
            input.TimeoutSeconds,
            input.Priority,
            input.AllowConcurrent,
            input.RetryCount,
            input.MaxRetryCount,
            input.Remark);
    }
}
