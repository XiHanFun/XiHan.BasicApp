// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Notification.Domain.Entities;

/// <summary>
/// 通知类型（4.x 同）。
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// 系统公告。
    /// </summary>
    System = 1,

    /// <summary>
    /// 安全通知。
    /// </summary>
    Security = 2,

    /// <summary>
    /// 业务通知。
    /// </summary>
    Business = 3,

    /// <summary>
    /// 待办通知。
    /// </summary>
    Todo = 4,

    /// <summary>
    /// 紧急通知。
    /// </summary>
    Emergency = 5,
}

/// <summary>
/// 优先级（4.x 同）。
/// </summary>
public enum NotificationPriority
{
    /// <summary>
    /// 低。
    /// </summary>
    Low = 1,

    /// <summary>
    /// 普通。
    /// </summary>
    Normal = 2,

    /// <summary>
    /// 高。
    /// </summary>
    High = 3,

    /// <summary>
    /// 紧急。
    /// </summary>
    Urgent = 4,
}

/// <summary>
/// 正文格式（4.x 同）：HTML 正文由前端按白名单清洗后渲染。
/// </summary>
public enum NotificationContentFormat
{
    /// <summary>
    /// 纯文本。
    /// </summary>
    Text = 1,

    /// <summary>
    /// Markdown。
    /// </summary>
    Markdown = 2,

    /// <summary>
    /// HTML。
    /// </summary>
    Html = 3,
}

/// <summary>
/// 投递目标（4.x 同）。
/// </summary>
public enum NotificationTargetType
{
    /// <summary>
    /// 当前一侧的全部正常账号。
    /// </summary>
    All = 0,

    /// <summary>
    /// 持有指定角色的用户。
    /// </summary>
    Role = 1,

    /// <summary>
    /// 指定部门及其下级部门的成员（只在租户一侧）。
    /// </summary>
    Department = 2,

    /// <summary>
    /// 指定用户。
    /// </summary>
    User = 3,
}

/// <summary>
/// 收件箱一条的阅读状态。
/// </summary>
public enum UserNotificationStatus
{
    /// <summary>
    /// 未读。
    /// </summary>
    Unread = 0,

    /// <summary>
    /// 已读。
    /// </summary>
    Read = 1,
}
