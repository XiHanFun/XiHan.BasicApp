#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TaskDto
// Guid:0aad8324-8f12-443f-a567-6c1c93fc4af0
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 12:30:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 任务 DTO
/// </summary>
public class TaskDto : BasicAppDto
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
    /// 任务类名
    /// </summary>
    public string TaskClass { get; set; } = string.Empty;

    /// <summary>
    /// 任务方法
    /// </summary>
    public string? TaskMethod { get; set; }

    /// <summary>
    /// 触发类型
    /// </summary>
    public TriggerType TriggerType { get; set; } = TriggerType.Immediate;

    /// <summary>
    /// Cron 表达式
    /// </summary>
    public string? CronExpression { get; set; }

    /// <summary>
    /// 运行状态
    /// </summary>
    public RunTaskStatus RunTaskStatus { get; set; } = RunTaskStatus.Pending;

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; } = YesOrNo.Yes;
}

/// <summary>
/// 创建任务 DTO
/// </summary>
public class TaskCreateDto : BasicAppCDto
{
    /// <summary>
    /// 任务编码
    /// </summary>
    [Required(ErrorMessage = "任务编码不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "任务编码长度必须在 1～100 之间")]
    public string TaskCode { get; set; } = string.Empty;

    /// <summary>
    /// 任务名称
    /// </summary>
    [Required(ErrorMessage = "任务名称不能为空")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "任务名称长度必须在 1～200 之间")]
    public string TaskName { get; set; } = string.Empty;

    /// <summary>
    /// 任务描述
    /// </summary>
    [StringLength(500, ErrorMessage = "任务描述长度不能超过 500")]
    public string? TaskDescription { get; set; }

    /// <summary>
    /// 任务分组
    /// </summary>
    [StringLength(50, ErrorMessage = "任务分组长度不能超过 50")]
    public string? TaskGroup { get; set; }

    /// <summary>
    /// 任务类名
    /// </summary>
    [Required(ErrorMessage = "任务类名不能为空")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "任务类名长度必须在 1～200 之间")]
    public string TaskClass { get; set; } = string.Empty;

    /// <summary>
    /// 任务方法
    /// </summary>
    [StringLength(100, ErrorMessage = "任务方法长度不能超过 100")]
    public string? TaskMethod { get; set; }

    /// <summary>
    /// 任务参数
    /// </summary>
    public string? TaskParams { get; set; }

    /// <summary>
    /// 触发类型
    /// </summary>
    public TriggerType TriggerType { get; set; } = TriggerType.Immediate;

    /// <summary>
    /// Cron 表达式
    /// </summary>
    [StringLength(100, ErrorMessage = "Cron 表达式长度不能超过 100")]
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
    /// 间隔秒数
    /// </summary>
    public int? IntervalSeconds { get; set; }

    /// <summary>
    /// 重复次数
    /// </summary>
    public int RepeatCount { get; set; } = -1;

    /// <summary>
    /// 超时秒数
    /// </summary>
    public int TimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// 优先级
    /// </summary>
    [Range(1, 5, ErrorMessage = "优先级范围为 1～5")]
    public int Priority { get; set; } = 3;

    /// <summary>
    /// 是否允许并发
    /// </summary>
    public bool AllowConcurrent { get; set; }

    /// <summary>
    /// 最大重试次数
    /// </summary>
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>
    /// 租户标识
    /// </summary>
    public long? TenantId { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [StringLength(500, ErrorMessage = "备注长度不能超过 500")]
    public string? Remark { get; set; }
}

/// <summary>
/// 更新任务 DTO
/// </summary>
public class TaskUpdateDto : BasicAppUDto
{
    /// <summary>
    /// 任务名称
    /// </summary>
    [Required(ErrorMessage = "任务名称不能为空")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "任务名称长度必须在 1～200 之间")]
    public string TaskName { get; set; } = string.Empty;

    /// <summary>
    /// 任务描述
    /// </summary>
    [StringLength(500, ErrorMessage = "任务描述长度不能超过 500")]
    public string? TaskDescription { get; set; }

    /// <summary>
    /// 任务分组
    /// </summary>
    [StringLength(50, ErrorMessage = "任务分组长度不能超过 50")]
    public string? TaskGroup { get; set; }

    /// <summary>
    /// 任务类名
    /// </summary>
    [Required(ErrorMessage = "任务类名不能为空")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "任务类名长度必须在 1～200 之间")]
    public string TaskClass { get; set; } = string.Empty;

    /// <summary>
    /// 任务方法
    /// </summary>
    [StringLength(100, ErrorMessage = "任务方法长度不能超过 100")]
    public string? TaskMethod { get; set; }

    /// <summary>
    /// 任务参数
    /// </summary>
    public string? TaskParams { get; set; }

    /// <summary>
    /// 触发类型
    /// </summary>
    public TriggerType TriggerType { get; set; } = TriggerType.Immediate;

    /// <summary>
    /// Cron 表达式
    /// </summary>
    [StringLength(100, ErrorMessage = "Cron 表达式长度不能超过 100")]
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
    /// 下次运行时间
    /// </summary>
    public DateTimeOffset? NextRunTime { get; set; }

    /// <summary>
    /// 间隔秒数
    /// </summary>
    public int? IntervalSeconds { get; set; }

    /// <summary>
    /// 重复次数
    /// </summary>
    public int RepeatCount { get; set; } = -1;

    /// <summary>
    /// 超时秒数
    /// </summary>
    public int TimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// 运行状态
    /// </summary>
    public RunTaskStatus RunTaskStatus { get; set; } = RunTaskStatus.Pending;

    /// <summary>
    /// 优先级
    /// </summary>
    [Range(1, 5, ErrorMessage = "优先级范围为 1～5")]
    public int Priority { get; set; } = 3;

    /// <summary>
    /// 是否允许并发
    /// </summary>
    public bool AllowConcurrent { get; set; }

    /// <summary>
    /// 最大重试次数
    /// </summary>
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 备注
    /// </summary>
    [StringLength(500, ErrorMessage = "备注长度不能超过 500")]
    public string? Remark { get; set; }
}
