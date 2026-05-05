#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:WorkflowCommandDtos
// Guid:fa305f29-7e63-49d8-89d0-c411106519c4
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/06 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

#pragma warning disable CS1591

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

public sealed class TaskCreateDto
{
    public string TaskCode { get; set; } = string.Empty;
    public string TaskName { get; set; } = string.Empty;
    public string? TaskDescription { get; set; }
    public string? TaskGroup { get; set; }
    public string TaskClass { get; set; } = string.Empty;
    public string? TaskMethod { get; set; }
    public string? TaskParams { get; set; }
    public TriggerType TriggerType { get; set; } = TriggerType.Immediate;
    public string? CronExpression { get; set; }
    public DateTimeOffset? StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
    public DateTimeOffset? NextRunTime { get; set; }
    public DateTimeOffset? LastRunTime { get; set; }
    public int? IntervalSeconds { get; set; }
    public int RepeatCount { get; set; } = -1;
    public int ExecutedCount { get; set; }
    public int TimeoutSeconds { get; set; } = 300;
    public RunTaskStatus RunTaskStatus { get; set; } = RunTaskStatus.Pending;
    public int Priority { get; set; }
    public bool AllowConcurrent { get; set; }
    public int RetryCount { get; set; }
    public int MaxRetryCount { get; set; } = 3;
    public EnableStatus Status { get; set; } = EnableStatus.Enabled;
    public string? Remark { get; set; }
}

public sealed class TaskUpdateDto : BasicAppUDto
{
    public string TaskName { get; set; } = string.Empty;
    public string? TaskDescription { get; set; }
    public string? TaskGroup { get; set; }
    public string TaskClass { get; set; } = string.Empty;
    public string? TaskMethod { get; set; }
    public string? TaskParams { get; set; }
    public TriggerType TriggerType { get; set; } = TriggerType.Immediate;
    public string? CronExpression { get; set; }
    public DateTimeOffset? StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
    public DateTimeOffset? NextRunTime { get; set; }
    public DateTimeOffset? LastRunTime { get; set; }
    public int? IntervalSeconds { get; set; }
    public int RepeatCount { get; set; } = -1;
    public int ExecutedCount { get; set; }
    public int TimeoutSeconds { get; set; } = 300;
    public int Priority { get; set; }
    public bool AllowConcurrent { get; set; }
    public int RetryCount { get; set; }
    public int MaxRetryCount { get; set; } = 3;
    public string? Remark { get; set; }
}

public sealed class TaskStatusUpdateDto : BasicAppDto
{
    public EnableStatus Status { get; set; } = EnableStatus.Enabled;
    public string? Remark { get; set; }
}

public sealed class TaskRunStatusUpdateDto : BasicAppDto
{
    public RunTaskStatus RunTaskStatus { get; set; } = RunTaskStatus.Pending;
    public DateTimeOffset? NextRunTime { get; set; }
    public DateTimeOffset? LastRunTime { get; set; }
    public int? ExecutedCount { get; set; }
    public int? RetryCount { get; set; }
    public string? Remark { get; set; }
}

public sealed class ReviewCreateDto
{
    public string ReviewCode { get; set; } = string.Empty;
    public string ReviewTitle { get; set; } = string.Empty;
    public string ReviewType { get; set; } = string.Empty;
    public string? ReviewContent { get; set; }
    public string? ReviewDescription { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? BusinessData { get; set; }
    public AuditStatus ReviewStatus { get; set; } = AuditStatus.Pending;
    public AuditResult? ReviewResult { get; set; }
    public int Priority { get; set; }
    public long? SubmitUserId { get; set; }
    public DateTimeOffset? SubmitTime { get; set; }
    public long? CurrentReviewUserId { get; set; }
    public string? ReviewUserIds { get; set; }
    public int ReviewLevel { get; set; } = 1;
    public int CurrentLevel { get; set; } = 1;
    public DateTimeOffset? ReviewStartTime { get; set; }
    public DateTimeOffset? ReviewEndTime { get; set; }
    public string? Attachments { get; set; }
    public string? ExtendData { get; set; }
    public EnableStatus Status { get; set; } = EnableStatus.Enabled;
    public string? Remark { get; set; }
}

public sealed class ReviewUpdateDto : BasicAppUDto
{
    public string ReviewTitle { get; set; } = string.Empty;
    public string ReviewType { get; set; } = string.Empty;
    public string? ReviewContent { get; set; }
    public string? ReviewDescription { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? BusinessData { get; set; }
    public int Priority { get; set; }
    public long? SubmitUserId { get; set; }
    public DateTimeOffset? SubmitTime { get; set; }
    public long? CurrentReviewUserId { get; set; }
    public string? ReviewUserIds { get; set; }
    public int ReviewLevel { get; set; } = 1;
    public int CurrentLevel { get; set; } = 1;
    public DateTimeOffset? ReviewStartTime { get; set; }
    public DateTimeOffset? ReviewEndTime { get; set; }
    public string? Attachments { get; set; }
    public string? ExtendData { get; set; }
    public string? Remark { get; set; }
}

public sealed class ReviewStatusUpdateDto : BasicAppDto
{
    public EnableStatus Status { get; set; } = EnableStatus.Enabled;
    public string? Remark { get; set; }
}

public sealed class ReviewAuditDto : BasicAppDto
{
    public AuditResult ReviewResult { get; set; } = AuditResult.Pass;
    public long? ReviewUserId { get; set; }
    public long? NextReviewUserId { get; set; }
    public string? ReviewComment { get; set; }
    public string? ReviewAction { get; set; }
    public DateTimeOffset? ReviewTime { get; set; }
    public string? Attachments { get; set; }
    public string? ExtendData { get; set; }
    public string? Remark { get; set; }
}

public sealed class ReviewWithdrawDto : BasicAppDto
{
    public string? Reason { get; set; }
    public DateTimeOffset? WithdrawTime { get; set; }
}
