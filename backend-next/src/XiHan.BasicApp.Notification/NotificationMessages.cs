// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Localization.Abstractions.Messages;

namespace XiHan.BasicApp.Notification;

/// <summary>
/// 通知包的消息键：操作说明与错误文案，十种区域文件在 <c>Localization/</c>。
/// </summary>
public static class NotificationMessages
{
    /// <summary>
    /// 消息目录名。
    /// </summary>
    public const string CatalogName = "basicapp.notification";

    /// <summary>
    /// 操作 <c>notifications.list</c> 的说明。
    /// </summary>
    public const string ListNotificationsDescription = "notifications.list.description";

    /// <summary>
    /// 操作 <c>notifications.get</c> 的说明。
    /// </summary>
    public const string GetNotificationDescription = "notifications.get.description";

    /// <summary>
    /// 操作 <c>notifications.create</c> 的说明。
    /// </summary>
    public const string CreateNotificationDescription = "notifications.create.description";

    /// <summary>
    /// 操作 <c>notifications.update</c> 的说明。
    /// </summary>
    public const string UpdateNotificationDescription = "notifications.update.description";

    /// <summary>
    /// 操作 <c>notifications.delete</c> 的说明。
    /// </summary>
    public const string DeleteNotificationsDescription = "notifications.delete.description";

    /// <summary>
    /// 操作 <c>notifications.publish</c> 的说明。
    /// </summary>
    public const string PublishNotificationDescription = "notifications.publish.description";

    /// <summary>
    /// 操作 <c>notifications.remind</c> 的说明。
    /// </summary>
    public const string RemindNotificationDescription = "notifications.remind.description";

    /// <summary>
    /// 操作 <c>notifications.read-stats</c> 的说明。
    /// </summary>
    public const string NotificationReadStatsDescription = "notifications.read_stats.description";

    /// <summary>
    /// 操作 <c>notifications.recipients</c> 的说明。
    /// </summary>
    public const string ListNotificationRecipientsDescription = "notifications.recipients.description";

    /// <summary>
    /// 操作 <c>inbox.list</c> 的说明。
    /// </summary>
    public const string ListInboxDescription = "inbox.list.description";

    /// <summary>
    /// 操作 <c>inbox.banners</c> 的说明。
    /// </summary>
    public const string ListInboxBannersDescription = "inbox.banners.description";

    /// <summary>
    /// 操作 <c>inbox.popups</c> 的说明。
    /// </summary>
    public const string ListInboxPopupsDescription = "inbox.popups.description";

    /// <summary>
    /// 操作 <c>inbox.mandatory</c> 的说明。
    /// </summary>
    public const string ListInboxMandatoryDescription = "inbox.mandatory.description";

    /// <summary>
    /// 操作 <c>inbox.mark-read</c> 的说明。
    /// </summary>
    public const string MarkInboxReadDescription = "inbox.mark_read.description";

    /// <summary>
    /// 操作 <c>inbox.mark-all-read</c> 的说明。
    /// </summary>
    public const string MarkInboxAllReadDescription = "inbox.mark_all_read.description";

    /// <summary>
    /// 操作 <c>inbox.confirm</c> 的说明。
    /// </summary>
    public const string ConfirmInboxDescription = "inbox.confirm.description";

    /// <summary>
    /// 操作 <c>inbox.mark-popup-shown</c> 的说明。
    /// </summary>
    public const string MarkInboxPopupShownDescription = "inbox.mark_popup_shown.description";

    /// <summary>
    /// 操作 <c>inbox.delete</c> 的说明。
    /// </summary>
    public const string DeleteInboxDescription = "inbox.delete.description";

    /// <summary>
    /// 通知不存在（或不在当前一侧）；参数 <c>id</c>。
    /// </summary>
    public const string NotificationNotFound = "notification.notification.not_found";

    /// <summary>
    /// 收件箱里没有这一条（或不是本人的）；参数 <c>id</c>。
    /// </summary>
    public const string InboxItemNotFound = "notification.inbox.not_found";

    /// <summary>
    /// 通知已经发布，不能再改或重复发布；参数 <c>id</c>。
    /// </summary>
    public const string AlreadyPublished = "notification.notification.published";

    /// <summary>
    /// 通知还没发布（提醒与接收人只对已发布的）；参数 <c>id</c>。
    /// </summary>
    public const string NotPublished = "notification.notification.not_published";

    /// <summary>
    /// 目标为角色、部门或用户时至少要指定一个；参数 <c>targetType</c>。
    /// </summary>
    public const string TargetsRequired = "notification.target.required";

    /// <summary>
    /// 平台一侧没有部门，不能按部门投递。
    /// </summary>
    public const string DepartmentNeedsTenant = "notification.target.department_platform";

    /// <summary>
    /// 到期时间须晚于可见起始时间。
    /// </summary>
    public const string WindowInvalid = "notification.window.invalid";

    /// <summary>
    /// 按目标解析不出任何正常的接收人，不能发布。
    /// </summary>
    public const string NoRecipients = "notification.recipients.none";

    /// <summary>
    /// 标题不能只有空白。
    /// </summary>
    public const string TitleBlank = "notification.notification.title_blank";

    /// <summary>
    /// 建好即发布需要发布权限（<c>notification.notifications.publish</c>）。
    /// </summary>
    public const string PublishDenied = "notification.notification.publish_denied";

    /// <summary>
    /// 没有装解析这类目标的包；参数 <c>targetType</c>。
    /// </summary>
    public const string RecipientSourceMissing = "notification.recipients.source_missing";

    /// <summary>
    /// 不支持该筛选；参数 <c>field</c>、<c>operator</c>。
    /// </summary>
    public const string FilterUnsupported = "notification.query.filter_unsupported";

    /// <summary>
    /// 不支持按该字段排序；参数 <c>field</c>。
    /// </summary>
    public const string SortUnsupported = "notification.query.sort_unsupported";

    private static readonly Lazy<MessageCatalog> _catalog = new(static () => MessageCatalog.FromEmbeddedResources(CatalogName, typeof(NotificationMessages)));

    /// <summary>
    /// 本包的消息目录（嵌入的 10 种区域文件）。
    /// </summary>
    public static MessageCatalog Catalog => _catalog.Value;
}
