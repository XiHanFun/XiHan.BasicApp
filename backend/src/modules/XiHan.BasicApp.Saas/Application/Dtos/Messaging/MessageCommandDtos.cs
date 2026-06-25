#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MessageCommandDtos
// Guid:e481adf5-2f67-4e70-bd51-08ec42274bcb
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/06 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

#pragma warning disable CS1591

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

public sealed class NotificationCreateDto
{
    public long? SendUserId { get; set; }
    public NotificationType NotificationType { get; set; } = NotificationType.System;
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }

    /// <summary>
    /// 模板编码（站内通知渠道；提供时按模板渲染覆盖 Title/Content，缺失回退原值）
    /// </summary>
    public string? TemplateCode { get; set; }

    /// <summary>
    /// 模板变量
    /// </summary>
    public Dictionary<string, string>? TemplateParams { get; set; }

    public string? Icon { get; set; }
    public string? Link { get; set; }
    public string? BusinessType { get; set; }
    public long? BusinessId { get; set; }
    public DateTimeOffset? SendTime { get; set; }
    public DateTimeOffset? ExpirationTime { get; set; }
    public NotificationTargetType TargetType { get; set; } = NotificationTargetType.User;
    public bool NeedConfirm { get; set; }
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public NotificationContentFormat ContentFormat { get; set; } = NotificationContentFormat.Markdown;
    public DateTimeOffset? StartTime { get; set; }
    public bool IsMandatory { get; set; }
    public bool IsBanner { get; set; }
    public bool IsPopup { get; set; }
    public IReadOnlyList<long> UserIds { get; set; } = [];
    public bool PublishImmediately { get; set; }
    public string? Remark { get; set; }
}

public sealed class NotificationUpdateDto : BasicAppUDto
{
    public NotificationType NotificationType { get; set; } = NotificationType.System;
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string? Icon { get; set; }
    public string? Link { get; set; }
    public string? BusinessType { get; set; }
    public long? BusinessId { get; set; }
    public DateTimeOffset? SendTime { get; set; }
    public DateTimeOffset? ExpirationTime { get; set; }
    public NotificationTargetType TargetType { get; set; } = NotificationTargetType.User;
    public bool NeedConfirm { get; set; }
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public NotificationContentFormat ContentFormat { get; set; } = NotificationContentFormat.Markdown;
    public DateTimeOffset? StartTime { get; set; }
    public bool IsMandatory { get; set; }
    public bool IsBanner { get; set; }
    public bool IsPopup { get; set; }
    public IReadOnlyList<long> UserIds { get; set; } = [];
    public string? Remark { get; set; }
}

public sealed class NotificationPublishDto : BasicAppDto
{
    public NotificationTargetType? TargetType { get; set; }
    public IReadOnlyList<long> UserIds { get; set; } = [];
}

public sealed class NotificationPublishResultDto
{
    public long BasicId { get; set; }
    public int RecipientCount { get; set; }
    public DateTimeOffset SendTime { get; set; }
}

public sealed class EmailCreateDto
{
    public long? SendUserId { get; set; }
    public long? ReceiveUserId { get; set; }
    public EmailType EmailType { get; set; } = EmailType.System;
    public string FromEmail { get; set; } = string.Empty;
    public string? FromName { get; set; }
    public string ToEmail { get; set; } = string.Empty;
    public string? CcEmail { get; set; }
    public string? BccEmail { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string? Content { get; set; }
    public bool IsHtml { get; set; } = true;
    public string? Attachments { get; set; }
    public string? TemplateCode { get; set; }
    public string? TemplateParams { get; set; }
    public DateTimeOffset? ScheduledTime { get; set; }
    public int MaxRetryCount { get; set; } = 3;
    public string? BusinessType { get; set; }
    public long? BusinessId { get; set; }
    public string? Remark { get; set; }
}

public sealed class EmailUpdateDto : BasicAppUDto
{
    public long? ReceiveUserId { get; set; }
    public EmailType EmailType { get; set; } = EmailType.System;
    public string FromEmail { get; set; } = string.Empty;
    public string? FromName { get; set; }
    public string ToEmail { get; set; } = string.Empty;
    public string? CcEmail { get; set; }
    public string? BccEmail { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string? Content { get; set; }
    public bool IsHtml { get; set; } = true;
    public string? Attachments { get; set; }
    public string? TemplateCode { get; set; }
    public string? TemplateParams { get; set; }
    public DateTimeOffset? ScheduledTime { get; set; }
    public int MaxRetryCount { get; set; } = 3;
    public string? BusinessType { get; set; }
    public long? BusinessId { get; set; }
    public string? Remark { get; set; }
}

public sealed class EmailStatusUpdateDto : BasicAppDto
{
    public EmailStatus EmailStatus { get; set; } = EmailStatus.Pending;
    public DateTimeOffset? SendTime { get; set; }
    public int? RetryCount { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Remark { get; set; }
}

public sealed class SmsCreateDto
{
    public long? SenderId { get; set; }
    public long? ReceiverId { get; set; }
    public SmsType SmsType { get; set; } = SmsType.Notification;
    public string ToPhone { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? TemplateCode { get; set; }
    public string? TemplateParams { get; set; }
    public string? Provider { get; set; }
    public DateTimeOffset? ScheduledTime { get; set; }
    public int MaxRetryCount { get; set; } = 3;
    public string? BusinessType { get; set; }
    public long? BusinessId { get; set; }
    public string? Remark { get; set; }
}

public sealed class SmsUpdateDto : BasicAppUDto
{
    public long? ReceiverId { get; set; }
    public SmsType SmsType { get; set; } = SmsType.Notification;
    public string ToPhone { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? TemplateCode { get; set; }
    public string? TemplateParams { get; set; }
    public string? Provider { get; set; }
    public DateTimeOffset? ScheduledTime { get; set; }
    public int MaxRetryCount { get; set; } = 3;
    public string? BusinessType { get; set; }
    public long? BusinessId { get; set; }
    public string? Remark { get; set; }
}

public sealed class SmsStatusUpdateDto : BasicAppDto
{
    public SmsStatus SmsStatus { get; set; } = SmsStatus.Pending;
    public DateTimeOffset? SendTime { get; set; }
    public string? ProviderMessageId { get; set; }
    public int? RetryCount { get; set; }
    public int? Cost { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Remark { get; set; }
}
