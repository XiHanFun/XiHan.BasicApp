#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysTaskDto
// Guid:f2a3b4c5-d6e7-8901-2345-67890f123456
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/8 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Dtos.Base;
using XiHan.BasicApp.Rbac.Enums;
using TaskStatus = XiHan.BasicApp.Rbac.Enums.TaskStatus;

namespace XiHan.BasicApp.Rbac.Services.Dtos;

/// <summary>
/// 系统任务创建 DTO
/// </summary>
public class SysTaskCreateDto : RbacCreationDtoBase
{
    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }

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
    /// 任务类名
    /// </summary>
    public string TaskClass { get; set; } = string.Empty;

    /// <summary>
    /// 任务方法
    /// </summary>
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
    /// Cron表达式
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
    /// 执行间隔（秒）
    /// </summary>
    public int? IntervalSeconds { get; set; }

    /// <summary>
    /// 重复次数
    /// </summary>
    public int RepeatCount { get; set; } = -1;

    /// <summary>
    /// 超时时间（秒）
    /// </summary>
    public int TimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// 任务状态
    /// </summary>
    public TaskStatus TaskStatus { get; set; } = TaskStatus.Pending;

    /// <summary>
    /// 优先级
    /// </summary>
    public int Priority { get; set; } = 3;

    /// <summary>
    /// 是否允许并发执行
    /// </summary>
    public bool AllowConcurrent { get; set; } = false;

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
    public string? Remark { get; set; }
}

/// <summary>
/// 系统任务更新 DTO
/// </summary>
public class SysTaskUpdateDto : RbacUpdateDtoBase
{
    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }

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
    /// 任务类名
    /// </summary>
    public string TaskClass { get; set; } = string.Empty;

    /// <summary>
    /// 任务方法
    /// </summary>
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
    /// Cron表达式
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
    /// 执行间隔（秒）
    /// </summary>
    public int? IntervalSeconds { get; set; }

    /// <summary>
    /// 重复次数
    /// </summary>
    public int RepeatCount { get; set; } = -1;

    /// <summary>
    /// 超时时间（秒）
    /// </summary>
    public int TimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// 任务状态
    /// </summary>
    public TaskStatus TaskStatus { get; set; } = TaskStatus.Pending;

    /// <summary>
    /// 优先级
    /// </summary>
    public int Priority { get; set; } = 3;

    /// <summary>
    /// 是否允许并发执行
    /// </summary>
    public bool AllowConcurrent { get; set; } = false;

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
    public string? Remark { get; set; }
}

/// <summary>
/// 系统任务查询 DTO
/// </summary>
public class SysTaskGetDto : RbacFullAuditedDtoBase
{
    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }

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
    /// 任务类名
    /// </summary>
    public string TaskClass { get; set; } = string.Empty;

    /// <summary>
    /// 任务方法
    /// </summary>
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
    /// Cron表达式
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
    public int RepeatCount { get; set; } = -1;

    /// <summary>
    /// 已执行次数
    /// </summary>
    public int ExecutedCount { get; set; } = 0;

    /// <summary>
    /// 超时时间（秒）
    /// </summary>
    public int TimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// 任务状态
    /// </summary>
    public TaskStatus TaskStatus { get; set; } = TaskStatus.Pending;

    /// <summary>
    /// 优先级
    /// </summary>
    public int Priority { get; set; } = 3;

    /// <summary>
    /// 是否允许并发执行
    /// </summary>
    public bool AllowConcurrent { get; set; } = false;

    /// <summary>
    /// 失败重试次数
    /// </summary>
    public int RetryCount { get; set; } = 0;

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
    public string? Remark { get; set; }
}
