// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json.Serialization;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Notification.Domain.Services;
using XiHan.BasicApp.Notification.Operations.Inbox;
using XiHan.BasicApp.Notification.Operations.Notifications;

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
public sealed partial class NotificationJsonContext : JsonSerializerContext;
