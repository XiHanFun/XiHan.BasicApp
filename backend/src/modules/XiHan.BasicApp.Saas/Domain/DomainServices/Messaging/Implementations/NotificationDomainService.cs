#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:NotificationDomainService
// Guid:5e44c54f-eb7f-4742-b0f7-2b7b2149ae79
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Text.Json;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 通知领域服务实现
/// </summary>
public sealed class NotificationDomainService
    : INotificationDomainService
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public NotificationDomainService(
        INotificationRepository notificationRepository,
        IUserNotificationRepository userNotificationRepository,
        IUserRepository userRepository)
    {
        _notificationRepository = notificationRepository;
        _userNotificationRepository = userNotificationRepository;
        _userRepository = userRepository;
    }

    private readonly INotificationRepository _notificationRepository;
    private readonly IUserNotificationRepository _userNotificationRepository;
    private readonly IUserRepository _userRepository;

    /// <inheritdoc />
    public async Task<NotificationCommandResult> CreateNotificationAsync(NotificationCreateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateNotificationInput(command.NotificationType, command.TargetType, command.Title, command.Icon, command.Link, command.BusinessType, command.BusinessId, command.SendTime, command.ExpireTime, command.Remark);
        var targetUserIds = command.TargetType == NotificationTargetType.All ? Array.Empty<long>() : NormalizeUserIds(command.UserIds);
        var notification = new SysNotification
        {
            SendUserId = command.SendUserId,
            NotificationType = command.NotificationType,
            Title = Required(command.Title, 200, nameof(command.Title), "通知标题不能超过 200 个字符。"),
            Content = NormalizeNullable(command.Content),
            Icon = Optional(command.Icon, 100, nameof(command.Icon), "通知图标不能超过 100 个字符。"),
            Link = Optional(command.Link, 500, nameof(command.Link), "通知链接不能超过 500 个字符。"),
            BusinessType = Optional(command.BusinessType, 100, nameof(command.BusinessType), "业务类型不能超过 100 个字符。"),
            BusinessId = command.BusinessId,
            SendTime = command.SendTime ?? DateTimeOffset.UtcNow,
            ExpireTime = command.ExpireTime,
            TargetType = command.TargetType,
            TargetValue = command.TargetType == NotificationTargetType.All ? null : JsonSerializer.Serialize(targetUserIds),
            NeedConfirm = command.NeedConfirm,
            IsPublished = false,
            Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。")
        };

        var savedNotification = await _notificationRepository.AddAsync(notification, cancellationToken);
        if (command.PublishImmediately)
        {
            _ = await PublishInternalAsync(savedNotification, command.UserIds ?? [], command.TargetType, cancellationToken);
            savedNotification = await _notificationRepository.UpdateAsync(savedNotification, cancellationToken);
        }

        return new NotificationCommandResult(savedNotification);
    }

    /// <inheritdoc />
    public async Task DeleteNotificationAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var notification = await GetNotificationOrThrowAsync(id, cancellationToken);
        _ = await _userNotificationRepository.DeleteAsync(item => item.NotificationId == notification.BasicId, cancellationToken);
        if (!await _notificationRepository.DeleteAsync(notification, cancellationToken))
        {
            throw new InvalidOperationException("系统通知删除失败。");
        }
    }

    /// <inheritdoc />
    public async Task<NotificationPublishCommandResult> PublishNotificationAsync(NotificationPublishCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var notification = await GetNotificationOrThrowAsync(command.BasicId, cancellationToken);
        var targetType = command.TargetType ?? notification.TargetType;
        IReadOnlyList<long> targetUserIds = command.UserIds ?? [];
        if (targetType == NotificationTargetType.User && targetUserIds.Count == 0 && !string.IsNullOrWhiteSpace(notification.TargetValue))
        {
            targetUserIds = JsonSerializer.Deserialize<long[]>(notification.TargetValue) ?? [];
        }

        var recipientCount = await PublishInternalAsync(notification, targetUserIds, targetType, cancellationToken);
        _ = await _notificationRepository.UpdateAsync(notification, cancellationToken);
        return new NotificationPublishCommandResult(notification, recipientCount);
    }

    /// <inheritdoc />
    public async Task<NotificationCommandResult> UpdateNotificationAsync(NotificationUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.BasicId, "系统通知主键必须大于 0。");
        ValidateNotificationInput(command.NotificationType, command.TargetType, command.Title, command.Icon, command.Link, command.BusinessType, command.BusinessId, command.SendTime, command.ExpireTime, command.Remark);
        var targetUserIds = command.TargetType == NotificationTargetType.All ? Array.Empty<long>() : NormalizeUserIds(command.UserIds);

        var notification = await GetNotificationOrThrowAsync(command.BasicId, cancellationToken);
        if (notification.IsPublished)
        {
            throw new InvalidOperationException("已发布通知不能直接更新，请重新创建通知。");
        }

        notification.NotificationType = command.NotificationType;
        notification.Title = Required(command.Title, 200, nameof(command.Title), "通知标题不能超过 200 个字符。");
        notification.Content = NormalizeNullable(command.Content);
        notification.Icon = Optional(command.Icon, 100, nameof(command.Icon), "通知图标不能超过 100 个字符。");
        notification.Link = Optional(command.Link, 500, nameof(command.Link), "通知链接不能超过 500 个字符。");
        notification.BusinessType = Optional(command.BusinessType, 100, nameof(command.BusinessType), "业务类型不能超过 100 个字符。");
        notification.BusinessId = command.BusinessId;
        notification.SendTime = command.SendTime ?? notification.SendTime;
        notification.ExpireTime = command.ExpireTime;
        notification.TargetType = command.TargetType;
        notification.TargetValue = command.TargetType == NotificationTargetType.All ? null : JsonSerializer.Serialize(targetUserIds);
        notification.NeedConfirm = command.NeedConfirm;
        notification.Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。");

        return new NotificationCommandResult(await _notificationRepository.UpdateAsync(notification, cancellationToken));
    }

    private static long[] NormalizeUserIds(IReadOnlyList<long>? inputUserIds)
    {
        var source = inputUserIds ?? [];
        var userIds = source
            .Where(userId => userId > 0)
            .Distinct()
            .ToArray();
        if (userIds.Length != source.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(inputUserIds), "接收用户主键必须大于 0 且不能重复。");
        }

        return userIds;
    }

    private static void ValidateNotificationInput(
        NotificationType notificationType,
        NotificationTargetType targetType,
        string title,
        string? icon,
        string? link,
        string? businessType,
        long? businessId,
        DateTimeOffset? sendTime,
        DateTimeOffset? expireTime,
        string? remark)
    {
        EnsureEnum(notificationType, nameof(notificationType));
        EnsureEnum(targetType, nameof(targetType));
        if (targetType is not NotificationTargetType.All and not NotificationTargetType.User)
        {
            throw new NotSupportedException("系统通知暂只支持全员或指定用户目标。");
        }

        _ = Required(title, 200, nameof(title), "通知标题不能超过 200 个字符。");
        _ = Optional(icon, 100, nameof(icon), "通知图标不能超过 100 个字符。");
        _ = Optional(link, 500, nameof(link), "通知链接不能超过 500 个字符。");
        _ = Optional(businessType, 100, nameof(businessType), "业务类型不能超过 100 个字符。");
        _ = Optional(remark, 500, nameof(remark), "备注不能超过 500 个字符。");
        EnsureOptionalId(businessId, nameof(businessId), "业务主键必须大于 0。");
        if (sendTime.HasValue && expireTime.HasValue && expireTime.Value <= sendTime.Value)
        {
            throw new ArgumentOutOfRangeException(nameof(expireTime), "过期时间必须晚于发送时间。");
        }
    }

    private async Task<SysNotification> GetNotificationOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        EnsureId(id, "系统通知主键必须大于 0。");
        return await _notificationRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("系统通知不存在。");
    }

    private async Task<int> PublishInternalAsync(SysNotification notification, IReadOnlyList<long> inputUserIds, NotificationTargetType targetType, CancellationToken cancellationToken)
    {
        if (notification.IsPublished)
        {
            throw new InvalidOperationException("系统通知已发布。");
        }

        var recipientIds = await ResolveRecipientIdsAsync(inputUserIds, targetType, cancellationToken);
        if (recipientIds.Count == 0)
        {
            throw new InvalidOperationException("系统通知没有有效接收用户。");
        }

        var existingItems = await _userNotificationRepository.GetListAsync(item => item.NotificationId == notification.BasicId, cancellationToken);
        var existingUserIds = existingItems.Select(item => item.UserId).ToHashSet();
        var addItems = recipientIds
            .Where(userId => !existingUserIds.Contains(userId))
            .Select(userId => new SysUserNotification
            {
                NotificationId = notification.BasicId,
                UserId = userId,
                NotificationStatus = NotificationStatus.Unread
            })
            .ToList();
        if (addItems.Count > 0)
        {
            _ = await _userNotificationRepository.AddRangeAsync(addItems, cancellationToken);
        }

        notification.IsPublished = true;
        notification.SendTime = notification.SendTime == default ? DateTimeOffset.UtcNow : notification.SendTime;
        notification.TargetType = targetType;
        notification.TargetValue = targetType == NotificationTargetType.All ? null : JsonSerializer.Serialize(recipientIds);
        return recipientIds.Count;
    }

    private async Task<IReadOnlyCollection<long>> ResolveRecipientIdsAsync(IReadOnlyList<long> inputUserIds, NotificationTargetType targetType, CancellationToken cancellationToken)
    {
        if (targetType == NotificationTargetType.All)
        {
            var users = await _userRepository.GetListAsync(user => user.Status == EnableStatus.Enabled, cancellationToken);
            return users.Select(user => user.BasicId).Distinct().ToArray();
        }

        if (targetType != NotificationTargetType.User)
        {
            throw new NotSupportedException("系统通知发布暂只支持全员或指定用户目标。");
        }

        return NormalizeUserIds(inputUserIds);
    }

    private static void EnsureEnum<TEnum>(TEnum value, string paramName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(paramName, "枚举值无效。");
        }
    }

    private static void EnsureId(long id, string message)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), message);
        }
    }

    private static void EnsureOptionalId(long? id, string paramName, string message)
    {
        if (id is <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? Optional(string? value, int maxLength, string paramName, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }

        return normalized;
    }

    private static string Required(string? value, int maxLength, string paramName, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }

        return normalized;
    }
}
