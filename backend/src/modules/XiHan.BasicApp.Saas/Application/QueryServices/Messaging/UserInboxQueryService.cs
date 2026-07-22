// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 用户站内信查询服务实现
/// </summary>
public sealed class UserInboxQueryService
    : IUserInboxQueryService
{
    private const int MaxInboxItems = 100;

    private readonly INotificationRepository _notificationRepository;

    private readonly IUserNotificationRepository _userNotificationRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public UserInboxQueryService(
        INotificationRepository notificationRepository,
        IUserNotificationRepository userNotificationRepository)
    {
        _notificationRepository = notificationRepository;
        _userNotificationRepository = userNotificationRepository;
    }

    /// <inheritdoc />
    public async Task<List<UserInboxItemDto>> GetListAsync(long userId, bool unreadOnly = false, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId), "用户主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();
        var now = DateTimeOffset.UtcNow;
        var expireFallback = now.AddYears(100);

        var userNotifications = unreadOnly
            ? await _userNotificationRepository.GetListAsync(
                item => item.UserId == userId &&
                        item.NotificationStatus != NotificationStatus.Deleted &&
                        (item.NotificationStatus == NotificationStatus.Unread ||
                         SqlFunc.IsNull(item.ConfirmTime, expireFallback) == expireFallback),
                cancellationToken)
            : await _userNotificationRepository.GetListAsync(
                item => item.UserId == userId &&
                        item.NotificationStatus != NotificationStatus.Deleted,
                cancellationToken);
        if (userNotifications.Count == 0)
        {
            return [];
        }

        userNotifications = [.. userNotifications
            .OrderByDescending(item => item.CreatedTime)
            .Take(MaxInboxItems * 3)];

        var notificationIds = userNotifications
            .Select(item => item.NotificationId)
            .Distinct()
            .ToArray();
        var notifications = await _notificationRepository.GetListAsync(
            item => SqlFunc.ContainsArray(notificationIds, item.BasicId) &&
                    item.IsPublished &&
                    SqlFunc.IsNull(item.ExpirationTime, expireFallback) > now,
            cancellationToken);
        var notificationMap = notifications.ToDictionary(item => item.BasicId);

        return [.. userNotifications
            .Where(item => notificationMap.ContainsKey(item.NotificationId))
            .Select(item => UserNotificationDispatchService.ToInboxItem(item, notificationMap[item.NotificationId]))
            .Where(item => !unreadOnly || item.NotificationStatus == NotificationStatus.Unread || (item.NeedConfirm && !item.ConfirmTime.HasValue))
            .OrderByDescending(item => item.SendTime)
            .ThenByDescending(item => item.BasicId)
            .Take(MaxInboxItems)];
    }

    /// <inheritdoc />
    public async Task<List<UserInboxItemDto>> GetMandatoryUnreadAsync(long userId, CancellationToken cancellationToken = default)
    {
        EnsureUser(userId);
        cancellationToken.ThrowIfCancellationRequested();
        var userNotifications = await _userNotificationRepository.GetListAsync(
            item => item.UserId == userId && item.NotificationStatus == NotificationStatus.Unread,
            cancellationToken);
        return await ResolveActiveAsync(userNotifications, notification => notification.IsMandatory, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<UserInboxItemDto>> GetBannerAsync(long userId, CancellationToken cancellationToken = default)
    {
        EnsureUser(userId);
        cancellationToken.ThrowIfCancellationRequested();
        var userNotifications = await _userNotificationRepository.GetListAsync(
            item => item.UserId == userId && item.NotificationStatus == NotificationStatus.Unread,
            cancellationToken);
        return await ResolveActiveAsync(userNotifications, notification => notification.IsBanner, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<UserInboxItemDto>> GetPopupAsync(long userId, CancellationToken cancellationToken = default)
    {
        EnsureUser(userId);
        cancellationToken.ThrowIfCancellationRequested();
        var popupFallback = DateTimeOffset.UtcNow.AddYears(100);
        // PopupShownTime 为空（尚未弹过）且未删除
        var userNotifications = await _userNotificationRepository.GetListAsync(
            item => item.UserId == userId &&
                    item.NotificationStatus != NotificationStatus.Deleted &&
                    SqlFunc.IsNull(item.PopupShownTime, popupFallback) == popupFallback,
            cancellationToken);
        return await ResolveActiveAsync(userNotifications, notification => notification.IsPopup, cancellationToken);
    }

    private static void EnsureUser(long userId)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId), "用户主键必须大于 0。");
        }
    }

    /// <summary>
    /// 将用户通知列表解析为「已发布 + 在生效窗口内 + 满足通知标志」的站内信项
    /// </summary>
    private async Task<List<UserInboxItemDto>> ResolveActiveAsync(
        IReadOnlyList<SysUserNotification> userNotifications,
        Func<SysNotification, bool> notificationFlagFilter,
        CancellationToken cancellationToken)
    {
        if (userNotifications.Count == 0)
        {
            return [];
        }

        var now = DateTimeOffset.UtcNow;
        var startFallback = now.AddYears(-100);
        var expireFallback = now.AddYears(100);
        var ordered = userNotifications
            .OrderByDescending(item => item.CreatedTime)
            .Take(MaxInboxItems * 3)
            .ToList();
        var notificationIds = ordered.Select(item => item.NotificationId).Distinct().ToArray();
        var notifications = await _notificationRepository.GetListAsync(
            item => SqlFunc.ContainsArray(notificationIds, item.BasicId) &&
                    item.IsPublished &&
                    SqlFunc.IsNull(item.StartTime, startFallback) <= now &&
                    SqlFunc.IsNull(item.ExpirationTime, expireFallback) > now,
            cancellationToken);
        var notificationMap = notifications.Where(notificationFlagFilter).ToDictionary(item => item.BasicId);

        return [.. ordered
            .Where(item => notificationMap.ContainsKey(item.NotificationId))
            .Select(item => UserNotificationDispatchService.ToInboxItem(item, notificationMap[item.NotificationId]))
            .OrderByDescending(item => item.SendTime)
            .ThenByDescending(item => item.BasicId)
            .Take(MaxInboxItems)];
    }
}
