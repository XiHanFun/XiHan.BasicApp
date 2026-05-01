#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TaskApplicationMapper
// Guid:4e76d6b6-7c76-4f50-b31f-a5f540a0a5bd
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 系统任务应用层映射器
/// </summary>
public static class TaskApplicationMapper
{
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
            HasExecutionTarget = !string.IsNullOrWhiteSpace(task.TaskClass) || !string.IsNullOrWhiteSpace(task.TaskMethod),
            HasRuntimeArgs = !string.IsNullOrWhiteSpace(task.TaskParams),
            HasNote = !string.IsNullOrWhiteSpace(task.Remark),
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
            HasExecutionTarget = item.HasExecutionTarget,
            HasRuntimeArgs = item.HasRuntimeArgs,
            HasNote = item.HasNote,
            CreatedTime = item.CreatedTime,
            CreatedId = task.CreatedId,
            CreatedBy = task.CreatedBy,
            ModifiedTime = item.ModifiedTime,
            ModifiedId = task.ModifiedId,
            ModifiedBy = task.ModifiedBy
        };
    }
}
