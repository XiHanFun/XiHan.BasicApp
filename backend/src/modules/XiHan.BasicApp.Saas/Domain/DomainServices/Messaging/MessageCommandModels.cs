#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MessageCommandModels
// Guid:b8fc8fc0-e5da-4924-ae99-1de891c0e3e9
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 邮件创建命令
/// </summary>
public sealed record EmailCreateCommand(
    long? SendUserId,
    long? ReceiveUserId,
    EmailType EmailType,
    string FromEmail,
    string? FromName,
    string ToEmail,
    string? CcEmail,
    string? BccEmail,
    string Subject,
    string? Content,
    bool IsHtml,
    string? Attachments,
    long? TemplateId,
    string? TemplateParams,
    DateTimeOffset? ScheduledTime,
    int MaxRetryCount,
    string? BusinessType,
    long? BusinessId,
    string? Remark);

/// <summary>
/// 邮件更新命令
/// </summary>
public sealed record EmailUpdateCommand(
    long BasicId,
    long? ReceiveUserId,
    EmailType EmailType,
    string FromEmail,
    string? FromName,
    string ToEmail,
    string? CcEmail,
    string? BccEmail,
    string Subject,
    string? Content,
    bool IsHtml,
    string? Attachments,
    long? TemplateId,
    string? TemplateParams,
    DateTimeOffset? ScheduledTime,
    int MaxRetryCount,
    string? BusinessType,
    long? BusinessId,
    string? Remark);

/// <summary>
/// 邮件状态更新命令
/// </summary>
public sealed record EmailStatusUpdateCommand(
    long BasicId,
    EmailStatus EmailStatus,
    DateTimeOffset? SendTime,
    int? RetryCount,
    string? ErrorMessage,
    string? Remark);

/// <summary>
/// 短信创建命令
/// </summary>
public sealed record SmsCreateCommand(
    long? SenderId,
    long? ReceiverId,
    SmsType SmsType,
    string ToPhone,
    string Content,
    long? TemplateId,
    string? TemplateParams,
    string? Provider,
    DateTimeOffset? ScheduledTime,
    int MaxRetryCount,
    string? BusinessType,
    long? BusinessId,
    string? Remark);

/// <summary>
/// 短信更新命令
/// </summary>
public sealed record SmsUpdateCommand(
    long BasicId,
    long? ReceiverId,
    SmsType SmsType,
    string ToPhone,
    string Content,
    long? TemplateId,
    string? TemplateParams,
    string? Provider,
    DateTimeOffset? ScheduledTime,
    int MaxRetryCount,
    string? BusinessType,
    long? BusinessId,
    string? Remark);

/// <summary>
/// 短信状态更新命令
/// </summary>
public sealed record SmsStatusUpdateCommand(
    long BasicId,
    SmsStatus SmsStatus,
    DateTimeOffset? SendTime,
    string? ProviderMessageId,
    int? RetryCount,
    int? Cost,
    string? ErrorMessage,
    string? Remark);

/// <summary>
/// 邮件命令结果
/// </summary>
public sealed record EmailCommandResult(SysEmail Email);

/// <summary>
/// 短信命令结果
/// </summary>
public sealed record SmsCommandResult(SysSms Sms);
