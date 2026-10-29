// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Security;

namespace XiHan.BasicApp.Notification;

/// <summary>
/// 通知包的权限码。
/// </summary>
/// <remarks>
/// 两侧都有，各自管理本侧的通知公告。收件箱是自助的，只要求已登录，不在这里。
/// </remarks>
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
    /// 查看消息模板。
    /// </summary>
    public const string TemplatesRead = "notification.message-templates.read";

    /// <summary>
    /// 新建消息模板。
    /// </summary>
    public const string TemplatesCreate = "notification.message-templates.create";

    /// <summary>
    /// 修改消息模板（含启停）。
    /// </summary>
    public const string TemplatesUpdate = "notification.message-templates.update";

    /// <summary>
    /// 删除消息模板。
    /// </summary>
    public const string TemplatesDelete = "notification.message-templates.delete";

    /// <summary>
    /// 查看邮件配置。
    /// </summary>
    public const string EmailConfigsRead = "notification.email-configs.read";

    /// <summary>
    /// 新建邮件配置。
    /// </summary>
    public const string EmailConfigsCreate = "notification.email-configs.create";

    /// <summary>
    /// 修改邮件配置、设缺省。
    /// </summary>
    public const string EmailConfigsUpdate = "notification.email-configs.update";

    /// <summary>
    /// 删除邮件配置。
    /// </summary>
    public const string EmailConfigsDelete = "notification.email-configs.delete";

    /// <summary>
    /// 查看短信配置。
    /// </summary>
    public const string SmsConfigsRead = "notification.sms-configs.read";

    /// <summary>
    /// 新建短信配置。
    /// </summary>
    public const string SmsConfigsCreate = "notification.sms-configs.create";

    /// <summary>
    /// 修改短信配置、设缺省。
    /// </summary>
    public const string SmsConfigsUpdate = "notification.sms-configs.update";

    /// <summary>
    /// 删除短信配置。
    /// </summary>
    public const string SmsConfigsDelete = "notification.sms-configs.delete";

    /// <summary>
    /// 查看机器人配置。
    /// </summary>
    public const string BotConfigsRead = "notification.bot-configs.read";

    /// <summary>
    /// 新建机器人配置。
    /// </summary>
    public const string BotConfigsCreate = "notification.bot-configs.create";

    /// <summary>
    /// 修改机器人配置、设缺省。
    /// </summary>
    public const string BotConfigsUpdate = "notification.bot-configs.update";

    /// <summary>
    /// 删除机器人配置。
    /// </summary>
    public const string BotConfigsDelete = "notification.bot-configs.delete";

    /// <summary>
    /// 查看 Telegram 机器人（平台）。
    /// </summary>
    public const string TelegramBotsRead = "notification.telegram-bots.read";

    /// <summary>
    /// 新建 Telegram 机器人（平台）。
    /// </summary>
    public const string TelegramBotsCreate = "notification.telegram-bots.create";

    /// <summary>
    /// 修改 Telegram 机器人（平台）。
    /// </summary>
    public const string TelegramBotsUpdate = "notification.telegram-bots.update";

    /// <summary>
    /// 删除 Telegram 机器人（平台）。
    /// </summary>
    public const string TelegramBotsDelete = "notification.telegram-bots.delete";

    /// <summary>
    /// 查看邮件发送记录。
    /// </summary>
    public const string EmailsRead = "notification.emails.read";

    /// <summary>
    /// 重发与取消邮件。
    /// </summary>
    public const string EmailsResend = "notification.emails.resend";

    /// <summary>
    /// 删除邮件发送记录。
    /// </summary>
    public const string EmailsDelete = "notification.emails.delete";

    /// <summary>
    /// 查看短信发送记录。
    /// </summary>
    public const string SmsRead = "notification.sms.read";

    /// <summary>
    /// 重发与取消短信。
    /// </summary>
    public const string SmsResend = "notification.sms.resend";

    /// <summary>
    /// 删除短信发送记录。
    /// </summary>
    public const string SmsDelete = "notification.sms.delete";

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
        new(TemplatesRead, "Read message templates", PermissionScope.Both, description: "View the email and SMS templates of the caller's side."),
        new(TemplatesCreate, "Create message templates", PermissionScope.Both, audited: true, description: "Create email and SMS templates on the caller's side."),
        new(TemplatesUpdate, "Update message templates", PermissionScope.Both, audited: true, description: "Edit, enable and disable message templates on the caller's side."),
        new(TemplatesDelete, "Delete message templates", PermissionScope.Both, audited: true, description: "Delete message templates on the caller's side; tenants fall back to the platform template of the same code."),
        new(EmailConfigsRead, "Read email configurations", PermissionScope.Both, description: "View the SMTP configurations of the caller's side; passwords are never shown."),
        new(EmailConfigsCreate, "Create email configurations", PermissionScope.Both, audited: true, description: "Create SMTP configurations on the caller's side."),
        new(EmailConfigsUpdate, "Update email configurations", PermissionScope.Both, audited: true, description: "Edit SMTP configurations and choose the default one of the caller's side."),
        new(EmailConfigsDelete, "Delete email configurations", PermissionScope.Both, audited: true, description: "Delete SMTP configurations on the caller's side other than the default."),
        new(SmsConfigsRead, "Read SMS configurations", PermissionScope.Both, description: "View the SMS provider configurations of the caller's side; access keys are never shown."),
        new(SmsConfigsCreate, "Create SMS configurations", PermissionScope.Both, audited: true, description: "Create SMS provider configurations on the caller's side."),
        new(SmsConfigsUpdate, "Update SMS configurations", PermissionScope.Both, audited: true, description: "Edit SMS provider configurations and choose the default one of the caller's side."),
        new(SmsConfigsDelete, "Delete SMS configurations", PermissionScope.Both, audited: true, description: "Delete SMS provider configurations on the caller's side other than the default."),
        new(BotConfigsRead, "Read bot configurations", PermissionScope.Both, description: "View the DingTalk, Lark and WeCom group bots of the caller's side; signing secrets are never shown."),
        new(BotConfigsCreate, "Create bot configurations", PermissionScope.Both, audited: true, description: "Create group bots on the caller's side."),
        new(BotConfigsUpdate, "Update bot configurations", PermissionScope.Both, audited: true, description: "Edit group bots and choose the default bot of each provider on the caller's side."),
        new(BotConfigsDelete, "Delete bot configurations", PermissionScope.Both, audited: true, description: "Delete group bots on the caller's side other than the defaults."),
        new(TelegramBotsRead, "Read Telegram bots", PermissionScope.Platform, description: "View the platform-hosted Telegram bots; tokens are never shown."),
        new(TelegramBotsCreate, "Create Telegram bots", PermissionScope.Platform, audited: true, description: "Create platform-hosted Telegram bots."),
        new(TelegramBotsUpdate, "Update Telegram bots", PermissionScope.Platform, audited: true, description: "Edit Telegram bots: token, admins, group and command allow-lists, fallback reply and broadcast chats."),
        new(TelegramBotsDelete, "Delete Telegram bots", PermissionScope.Platform, audited: true, description: "Delete platform-hosted Telegram bots."),
        new(EmailsRead, "Read sent emails", PermissionScope.Both, description: "View the email send records of the caller's side."),
        new(EmailsResend, "Resend emails", PermissionScope.Both, audited: true, description: "Resend failed, cancelled or sent emails, and cancel queued ones."),
        new(EmailsDelete, "Delete sent emails", PermissionScope.Both, audited: true, description: "Delete email send records of the caller's side."),
        new(SmsRead, "Read sent SMS", PermissionScope.Both, description: "View the SMS send records of the caller's side."),
        new(SmsResend, "Resend SMS", PermissionScope.Both, audited: true, description: "Resend failed, cancelled or sent SMS, and cancel queued ones."),
        new(SmsDelete, "Delete sent SMS", PermissionScope.Both, audited: true, description: "Delete SMS send records of the caller's side."),
    ];
}
