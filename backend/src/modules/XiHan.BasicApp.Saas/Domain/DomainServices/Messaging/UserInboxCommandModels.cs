// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 用户站内信投递命令
/// </summary>
public sealed record UserInboxDispatchCommand(
    long UserId,
    string Title,
    string? Content,
    NotificationType NotificationType,
    string? BusinessType,
    long? BusinessId,
    bool NeedConfirm,
    string? Link,
    string? Icon);

/// <summary>
/// 用户站内信投递结果
/// </summary>
public sealed record UserInboxDispatchResult(SysNotification Notification, SysUserNotification UserNotification);
