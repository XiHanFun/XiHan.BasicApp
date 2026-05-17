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
using XiHan.BasicApp.Saas.Infrastructure.Tasks.Jobs;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Tasks.ScheduledJobs.Abstractions;
using XiHan.Framework.Tasks.ScheduledJobs.Models;
using XiHan.Framework.Uow.Attributes;
using static XiHan.BasicApp.Saas.Application.AppServices.SaasCommandValidation;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 系统任务命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "系统任务")]
public sealed class TaskAppService : SaasApplicationService, ITaskAppService
{
    private readonly IJobScheduler _jobScheduler;
    private readonly ITaskRepository _taskRepository;

    public TaskAppService(ITaskRepository taskRepository, IJobScheduler jobScheduler)
    {
        _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
        _jobScheduler = jobScheduler ?? throw new ArgumentNullException(nameof(jobScheduler));
    }

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

        // 注册到框架调度器
        if (savedTask.Status == EnableStatus.Enabled)
        {
            var jobInfo = BuildJobInfo(savedTask);
            _jobScheduler.RegisterJob(jobInfo);
        }

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

        // 先取消注册调度器，再删除实体
        _jobScheduler.UnregisterJob(task.TaskCode);

        if (!await _taskRepository.DeleteAsync(task, cancellationToken))
        {
            // 删除失败时重新注册任务
            if (task.Status == EnableStatus.Enabled)
            {
                _jobScheduler.RegisterJob(BuildJobInfo(task));
            }
            throw new InvalidOperationException("系统任务删除失败。");
        }
    }

    /// <summary>
    /// 同步所有活跃的 SysTask 到框架调度器（用于应用启动时初始化）
    /// </summary>
    public async Task SyncAllActiveJobsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var allTasks = await _taskRepository.GetListAsync(
            task => task.Status == EnableStatus.Enabled,
            cancellationToken);

        if (allTasks.Count == 0)
        {
            return;
        }

        foreach (var task in allTasks)
        {
            // 跳过已注册的同名任务，避免重复注册
            var existingJobs = _jobScheduler.GetAllJobs();
            if (existingJobs.Any(j => j.JobName == task.TaskCode))
            {
                continue;
            }

            var jobInfo = BuildJobInfo(task);
            _jobScheduler.RegisterJob(jobInfo);
        }
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

        var wasEnabled = task.Status == EnableStatus.Enabled;

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

        // 重新注册到框架调度器（先取消注册旧配置，再注册新配置）
        _jobScheduler.UnregisterJob(savedTask.TaskCode);
        if (savedTask.Status == EnableStatus.Enabled)
        {
            var jobInfo = BuildJobInfo(savedTask);
            _jobScheduler.RegisterJob(jobInfo);
        }

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

        var oldRunStatus = task.RunTaskStatus;

        task.RunTaskStatus = input.RunTaskStatus;
        task.NextRunTime = input.NextRunTime ?? task.NextRunTime;
        task.LastRunTime = input.LastRunTime ?? task.LastRunTime;
        task.ExecutedCount = input.ExecutedCount ?? task.ExecutedCount;
        task.RetryCount = input.RetryCount ?? task.RetryCount;
        task.Remark = Optional(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。") ?? task.Remark;

        var savedTask = await _taskRepository.UpdateAsync(task, cancellationToken);

        // 运行状态变更时同步调度器
        if (oldRunStatus != RunTaskStatus.Paused && input.RunTaskStatus == RunTaskStatus.Paused)
        {
            _jobScheduler.PauseJob(savedTask.TaskCode);
        }
        else if (oldRunStatus == RunTaskStatus.Paused && input.RunTaskStatus != RunTaskStatus.Paused)
        {
            _jobScheduler.ResumeJob(savedTask.TaskCode);
        }

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
        var wasEnabled = task.Status == EnableStatus.Enabled;

        task.Status = input.Status;
        if (input.Status == EnableStatus.Disabled && task.RunTaskStatus == RunTaskStatus.Running)
        {
            task.RunTaskStatus = RunTaskStatus.Stopped;
        }

        task.Remark = Optional(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。") ?? task.Remark;

        var savedTask = await _taskRepository.UpdateAsync(task, cancellationToken);

        // 启停状态变更时同步调度器
        if (wasEnabled && input.Status == EnableStatus.Disabled)
        {
            _jobScheduler.PauseJob(savedTask.TaskCode);
        }
        else if (!wasEnabled && input.Status == EnableStatus.Enabled)
        {
            // 重新注册
            _jobScheduler.UnregisterJob(savedTask.TaskCode);
            var jobInfo = BuildJobInfo(savedTask);
            _jobScheduler.RegisterJob(jobInfo);
        }

        return TaskApplicationMapper.ToDetailDto(savedTask);
    }

    #region Private Helpers

    /// <summary>
    /// 从 SysTask 实体构建 JobInfo
    /// </summary>
    private static JobInfo BuildJobInfo(SysTask task)
    {
        return new JobInfo
        {
            JobName = task.TaskCode,
            Description = task.TaskDescription,
            JobType = typeof(DynamicJobWorker),
            TriggerType = MapTriggerType(task.TriggerType),
            CronExpression = task.CronExpression,
            Interval = task.IntervalSeconds.HasValue
                ? TimeSpan.FromSeconds(task.IntervalSeconds.Value)
                : null,
            Priority = MapPriority(task.Priority),
            AllowConcurrent = task.AllowConcurrent,
            TimeoutMilliseconds = task.TimeoutSeconds * 1000,
            RetryPolicy = new JobRetryPolicy
            {
                MaxRetryCount = task.MaxRetryCount,
                RetryIntervalMilliseconds = 1000,
                UseExponentialBackoff = true
            },
            IsEnabled = task.Status == EnableStatus.Enabled,
            TenantId = task.TenantId,
            DefaultParameters = new Dictionary<string, object?>
            {
                ["taskId"] = task.BasicId,
                ["taskClass"] = task.TaskClass,
                ["taskMethod"] = task.TaskMethod,
                ["taskParams"] = task.TaskParams
            }
        };
    }

    private static void EnsureCodeHasNoWhitespace(string value, string message)
    {
        if (value.Any(char.IsWhiteSpace))
        {
            throw new InvalidOperationException(message);
        }
    }

    /// <summary>
    /// 将领域优先级（int）映射到框架 JobPriority
    /// </summary>
    private static JobPriority MapPriority(int priority) => priority switch
    {
        <= 0 => JobPriority.Normal,
        1 => JobPriority.High,
        _ => JobPriority.Critical
    };

    /// <summary>
    /// 将领域 TriggerType 映射到框架 JobTriggerType
    /// </summary>
    private static JobTriggerType MapTriggerType(TriggerType triggerType) => triggerType switch
    {
        TriggerType.Immediate => JobTriggerType.Manual,
        TriggerType.Schedule => JobTriggerType.Delay,
        TriggerType.Recurring => JobTriggerType.Interval,
        TriggerType.Cron => JobTriggerType.Cron,
        _ => JobTriggerType.Manual
    };

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

    private async Task<SysTask> GetTaskOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        EnsureId(id, "系统任务主键必须大于 0。");
        return await _taskRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("系统任务不存在。");
    }

    #endregion
}
