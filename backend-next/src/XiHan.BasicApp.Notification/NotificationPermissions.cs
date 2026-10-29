// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Security;

namespace XiHan.BasicApp.Notification;

/// <summary>
/// 通知包的权限码：两侧都有，各自管理本侧的通知公告。收件箱是自助的，只要求已登录，不在这里。
/// </summary>
public static class NotificationPermissions
{
    /// <summary>
    /// 查看通知公告、阅读统计与接收人。
    /// </summary>
    public const string NotificationsRead = "notification.notifications.read";

    /// <summary>
    /// 新建通知（草稿）。
    /// </summary>
    public const string NotificationsCreate = "notification.notifications.create";

    /// <summary>
    /// 修改草稿。
    /// </summary>
    public const string NotificationsUpdate = "notification.notifications.update";

    /// <summary>
    /// 删除通知（已发布的连同收件箱里的那一条）。
    /// </summary>
    public const string NotificationsDelete = "notification.notifications.delete";

    /// <summary>
    /// 发布与提醒。
    /// </summary>
    public const string NotificationsPublish = "notification.notifications.publish";

    /// <summary>
    /// 本包全部权限的描述。
    /// </summary>
    public static IReadOnlyList<PermissionDescriptor> All { get; } =
    [
        new(NotificationsRead, "Read notifications", PermissionScope.Both, description: "View the caller's side's notifications with their read statistics and recipients."),
        new(NotificationsCreate, "Create notifications", PermissionScope.Both, audited: true, description: "Create notification drafts on the caller's side."),
        new(NotificationsUpdate, "Update notifications", PermissionScope.Both, audited: true, description: "Edit notification drafts; published notifications cannot be changed."),
        new(NotificationsDelete, "Delete notifications", PermissionScope.Both, audited: true, description: "Delete notifications; a published one also leaves its recipients' inboxes."),
        new(NotificationsPublish, "Publish notifications", PermissionScope.Both, audited: true, description: "Publish notifications to all users, roles, departments or chosen users of the caller's side and remind those who have not read them."),
    ];
}
