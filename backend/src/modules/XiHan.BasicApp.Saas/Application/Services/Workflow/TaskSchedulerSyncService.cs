#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TaskSchedulerSyncService
// Guid:2f411172-c7d2-410c-97c2-e2d9339b07c0
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.BasicApp.Saas.Infrastructure.Tasks;
using XiHan.Framework.Tasks.ScheduledJobs.Abstractions;
using XiHan.Framework.Tasks.ScheduledJobs.Models;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 任务调度器同步服务实现
/// </summary>
public sealed class TaskSchedulerSyncService
    : ITaskSchedulerSyncService
{
    private readonly IJobScheduler _jobScheduler;

    private readonly ITaskSchedulerQueryService _taskSchedulerQueryService;

    private readonly ITaskRepository _taskRepository;

    private readonly ILogger<TaskSchedulerSyncService> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public TaskSchedulerSyncService(
        ITaskSchedulerQueryService taskSchedulerQueryService,
        ITaskRepository taskRepository,
        IJobScheduler jobScheduler,
        ILogger<TaskSchedulerSyncService> logger)
    {
        _taskSchedulerQueryService = taskSchedulerQueryService;
        _taskRepository = taskRepository;
        _jobScheduler = jobScheduler;
        _logger = logger;
    }

    /// <inheritdoc />
    public void Apply(SysTask task, TaskSchedulerSyncAction syncAction)
    {
        ArgumentNullException.ThrowIfNull(task);

        switch (syncAction)
        {
            case TaskSchedulerSyncAction.None:
                return;

            case TaskSchedulerSyncAction.Register:
                _jobScheduler.RegisterJob(BuildJobInfo(task));
                TriggerImmediateIfNeeded(task);
                return;

            case TaskSchedulerSyncAction.Unregister:
                _jobScheduler.UnregisterJob(task.TaskCode);
                return;

            case TaskSchedulerSyncAction.Replace:
                _jobScheduler.UnregisterJob(task.TaskCode);
                if (task.Status == EnableStatus.Enabled)
                {
                    _jobScheduler.RegisterJob(BuildJobInfo(task));
                    TriggerImmediateIfNeeded(task);
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

    /// <inheritdoc />
    public async Task SyncAllActiveJobsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 崩溃残留复位：启动时不存在任何运行中实例，库里遗留的 Running 一律视为上次进程
        // 异常退出的脏状态，复位为 Failed，避免非并发任务被"运行中"闸门永久跳过
        var staleRunning = await _taskRepository.GetListAsync(
            task => task.RunTaskStatus == RunTaskStatus.Running,
            cancellationToken);
        foreach (var stale in staleRunning)
        {
            stale.RunTaskStatus = RunTaskStatus.Failed;
            _ = await _taskRepository.UpdateAsync(stale, cancellationToken);
            _logger.LogWarning("启动复位脏运行状态：任务 {TaskCode} 上次执行未正常收尾，已标记为失败", stale.TaskCode);
        }

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
            // 定时执行(Schedule→Delay)：按 StartTime 推延迟；缺省/已过期给 1 秒兜底立即跑一次。
            // 不设置 Delay 时框架算不出下次触发时间，该类任务会注册了也永不执行。
            Delay = task.TriggerType == TriggerType.Schedule
                ? (task.StartTime.HasValue && task.StartTime.Value > DateTimeOffset.UtcNow
                    ? task.StartTime.Value - DateTimeOffset.UtcNow
                    : TimeSpan.FromSeconds(1))
                : null,
            EndTime = task.EndTime,
            // SysTask.RepeatCount 语义与框架一致：-1 不限，达到次数后不再触发
            RepeatCount = task.RepeatCount,
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

    private static JobPriority MapPriority(int priority) => priority switch
    {
        <= 0 => JobPriority.Normal,
        1 => JobPriority.High,
        _ => JobPriority.Critical
    };

    private static JobTriggerType MapTriggerType(TriggerType triggerType) => triggerType switch
    {
        TriggerType.Immediate => JobTriggerType.Manual,
        TriggerType.Schedule => JobTriggerType.Delay,
        TriggerType.Recurring => JobTriggerType.Interval,
        TriggerType.Cron => JobTriggerType.Cron,
        _ => JobTriggerType.Manual
    };

    /// <summary>
    /// 立即执行型任务注册即触发一次（Immediate 映射为框架 Manual，无自动触发时机）
    /// </summary>
    private void TriggerImmediateIfNeeded(SysTask task)
    {
        if (task.TriggerType != TriggerType.Immediate || task.Status != EnableStatus.Enabled)
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                _ = await _jobScheduler.TriggerJobAsync(task.TaskCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "立即执行型任务注册触发失败：{TaskCode}", task.TaskCode);
            }
        });
    }
}
