#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserInboxCommandModels
// Guid:f35df969-71d7-4f21-92db-aeab2f12728e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
