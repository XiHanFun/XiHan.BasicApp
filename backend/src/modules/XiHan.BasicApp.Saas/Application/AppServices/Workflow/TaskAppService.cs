#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TaskAppService
// Guid:2a47620b-74e8-4f4e-bcad-7a0e7f2db207
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/06 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Uow.Attributes;
using static XiHan.BasicApp.Saas.Application.AppServices.SaasCommandValidation;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 系统任务命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "系统任务")]
public sealed class TaskAppService(ITaskRepository taskRepository)
    : SaasApplicationService, ITaskAppService
{
    private readonly ITaskRepository _taskRepository = taskRepository;

    /// <summary>
    /// 创建系统任务
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Task.Create)]
    public async Task<TaskDetailDto> CreateTaskAsync(TaskCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateCreateInput(input);
        var taskCode = Required(input.TaskCode, 100, nameof(input.TaskCode), "任务编码不能超过 100 个字符。");
        EnsureCodeHasNoWhitespace(taskCode, "任务编码不能包含空白字符。");
        if (await _taskRepository.AnyAsync(task => task.TaskCode == taskCode, cancellationToken))
        {
            throw new InvalidOperationException("任务编码已存在。");
        }

        var task = new SysTask
        {
            TaskCode = taskCode,
            TaskName = Required(input.TaskName, 200, nameof(input.TaskName), "任务名称不能超过 200 个字符。"),
            TaskDescription = Optional(input.TaskDescription, 500, nameof(input.TaskDescription), "任务描述不能超过 500 个字符。"),
            TaskGroup = Optional(input.TaskGroup, 50, nameof(input.TaskGroup), "任务分组不能超过 50 个字符。"),
            TaskClass = Required(input.TaskClass, 200, nameof(input.TaskClass), "任务类名不能超过 200 个字符。"),
            TaskMethod = Optional(input.TaskMethod, 100, nameof(input.TaskMethod), "任务方法不能超过 100 个字符。"),
            TaskParams = OptionalJson(input.TaskParams, "任务参数必须是有效 JSON。"),
            TriggerType = input.TriggerType,
            CronExpression = Optional(input.CronExpression, 100, nameof(input.CronExpression), "Cron 表达式不能超过 100 个字符。"),
            StartTime = input.StartTime,
            EndTime = input.EndTime,
            NextRunTime = input.NextRunTime,
            LastRunTime = input.LastRunTime,
            IntervalSeconds = input.IntervalSeconds,
            RepeatCount = input.RepeatCount,
            ExecutedCount = input.ExecutedCount,
            TimeoutSeconds = input.TimeoutSeconds,
            RunTaskStatus = input.RunTaskStatus,
            Priority = input.Priority,
            AllowConcurrent = input.AllowConcurrent,
            RetryCount = input.RetryCount,
            MaxRetryCount = input.MaxRetryCount,
            Status = input.Status,
            Remark = Optional(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。")
        };

        var savedTask = await _taskRepository.AddAsync(task, cancellationToken);
        return TaskApplicationMapper.ToDetailDto(savedTask);
    }

    /// <summary>
    /// 更新系统任务
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Task.Update)]
    public async Task<TaskDetailDto> UpdateTaskAsync(TaskUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateUpdateInput(input);
        var task = await GetTaskOrThrowAsync(input.BasicId, cancellationToken);
        if (task.RunTaskStatus == RunTaskStatus.Running)
        {
            throw new InvalidOperationException("运行中的任务不能修改任务配置。");
        }

        task.TaskName = Required(input.TaskName, 200, nameof(input.TaskName), "任务名称不能超过 200 个字符。");
        task.TaskDescription = Optional(input.TaskDescription, 500, nameof(input.TaskDescription), "任务描述不能超过 500 个字符。");
        task.TaskGroup = Optional(input.TaskGroup, 50, nameof(input.TaskGroup), "任务分组不能超过 50 个字符。");
        task.TaskClass = Required(input.TaskClass, 200, nameof(input.TaskClass), "任务类名不能超过 200 个字符。");
        task.TaskMethod = Optional(input.TaskMethod, 100, nameof(input.TaskMethod), "任务方法不能超过 100 个字符。");
        task.TaskParams = OptionalJson(input.TaskParams, "任务参数必须是有效 JSON。");
        task.TriggerType = input.TriggerType;
        task.CronExpression = Optional(input.CronExpression, 100, nameof(input.CronExpression), "Cron 表达式不能超过 100 个字符。");
        task.StartTime = input.StartTime;
        task.EndTime = input.EndTime;
        task.NextRunTime = input.NextRunTime;
        task.LastRunTime = input.LastRunTime;
        task.IntervalSeconds = input.IntervalSeconds;
        task.RepeatCount = input.RepeatCount;
        task.ExecutedCount = input.ExecutedCount;
        task.TimeoutSeconds = input.TimeoutSeconds;
        task.Priority = input.Priority;
        task.AllowConcurrent = input.AllowConcurrent;
        task.RetryCount = input.RetryCount;
        task.MaxRetryCount = input.MaxRetryCount;
        task.Remark = Optional(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。");

        var savedTask = await _taskRepository.UpdateAsync(task, cancellationToken);
        return TaskApplicationMapper.ToDetailDto(savedTask);
    }

    /// <summary>
    /// 更新系统任务启停状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Task.Status)]
    public async Task<TaskDetailDto> UpdateTaskStatusAsync(TaskStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(input.BasicId, "系统任务主键必须大于 0。");
        EnsureEnum(input.Status, nameof(input.Status));

        var task = await GetTaskOrThrowAsync(input.BasicId, cancellationToken);
        task.Status = input.Status;
        if (input.Status == EnableStatus.Disabled && task.RunTaskStatus == RunTaskStatus.Running)
        {
            task.RunTaskStatus = RunTaskStatus.Stopped;
        }

        task.Remark = Optional(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。") ?? task.Remark;

        var savedTask = await _taskRepository.UpdateAsync(task, cancellationToken);
        return TaskApplicationMapper.ToDetailDto(savedTask);
    }

    /// <summary>
    /// 更新系统任务运行状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Task.RunStatus)]
    public async Task<TaskDetailDto> UpdateTaskRunStatusAsync(TaskRunStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(input.BasicId, "系统任务主键必须大于 0。");
        EnsureEnum(input.RunTaskStatus, nameof(input.RunTaskStatus));
        EnsureOptionalNonNegative(input.ExecutedCount, nameof(input.ExecutedCount), "已执行次数不能小于 0。");
        EnsureOptionalNonNegative(input.RetryCount, nameof(input.RetryCount), "失败重试次数不能小于 0。");

        var task = await GetTaskOrThrowAsync(input.BasicId, cancellationToken);
        if (task.Status == EnableStatus.Disabled && input.RunTaskStatus == RunTaskStatus.Running)
        {
            throw new InvalidOperationException("停用任务不能设置为执行中。");
        }

        task.RunTaskStatus = input.RunTaskStatus;
        task.NextRunTime = input.NextRunTime ?? task.NextRunTime;
        task.LastRunTime = input.LastRunTime ?? task.LastRunTime;
        task.ExecutedCount = input.ExecutedCount ?? task.ExecutedCount;
        task.RetryCount = input.RetryCount ?? task.RetryCount;
        task.Remark = Optional(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。") ?? task.Remark;

        var savedTask = await _taskRepository.UpdateAsync(task, cancellationToken);
        return TaskApplicationMapper.ToDetailDto(savedTask);
    }

    /// <summary>
    /// 删除系统任务
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Task.Delete)]
    public async Task DeleteTaskAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var task = await GetTaskOrThrowAsync(id, cancellationToken);
        if (task.RunTaskStatus == RunTaskStatus.Running)
        {
            throw new InvalidOperationException("运行中的任务不能删除。");
        }

        if (!await _taskRepository.DeleteAsync(task, cancellationToken))
        {
            throw new InvalidOperationException("系统任务删除失败。");
        }
    }

    private async Task<SysTask> GetTaskOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        EnsureId(id, "系统任务主键必须大于 0。");
        return await _taskRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("系统任务不存在。");
    }

    private static void ValidateCreateInput(TaskCreateDto input)
    {
        ValidateTaskInput(
            input.TriggerType,
            input.CronExpression,
            input.StartTime,
            input.EndTime,
            input.IntervalSeconds,
            input.RepeatCount,
            input.ExecutedCount,
            input.TimeoutSeconds,
            input.RunTaskStatus,
            input.Status,
            input.RetryCount,
            input.MaxRetryCount);
    }

    private static void ValidateUpdateInput(TaskUpdateDto input)
    {
        EnsureId(input.BasicId, "系统任务主键必须大于 0。");
        ValidateTaskInput(
            input.TriggerType,
            input.CronExpression,
            input.StartTime,
            input.EndTime,
            input.IntervalSeconds,
            input.RepeatCount,
            input.ExecutedCount,
            input.TimeoutSeconds,
            null,
            null,
            input.RetryCount,
            input.MaxRetryCount);
    }

    private static void ValidateTaskInput(
        TriggerType triggerType,
        string? cronExpression,
        DateTimeOffset? startTime,
        DateTimeOffset? endTime,
        int? intervalSeconds,
        int repeatCount,
        int executedCount,
        int timeoutSeconds,
        RunTaskStatus? runTaskStatus,
        EnableStatus? status,
        int retryCount,
        int maxRetryCount)
    {
        EnsureEnum(triggerType, nameof(triggerType));
        if (runTaskStatus.HasValue)
        {
            EnsureEnum(runTaskStatus.Value, nameof(runTaskStatus));
        }

        if (status.HasValue)
        {
            EnsureEnum(status.Value, nameof(status));
        }

        if (startTime.HasValue && endTime.HasValue && endTime.Value <= startTime.Value)
        {
            throw new InvalidOperationException("结束时间必须晚于开始时间。");
        }

        if (triggerType == TriggerType.Cron && string.IsNullOrWhiteSpace(cronExpression))
        {
            throw new InvalidOperationException("Cron 触发任务必须填写 Cron 表达式。");
        }

        if (triggerType == TriggerType.Recurring && (!intervalSeconds.HasValue || intervalSeconds.Value <= 0))
        {
            throw new InvalidOperationException("循环执行任务必须填写大于 0 的执行间隔。");
        }

        if (intervalSeconds is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(intervalSeconds), "执行间隔必须大于 0。");
        }

        if (repeatCount < -1)
        {
            throw new ArgumentOutOfRangeException(nameof(repeatCount), "重复次数不能小于 -1。");
        }

        EnsureNonNegative(executedCount, nameof(executedCount), "已执行次数不能小于 0。");
        if (timeoutSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(timeoutSeconds), "超时时间必须大于 0。");
        }

        EnsureNonNegative(retryCount, nameof(retryCount), "失败重试次数不能小于 0。");
        EnsureNonNegative(maxRetryCount, nameof(maxRetryCount), "最大重试次数不能小于 0。");
        if (retryCount > maxRetryCount)
        {
            throw new InvalidOperationException("失败重试次数不能大于最大重试次数。");
        }
    }

    private static void EnsureCodeHasNoWhitespace(string value, string message)
    {
        if (value.Any(char.IsWhiteSpace))
        {
            throw new InvalidOperationException(message);
        }
    }
}
