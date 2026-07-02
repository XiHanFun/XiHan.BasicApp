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
    /// 全部合法投递渠道位（用于 [Flags] 组合校验，防未定义位入库）
    /// </summary>
    private const MessageChannel AllDeliveryChannels =
        MessageChannel.SiteNotification | MessageChannel.Email | MessageChannel.Sms | MessageChannel.Bot;

    private readonly INotificationRepository _notificationRepository;

    private readonly IUserNotificationRepository _userNotificationRepository;

    private readonly IUserRepository _userRepository;

    private readonly IUserRoleRepository _userRoleRepository;

    private readonly IUserDepartmentRepository _userDepartmentRepository;

    private readonly IUserNotificationPreferenceRepository _preferenceRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public NotificationDomainService(
        INotificationRepository notificationRepository,
        IUserNotificationRepository userNotificationRepository,
        IUserRepository userRepository,
        IUserRoleRepository userRoleRepository,
        IUserDepartmentRepository userDepartmentRepository,
        IUserNotificationPreferenceRepository preferenceRepository)
    {
        _notificationRepository = notificationRepository;
        _userNotificationRepository = userNotificationRepository;
        _userRepository = userRepository;
        _userRoleRepository = userRoleRepository;
        _userDepartmentRepository = userDepartmentRepository;
        _preferenceRepository = preferenceRepository;
    }

    /// <inheritdoc />
    public async Task<NotificationCommandResult> CreateNotificationAsync(NotificationCreateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateNotificationInput(command.NotificationType, command.TargetType, command.Title, command.Icon, command.Link, command.BusinessType, command.BusinessId, command.SendTime, command.ExpirationTime, command.Remark);
        var targetUserIds = command.TargetType == NotificationTargetType.All ? [] : NormalizeUserIds(command.UserIds);
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
            ExpirationTime = command.ExpirationTime,
            TargetType = command.TargetType,
            TargetValue = command.TargetType == NotificationTargetType.All ? null : JsonSerializer.Serialize(targetUserIds),
            NeedConfirm = command.NeedConfirm,
            Priority = command.Priority,
            ContentFormat = command.ContentFormat,
            DeliveryChannels = EnsureDeliveryChannels(command.DeliveryChannels),
            StartTime = command.StartTime,
            IsMandatory = command.IsMandatory,
            IsBanner = command.IsBanner,
            IsPopup = command.IsPopup,
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
        // 定向类型(用户/角色/部门)未显式传目标时，从 TargetValue 还原原目标 ID
        if (targetType is NotificationTargetType.User or NotificationTargetType.Role or NotificationTargetType.Department
            && targetUserIds.Count == 0 && !string.IsNullOrWhiteSpace(notification.TargetValue))
        {
            targetUserIds = JsonSerializer.Deserialize<long[]>(notification.TargetValue) ?? [];
        }

        var recipientCount = await PublishInternalAsync(notification, targetUserIds, targetType, cancellationToken);
        _ = await _notificationRepository.UpdateAsync(notification, cancellationToken);
        return new NotificationPublishCommandResult(notification, recipientCount);
    }

    /// <inheritdoc />
    public async Task<NotificationChannelRecipientsResult> ResolveChannelRecipientsAsync(SysNotification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        cancellationToken.ThrowIfCancellationRequested();

        var needEmail = notification.DeliveryChannels.HasFlag(MessageChannel.Email);
        var needSms = notification.DeliveryChannels.HasFlag(MessageChannel.Sms);
        if (!needEmail && !needSms)
        {
            return NotificationChannelRecipientsResult.Empty;
        }

        // 从持久化的 TargetValue 还原目标 ID（全员为 null → 空数组），复用站内信同一解析链
        long[] targetIds = string.IsNullOrWhiteSpace(notification.TargetValue)
            ? []
            : JsonSerializer.Deserialize<long[]>(notification.TargetValue) ?? [];
        var recipientIds = await ResolveRecipientIdsAsync(targetIds, notification.TargetType, cancellationToken);
        if (recipientIds.Count == 0)
        {
            return NotificationChannelRecipientsResult.Empty;
        }

        IReadOnlyCollection<long> emailUserIds = needEmail
            ? await FilterByPreferenceAsync(recipientIds, notification, MessageChannel.Email, cancellationToken)
            : [];
        IReadOnlyCollection<long> smsUserIds = needSms
            ? await FilterByPreferenceAsync(recipientIds, notification, MessageChannel.Sms, cancellationToken)
            : [];
        return new NotificationChannelRecipientsResult(emailUserIds, smsUserIds);
    }

    /// <inheritdoc />
    public async Task<NotificationCommandResult> UpdateNotificationAsync(NotificationUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.BasicId, "系统通知主键必须大于 0。");
        ValidateNotificationInput(command.NotificationType, command.TargetType, command.Title, command.Icon, command.Link, command.BusinessType, command.BusinessId, command.SendTime, command.ExpirationTime, command.Remark);
        var targetUserIds = command.TargetType == NotificationTargetType.All ? [] : NormalizeUserIds(command.UserIds);

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
        notification.ExpirationTime = command.ExpirationTime;
        notification.TargetType = command.TargetType;
        notification.TargetValue = command.TargetType == NotificationTargetType.All ? null : JsonSerializer.Serialize(targetUserIds);
        notification.NeedConfirm = command.NeedConfirm;
        notification.Priority = command.Priority;
        notification.ContentFormat = command.ContentFormat;
        notification.DeliveryChannels = EnsureDeliveryChannels(command.DeliveryChannels);
        notification.StartTime = command.StartTime;
        notification.IsMandatory = command.IsMandatory;
        notification.IsBanner = command.IsBanner;
        notification.IsPopup = command.IsPopup;
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

    /// <summary>
    /// 校验投递渠道组合：必含站内信位（前端固定勾选），且不得携带未定义渠道位（[Flags] 组合无法用 Enum.IsDefined 校验）
    /// </summary>
    private static MessageChannel EnsureDeliveryChannels(MessageChannel channels)
    {
        if ((channels & ~AllDeliveryChannels) != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(channels), "投递渠道包含未定义的渠道位。");
        }

        if (!channels.HasFlag(MessageChannel.SiteNotification))
        {
            throw new ArgumentOutOfRangeException(nameof(channels), "投递渠道必须包含站内信。");
        }

        return channels;
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

        // 偏好门控（强制/紧急不受门控）；门控后可能少于解析结果，但仅当目标解析为 0 才视为无效
        var deliverIds = await FilterByPreferenceAsync(recipientIds, notification, MessageChannel.SiteNotification, cancellationToken);

        var existingItems = await _userNotificationRepository.GetListAsync(item => item.NotificationId == notification.BasicId, cancellationToken);
        var existingUserIds = existingItems.Select(item => item.UserId).ToHashSet();
        var addItems = deliverIds
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
        // TargetValue 存「目标 ID」(角色/部门/用户)，全员为 null；供重新发布时还原目标
        notification.TargetValue = targetType == NotificationTargetType.All ? null : JsonSerializer.Serialize(NormalizeUserIds(inputUserIds));
        return deliverIds.Count;
    }

    /// <summary>
    /// 解析收件人：按目标类型把「目标 ID（全员无 / 角色 ID / 部门 ID / 用户 ID）」展开为去重用户 ID 集合
    /// </summary>
    private async Task<IReadOnlyCollection<long>> ResolveRecipientIdsAsync(IReadOnlyList<long> inputTargetIds, NotificationTargetType targetType, CancellationToken cancellationToken)
    {
        switch (targetType)
        {
            case NotificationTargetType.All:
                {
                    var users = await _userRepository.GetListAsync(user => user.Status == EnableStatus.Enabled, cancellationToken);
                    return users.Select(user => user.BasicId).Distinct().ToArray();
                }

            case NotificationTargetType.Role:
                {
                    var roleIds = NormalizeUserIds(inputTargetIds);
                    if (roleIds.Length == 0)
                    {
                        return [];
                    }

                    var userRoles = await _userRoleRepository.GetListAsync(userRole => roleIds.Contains(userRole.RoleId), cancellationToken);
                    return userRoles.Select(userRole => userRole.UserId).Distinct().ToArray();
                }

            case NotificationTargetType.Department:
                {
                    var departmentIds = NormalizeUserIds(inputTargetIds);
                    if (departmentIds.Length == 0)
                    {
                        return [];
                    }

                    var userIds = await _userDepartmentRepository.GetUserIdsByDepartmentIdsAsync(departmentIds, cancellationToken);
                    return userIds.Distinct().ToArray();
                }

            case NotificationTargetType.User:
            default:
                return NormalizeUserIds(inputTargetIds);
        }
    }

    /// <summary>
    /// 偏好门控：按用户对应渠道偏好（渠道 + 类型开关）过滤收件人；强制阅读 / 紧急通知一律送达，不受门控
    /// </summary>
    /// <remarks>
    /// 渠道开关取用户偏好中该渠道的独立布尔列（站内信/邮箱/短信/机器人），豁免语义各渠道一致；
    /// 站内信发布链路传 <see cref="MessageChannel.SiteNotification"/>，后续渠道扇出（如机器人）复用本门控传对应渠道。
    /// </remarks>
    private async Task<IReadOnlyCollection<long>> FilterByPreferenceAsync(IReadOnlyCollection<long> userIds, SysNotification notification, MessageChannel channel, CancellationToken cancellationToken)
    {
        if (userIds.Count == 0 || notification.IsMandatory || notification.NotificationType == NotificationType.Emergency)
        {
            return userIds;
        }

        var preferences = await _preferenceRepository.GetListAsync(preference => userIds.Contains(preference.UserId), cancellationToken);
        var preferenceMap = preferences.ToDictionary(preference => preference.UserId);
        return userIds.Where(userId =>
        {
            // 无偏好记录 = 默认全收
            if (!preferenceMap.TryGetValue(userId, out var preference))
            {
                return true;
            }

            // 用户关闭该接收渠道则不投递（机器人与其它渠道同语义）
            if (!IsChannelEnabled(preference, channel))
            {
                return false;
            }

            return notification.NotificationType switch
            {
                NotificationType.Security => preference.TypeSecurity,
                NotificationType.Todo => preference.TypeTask,
                _ => preference.TypeAnnouncement
            };
        }).ToArray();
    }

    /// <summary>
    /// 渠道开关映射：消息通道 → 用户偏好对应渠道布尔列（仅支持单一通道，组合位视为非法入参）
    /// </summary>
    private static bool IsChannelEnabled(SysUserNotificationPreference preference, MessageChannel channel)
    {
        return channel switch
        {
            MessageChannel.SiteNotification => preference.ChannelInApp,
            MessageChannel.Email => preference.ChannelEmail,
            MessageChannel.Sms => preference.ChannelSms,
            MessageChannel.Bot => preference.ChannelBot,
            _ => throw new ArgumentOutOfRangeException(nameof(channel), "偏好门控仅支持单一消息通道。")
        };
    }
}
