#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TaskDomainService
// Guid:d2c8c2e5-f94f-43f2-8f86-660a862a29a2
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Text.Json;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 任务领域服务实现
/// </summary>
public sealed class TaskDomainService
    : ITaskDomainService
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public TaskDomainService(
        ITaskRepository taskRepository,
        ITaskScheduleDomainService taskScheduleDomainService)
    {
        _taskRepository = taskRepository;
        _taskScheduleDomainService = taskScheduleDomainService;
    }

    private readonly ITaskRepository _taskRepository;
    private readonly ITaskScheduleDomainService _taskScheduleDomainService;

    /// <inheritdoc />
    public async Task<TaskCommandResult> CreateTaskAsync(TaskCreateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateCreateCommand(command);
        var taskCode = Required(command.TaskCode, 100, nameof(command.TaskCode), "任务编码不能超过 100 个字符。");
        EnsureCodeHasNoWhitespace(taskCode, "任务编码不能包含空白字符。");
        if (await _taskRepository.AnyAsync(task => task.TaskCode == taskCode, cancellationToken))
        {
            throw new InvalidOperationException("任务编码已存在。");
        }

        var task = new SysTask
        {
            TaskCode = taskCode,
            TaskName = Required(command.TaskName, 200, nameof(command.TaskName), "任务名称不能超过 200 个字符。"),
            TaskDescription = Optional(command.TaskDescription, 500, nameof(command.TaskDescription), "任务描述不能超过 500 个字符。"),
            TaskGroup = Optional(command.TaskGroup, 50, nameof(command.TaskGroup), "任务分组不能超过 50 个字符。"),
            TaskClass = Required(command.TaskClass, 200, nameof(command.TaskClass), "任务类名不能超过 200 个字符。"),
            TaskMethod = Optional(command.TaskMethod, 100, nameof(command.TaskMethod), "任务方法不能超过 100 个字符。"),
            TaskParams = OptionalJson(command.TaskParams, "任务参数必须是有效 JSON。"),
            TriggerType = command.TriggerType,
            CronExpression = Optional(command.CronExpression, 100, nameof(command.CronExpression), "Cron 表达式不能超过 100 个字符。"),
            StartTime = command.StartTime,
            EndTime = command.EndTime,
            NextRunTime = command.NextRunTime,
            LastRunTime = command.LastRunTime,
            IntervalSeconds = command.IntervalSeconds,
            RepeatCount = command.RepeatCount,
            ExecutedCount = command.ExecutedCount,
            TimeoutSeconds = command.TimeoutSeconds,
            RunTaskStatus = command.RunTaskStatus,
            Priority = command.Priority,
            AllowConcurrent = command.AllowConcurrent,
            RetryCount = command.RetryCount,
            MaxRetryCount = command.MaxRetryCount,
            Status = command.Status,
            Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。")
        };

        var savedTask = await _taskRepository.AddAsync(task, cancellationToken);
        var syncAction = savedTask.Status == EnableStatus.Enabled ? TaskSchedulerSyncAction.Register : TaskSchedulerSyncAction.None;
        return new TaskCommandResult(savedTask, syncAction);
    }

    /// <inheritdoc />
    public async Task<TaskCommandResult> DeleteTaskAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var task = await GetTaskOrThrowAsync(id, cancellationToken);
        _taskScheduleDomainService.EnsureCanDelete(task);
        if (!await _taskRepository.DeleteAsync(task, cancellationToken))
        {
            throw new InvalidOperationException("系统任务删除失败。");
        }

        return new TaskCommandResult(task, TaskSchedulerSyncAction.Unregister);
    }

    /// <inheritdoc />
    public async Task<TaskCommandResult> UpdateTaskAsync(TaskUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateUpdateCommand(command);
        var task = await GetTaskOrThrowAsync(command.BasicId, cancellationToken);
        _taskScheduleDomainService.EnsureCanModify(task);

        task.TaskName = Required(command.TaskName, 200, nameof(command.TaskName), "任务名称不能超过 200 个字符。");
        task.TaskDescription = Optional(command.TaskDescription, 500, nameof(command.TaskDescription), "任务描述不能超过 500 个字符。");
        task.TaskGroup = Optional(command.TaskGroup, 50, nameof(command.TaskGroup), "任务分组不能超过 50 个字符。");
        task.TaskClass = Required(command.TaskClass, 200, nameof(command.TaskClass), "任务类名不能超过 200 个字符。");
        task.TaskMethod = Optional(command.TaskMethod, 100, nameof(command.TaskMethod), "任务方法不能超过 100 个字符。");
        task.TaskParams = OptionalJson(command.TaskParams, "任务参数必须是有效 JSON。");
        task.TriggerType = command.TriggerType;
        task.CronExpression = Optional(command.CronExpression, 100, nameof(command.CronExpression), "Cron 表达式不能超过 100 个字符。");
        task.StartTime = command.StartTime;
        task.EndTime = command.EndTime;
        task.NextRunTime = command.NextRunTime;
        task.LastRunTime = command.LastRunTime;
        task.IntervalSeconds = command.IntervalSeconds;
        task.RepeatCount = command.RepeatCount;
        task.ExecutedCount = command.ExecutedCount;
        task.TimeoutSeconds = command.TimeoutSeconds;
        task.Priority = command.Priority;
        task.AllowConcurrent = command.AllowConcurrent;
        task.RetryCount = command.RetryCount;
        task.MaxRetryCount = command.MaxRetryCount;
        task.Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。");

        var savedTask = await _taskRepository.UpdateAsync(task, cancellationToken);
        return new TaskCommandResult(savedTask, TaskSchedulerSyncAction.Replace);
    }

    /// <inheritdoc />
    public async Task<TaskCommandResult> UpdateTaskRunStatusAsync(TaskRunStatusChangeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.BasicId, "系统任务主键必须大于 0。");
        var task = await GetTaskOrThrowAsync(command.BasicId, cancellationToken);
        var oldRunStatus = task.RunTaskStatus;

        _taskScheduleDomainService.ApplyRunStatus(
            task,
            command.RunTaskStatus,
            command.NextRunTime,
            command.LastRunTime,
            command.ExecutedCount,
            command.RetryCount,
            command.Remark);

        var savedTask = await _taskRepository.UpdateAsync(task, cancellationToken);
        return new TaskCommandResult(savedTask, ResolveRunStatusSyncAction(oldRunStatus, command.RunTaskStatus));
    }

    /// <inheritdoc />
    public async Task<TaskCommandResult> UpdateTaskStatusAsync(TaskStatusChangeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.BasicId, "系统任务主键必须大于 0。");
        var task = await GetTaskOrThrowAsync(command.BasicId, cancellationToken);
        var wasEnabled = task.Status == EnableStatus.Enabled;

        _taskScheduleDomainService.ApplyStatus(task, command.Status, command.Remark);

        var savedTask = await _taskRepository.UpdateAsync(task, cancellationToken);
        return new TaskCommandResult(savedTask, ResolveStatusSyncAction(wasEnabled, command.Status));
    }

    private static void EnsureCodeHasNoWhitespace(string value, string message)
    {
        if (value.Any(char.IsWhiteSpace))
        {
            throw new InvalidOperationException(message);
        }
    }

    private static TaskSchedulerSyncAction ResolveRunStatusSyncAction(RunTaskStatus oldRunStatus, RunTaskStatus newRunStatus)
    {
        if (oldRunStatus != RunTaskStatus.Paused && newRunStatus == RunTaskStatus.Paused)
        {
            return TaskSchedulerSyncAction.Pause;
        }

        return oldRunStatus == RunTaskStatus.Paused && newRunStatus != RunTaskStatus.Paused
            ? TaskSchedulerSyncAction.Resume
            : TaskSchedulerSyncAction.None;
    }

    private static TaskSchedulerSyncAction ResolveStatusSyncAction(bool wasEnabled, EnableStatus newStatus)
    {
        if (wasEnabled && newStatus == EnableStatus.Disabled)
        {
            return TaskSchedulerSyncAction.Pause;
        }

        return !wasEnabled && newStatus == EnableStatus.Enabled
            ? TaskSchedulerSyncAction.Replace
            : TaskSchedulerSyncAction.None;
    }

    private void ValidateCreateCommand(TaskCreateCommand command)
    {
        EnsureEnum(command.RunTaskStatus, nameof(command.RunTaskStatus));
        EnsureEnum(command.Status, nameof(command.Status));
        _taskScheduleDomainService.EnsureScheduleConfiguration(
            command.TriggerType,
            command.CronExpression,
            command.StartTime,
            command.EndTime,
            command.IntervalSeconds,
            command.RepeatCount,
            command.ExecutedCount,
            command.TimeoutSeconds,
            command.RetryCount,
            command.MaxRetryCount);
    }

    private void ValidateUpdateCommand(TaskUpdateCommand command)
    {
        EnsureId(command.BasicId, "系统任务主键必须大于 0。");
        _taskScheduleDomainService.EnsureScheduleConfiguration(
            command.TriggerType,
            command.CronExpression,
            command.StartTime,
            command.EndTime,
            command.IntervalSeconds,
            command.RepeatCount,
            command.ExecutedCount,
            command.TimeoutSeconds,
            command.RetryCount,
            command.MaxRetryCount);
    }

    private async Task<SysTask> GetTaskOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        EnsureId(id, "系统任务主键必须大于 0。");
        return await _taskRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("系统任务不存在。");
    }

    private static void EnsureEnum<TEnum>(TEnum value, string paramName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(paramName, "枚举值无效。");
        }
    }

    private static void EnsureId(long id, string message)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), message);
        }
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? Optional(string? value, int maxLength, string paramName, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }

        return normalized;
    }

    private static string? OptionalJson(string? value, string message)
    {
        var normalized = NormalizeNullable(value);
        if (normalized is null)
        {
            return null;
        }

        try
        {
            using var _ = JsonDocument.Parse(normalized);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(message, exception);
        }

        return normalized;
    }

    private static string Required(string? value, int maxLength, string paramName, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }

        return normalized;
    }
}
