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
using XiHan.BasicApp.Rbac.Domain.Enums;

namespace XiHan.BasicApp.Rbac.Application.Dtos;

/// <summary>
/// 任务 DTO
/// </summary>
public class TaskDto : BasicAppDto
{
    public string TaskCode { get; set; } = string.Empty;

    public string TaskName { get; set; } = string.Empty;

    public string TaskClass { get; set; } = string.Empty;

    public string? TaskMethod { get; set; }

    public TriggerType TriggerType { get; set; } = TriggerType.Immediate;

    public string? CronExpression { get; set; }

    public RunTaskStatus RunTaskStatus { get; set; } = RunTaskStatus.Pending;

    public YesOrNo Status { get; set; } = YesOrNo.Yes;
}

/// <summary>
/// 创建任务 DTO
/// </summary>
public class TaskCreateDto : BasicAppCDto
{
    [Required(ErrorMessage = "任务编码不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "任务编码长度必须在 1～100 之间")]
    public string TaskCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "任务名称不能为空")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "任务名称长度必须在 1～200 之间")]
    public string TaskName { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "任务描述长度不能超过 500")]
    public string? TaskDescription { get; set; }

    [StringLength(50, ErrorMessage = "任务分组长度不能超过 50")]
    public string? TaskGroup { get; set; }

    [Required(ErrorMessage = "任务类名不能为空")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "任务类名长度必须在 1～200 之间")]
    public string TaskClass { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "任务方法长度不能超过 100")]
    public string? TaskMethod { get; set; }

    public string? TaskParams { get; set; }

    public TriggerType TriggerType { get; set; } = TriggerType.Immediate;

    [StringLength(100, ErrorMessage = "Cron 表达式长度不能超过 100")]
    public string? CronExpression { get; set; }

    public DateTimeOffset? StartTime { get; set; }

    public DateTimeOffset? EndTime { get; set; }

    public int? IntervalSeconds { get; set; }

    public int RepeatCount { get; set; } = -1;

    public int TimeoutSeconds { get; set; } = 300;

    [Range(1, 5, ErrorMessage = "优先级范围为 1～5")]
    public int Priority { get; set; } = 3;

    public bool AllowConcurrent { get; set; }

    public int MaxRetryCount { get; set; } = 3;

    public long? TenantId { get; set; }

    [StringLength(500, ErrorMessage = "备注长度不能超过 500")]
    public string? Remark { get; set; }
}

/// <summary>
/// 更新任务 DTO
/// </summary>
public class TaskUpdateDto : BasicAppUDto
{
    [Required(ErrorMessage = "任务名称不能为空")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "任务名称长度必须在 1～200 之间")]
    public string TaskName { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "任务描述长度不能超过 500")]
    public string? TaskDescription { get; set; }

    [StringLength(50, ErrorMessage = "任务分组长度不能超过 50")]
    public string? TaskGroup { get; set; }

    [Required(ErrorMessage = "任务类名不能为空")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "任务类名长度必须在 1～200 之间")]
    public string TaskClass { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "任务方法长度不能超过 100")]
    public string? TaskMethod { get; set; }

    public string? TaskParams { get; set; }

    public TriggerType TriggerType { get; set; } = TriggerType.Immediate;

    [StringLength(100, ErrorMessage = "Cron 表达式长度不能超过 100")]
    public string? CronExpression { get; set; }

    public DateTimeOffset? StartTime { get; set; }

    public DateTimeOffset? EndTime { get; set; }

    public DateTimeOffset? NextRunTime { get; set; }

    public int? IntervalSeconds { get; set; }

    public int RepeatCount { get; set; } = -1;

    public int TimeoutSeconds { get; set; } = 300;

    public RunTaskStatus RunTaskStatus { get; set; } = RunTaskStatus.Pending;

    [Range(1, 5, ErrorMessage = "优先级范围为 1～5")]
    public int Priority { get; set; } = 3;

    public bool AllowConcurrent { get; set; }

    public int MaxRetryCount { get; set; } = 3;

    public YesOrNo Status { get; set; } = YesOrNo.Yes;

    [StringLength(500, ErrorMessage = "备注长度不能超过 500")]
    public string? Remark { get; set; }
}
