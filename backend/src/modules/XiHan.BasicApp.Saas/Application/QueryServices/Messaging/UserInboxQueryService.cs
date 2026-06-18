#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserInboxQueryService
// Guid:df2fb8f4-1916-4f89-a2ac-31ea1763bb2f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
}
