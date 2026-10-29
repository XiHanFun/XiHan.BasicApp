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
    /// 操作 <c>message-templates.list</c> 的说明。
    /// </summary>
    public const string ListMessageTemplatesDescription = "message_templates.list.description";

    /// <summary>
    /// 操作 <c>message-templates.get</c> 的说明。
    /// </summary>
    public const string GetMessageTemplateDescription = "message_templates.get.description";

    /// <summary>
    /// 操作 <c>message-templates.create</c> 的说明。
    /// </summary>
    public const string CreateMessageTemplateDescription = "message_templates.create.description";

    /// <summary>
    /// 操作 <c>message-templates.update</c> 的说明。
    /// </summary>
    public const string UpdateMessageTemplateDescription = "message_templates.update.description";

    /// <summary>
    /// 操作 <c>message-templates.delete</c> 的说明。
    /// </summary>
    public const string DeleteMessageTemplatesDescription = "message_templates.delete.description";

    /// <summary>
    /// 操作 <c>email-configs.list</c> 的说明。
    /// </summary>
    public const string ListEmailConfigsDescription = "email_configs.list.description";

    /// <summary>
    /// 操作 <c>email-configs.get</c> 的说明。
    /// </summary>
    public const string GetEmailConfigDescription = "email_configs.get.description";

    /// <summary>
    /// 操作 <c>email-configs.create</c> 的说明。
    /// </summary>
    public const string CreateEmailConfigDescription = "email_configs.create.description";

    /// <summary>
    /// 操作 <c>email-configs.update</c> 的说明。
    /// </summary>
    public const string UpdateEmailConfigDescription = "email_configs.update.description";

    /// <summary>
    /// 操作 <c>email-configs.set-default</c> 的说明。
    /// </summary>
    public const string SetDefaultEmailConfigDescription = "email_configs.set_default.description";

    /// <summary>
    /// 操作 <c>email-configs.delete</c> 的说明。
    /// </summary>
    public const string DeleteEmailConfigsDescription = "email_configs.delete.description";

    /// <summary>
    /// 操作 <c>sms-configs.list</c> 的说明。
    /// </summary>
    public const string ListSmsConfigsDescription = "sms_configs.list.description";

    /// <summary>
    /// 操作 <c>sms-configs.get</c> 的说明。
    /// </summary>
    public const string GetSmsConfigDescription = "sms_configs.get.description";

    /// <summary>
    /// 操作 <c>sms-configs.create</c> 的说明。
    /// </summary>
    public const string CreateSmsConfigDescription = "sms_configs.create.description";

    /// <summary>
    /// 操作 <c>sms-configs.update</c> 的说明。
    /// </summary>
    public const string UpdateSmsConfigDescription = "sms_configs.update.description";

    /// <summary>
    /// 操作 <c>sms-configs.set-default</c> 的说明。
    /// </summary>
    public const string SetDefaultSmsConfigDescription = "sms_configs.set_default.description";

    /// <summary>
    /// 操作 <c>sms-configs.delete</c> 的说明。
    /// </summary>
    public const string DeleteSmsConfigsDescription = "sms_configs.delete.description";

    /// <summary>
    /// 操作 <c>emails.list</c> 的说明。
    /// </summary>
    public const string ListEmailsDescription = "emails.list.description";

    /// <summary>
    /// 操作 <c>emails.get</c> 的说明。
    /// </summary>
    public const string GetEmailDescription = "emails.get.description";

    /// <summary>
    /// 操作 <c>emails.resend</c> 的说明。
    /// </summary>
    public const string ResendEmailsDescription = "emails.resend.description";

    /// <summary>
    /// 操作 <c>emails.cancel</c> 的说明。
    /// </summary>
    public const string CancelEmailsDescription = "emails.cancel.description";

    /// <summary>
    /// 操作 <c>emails.delete</c> 的说明。
    /// </summary>
    public const string DeleteEmailsDescription = "emails.delete.description";

    /// <summary>
    /// 操作 <c>sms.list</c> 的说明。
    /// </summary>
    public const string ListSmsDescription = "sms.list.description";

    /// <summary>
    /// 操作 <c>sms.get</c> 的说明。
    /// </summary>
    public const string GetSmsDescription = "sms.get.description";

    /// <summary>
    /// 操作 <c>sms.resend</c> 的说明。
    /// </summary>
    public const string ResendSmsDescription = "sms.resend.description";

    /// <summary>
    /// 操作 <c>sms.cancel</c> 的说明。
    /// </summary>
    public const string CancelSmsDescription = "sms.cancel.description";

    /// <summary>
    /// 操作 <c>sms.delete</c> 的说明。
    /// </summary>
    public const string DeleteSmsDescription = "sms.delete.description";

    /// <summary>
    /// 操作 <c>bot-configs.list</c> 的说明。
    /// </summary>
    public const string ListBotConfigsDescription = "bot_configs.list.description";

    /// <summary>
    /// 操作 <c>bot-configs.get</c> 的说明。
    /// </summary>
    public const string GetBotConfigDescription = "bot_configs.get.description";

    /// <summary>
    /// 操作 <c>bot-configs.create</c> 的说明。
    /// </summary>
    public const string CreateBotConfigDescription = "bot_configs.create.description";

    /// <summary>
    /// 操作 <c>bot-configs.update</c> 的说明。
    /// </summary>
    public const string UpdateBotConfigDescription = "bot_configs.update.description";

    /// <summary>
    /// 操作 <c>bot-configs.set-default</c> 的说明。
    /// </summary>
    public const string SetDefaultBotConfigDescription = "bot_configs.set_default.description";

    /// <summary>
    /// 操作 <c>bot-configs.delete</c> 的说明。
    /// </summary>
    public const string DeleteBotConfigsDescription = "bot_configs.delete.description";

    /// <summary>
    /// 操作 <c>telegram-bots.list</c> 的说明。
    /// </summary>
    public const string ListTelegramBotsDescription = "telegram_bots.list.description";

    /// <summary>
    /// 操作 <c>telegram-bots.get</c> 的说明。
    /// </summary>
    public const string GetTelegramBotDescription = "telegram_bots.get.description";

    /// <summary>
    /// 操作 <c>telegram-bots.create</c> 的说明。
    /// </summary>
    public const string CreateTelegramBotDescription = "telegram_bots.create.description";

    /// <summary>
    /// 操作 <c>telegram-bots.update</c> 的说明。
    /// </summary>
    public const string UpdateTelegramBotDescription = "telegram_bots.update.description";

    /// <summary>
    /// 操作 <c>telegram-bots.delete</c> 的说明。
    /// </summary>
    public const string DeleteTelegramBotsDescription = "telegram_bots.delete.description";

    /// <summary>
    /// 通知不存在（或不在当前一侧）。
    /// </summary>
    /// <remarks>
    /// 参数 <c>id</c>。
    /// </remarks>
    public const string NotificationNotFound = "notification.notification.not_found";

    /// <summary>
    /// 收件箱里没有这一条（或不是本人的）。
    /// </summary>
    /// <remarks>
    /// 参数 <c>id</c>。
    /// </remarks>
    public const string InboxItemNotFound = "notification.inbox.not_found";

    /// <summary>
    /// 通知已经发布，不能再改或重复发布。
    /// </summary>
    /// <remarks>
    /// 参数 <c>id</c>。
    /// </remarks>
    public const string AlreadyPublished = "notification.notification.published";

    /// <summary>
    /// 通知还没发布（提醒与接收人只对已发布的）。
    /// </summary>
    /// <remarks>
    /// 参数 <c>id</c>。
    /// </remarks>
    public const string NotPublished = "notification.notification.not_published";

    /// <summary>
    /// 目标为角色、部门或用户时至少要指定一个。
    /// </summary>
    /// <remarks>
    /// 参数 <c>targetType</c>。
    /// </remarks>
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
    /// Telegram 平台设置的 JSON 写错了。
    /// </summary>
    /// <remarks>
    /// 参数 <c>key</c>、<c>detail</c>。
    /// </remarks>
    public const string TelegramSettingsInvalid = "notification.telegram.settings_invalid";

    /// <summary>
    /// Telegram 机器人不存在。
    /// </summary>
    /// <remarks>
    /// 参数 <c>id</c>。
    /// </remarks>
    public const string TelegramBotNotFound = "notification.telegram_bot.not_found";

    /// <summary>
    /// 已有这个名称的 Telegram 机器人。
    /// </summary>
    /// <remarks>
    /// 参数 <c>name</c>。
    /// </remarks>
    public const string TelegramBotNameTaken = "notification.telegram_bot.name_taken";

    /// <summary>
    /// 令牌须形如「数字:字符串」（BotFather 给的）。
    /// </summary>
    public const string TelegramTokenInvalid = "notification.telegram_bot.token_invalid";

    /// <summary>
    /// ID 列表里有不是整数的项。
    /// </summary>
    /// <remarks>
    /// 参数 <c>field</c>、<c>item</c>。
    /// </remarks>
    public const string TelegramIdsInvalid = "notification.telegram_bot.ids_invalid";

    /// <summary>
    /// 命令名只能是小写字母、数字与下划线，最长 32。
    /// </summary>
    /// <remarks>
    /// 参数 <c>item</c>。
    /// </remarks>
    public const string TelegramCommandsInvalid = "notification.telegram_bot.commands_invalid";

    /// <summary>
    /// 机器人配置不存在。
    /// </summary>
    /// <remarks>
    /// 参数 <c>id</c>。
    /// </remarks>
    public const string BotConfigNotFound = "notification.bot_config.not_found";

    /// <summary>
    /// Webhook 地址须是所选服务商的官方 https 地址。
    /// </summary>
    /// <remarks>
    /// 参数 <c>url</c>。
    /// </remarks>
    public const string BotWebhookInvalid = "notification.bot_config.webhook_invalid";

    /// <summary>
    /// 消息模板不存在（或不在当前一侧）。
    /// </summary>
    /// <remarks>
    /// 参数 <c>id</c>。
    /// </remarks>
    public const string TemplateNotFound = "notification.template.not_found";

    /// <summary>
    /// 邮件配置不存在。
    /// </summary>
    /// <remarks>
    /// 参数 <c>id</c>。
    /// </remarks>
    public const string EmailConfigNotFound = "notification.email_config.not_found";

    /// <summary>
    /// 短信配置不存在。
    /// </summary>
    /// <remarks>
    /// 参数 <c>id</c>。
    /// </remarks>
    public const string SmsConfigNotFound = "notification.sms_config.not_found";

    /// <summary>
    /// 邮件发送记录不存在。
    /// </summary>
    /// <remarks>
    /// 参数 <c>id</c>。
    /// </remarks>
    public const string EmailRecordNotFound = "notification.email.not_found";

    /// <summary>
    /// 短信发送记录不存在。
    /// </summary>
    /// <remarks>
    /// 参数 <c>id</c>。
    /// </remarks>
    public const string SmsRecordNotFound = "notification.sms.not_found";

    /// <summary>
    /// 同一侧、同一渠道里已有这个编码的模板。
    /// </summary>
    /// <remarks>
    /// 参数 <c>code</c>。
    /// </remarks>
    public const string TemplateCodeTaken = "notification.template.code_taken";

    /// <summary>
    /// 本侧已有这个编码的配置。
    /// </summary>
    /// <remarks>
    /// 参数 <c>code</c>。
    /// </remarks>
    public const string ConfigCodeTaken = "notification.config.code_taken";

    /// <summary>
    /// 缺省配置须启用，不能停用或删除。
    /// </summary>
    /// <remarks>
    /// 停用的配置不能设为缺省；参数 <c>code</c>。
    /// </remarks>
    public const string DefaultConfigLocked = "notification.config.default_locked";

    /// <summary>
    /// 记录正在排队或发送中。
    /// </summary>
    public const string RecordBusy = "notification.record.busy";

    /// <summary>
    /// 只有排队中的记录能取消。
    /// </summary>
    public const string RecordNotPending = "notification.record.not_pending";

    /// <summary>
    /// 模板语法错误或渲染失败。
    /// </summary>
    /// <remarks>
    /// 参数 <c>detail</c>。
    /// </remarks>
    public const string TemplateInvalid = "notification.template.invalid";

    /// <summary>
    /// 邮件地址格式不对。
    /// </summary>
    /// <remarks>
    /// 参数 <c>address</c>。
    /// </remarks>
    public const string EmailAddressInvalid = "notification.email.address_invalid";

    /// <summary>
    /// 模板映射须是字符串到字符串的 JSON 对象。
    /// </summary>
    public const string TemplateMapInvalid = "notification.sms_config.template_map_invalid";

    /// <summary>
    /// 投递渠道须包含站内信。
    /// </summary>
    public const string ChannelsInvalid = "notification.channels.invalid";

    /// <summary>
    /// 当前一侧与平台都没有启用的这个模板。
    /// </summary>
    /// <remarks>
    /// 参数 <c>code</c>。
    /// </remarks>
    public const string TemplateMissing = "notification.template.missing";

    /// <summary>
    /// 当前一侧与平台都没有启用的缺省配置。
    /// </summary>
    /// <remarks>
    /// 参数 <c>kind</c>。
    /// </remarks>
    public const string ChannelUnavailable = "notification.channel.unavailable";

    /// <summary>
    /// 标题不能只有空白。
    /// </summary>
    public const string TitleBlank = "notification.notification.title_blank";

    /// <summary>
    /// 建好即发布需要发布权限。
    /// </summary>
    /// <remarks>
    /// 权限码 <c>notification.notifications.publish</c>。
    /// </remarks>
    public const string PublishDenied = "notification.notification.publish_denied";

    /// <summary>
    /// 没有装解析这类目标的包。
    /// </summary>
    /// <remarks>
    /// 参数 <c>targetType</c>。
    /// </remarks>
    public const string RecipientSourceMissing = "notification.recipients.source_missing";

    /// <summary>
    /// 不支持该筛选。
    /// </summary>
    /// <remarks>
    /// 参数 <c>field</c>、<c>operator</c>。
    /// </remarks>
    public const string FilterUnsupported = "notification.query.filter_unsupported";

    /// <summary>
    /// 不支持按该字段排序。
    /// </summary>
    /// <remarks>
    /// 参数 <c>field</c>。
    /// </remarks>
    public const string SortUnsupported = "notification.query.sort_unsupported";

    private static readonly Lazy<MessageCatalog> _catalog = new(static () => MessageCatalog.FromEmbeddedResources(CatalogName, typeof(NotificationMessages)));

    /// <summary>
    /// 本包的消息目录（嵌入的 10 种区域文件）。
    /// </summary>
    public static MessageCatalog Catalog => _catalog.Value;
}
