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
    /// 邮件短信记录页。
    /// </summary>
    public static PageDescriptor Messages { get; } = new("notification.messages", "/notification/messages", "notification.messages.title", "lucide:send", 482, NotificationPermissions.EmailsRead);

    /// <summary>
    /// 消息模板页。
    /// </summary>
    public static PageDescriptor Templates { get; } = new("notification.templates", "/notification/templates", "notification.templates.title", "lucide:file-code-2", 484, NotificationPermissions.TemplatesRead);

    /// <summary>
    /// 邮件配置页。
    /// </summary>
    public static PageDescriptor EmailConfigs { get; } = new("notification.email-configs", "/notification/email-configs", "notification.email_configs.title", "lucide:mail-plus", 486, NotificationPermissions.EmailConfigsRead);

    /// <summary>
    /// 短信配置页。
    /// </summary>
    public static PageDescriptor SmsConfigs { get; } = new("notification.sms-configs", "/notification/sms-configs", "notification.sms_configs.title", "lucide:message-square-code", 488, NotificationPermissions.SmsConfigsRead);

    /// <summary>
    /// 机器人配置页。
    /// </summary>
    public static PageDescriptor BotConfigs { get; } = new("notification.bot-configs", "/notification/bot-configs", "notification.bot_configs.title", "lucide:bot", 489, NotificationPermissions.BotConfigsRead);

    /// <summary>
    /// Telegram 机器人页（平台）。
    /// </summary>
    public static PageDescriptor TelegramBots { get; } = new("notification.telegram-bots", "/notification/telegram-bots", "notification.telegram_bots.title", "lucide:bot-message-square", 490, NotificationPermissions.TelegramBotsRead);

    /// <summary>
    /// 我的消息页（本人的收件箱，登录即可；4.x 同不设权限）。
    /// </summary>
    public static PageDescriptor Inbox { get; } = new("notification.inbox", "/notification/inbox", "notification.inbox.title", "lucide:inbox", 495);

    /// <summary>
    /// 本包全部页面。
    /// </summary>
    public static IReadOnlyList<PageDescriptor> All { get; } = [Notifications, Messages, Templates, EmailConfigs, SmsConfigs, BotConfigs, TelegramBots, Inbox];
}
