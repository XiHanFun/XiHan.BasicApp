#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserInboxAppService
// Guid:c7e61e82-8a58-45bb-ae47-25f24f3eea06
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/04 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using SqlSugar;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 当前用户站内信应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "用户站内信")]
public sealed class UserInboxAppService
    : SaasApplicationService, IUserInboxAppService
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public UserInboxAppService(
        INotificationRepository notificationRepository,
        IUserNotificationRepository userNotificationRepository,
        ICurrentUser currentUser)
    {
        _notificationRepository = notificationRepository;
        _userNotificationRepository = userNotificationRepository;
        _currentUser = currentUser;
    }

    private const int MaxInboxItems = 100;
    private readonly INotificationRepository _notificationRepository;
    private readonly IUserNotificationRepository _userNotificationRepository;
    private readonly ICurrentUser _currentUser;

    /// <inheritdoc />
    public async Task<List<UserInboxItemDto>> GetListAsync(bool unreadOnly = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var userId = GetCurrentUserIdOrThrow();
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
                    SqlFunc.IsNull(item.ExpireTime, expireFallback) > now,
            cancellationToken);
        var notificationMap = notifications.ToDictionary(item => item.BasicId);

        return [.. userNotifications
            .Where(item => notificationMap.ContainsKey(item.NotificationId))
            .Select(item => UserNotificationDispatchService.ToInboxItem(item, notificationMap[item.NotificationId]))
            .Where(item => !unreadOnly || item.NotificationStatus == (int)NotificationStatus.Unread || (item.NeedConfirm && !item.ConfirmTime.HasValue))
            .OrderByDescending(item => item.SendTime)
            .ThenByDescending(item => item.BasicId)
            .Take(MaxInboxItems)];
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    public async Task MarkReadAsync(UserInboxUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var userNotification = await GetCurrentUserNotificationOrThrowAsync(input.BasicId, cancellationToken);
        MarkRead(userNotification, DateTimeOffset.UtcNow);
        _ = await _userNotificationRepository.UpdateAsync(userNotification, cancellationToken);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    public async Task MarkAllReadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var userId = GetCurrentUserIdOrThrow();
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
    [UnitOfWork(true)]
    public async Task ConfirmAsync(UserInboxUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var userNotification = await GetCurrentUserNotificationOrThrowAsync(input.BasicId, cancellationToken);
        var notification = await _notificationRepository.GetByIdAsync(userNotification.NotificationId, cancellationToken)
            ?? throw new InvalidOperationException("通知不存在。");
        if (!notification.NeedConfirm)
        {
            MarkRead(userNotification, DateTimeOffset.UtcNow);
        }
        else
        {
            var now = DateTimeOffset.UtcNow;
            MarkRead(userNotification, now);
            userNotification.ConfirmTime ??= now;
        }

        _ = await _userNotificationRepository.UpdateAsync(userNotification, cancellationToken);
    }

    private long GetCurrentUserIdOrThrow()
    {
        return _currentUser.UserId ?? throw new InvalidOperationException("当前用户未登录。");
    }

    private async Task<SysUserNotification> GetCurrentUserNotificationOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserIdOrThrow();
        var userNotification = await _userNotificationRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("用户通知不存在。");
        if (userNotification.UserId != userId || userNotification.NotificationStatus == NotificationStatus.Deleted)
        {
            throw new InvalidOperationException("用户通知不存在。");
        }

        return userNotification;
    }

    private static void MarkRead(SysUserNotification userNotification, DateTimeOffset now)
    {
        if (userNotification.NotificationStatus == NotificationStatus.Unread)
        {
            userNotification.NotificationStatus = NotificationStatus.Read;
            userNotification.ReadTime ??= now;
        }
    }
}
