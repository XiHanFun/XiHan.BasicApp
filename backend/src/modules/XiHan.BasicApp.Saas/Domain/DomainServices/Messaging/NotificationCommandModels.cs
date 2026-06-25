#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:NotificationCommandModels
// Guid:a3d0df0d-d1d8-4bc1-ad23-ddf5c0598ac0
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
/// 通知命令结果
/// </summary>
public sealed record NotificationCommandResult(SysNotification Notification);

/// <summary>
/// 通知发布命令结果
/// </summary>
public sealed record NotificationPublishCommandResult(SysNotification Notification, int RecipientCount);
