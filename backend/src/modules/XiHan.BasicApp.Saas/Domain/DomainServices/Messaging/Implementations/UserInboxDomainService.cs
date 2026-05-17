#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserInboxDomainService
// Guid:5d38d787-f031-49bc-b8f7-e66c0d9c4271
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 用户站内信领域服务实现
/// </summary>
public sealed class UserInboxDomainService
    : IUserInboxDomainService
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public UserInboxDomainService(
        INotificationRepository notificationRepository,
        IUserNotificationRepository userNotificationRepository)
    {
        _notificationRepository = notificationRepository;
        _userNotificationRepository = userNotificationRepository;
    }

    private readonly INotificationRepository _notificationRepository;
    private readonly IUserNotificationRepository _userNotificationRepository;

    /// <inheritdoc />
    public async Task ConfirmAsync(long userNotificationId, long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var userNotification = await GetUserNotificationOrThrowAsync(userNotificationId, userId, cancellationToken);
        var notification = await _notificationRepository.GetByIdAsync(userNotification.NotificationId, cancellationToken)
            ?? throw new InvalidOperationException("通知不存在。");
        var now = DateTimeOffset.UtcNow;
        MarkRead(userNotification, now);
        if (notification.NeedConfirm)
        {
            userNotification.ConfirmTime ??= now;
        }

        _ = await _userNotificationRepository.UpdateAsync(userNotification, cancellationToken);
    }

    /// <inheritdoc />
    public async Task MarkAllReadAsync(long userId, CancellationToken cancellationToken = default)
    {
        EnsureUserId(userId);
        cancellationToken.ThrowIfCancellationRequested();

        var userNotifications = await _userNotificationRepository.GetListAsync(
            item => item.UserId == userId && item.NotificationStatus == NotificationStatus.Unread,
            cancellationToken);
        if (userNotifications.Count == 0)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var userNotification in userNotifications)
        {
            MarkRead(userNotification, now);
        }

        _ = await _userNotificationRepository.UpdateRangeAsync(userNotifications, cancellationToken);
    }

    /// <inheritdoc />
    public async Task MarkReadAsync(long userNotificationId, long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var userNotification = await GetUserNotificationOrThrowAsync(userNotificationId, userId, cancellationToken);
        MarkRead(userNotification, DateTimeOffset.UtcNow);
        _ = await _userNotificationRepository.UpdateAsync(userNotification, cancellationToken);
    }

    private static void MarkRead(SysUserNotification userNotification, DateTimeOffset now)
    {
        if (userNotification.NotificationStatus == NotificationStatus.Unread)
        {
            userNotification.NotificationStatus = NotificationStatus.Read;
            userNotification.ReadTime ??= now;
        }
    }

    private async Task<SysUserNotification> GetUserNotificationOrThrowAsync(long id, long userId, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "用户通知主键必须大于 0。");
        }

        EnsureUserId(userId);
        var userNotification = await _userNotificationRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("用户通知不存在。");
        if (userNotification.UserId != userId || userNotification.NotificationStatus == NotificationStatus.Deleted)
        {
            throw new InvalidOperationException("用户通知不存在。");
        }

        return userNotification;
    }

    private static void EnsureUserId(long userId)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId), "用户主键必须大于 0。");
        }
    }
}
