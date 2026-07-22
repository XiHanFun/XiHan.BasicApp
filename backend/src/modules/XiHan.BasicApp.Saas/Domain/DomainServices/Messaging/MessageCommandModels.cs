// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
    string? TemplateCode,
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
    string? TemplateCode,
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
    string? TemplateCode,
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
    string? TemplateCode,
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
