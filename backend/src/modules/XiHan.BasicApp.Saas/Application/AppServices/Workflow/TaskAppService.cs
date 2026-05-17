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
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Infrastructure.Tasks.Jobs;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Tasks.ScheduledJobs.Abstractions;
using XiHan.Framework.Tasks.ScheduledJobs.Models;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 系统任务命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "系统任务")]
public sealed class TaskAppService : SaasApplicationService, ITaskAppService
{
    private readonly IJobScheduler _jobScheduler;
    private readonly ITaskDomainService _taskDomainService;
    private readonly ITaskSchedulerQueryService _taskSchedulerQueryService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public TaskAppService(
        ITaskDomainService taskDomainService,
        ITaskSchedulerQueryService taskSchedulerQueryService,
        IJobScheduler jobScheduler)
    {
        _taskDomainService = taskDomainService;
        _taskSchedulerQueryService = taskSchedulerQueryService;
        _jobScheduler = jobScheduler;
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

        var result = await _taskDomainService.CreateTaskAsync(ToCreateCommand(input), cancellationToken);
        ApplySchedulerSync(result.Task, result.SchedulerSyncAction);
        return TaskApplicationMapper.ToDetailDto(result.Task);
    }

    /// <summary>
    /// 删除系统任务
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Task.Delete)]
    public async Task DeleteTaskAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _taskDomainService.DeleteTaskAsync(id, cancellationToken);
        ApplySchedulerSync(result.Task, result.SchedulerSyncAction);
    }

    /// <summary>
    /// 同步所有活跃的 SysTask 到框架调度器（用于应用启动时初始化）
    /// </summary>
    public async Task SyncAllActiveJobsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tasks = await _taskSchedulerQueryService.GetEnabledTasksAsync(cancellationToken);
        if (tasks.Count == 0)
        {
            return;
        }

        var registeredJobNames = _jobScheduler
            .GetAllJobs()
            .Select(job => job.JobName)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var task in tasks)
        {
            if (registeredJobNames.Contains(task.TaskCode))
            {
                continue;
            }

            _jobScheduler.RegisterJob(BuildJobInfo(task));
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

        var result = await _taskDomainService.UpdateTaskAsync(ToUpdateCommand(input), cancellationToken);
        ApplySchedulerSync(result.Task, result.SchedulerSyncAction);
        return TaskApplicationMapper.ToDetailDto(result.Task);
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

        var result = await _taskDomainService.UpdateTaskRunStatusAsync(ToRunStatusCommand(input), cancellationToken);
        ApplySchedulerSync(result.Task, result.SchedulerSyncAction);
        return TaskApplicationMapper.ToDetailDto(result.Task);
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

        var result = await _taskDomainService.UpdateTaskStatusAsync(ToStatusCommand(input), cancellationToken);
        ApplySchedulerSync(result.Task, result.SchedulerSyncAction);
        return TaskApplicationMapper.ToDetailDto(result.Task);
    }

    private static TaskCreateCommand ToCreateCommand(TaskCreateDto input)
    {
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

    private static TaskRunStatusChangeCommand ToRunStatusCommand(TaskRunStatusUpdateDto input)
    {
        return new TaskRunStatusChangeCommand(
            input.BasicId,
            input.RunTaskStatus,
            input.NextRunTime,
            input.LastRunTime,
            input.ExecutedCount,
            input.RetryCount,
            input.Remark);
    }

    private static TaskStatusChangeCommand ToStatusCommand(TaskStatusUpdateDto input)
    {
        return new TaskStatusChangeCommand(input.BasicId, input.Status, input.Remark);
    }

    private static TaskUpdateCommand ToUpdateCommand(TaskUpdateDto input)
    {
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

    private void ApplySchedulerSync(SysTask task, TaskSchedulerSyncAction syncAction)
    {
        switch (syncAction)
        {
            case TaskSchedulerSyncAction.None:
                return;
            case TaskSchedulerSyncAction.Register:
                _jobScheduler.RegisterJob(BuildJobInfo(task));
                return;
            case TaskSchedulerSyncAction.Unregister:
                _jobScheduler.UnregisterJob(task.TaskCode);
                return;
            case TaskSchedulerSyncAction.Replace:
                _jobScheduler.UnregisterJob(task.TaskCode);
                if (task.Status == EnableStatus.Enabled)
                {
                    _jobScheduler.RegisterJob(BuildJobInfo(task));
                }

                return;
            case TaskSchedulerSyncAction.Pause:
                _jobScheduler.PauseJob(task.TaskCode);
                return;
            case TaskSchedulerSyncAction.Resume:
                _jobScheduler.ResumeJob(task.TaskCode);
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(syncAction), "任务调度同步动作无效。");
        }
    }

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

    /// <summary>
    /// 将领域优先级映射到框架 JobPriority
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
}
