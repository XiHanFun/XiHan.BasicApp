// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 任务调度同步动作
/// </summary>
public enum TaskSchedulerSyncAction
{
    /// <summary>
    /// 不同步
    /// </summary>
    None = 0,

    /// <summary>
    /// 注册任务
    /// </summary>
    Register = 1,

    /// <summary>
    /// 取消注册任务
    /// </summary>
    Unregister = 2,

    /// <summary>
    /// 重新注册任务
    /// </summary>
    Replace = 3,

    /// <summary>
    /// 暂停任务
    /// </summary>
    Pause = 4,

    /// <summary>
    /// 恢复任务
    /// </summary>
    Resume = 5
}

/// <summary>
/// 任务创建命令
/// </summary>
public sealed record TaskCreateCommand(
    string TaskCode,
    string TaskName,
    string? TaskDescription,
    string? TaskGroup,
    string TaskClass,
    string? TaskMethod,
    string? TaskParams,
    TriggerType TriggerType,
    string? CronExpression,
    DateTimeOffset? StartTime,
    DateTimeOffset? EndTime,
    DateTimeOffset? NextRunTime,
    DateTimeOffset? LastRunTime,
    int? IntervalSeconds,
    int RepeatCount,
    int ExecutedCount,
    int TimeoutSeconds,
    RunTaskStatus RunTaskStatus,
    int Priority,
    bool AllowConcurrent,
    int RetryCount,
    int MaxRetryCount,
    EnableStatus Status,
    string? Remark);

/// <summary>
/// 任务更新命令
/// </summary>
public sealed record TaskUpdateCommand(
    long BasicId,
    string TaskName,
    string? TaskDescription,
    string? TaskGroup,
    string TaskClass,
    string? TaskMethod,
    string? TaskParams,
    TriggerType TriggerType,
    string? CronExpression,
    DateTimeOffset? StartTime,
    DateTimeOffset? EndTime,
    DateTimeOffset? NextRunTime,
    DateTimeOffset? LastRunTime,
    int? IntervalSeconds,
    int RepeatCount,
    int ExecutedCount,
    int TimeoutSeconds,
    int Priority,
    bool AllowConcurrent,
    int RetryCount,
    int MaxRetryCount,
    string? Remark);

/// <summary>
/// 任务启停状态变更命令
/// </summary>
public sealed record TaskStatusChangeCommand(long BasicId, EnableStatus Status, string? Remark);

/// <summary>
/// 任务运行状态变更命令
/// </summary>
public sealed record TaskRunStatusChangeCommand(
    long BasicId,
    RunTaskStatus RunTaskStatus,
    DateTimeOffset? NextRunTime,
    DateTimeOffset? LastRunTime,
    int? ExecutedCount,
    int? RetryCount,
    string? Remark);

/// <summary>
/// 任务命令结果
/// </summary>
public sealed record TaskCommandResult(SysTask Task, TaskSchedulerSyncAction SchedulerSyncAction);
