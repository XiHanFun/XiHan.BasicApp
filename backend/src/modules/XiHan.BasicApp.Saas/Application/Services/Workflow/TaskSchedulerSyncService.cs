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

using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Infrastructure.Tasks.Jobs;
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

    /// <summary>
    /// 构造函数
    /// </summary>
    public TaskSchedulerSyncService(
        ITaskSchedulerQueryService taskSchedulerQueryService,
        IJobScheduler jobScheduler)
    {
        _taskSchedulerQueryService = taskSchedulerQueryService;
        _jobScheduler = jobScheduler;
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

    /// <inheritdoc />
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
}
