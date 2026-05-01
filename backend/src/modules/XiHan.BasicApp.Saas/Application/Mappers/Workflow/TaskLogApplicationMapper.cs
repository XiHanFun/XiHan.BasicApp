#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TaskLogApplicationMapper
// Guid:a3f314e5-dc8f-4d22-91a6-c81c6e4447b2
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 任务日志应用层映射器
/// </summary>
public static class TaskLogApplicationMapper
{
    /// <summary>
    /// 映射任务日志列表项
    /// </summary>
    /// <param name="taskLog">任务日志实体</param>
    /// <returns>任务日志列表项 DTO</returns>
    public static TaskLogListItemDto ToListItemDto(SysTaskLog taskLog)
    {
        ArgumentNullException.ThrowIfNull(taskLog);

        return new TaskLogListItemDto
        {
            BasicId = taskLog.BasicId,
            TaskId = taskLog.TaskId,
            TaskCode = taskLog.TaskCode,
            TaskName = taskLog.TaskName,
            BatchNumber = taskLog.BatchNumber,
            TaskStatus = taskLog.TaskStatus,
            StartTime = taskLog.StartTime,
            EndTime = taskLog.EndTime,
            ExecutionTime = taskLog.ExecutionTime,
            RetryCount = taskLog.RetryCount,
            TriggerMode = taskLog.TriggerMode,
            HasRunResult = !string.IsNullOrWhiteSpace(taskLog.ExecutionResult),
            HasExceptionText = !string.IsNullOrWhiteSpace(taskLog.ExceptionMessage),
            HasStack = !string.IsNullOrWhiteSpace(taskLog.ExceptionStackTrace),
            HasOutputTrace = !string.IsNullOrWhiteSpace(taskLog.OutputLog),
            HasExtension = !string.IsNullOrWhiteSpace(taskLog.ExtendData),
            HasNote = !string.IsNullOrWhiteSpace(taskLog.Remark),
            CreatedTime = taskLog.CreatedTime
        };
    }

    /// <summary>
    /// 映射任务日志详情
    /// </summary>
    /// <param name="taskLog">任务日志实体</param>
    /// <returns>任务日志详情 DTO</returns>
    public static TaskLogDetailDto ToDetailDto(SysTaskLog taskLog)
    {
        ArgumentNullException.ThrowIfNull(taskLog);

        var item = ToListItemDto(taskLog);
        return new TaskLogDetailDto
        {
            BasicId = item.BasicId,
            TaskId = item.TaskId,
            TaskCode = item.TaskCode,
            TaskName = item.TaskName,
            BatchNumber = item.BatchNumber,
            TaskStatus = item.TaskStatus,
            StartTime = item.StartTime,
            EndTime = item.EndTime,
            ExecutionTime = item.ExecutionTime,
            RetryCount = item.RetryCount,
            TriggerMode = item.TriggerMode,
            HasRunResult = item.HasRunResult,
            HasExceptionText = item.HasExceptionText,
            HasStack = item.HasStack,
            HasOutputTrace = item.HasOutputTrace,
            HasExtension = item.HasExtension,
            HasNote = item.HasNote,
            CreatedTime = item.CreatedTime,
            CreatedId = taskLog.CreatedId,
            CreatedBy = taskLog.CreatedBy
        };
    }
}
