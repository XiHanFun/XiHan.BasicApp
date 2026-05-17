#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserNotificationDispatchService
// Guid:fbd1c544-f77b-48c5-91df-aa6241ad849b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/04 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
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
    private readonly INotificationRepository _notificationRepository;

    private readonly IUserNotificationRepository _userNotificationRepository;

    private readonly IRealtimeNotificationService<BasicAppNotificationHub> _realtimeNotificationService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public UserNotificationDispatchService(
        INotificationRepository notificationRepository,
        IUserNotificationRepository userNotificationRepository,
        IRealtimeNotificationService<BasicAppNotificationHub> realtimeNotificationService)
    {
        _notificationRepository = notificationRepository;
        _userNotificationRepository = userNotificationRepository;
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
            NotificationType = (int)notification.NotificationType,
            NotificationStatus = (int)userNotification.NotificationStatus,
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

        var now = DateTimeOffset.UtcNow;
        var notification = await _notificationRepository.AddAsync(new SysNotification
        {
            SendUserId = null,
            NotificationType = notificationType,
            Title = NormalizeRequired(title, 200),
            Content = NormalizeNullable(content, 4000),
            Icon = NormalizeNullable(icon, 100),
            Link = NormalizeNullable(link, 500),
            BusinessType = NormalizeNullable(businessType, 50),
            BusinessId = businessId,
            SendTime = now,
            ExpireTime = null,
            TargetType = NotificationTargetType.User,
            TargetValue = $"[{userId}]",
            NeedConfirm = needConfirm,
            IsPublished = true
        }, cancellationToken);

        var userNotification = await _userNotificationRepository.AddAsync(new SysUserNotification
        {
            NotificationId = notification.BasicId,
            UserId = userId,
            NotificationStatus = NotificationStatus.Unread,
            ReadTime = null,
            ConfirmTime = null
        }, cancellationToken);

        var item = ToInboxItem(userNotification, notification);
        await TryPushRealtimeAsync(userId, item);
        return item;
    }

    private static string ToRealtimeType(int notificationType)
    {
        return notificationType switch
        {
            (int)NotificationType.Warning => "Warning",
            (int)NotificationType.Error => "Error",
            (int)NotificationType.User => "Success",
            _ => "Info"
        };
    }

    private static string NormalizeRequired(string value, int maxLength)
    {
        var normalized = value.Trim();
        return normalized.Length > maxLength ? normalized[..maxLength] : normalized;
    }

    private static string? NormalizeNullable(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length > maxLength ? normalized[..maxLength] : normalized;
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
