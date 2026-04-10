#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:NotificationAppService
// Guid:2ebf5b34-fd06-4e9e-a4cd-839610e86533
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 10:51:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Application.UseCases.Commands;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.BasicApp.Saas.Hubs;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.Uow;
using XiHan.Framework.Uow.Options;
using XiHan.Framework.Web.RealTime.Constants;
using XiHan.Framework.Web.RealTime.Services;

namespace XiHan.BasicApp.Saas.Application.AppServices.Implementations;

/// <summary>
/// 通知应用服务
/// </summary>
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统Saas服务")]
public class NotificationAppService
    : CrudApplicationServiceBase<SysNotification, NotificationDto, long, NotificationCreateDto, NotificationUpdateDto, BasicAppPRDto>,
        INotificationAppService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationQueryService _queryService;
    private readonly IMessageCacheService _messageCacheService;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IRealtimeNotificationService<BasicAppNotificationHub> _realtimeNotifier;

    /// <summary>
    /// 构造函数
    /// </summary>
    public NotificationAppService(
        INotificationRepository notificationRepository,
        IUserRepository userRepository,
        INotificationQueryService queryService,
        IMessageCacheService messageCacheService,
        IUnitOfWorkManager unitOfWorkManager,
        IRealtimeNotificationService<BasicAppNotificationHub> realtimeNotifier)
        : base(notificationRepository)
    {
        _notificationRepository = notificationRepository;
        _userRepository = userRepository;
        _queryService = queryService;
        _messageCacheService = messageCacheService;
        _unitOfWorkManager = unitOfWorkManager;
        _realtimeNotifier = realtimeNotifier;
    }

    /// <summary>
    /// ID 查询（委托 QueryService，走缓存）
    /// </summary>
    public override async Task<NotificationDto?> GetByIdAsync(long id)
    {
        return await _queryService.GetByIdAsync(id);
    }

    /// <summary>
    /// 获取用户通知列表（通知内容 + 用户级已读状态合并返回）
    /// </summary>
    public async Task<IReadOnlyList<NotificationDto>> GetUserNotificationsAsync(long userId, bool includeRead = true, long? tenantId = null)
    {
        if (userId <= 0)
        {
            throw new ArgumentException("用户 ID 无效", nameof(userId));
        }

        var resolvedTenantId = NormalizeTenantId(tenantId);
        var entities = await _notificationRepository.GetUserNotificationsAsync(userId, includeRead, resolvedTenantId);
        return entities.Select(static entity => entity.Adapt<NotificationDto>()!).ToArray();
    }

    /// <summary>
    /// 获取用户未读通知数量
    /// </summary>
    public async Task<int> GetUnreadCountAsync(long userId, long? tenantId = null)
    {
        if (userId <= 0)
        {
            throw new ArgumentException("用户 ID 无效", nameof(userId));
        }

        var resolvedTenantId = NormalizeTenantId(tenantId);
        return await _messageCacheService.GetUnreadCountAsync(
            userId,
            resolvedTenantId,
            token => _notificationRepository.GetUnreadCountAsync(userId, resolvedTenantId, token));
    }

    /// <summary>
    /// 标记通知为已读
    /// </summary>
    public async Task<bool> MarkAsReadAsync(long notificationId, long userId, long? tenantId = null)
    {
        if (notificationId <= 0 || userId <= 0)
        {
            throw new ArgumentException("通知 ID 或用户 ID 无效");
        }

        var resolvedTenantId = NormalizeTenantId(tenantId);
        var changed = await _notificationRepository.MarkAsReadAsync(notificationId, userId, resolvedTenantId);
        if (changed)
        {
            await _messageCacheService.InvalidateUnreadCountAsync(resolvedTenantId);
        }

        return changed;
    }

    /// <summary>
    /// 标记全部通知为已读
    /// </summary>
    public async Task<int> MarkAllAsReadAsync(long userId, long? tenantId = null)
    {
        if (userId <= 0)
        {
            throw new ArgumentException("用户 ID 无效", nameof(userId));
        }

        var resolvedTenantId = NormalizeTenantId(tenantId);
        var count = await _notificationRepository.MarkAllAsReadAsync(userId, resolvedTenantId);
        if (count > 0)
        {
            await _messageCacheService.InvalidateUnreadCountAsync(resolvedTenantId);
        }

        return count;
    }

    /// <summary>
    /// 确认通知（更新 SysUserNotification 的确认和已读状态）
    /// </summary>
    public async Task<bool> ConfirmAsync(long notificationId, long userId, long? tenantId = null)
    {
        if (notificationId <= 0 || userId <= 0)
        {
            throw new ArgumentException("通知 ID 或用户 ID 无效");
        }

        // 先验证通知本身是否有效且需要确认
        var notification = await _notificationRepository.GetByIdAsync(notificationId);
        if (notification is null || !notification.IsActive() || !notification.NeedConfirm)
        {
            return false;
        }

        var resolvedTenantId = NormalizeTenantId(tenantId);
        if (resolvedTenantId.HasValue && notification.TenantId.HasValue
            && notification.TenantId.Value != resolvedTenantId.Value)
        {
            return false;
        }

        var confirmed = await _notificationRepository.ConfirmRecipientAsync(notificationId, userId);
        if (confirmed)
        {
            await _messageCacheService.InvalidateUnreadCountAsync(notification.TenantId);
        }

        return confirmed;
    }

    /// <summary>
    /// 推送通知：创建通知记录 + 为每个目标用户创建 SysUserNotification 接收记录
    /// </summary>
    public async Task<int> PushAsync(PushNotificationCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        var title = command.Title.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("通知标题不能为空", nameof(command.Title));
        }

        if (command.IsGlobal && command.RecipientUserIds.Count > 0)
        {
            throw new BusinessException(message: "全员通知不允许指定接收人");
        }

        // 前端传字符串 ID，避免 JS Number 丢失雪花 ID 精度
        var recipientIds = command.RecipientUserIds
            .Where(s => long.TryParse(s, out var v) && v > 0)
            .Select(s => long.Parse(s))
            .Distinct()
            .ToArray();

        if (!command.IsGlobal && recipientIds.Length == 0)
        {
            throw new BusinessException(message: "非全员通知必须指定至少一个接收人");
        }

        long? resolvedTenantId = NormalizeTenantId(ParseNullableLong(command.TenantId));
        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);

        SysUser? sender = null;
        var parsedSendUserId = ParseNullableLong(command.SendUserId);
        if (parsedSendUserId.HasValue)
        {
            sender = await _userRepository.GetByIdAsync(parsedSendUserId.Value)
                     ?? throw new KeyNotFoundException($"未找到发送用户: {parsedSendUserId.Value}");

            if (resolvedTenantId.HasValue && sender.TenantId != resolvedTenantId.Value)
            {
                throw new BusinessException(message: "发送用户与通知租户不一致");
            }

            resolvedTenantId ??= sender.TenantId;
        }

        // 确定目标用户列表
        IReadOnlyList<long> targetUserIds;
        if (command.IsGlobal)
        {
            var targetUsers = resolvedTenantId.HasValue
                ? await _userRepository.GetListAsync(user =>
                    user.TenantId == resolvedTenantId.Value && user.Status == YesOrNo.Yes)
                : await _userRepository.GetListAsync(user =>
                    user.Status == YesOrNo.Yes);
            targetUserIds = targetUsers.Select(u => u.BasicId).Distinct().ToArray();
        }
        else
        {
            var entities = await _userRepository.GetByIdsAsync(recipientIds);
            if (entities.Count != recipientIds.Length)
            {
                throw new BusinessException(message: "存在无效接收用户 ID");
            }

            if (resolvedTenantId.HasValue && entities.Any(user => user.TenantId != resolvedTenantId.Value))
            {
                throw new BusinessException(message: "接收用户与通知租户不一致");
            }

            if (!resolvedTenantId.HasValue)
            {
                var distinctTenantIds = entities.Select(user => user.TenantId).Distinct().ToArray();
                if (distinctTenantIds.Length > 1)
                {
                    throw new BusinessException(message: "跨租户通知必须显式指定租户");
                }

                resolvedTenantId = distinctTenantIds[0];
            }

            targetUserIds = recipientIds;
        }

        var sendTime = DateTimeOffset.UtcNow;
        if (command.ExpireTime.HasValue && command.ExpireTime <= sendTime)
        {
            throw new BusinessException(message: "过期时间必须晚于当前时间");
        }

        // 创建通知主记录
        var notification = new SysNotification
        {
            TenantId = resolvedTenantId,
            RecipientUserId = command.IsGlobal ? null : recipientIds.FirstOrDefault(),
            SendUserId = parsedSendUserId,
            NotificationType = command.NotificationType,
            Title = title,
            Content = command.Content,
            Icon = command.Icon,
            Link = command.Link,
            BusinessType = command.BusinessType,
            BusinessId = command.BusinessId,
            NotificationStatus = NotificationStatus.Unread,
            SendTime = sendTime,
            ExpireTime = command.ExpireTime,
            IsGlobal = command.IsGlobal,
            NeedConfirm = command.NeedConfirm,
            Status = YesOrNo.Yes,
            Remark = command.Remark
        };

        var saved = await _notificationRepository.AddAsync(notification);

        // 为每个目标用户创建接收记录
        if (targetUserIds.Count > 0)
        {
            var userNotifications = targetUserIds.Select(uid => new SysUserNotification
            {
                TenantId = resolvedTenantId,
                NotificationId = saved.BasicId,
                UserId = uid,
                NotificationStatus = NotificationStatus.Unread,
            });

            await _notificationRepository.AddRecipientsAsync(userNotifications);
        }

        await uow.CompleteAsync();
        await _messageCacheService.InvalidateUnreadCountAsync(resolvedTenantId);

        // SignalR 实时推送（失败不影响主流程）
        try
        {
            await PushRealtimeNotificationAsync(command, title, resolvedTenantId, targetUserIds);
        }
        catch
        {
            // SignalR 推送失败不影响通知发布主流程
        }

        return targetUserIds.Count;
    }

    /// <summary>
    /// 创建通知（管理页面）：创建通知记录 + 为目标用户创建 SysUserNotification
    /// </summary>
    public override async Task<NotificationDto> CreateAsync(NotificationCreateDto input)
    {
        input.ValidateAnnotations();
        ValidateNotificationPayload(input.Title, input.IsGlobal, input.RecipientUserId, input.SendTime, input.ExpireTime);
        await EnsureUsersExistsAsync(input.RecipientUserId, input.SendUserId, input.TenantId);

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);
        var entity = await MapDtoToEntityAsync(input);
        var saved = await _notificationRepository.AddAsync(entity);

        // 为目标用户创建接收记录
        var resolvedTenantId = NormalizeTenantId(input.TenantId);
        var targetUserIds = await ResolveTargetUserIdsAsync(input.IsGlobal, input.RecipientUserId, resolvedTenantId);

        if (targetUserIds.Count > 0)
        {
            var userNotifications = targetUserIds.Select(uid => new SysUserNotification
            {
                TenantId = resolvedTenantId,
                NotificationId = saved.BasicId,
                UserId = uid,
                NotificationStatus = NotificationStatus.Unread,
            });

            await _notificationRepository.AddRecipientsAsync(userNotifications);
        }

        await uow.CompleteAsync();
        await _messageCacheService.InvalidateUnreadCountAsync(input.TenantId);
        return saved.Adapt<NotificationDto>()!;
    }

    /// <summary>
    /// 更新通知（仅更新通知内容，不影响用户级已读状态）
    /// </summary>
    public override async Task<NotificationDto> UpdateAsync(NotificationUpdateDto input)
    {
        input.ValidateAnnotations();

        var notification = await _notificationRepository.GetByIdAsync(input.BasicId)
                           ?? throw new KeyNotFoundException($"未找到通知: {input.BasicId}");

        ValidateNotificationPayload(input.Title, input.IsGlobal, input.RecipientUserId, input.SendTime, input.ExpireTime);
        await EnsureUsersExistsAsync(input.RecipientUserId, input.SendUserId, notification.TenantId);

        await MapDtoToEntityAsync(input, notification);
        var updated = await _notificationRepository.UpdateAsync(notification);
        await _messageCacheService.InvalidateUnreadCountAsync(notification.TenantId);
        return updated.Adapt<NotificationDto>()!;
    }

    /// <summary>
    /// 删除通知
    /// </summary>
    public override async Task<bool> DeleteAsync(long id)
    {
        var entity = await _notificationRepository.GetByIdAsync(id);
        if (entity is null)
        {
            return false;
        }

        var deleted = await base.DeleteAsync(id);
        if (deleted)
        {
            await _messageCacheService.InvalidateUnreadCountAsync(entity.TenantId);
        }

        return deleted;
    }

    /// <summary>
    /// 映射创建 DTO 到实体
    /// </summary>
    protected override Task<SysNotification> MapDtoToEntityAsync(NotificationCreateDto createDto)
    {
        var entity = new SysNotification
        {
            TenantId = createDto.TenantId,
            RecipientUserId = createDto.IsGlobal ? null : createDto.RecipientUserId,
            SendUserId = createDto.SendUserId,
            NotificationType = createDto.NotificationType,
            Title = createDto.Title.Trim(),
            Content = createDto.Content,
            Icon = createDto.Icon,
            Link = createDto.Link,
            BusinessType = createDto.BusinessType,
            BusinessId = createDto.BusinessId,
            NotificationStatus = NotificationStatus.Unread,
            SendTime = createDto.SendTime,
            ExpireTime = createDto.ExpireTime,
            IsGlobal = createDto.IsGlobal,
            NeedConfirm = createDto.NeedConfirm,
            Status = YesOrNo.Yes,
            Remark = createDto.Remark
        };

        return Task.FromResult(entity);
    }

    /// <summary>
    /// 映射更新 DTO 到实体（不更新用户级已读状态字段）
    /// </summary>
    protected override Task MapDtoToEntityAsync(NotificationUpdateDto updateDto, SysNotification entity)
    {
        entity.RecipientUserId = updateDto.IsGlobal ? null : updateDto.RecipientUserId;
        entity.SendUserId = updateDto.SendUserId;
        entity.NotificationType = updateDto.NotificationType;
        entity.Title = updateDto.Title.Trim();
        entity.Content = updateDto.Content;
        entity.Icon = updateDto.Icon;
        entity.Link = updateDto.Link;
        entity.BusinessType = updateDto.BusinessType;
        entity.BusinessId = updateDto.BusinessId;
        entity.SendTime = updateDto.SendTime;
        entity.ExpireTime = updateDto.ExpireTime;
        entity.IsGlobal = updateDto.IsGlobal;
        entity.NeedConfirm = updateDto.NeedConfirm;
        entity.Status = updateDto.Status;
        entity.Remark = updateDto.Remark;
        return Task.CompletedTask;
    }

    /// <summary>
    /// 校验通知载荷
    /// </summary>
    private static void ValidateNotificationPayload(
        string title,
        bool isGlobal,
        long? recipientUserId,
        DateTimeOffset sendTime,
        DateTimeOffset? expireTime)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new BusinessException(message: "通知标题不能为空");
        }

        if (!isGlobal && (!recipientUserId.HasValue || recipientUserId <= 0))
        {
            throw new BusinessException(message: "非全员通知必须指定接收用户");
        }

        if (isGlobal && recipientUserId.HasValue)
        {
            throw new BusinessException(message: "全员通知不允许指定接收用户");
        }

        if (expireTime.HasValue && expireTime <= sendTime)
        {
            throw new BusinessException(message: "过期时间必须晚于发送时间");
        }
    }

    private static long? NormalizeTenantId(long? tenantId)
    {
        return tenantId.HasValue && tenantId.Value > 0 ? tenantId.Value : null;
    }

    /// <summary>
    /// 将前端传入的字符串 ID 安全解析为 long?，避免 JS Number 精度丢失
    /// </summary>
    private static long? ParseNullableLong(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return long.TryParse(value, out var result) && result > 0 ? result : null;
    }

    /// <summary>
    /// 解析目标用户 ID 列表
    /// </summary>
    private async Task<IReadOnlyList<long>> ResolveTargetUserIdsAsync(bool isGlobal, long? recipientUserId, long? tenantId)
    {
        if (!isGlobal)
        {
            return recipientUserId is > 0 ? [recipientUserId.Value] : [];
        }

        var users = tenantId.HasValue
            ? await _userRepository.GetListAsync(u =>
                u.TenantId == tenantId.Value && u.Status == YesOrNo.Yes)
            : await _userRepository.GetListAsync(u => u.Status == YesOrNo.Yes);
        return users.Select(u => u.BasicId).Distinct().ToArray();
    }

    /// <summary>
    /// 校验发送用户与接收用户是否存在且租户一致
    /// </summary>
    private async Task EnsureUsersExistsAsync(long? recipientUserId, long? sendUserId, long? tenantId)
    {
        var userIds = new[] { recipientUserId, sendUserId }
            .Where(id => id.HasValue && id.Value > 0)
            .Select(id => id!.Value)
            .Distinct()
            .ToArray();

        if (userIds.Length == 0)
        {
            return;
        }

        var users = await _userRepository.GetByIdsAsync(userIds);
        if (users.Count != userIds.Length)
        {
            throw new BusinessException(message: "通知用户不存在");
        }

        if (tenantId.HasValue && users.Any(user => user.TenantId != tenantId.Value))
        {
            throw new BusinessException(message: "通知用户与租户不一致");
        }
    }

    /// <summary>
    /// SignalR 实时推送
    /// </summary>
    private async Task PushRealtimeNotificationAsync(
        PushNotificationCommand command,
        string title,
        long? tenantId,
        IReadOnlyCollection<long> targetUserIds)
    {
        var payload = new
        {
            Type = command.NotificationType switch
            {
                NotificationType.Warning => "Warning",
                NotificationType.Error => "Error",
                NotificationType.Announcement => "Info",
                _ => "Info"
            },
            Title = title,
            Content = command.Content ?? string.Empty,
            NeedConfirm = command.NeedConfirm,
            IsGlobal = command.IsGlobal,
            NotificationType = (int)command.NotificationType,
            TenantId = tenantId,
            SendTime = DateTimeOffset.UtcNow
        };

        if (command.IsGlobal && !tenantId.HasValue)
        {
            await _realtimeNotifier.SendToAllAsync(
                SignalRConstants.ClientMethods.ReceiveNotification,
                payload);
            return;
        }

        if (targetUserIds.Count == 0)
        {
            return;
        }

        var userIdStrings = targetUserIds
            .Select(static id => id.ToString())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        await _realtimeNotifier.SendToUsersAsync(
            userIdStrings,
            SignalRConstants.ClientMethods.ReceiveNotification,
            payload);
    }
}
