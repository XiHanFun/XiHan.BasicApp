// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 用户站内信领域服务实现
/// </summary>
public sealed class UserInboxDomainService
    : IUserInboxDomainService
{
    private readonly INotificationRepository _notificationRepository;

    private readonly IUserNotificationRepository _userNotificationRepository;

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

    /// <inheritdoc />
    public async Task<UserInboxDispatchResult> DispatchToUserAsync(UserInboxDispatchCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        EnsureUserId(command.UserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Title);
        cancellationToken.ThrowIfCancellationRequested();

        var now = DateTimeOffset.UtcNow;
        var notification = await _notificationRepository.AddAsync(new SysNotification
        {
            SendUserId = null,
            NotificationType = command.NotificationType,
            Title = NormalizeRequired(command.Title, 200),
            Content = NormalizeNullable(command.Content, 4000),
            Icon = NormalizeNullable(command.Icon, 100),
            Link = NormalizeNullable(command.Link, 500),
            BusinessType = NormalizeNullable(command.BusinessType, 50),
            BusinessId = command.BusinessId,
            SendTime = now,
            ExpirationTime = null,
            TargetType = NotificationTargetType.User,
            TargetValue = $"[{command.UserId}]",
            NeedConfirm = command.NeedConfirm,
            // 自动派生的账号通知均为纯文本（登录/登出/改密等），按纯文本渲染，避免被当 Markdown
            ContentFormat = NotificationContentFormat.Text,
            IsPublished = true
        }, cancellationToken);

        var userNotification = await _userNotificationRepository.AddAsync(new SysUserNotification
        {
            NotificationId = notification.BasicId,
            UserId = command.UserId,
            NotificationStatus = NotificationStatus.Unread,
            ReadTime = null,
            ConfirmTime = null
        }, cancellationToken);

        return new UserInboxDispatchResult(notification, userNotification);
    }

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

        _ = await AsSelfWriteAsync(() => _userNotificationRepository.UpdateAsync(userNotification, cancellationToken));
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

        _ = await AsSelfWriteAsync(() => _userNotificationRepository.UpdateRangeAsync(userNotifications, cancellationToken));
    }

    /// <inheritdoc />
    public async Task MarkReadAsync(long userNotificationId, long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var userNotification = await GetUserNotificationOrThrowAsync(userNotificationId, userId, cancellationToken);
        MarkRead(userNotification, DateTimeOffset.UtcNow);
        _ = await AsSelfWriteAsync(() => _userNotificationRepository.UpdateAsync(userNotification, cancellationToken));
    }

    /// <inheritdoc />
    public async Task MarkPopupShownAsync(long userNotificationId, long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var userNotification = await GetUserNotificationOrThrowAsync(userNotificationId, userId, cancellationToken);
        // 仅弹一次：已弹过不重复写
        if (userNotification.PopupShownTime is null)
        {
            userNotification.PopupShownTime = DateTimeOffset.UtcNow;
            _ = await AsSelfWriteAsync(() => _userNotificationRepository.UpdateAsync(userNotification, cancellationToken));
        }
    }

    /// <summary>
    /// 在写路径租户边界豁免作用域内执行收件行自有写入
    /// </summary>
    /// <remarks>
    /// 收件行（SysUserNotification）按 UserId 归属：平台态发布的全局公告收件行 TenantId=0，
    /// 租户用户已读/确认/弹窗登记写自己的收件行是合法路径，须经 <see cref="TenantWriteGuard"/> 显式豁免。
    /// 本服务全部写入均先经 GetUserNotificationOrThrowAsync/UserId 条件锁定为当前用户自有行。
    /// </remarks>
    private static async Task<T> AsSelfWriteAsync<T>(Func<Task<T>> write)
    {
        using (TenantWriteGuard.Suppress())
        {
            return await write();
        }
    }

    private static void MarkRead(SysUserNotification userNotification, DateTimeOffset now)
    {
        if (userNotification.NotificationStatus == NotificationStatus.Unread)
        {
            userNotification.NotificationStatus = NotificationStatus.Read;
            userNotification.ReadTime ??= now;
        }
    }

    private static void EnsureUserId(long userId)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId), "用户主键必须大于 0。");
        }
    }

    private static string ToMaxLength(string value, int maxLength)
    {
        return value.Length > maxLength ? value[..maxLength] : value;
    }

    private static string? NormalizeNullable(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return ToMaxLength(value.Trim(), maxLength);
    }

    private static string NormalizeRequired(string value, int maxLength)
    {
        return ToMaxLength(value.Trim(), maxLength);
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
}
