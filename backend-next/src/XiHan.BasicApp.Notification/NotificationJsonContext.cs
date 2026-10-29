// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Notification.Domain.Services;
using XiHan.BasicApp.Notification.Operations.BotConfigs;
using XiHan.BasicApp.Notification.Operations.Inbox;
using XiHan.BasicApp.Notification.Operations.Messaging;
using XiHan.BasicApp.Notification.Operations.Notifications;
using XiHan.BasicApp.Notification.Operations.TelegramBots;

namespace XiHan.BasicApp.Notification;

/// <summary>
/// 通知包操作的请求 / 响应与推送载荷的序列化上下文（源生成；枚举按字符串）。
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    NumberHandling = JsonNumberHandling.AllowReadingFromString,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(PageRequest))]
[JsonSerializable(typeof(BatchResult))]
[JsonSerializable(typeof(List<Guid>))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(NotificationResponse))]
[JsonSerializable(typeof(PageResult<NotificationResponse>))]
[JsonSerializable(typeof(CreateNotificationRequest))]
[JsonSerializable(typeof(UpdateNotificationRequest))]
[JsonSerializable(typeof(NotificationIdRequest))]
[JsonSerializable(typeof(NotificationIdsRequest))]
[JsonSerializable(typeof(NotificationDeliveryResponse))]
[JsonSerializable(typeof(NotificationReadStatsResponse))]
[JsonSerializable(typeof(NotificationRecipientsRequest))]
[JsonSerializable(typeof(NotificationRecipientResponse))]
[JsonSerializable(typeof(PageResult<NotificationRecipientResponse>))]
[JsonSerializable(typeof(InboxItemResponse))]
[JsonSerializable(typeof(PageResult<InboxItemResponse>))]
[JsonSerializable(typeof(InboxShowcaseRequest))]
[JsonSerializable(typeof(InboxItemsResponse))]
[JsonSerializable(typeof(InboxIdRequest))]
[JsonSerializable(typeof(InboxIdsRequest))]
[JsonSerializable(typeof(MarkAllInboxReadRequest))]
[JsonSerializable(typeof(InboxCountResponse))]
[JsonSerializable(typeof(NotificationPushPayload))]
[JsonSerializable(typeof(MessagingIdRequest))]
[JsonSerializable(typeof(MessagingIdsRequest))]
[JsonSerializable(typeof(MessageTemplateResponse))]
[JsonSerializable(typeof(PageResult<MessageTemplateResponse>))]
[JsonSerializable(typeof(CreateMessageTemplateRequest))]
[JsonSerializable(typeof(UpdateMessageTemplateRequest))]
[JsonSerializable(typeof(EmailConfigResponse))]
[JsonSerializable(typeof(PageResult<EmailConfigResponse>))]
[JsonSerializable(typeof(CreateEmailConfigRequest))]
[JsonSerializable(typeof(UpdateEmailConfigRequest))]
[JsonSerializable(typeof(SmsConfigResponse))]
[JsonSerializable(typeof(PageResult<SmsConfigResponse>))]
[JsonSerializable(typeof(CreateSmsConfigRequest))]
[JsonSerializable(typeof(UpdateSmsConfigRequest))]
[JsonSerializable(typeof(EmailRecordResponse))]
[JsonSerializable(typeof(PageResult<EmailRecordResponse>))]
[JsonSerializable(typeof(SmsRecordResponse))]
[JsonSerializable(typeof(PageResult<SmsRecordResponse>))]
[JsonSerializable(typeof(BotConfigResponse))]
[JsonSerializable(typeof(PageResult<BotConfigResponse>))]
[JsonSerializable(typeof(CreateBotConfigRequest))]
[JsonSerializable(typeof(UpdateBotConfigRequest))]
[JsonSerializable(typeof(JsonObject))]
[JsonSerializable(typeof(TelegramBotResponse))]
[JsonSerializable(typeof(PageResult<TelegramBotResponse>))]
[JsonSerializable(typeof(CreateTelegramBotRequest))]
[JsonSerializable(typeof(UpdateTelegramBotRequest))]
public sealed partial class NotificationJsonContext : JsonSerializerContext;
