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
/// 照 4.x 的待办提醒：在调用所在的一侧建一条指定用户的「待办」通知（纯文本、普通优先级、只走站内）并立即发布，
/// 收件人按站内偏好过滤，实时推送在提交之后发出（<see cref="NotificationService.PublishAsync"/>）
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

        var draft = new NotificationDraft(
            message.Title,
            message.Content,
            NotificationContentFormat.Text,
            NotificationType.Todo,
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
            ? await notifications.PublishAsync(context, notification).ConfigureAwait(false)
            : created.Error!;
    }
}
