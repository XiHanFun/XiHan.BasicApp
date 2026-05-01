#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TaskListItemDto
// Guid:4b82d47d-95f8-4456-8d0a-68d4956f3906
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 系统任务列表项 DTO
/// </summary>
public class TaskListItemDto : BasicAppDto
{
    /// <summary>
    /// 任务编码
    /// </summary>
    public string TaskCode { get; set; } = string.Empty;

    /// <summary>
    /// 任务名称
    /// </summary>
    public string TaskName { get; set; } = string.Empty;

    /// <summary>
    /// 任务描述
    /// </summary>
    public string? TaskDescription { get; set; }

    /// <summary>
    /// 任务分组
    /// </summary>
    public string? TaskGroup { get; set; }

    /// <summary>
    /// 触发类型
    /// </summary>
    public TriggerType TriggerType { get; set; }

    /// <summary>
    /// Cron 表达式
    /// </summary>
    public string? CronExpression { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTimeOffset? StartTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTimeOffset? EndTime { get; set; }

    /// <summary>
    /// 下次执行时间
    /// </summary>
    public DateTimeOffset? NextRunTime { get; set; }

    /// <summary>
    /// 上次执行时间
    /// </summary>
    public DateTimeOffset? LastRunTime { get; set; }

    /// <summary>
    /// 执行间隔（秒）
    /// </summary>
    public int? IntervalSeconds { get; set; }

    /// <summary>
    /// 重复次数
    /// </summary>
    public int RepeatCount { get; set; }

    /// <summary>
    /// 已执行次数
    /// </summary>
    public int ExecutedCount { get; set; }

    /// <summary>
    /// 超时时间（秒）
    /// </summary>
    public int TimeoutSeconds { get; set; }

    /// <summary>
    /// 运行状态
    /// </summary>
    public RunTaskStatus RunTaskStatus { get; set; }

    /// <summary>
    /// 优先级
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// 是否允许并发执行
    /// </summary>
    public bool AllowConcurrent { get; set; }

    /// <summary>
    /// 失败重试次数
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// 最大重试次数
    /// </summary>
    public int MaxRetryCount { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public EnableStatus Status { get; set; }

    /// <summary>
    /// 是否包含执行目标
    /// </summary>
    public bool HasExecutionTarget { get; set; }

    /// <summary>
    /// 是否包含运行参数
    /// </summary>
    public bool HasRuntimeArgs { get; set; }

    /// <summary>
    /// 是否包含备注
    /// </summary>
    public bool HasNote { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }

    /// <summary>
    /// 修改时间
    /// </summary>
    public DateTimeOffset? ModifiedTime { get; set; }
}
