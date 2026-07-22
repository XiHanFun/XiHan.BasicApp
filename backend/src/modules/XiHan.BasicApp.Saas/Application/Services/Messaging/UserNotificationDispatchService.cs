// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Hubs;
using XiHan.Framework.Core.DependencyInjection.ServiceLifetimes;
using XiHan.Framework.Web.RealTime.Constants;
using XiHan.Framework.Web.RealTime.Services;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 当前用户通知投递服务
/// </summary>
public sealed class UserNotificationDispatchService
    : IUserNotificationDispatchService, IScopedDependency
{
    private readonly IUserInboxDomainService _userInboxDomainService;

    private readonly IRealtimeNotificationService<BasicAppNotificationHub> _realtimeNotificationService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public UserNotificationDispatchService(
        IUserInboxDomainService userInboxDomainService,
        IRealtimeNotificationService<BasicAppNotificationHub> realtimeNotificationService)
    {
        _userInboxDomainService = userInboxDomainService;
        _realtimeNotificationService = realtimeNotificationService;
    }

    /// <summary>
    /// 构造站内信 DTO
    /// </summary>
    public static UserInboxItemDto ToInboxItem(SysUserNotification userNotification, SysNotification notification)
    {
        ArgumentNullException.ThrowIfNull(userNotification);
        ArgumentNullException.ThrowIfNull(notification);

        return new UserInboxItemDto
        {
            BasicId = userNotification.BasicId,
            NotificationId = notification.BasicId,
            Title = notification.Title,
            Content = notification.Content,
            NotificationType = notification.NotificationType,
            Priority = notification.Priority,
            ContentFormat = notification.ContentFormat,
            IsMandatory = notification.IsMandatory,
            IsBanner = notification.IsBanner,
            IsPopup = notification.IsPopup,
            NotificationStatus = userNotification.NotificationStatus,
            SendTime = notification.SendTime,
            ReadTime = userNotification.ReadTime,
            ConfirmTime = userNotification.ConfirmTime,
            IsGlobal = notification.TargetType == NotificationTargetType.All,
            NeedConfirm = notification.NeedConfirm,
            Icon = notification.Icon,
            Link = notification.Link
        };
    }

    /// <inheritdoc />
    public async Task<UserInboxItemDto> DispatchToUserAsync(
        long userId,
        string title,
        string? content,
        NotificationType notificationType = NotificationType.System,
        string? businessType = null,
        long? businessId = null,
        bool needConfirm = false,
        string? link = null,
        string? icon = null,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId), "用户主键必须大于 0。");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _userInboxDomainService.DispatchToUserAsync(
            new UserInboxDispatchCommand(userId, title, content, notificationType, businessType, businessId, needConfirm, link, icon),
            cancellationToken);
        var item = ToInboxItem(result.UserNotification, result.Notification);
        await TryPushRealtimeAsync(userId, item);
        return item;
    }

    private static string ToRealtimeType(NotificationType notificationType)
    {
        return notificationType switch
        {
            NotificationType.Emergency => "Error",
            NotificationType.Security => "Warning",
            NotificationType.Business => "Success",
            _ => "Info"
        };
    }

    /// <summary>
    /// 尝试实时推送，失败不影响站内信落库
    /// </summary>
    private async Task TryPushRealtimeAsync(long userId, UserInboxItemDto item)
    {
        try
        {
            await _realtimeNotificationService.SendToUserAsync(
                userId.ToString(),
                SignalRConstants.ClientMethods.ReceiveNotification,
                new
                {
                    type = ToRealtimeType(item.NotificationType),
                    title = item.Title,
                    content = item.Content,
                    contentFormat = item.ContentFormat,
                    basicId = item.BasicId,
                    notificationId = item.NotificationId,
                    notificationType = item.NotificationType,
                    notificationStatus = item.NotificationStatus,
                    sendTime = item.SendTime
                });
        }
        catch
        {
            // 实时推送失败不应影响通知持久化与业务流程。
        }
    }
}
