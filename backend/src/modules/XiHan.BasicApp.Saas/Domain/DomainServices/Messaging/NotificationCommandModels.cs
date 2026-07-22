// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 通知创建命令
/// </summary>
public sealed record NotificationCreateCommand(
    long? SendUserId,
    NotificationType NotificationType,
    string Title,
    string? Content,
    string? Icon,
    string? Link,
    string? BusinessType,
    long? BusinessId,
    DateTimeOffset? SendTime,
    DateTimeOffset? ExpirationTime,
    NotificationTargetType TargetType,
    IReadOnlyList<long>? UserIds,
    bool NeedConfirm,
    NotificationPriority Priority,
    NotificationContentFormat ContentFormat,
    MessageChannel DeliveryChannels,
    DateTimeOffset? StartTime,
    bool IsMandatory,
    bool IsBanner,
    bool IsPopup,
    bool PublishImmediately,
    string? Remark);

/// <summary>
/// 通知更新命令
/// </summary>
public sealed record NotificationUpdateCommand(
    long BasicId,
    NotificationType NotificationType,
    string Title,
    string? Content,
    string? Icon,
    string? Link,
    string? BusinessType,
    long? BusinessId,
    DateTimeOffset? SendTime,
    DateTimeOffset? ExpirationTime,
    NotificationTargetType TargetType,
    IReadOnlyList<long>? UserIds,
    bool NeedConfirm,
    NotificationPriority Priority,
    NotificationContentFormat ContentFormat,
    MessageChannel DeliveryChannels,
    DateTimeOffset? StartTime,
    bool IsMandatory,
    bool IsBanner,
    bool IsPopup,
    string? Remark);

/// <summary>
/// 通知发布命令
/// </summary>
public sealed record NotificationPublishCommand(long BasicId, NotificationTargetType? TargetType, IReadOnlyList<long>? UserIds);

/// <summary>
/// 通知渠道收件人解析结果（邮箱/短信位各自经偏好门控后的用户主键集合；未勾选渠道为空集合）
/// </summary>
public sealed record NotificationChannelRecipientsResult(
    IReadOnlyCollection<long> EmailUserIds,
    IReadOnlyCollection<long> SmsUserIds)
{
    /// <summary>
    /// 空结果（无邮箱/短信投递位时使用）
    /// </summary>
    public static NotificationChannelRecipientsResult Empty { get; } = new([], []);
}

/// <summary>
/// 通知命令结果
/// </summary>
public sealed record NotificationCommandResult(SysNotification Notification);

/// <summary>
/// 通知发布命令结果
/// </summary>
public sealed record NotificationPublishCommandResult(SysNotification Notification, int RecipientCount);
