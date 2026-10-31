// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Notification.Domain.Entities;
using XiHan.BasicApp.Notification.Operations.BotConfigs;
using XiHan.BasicApp.Notification.Operations.EmailConfigs;
using XiHan.BasicApp.Notification.Operations.Emails;
using XiHan.BasicApp.Notification.Operations.MessageTemplates;
using XiHan.BasicApp.Notification.Operations.Messaging;
using XiHan.BasicApp.Notification.Operations.Notifications;
using XiHan.BasicApp.Notification.Operations.Sms;
using XiHan.BasicApp.Notification.Operations.SmsConfigs;
using XiHan.BasicApp.Notification.Operations.TelegramBots;
using XiHan.BasicApp.Runtime.Queries;

namespace XiHan.BasicApp.Notification;

/// <summary>
/// 通知包登记的字段安全实体。
/// </summary>
/// <remarks>
/// 4.x 同八个实体：通知公告、消息模板、邮件与短信的发送记录、邮件 / 短信 / 机器人配置与 Telegram 机器人（收件箱是本人自己的，不套用）。
/// 字段由响应类型生成，规则由 Access 的字段安全中间件在读、建、改处统一套用。
/// </remarks>
public static class NotificationFieldSecurity
{
    /// <summary>
    /// 登记本包的实体与操作。
    /// </summary>
    /// <param name="services">服务集合。</param>
    public static void Register(IServiceCollection services)
        => services.AddFieldSecurity("notification", NotificationJsonContext.Default, fields => fields
            .Entity<NotificationResponse>("notification.notifications", "notification.notifications.title", "notification.notifications.col_")
                .Reads(ListNotifications.Id, GetNotification.Id)
                .Creates(CreateNotification.Id)
                .Updates<UpdateNotificationRequest>(UpdateNotification.Id, static (request, context) => FieldSecurityOriginal.FindAsync<SysNotification>(context, request.Id, NotificationResponse.From))
                .Keyword("title", "content")
            .Entity<MessageTemplateResponse>("notification.message-templates", "notification.templates.title", "notification.templates.col_")
                .Reads(ListMessageTemplates.Id, GetMessageTemplate.Id)
                .Creates(CreateMessageTemplate.Id)
                .Updates<UpdateMessageTemplateRequest>(UpdateMessageTemplate.Id, static (request, context) => FieldSecurityOriginal.FindAsync<SysMessageTemplate>(context, request.Id, MessageTemplateResponse.From))
                .Keyword("code", "name")
            .Entity<EmailRecordResponse>("notification.emails", "notification.messages.tab_email", "notification.messages.col_")
                .Reads(ListEmails.Id, GetEmail.Id)
                .Keyword("toEmail", "subject")
            .Entity<SmsRecordResponse>("notification.sms", "notification.messages.tab_sms", "notification.messages.col_")
                .Reads(ListSms.Id, GetSms.Id)
                .Keyword("toPhone", "content")
            .Entity<EmailConfigResponse>("notification.email-configs", "notification.email_configs.title", "notification.email_configs.columns.")
                .Reads(ListEmailConfigs.Id, GetEmailConfig.Id)
                .Creates(CreateEmailConfig.Id)
                .Updates<UpdateEmailConfigRequest>(UpdateEmailConfig.Id, static (request, context) => FieldSecurityOriginal.FindAsync<SysEmailConfig>(context, request.Id, EmailConfigResponse.From))
                .Keyword("code", "name", "smtpHost")
            .Entity<SmsConfigResponse>("notification.sms-configs", "notification.sms_configs.title", "notification.sms_configs.columns.")
                .Reads(ListSmsConfigs.Id, GetSmsConfig.Id)
                .Creates(CreateSmsConfig.Id)
                .Updates<UpdateSmsConfigRequest>(UpdateSmsConfig.Id, static (request, context) => FieldSecurityOriginal.FindAsync<SysSmsConfig>(context, request.Id, SmsConfigResponse.From))
                .Keyword("code", "name")
            .Entity<BotConfigResponse>("notification.bot-configs", "notification.bot_configs.title", "notification.bot_configs.columns.")
                .Reads(ListBotConfigs.Id, GetBotConfig.Id)
                .Creates(CreateBotConfig.Id)
                .Updates<UpdateBotConfigRequest>(UpdateBotConfig.Id, static (request, context) => FieldSecurityOriginal.FindAsync<SysBotConfig>(context, request.Id, BotConfigResponse.From))
                .Keyword("code", "name")
            .Entity<TelegramBotResponse>("notification.telegram-bots", "notification.telegram_bots.title", "notification.telegram_bots.columns.")
                .Reads(ListTelegramBots.Id, GetTelegramBot.Id)
                .Creates(CreateTelegramBot.Id)
                .Updates<UpdateTelegramBotRequest>(UpdateTelegramBot.Id, static (request, context) => FieldSecurityOriginal.FindAsync<SysTelegramBot>(context, request.Id, TelegramBotResponse.From))
                .Keyword("name"));
}
