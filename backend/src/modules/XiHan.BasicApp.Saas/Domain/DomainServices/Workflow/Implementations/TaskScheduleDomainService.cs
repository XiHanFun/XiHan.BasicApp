#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TaskScheduleDomainService
// Guid:6f1666a4-49b8-4939-9f04-c7bf9cb8bd7a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/06 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Tasks.ScheduledJobs.Crons;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 任务调度领域服务实现
/// </summary>
public sealed class TaskScheduleDomainService : ITaskScheduleDomainService
{
    /// <inheritdoc />
    public void EnsureScheduleConfiguration(
        TriggerType triggerType,
        string? cronExpression,
        DateTimeOffset? startTime,
        DateTimeOffset? endTime,
        int? intervalSeconds,
        int repeatCount,
        int executedCount,
        int timeoutSeconds,
        int retryCount,
        int maxRetryCount)
    {
        if (!Enum.IsDefined(triggerType))
        {
            throw new ArgumentOutOfRangeException(nameof(triggerType), "触发类型无效。");
        }

        if (startTime.HasValue && endTime.HasValue && endTime.Value <= startTime.Value)
        {
            throw new InvalidOperationException("结束时间必须晚于开始时间。");
        }

        if (triggerType == TriggerType.Cron)
        {
            if (string.IsNullOrWhiteSpace(cronExpression))
            {
                throw new InvalidOperationException("Cron 触发任务必须填写 Cron 表达式。");
            }

            _ = CronHelper.ParseExpression(cronExpression);
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

        if (executedCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(executedCount), "已执行次数不能小于 0。");
        }

        if (timeoutSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(timeoutSeconds), "超时时间必须大于 0。");
        }

        if (retryCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(retryCount), "失败重试次数不能小于 0。");
        }

        if (maxRetryCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxRetryCount), "最大重试次数不能小于 0。");
        }

        if (retryCount > maxRetryCount)
        {
            throw new InvalidOperationException("失败重试次数不能大于最大重试次数。");
        }
    }

    /// <inheritdoc />
    public void EnsureCanModify(SysTask task)
    {
        ArgumentNullException.ThrowIfNull(task);

        if (task.RunTaskStatus == RunTaskStatus.Running)
        {
            throw new InvalidOperationException("运行中的任务不能修改任务配置。");
        }
    }

    /// <inheritdoc />
    public void EnsureCanDelete(SysTask task)
    {
        ArgumentNullException.ThrowIfNull(task);

        if (task.RunTaskStatus == RunTaskStatus.Running)
        {
            throw new InvalidOperationException("运行中的任务不能删除。");
        }
    }

    /// <inheritdoc />
    public void ApplyStatus(SysTask task, EnableStatus status, string? remark)
    {
        ArgumentNullException.ThrowIfNull(task);

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), "任务启停状态无效。");
        }

        task.Status = status;
        if (status == EnableStatus.Disabled && task.RunTaskStatus == RunTaskStatus.Running)
        {
            task.RunTaskStatus = RunTaskStatus.Stopped;
        }

        task.Remark = NormalizeText(remark, 500) ?? task.Remark;
    }

    /// <inheritdoc />
    public void ApplyRunStatus(
        SysTask task,
        RunTaskStatus runTaskStatus,
        DateTimeOffset? nextRunTime,
        DateTimeOffset? lastRunTime,
        int? executedCount,
        int? retryCount,
        string? remark)
    {
        ArgumentNullException.ThrowIfNull(task);

        if (!Enum.IsDefined(runTaskStatus))
        {
            throw new ArgumentOutOfRangeException(nameof(runTaskStatus), "任务运行状态无效。");
        }

        if (task.Status == EnableStatus.Disabled && runTaskStatus == RunTaskStatus.Running)
        {
            throw new InvalidOperationException("停用任务不能设置为执行中。");
        }

        if (executedCount is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(executedCount), "已执行次数不能小于 0。");
        }

        if (retryCount is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(retryCount), "失败重试次数不能小于 0。");
        }

        task.RunTaskStatus = runTaskStatus;
        task.NextRunTime = nextRunTime ?? task.NextRunTime;
        task.LastRunTime = lastRunTime ?? task.LastRunTime;
        task.ExecutedCount = executedCount ?? task.ExecutedCount;
        task.RetryCount = retryCount ?? task.RetryCount;
        task.Remark = NormalizeText(remark, 500) ?? task.Remark;
    }

    /// <inheritdoc />
    public DateTimeOffset? ResolveNextRunTime(SysTask task, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(task);

        if (task.Status == EnableStatus.Disabled || task.EndTime.HasValue && task.EndTime.Value <= now)
        {
            return null;
        }

        var start = task.StartTime.HasValue && task.StartTime.Value > now ? task.StartTime.Value : now;
        return task.TriggerType switch
        {
            TriggerType.Immediate => start,
            TriggerType.Schedule => task.NextRunTime ?? task.StartTime,
            TriggerType.Cron when !string.IsNullOrWhiteSpace(task.CronExpression) => ResolveCronNext(task.CronExpression, start),
            TriggerType.Recurring when task.IntervalSeconds.HasValue => start.AddSeconds(task.IntervalSeconds.Value),
            _ => task.NextRunTime
        };
    }

    private static DateTimeOffset? ResolveCronNext(string cronExpression, DateTimeOffset start)
    {
        var next = CronHelper.GetNextOccurrence(cronExpression, start.LocalDateTime);
        return next.HasValue ? new DateTimeOffset(next.Value, start.Offset) : null;
    }

    private static string? NormalizeText(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
