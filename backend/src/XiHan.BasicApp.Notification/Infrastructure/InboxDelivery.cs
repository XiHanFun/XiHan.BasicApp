// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Notification;
using XiHan.BasicApp.Notification.Domain.Entities;
using XiHan.BasicApp.Notification.Domain.Services;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Notification.Infrastructure;

/// <summary>
/// <see cref="IInboxDelivery"/> 的实现
/// </summary>
/// <remarks>
/// 照 4.x 的待办提醒与账号通知：在调用所在的一侧建一条指定用户的通知（纯文本、普通优先级、只走站内）并立即发布，实时推送在提交之后发出（<see cref="NotificationService.PublishAsync"/>）
/// 待办按收件人的站内偏好过滤；账号安全不看偏好，4.x 的账号通知同样不经偏好门控
/// </remarks>
/// <param name="notifications">通知域服务</param>
public sealed class InboxDelivery(NotificationService notifications) : IInboxDelivery
{
    /// <inheritdoc />
    public async Task<Result<int>> DeliverAsync(InvocationContext context, InboxMessage message)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(message);
        var recipients = message.UserIds.Where(static id => id != Guid.Empty).Distinct().ToList();
        if (recipients.Count == 0)
        {
            return 0;
        }

        var security = message.Kind == InboxMessageKind.Security;
        var draft = new NotificationDraft(
            message.Title,
            message.Content,
            NotificationContentFormat.Text,
            security ? NotificationType.Security : NotificationType.Todo,
            NotificationPriority.Normal,
            NotificationTargetType.User,
            recipients,
            Icon: null,
            message.Link,
            message.BusinessType,
            message.BusinessId,
            NeedConfirm: false,
            IsMandatory: false,
            IsBanner: false,
            IsPopup: false,
            StartsAt: null,
            ExpiresAt: null,
            Remark: null,
            MessageChannels.InApp);
        var created = await notifications.CreateAsync(context, draft).ConfigureAwait(false);
        return created.TryGetValue(out var notification)
            ? await notifications.PublishAsync(context, notification, ignorePreferences: security).ConfigureAwait(false)
            : created.Error!;
    }
}
