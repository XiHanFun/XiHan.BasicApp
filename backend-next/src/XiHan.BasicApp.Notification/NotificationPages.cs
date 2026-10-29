// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Pages;

namespace XiHan.BasicApp.Notification;

/// <summary>
/// 通知包的页面登记（方案 D4）。
/// </summary>
public static class NotificationPages
{
    /// <summary>
    /// 通知公告页。
    /// </summary>
    public static PageDescriptor Notifications { get; } = new("notification.notifications", "/notification/notifications", "notification.notifications.title", "lucide:megaphone", 480, NotificationPermissions.NotificationsRead);

    /// <summary>
    /// 我的消息页（本人的收件箱，登录即可；4.x 同不设权限）。
    /// </summary>
    public static PageDescriptor Inbox { get; } = new("notification.inbox", "/notification/inbox", "notification.inbox.title", "lucide:inbox", 490);

    /// <summary>
    /// 本包全部页面。
    /// </summary>
    public static IReadOnlyList<PageDescriptor> All { get; } = [Notifications, Inbox];
}
