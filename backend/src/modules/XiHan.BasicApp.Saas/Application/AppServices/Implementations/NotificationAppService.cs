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
/// 通知管理服务 —— 管理页 CRUD + 发布 + 内部推送，
/// 用户收件箱操作见 UserInboxAppService
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

    #region 管理页 CRUD

    /// <summary>
    /// 创建通知草稿：非全员通知同时创建 SysUserNotification 接收记录
    /// </summary>
    public override async Task<NotificationDto> CreateAsync(NotificationCreateDto input)
    {
        input.ValidateAnnotations();

        var parsedRecipientUserId = ParseNullableLong(input.RecipientUserId);
        ValidateNotificationPayload(input.Title, input.IsGlobal, parsedRecipientUserId, input.SendTime, input.ExpireTime);
        await EnsureUsersExistsAsync(parsedRecipientUserId, input.SendUserId, input.TenantId);

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);

        var entity = await MapDtoToEntityAsync(input);
        var saved = await _notificationRepository.AddAsync(entity);

        if (!input.IsGlobal && parsedRecipientUserId.HasValue)
        {
            await _notificationRepository.AddRecipientsAsync(
            [
                new SysUserNotification
                {
                    TenantId = saved.TenantId,
                    NotificationId = saved.BasicId,
                    UserId = parsedRecipientUserId.Value,
                    NotificationStatus = NotificationStatus.Unread,
                }
            ]);
        }

        await uow.CompleteAsync();
        return saved.Adapt<NotificationDto>()!;
    }

    /// <summary>
    /// 更新通知草稿（仅未发布的通知可编辑）
    /// </summary>
    public override async Task<NotificationDto> UpdateAsync(NotificationUpdateDto input)
    {
        input.ValidateAnnotations();

        var notification = await _notificationRepository.GetByIdAsync(input.BasicId)
                           ?? throw new KeyNotFoundException($"未找到通知: {input.BasicId}");

        if (notification.IsPublished)
        {
            throw new BusinessException(message: "已发布的通知不可编辑");
        }

        var parsedRecipientUserId = ParseNullableLong(input.RecipientUserId);
        ValidateNotificationPayload(input.Title, input.IsGlobal, parsedRecipientUserId, input.SendTime, input.ExpireTime);
        await EnsureUsersExistsAsync(parsedRecipientUserId, input.SendUserId, notification.TenantId);

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);

        await MapDtoToEntityAsync(input, notification);
        var updated = await _notificationRepository.UpdateAsync(notification);

        await _notificationRepository.DeleteRecipientsByNotificationIdAsync(notification.BasicId);
        if (!input.IsGlobal && parsedRecipientUserId.HasValue)
        {
            await _notificationRepository.AddRecipientsAsync(
            [
                new SysUserNotification
                {
                    TenantId = notification.TenantId,
                    NotificationId = notification.BasicId,
                    UserId = parsedRecipientUserId.Value,
                    NotificationStatus = NotificationStatus.Unread,
                }
            ]);
        }

        await uow.CompleteAsync();
        await _messageCacheService.InvalidateUnreadCountAsync(notification.TenantId);
        return updated.Adapt<NotificationDto>()!;
    }

    /// <summary>
    /// 删除通知草稿（已发布不可删除，同时清理接收记录）
    /// </summary>
    public override async Task<bool> DeleteAsync(long id)
    {
        var entity = await _notificationRepository.GetByIdAsync(id);
        if (entity is null)
        {
            return false;
        }

        if (entity.IsPublished)
        {
            throw new BusinessException(message: "已发布的通知不可删除");
        }

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);

        await _notificationRepository.DeleteRecipientsByNotificationIdAsync(id);
        var deleted = await _notificationRepository.DeleteAsync(entity);

        await uow.CompleteAsync();

        if (deleted)
        {
            await _messageCacheService.InvalidateUnreadCountAsync(entity.TenantId);
        }

        return deleted;
    }

    /// <summary>
    /// 获取通知的接收用户ID列表
    /// </summary>
    public async Task<IReadOnlyList<long>> GetRecipientsAsync(long notificationId)
    {
        return await _notificationRepository.GetRecipientUserIdsByNotificationIdAsync(notificationId);
    }

    #endregion

    #region 发布 + 推送

    /// <summary>
    /// 发布已有草稿通知
    /// </summary>
    public async Task<int> PublishAsync(long notificationId)
    {
        var notification = await _notificationRepository.GetByIdAsync(notificationId)
                           ?? throw new KeyNotFoundException($"未找到通知: {notificationId}");

        if (notification.IsPublished)
        {
            throw new BusinessException(message: "该通知已发布，不可重复发布");
        }

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);

        var resolvedTenantId = NormalizeTenantId(notification.TenantId);
        IReadOnlyList<long> targetUserIds;

        if (notification.IsGlobal)
        {
            targetUserIds = await ResolveAllActiveUserIdsAsync(resolvedTenantId);
            if (targetUserIds.Count == 0)
            {
                throw new BusinessException(message: "没有可推送的目标用户");
            }

            var userNotifications = targetUserIds.Select(uid => new SysUserNotification
            {
                TenantId = resolvedTenantId,
                NotificationId = notification.BasicId,
                UserId = uid,
                NotificationStatus = NotificationStatus.Unread,
            });
            await _notificationRepository.AddRecipientsAsync(userNotifications);
        }
        else
        {
            targetUserIds = await _notificationRepository.GetRecipientUserIdsByNotificationIdAsync(notification.BasicId);
            if (targetUserIds.Count == 0)
            {
                throw new BusinessException(message: "没有可推送的目标用户，请检查是否已指定接收人");
            }
        }

        notification.IsPublished = true;
        notification.SendTime = DateTimeOffset.UtcNow;
        await _notificationRepository.UpdateAsync(notification);

        await uow.CompleteAsync();
        await _messageCacheService.InvalidateUnreadCountAsync(resolvedTenantId);

        try
        {
            await PushRealtimeAsync(notification, resolvedTenantId, targetUserIds);
        }
        catch
        {
            // SignalR 推送失败不影响主流程
        }

        return targetUserIds.Count;
    }

    /// <summary>
    /// 推送通知（由 MessageAppService 等内部调用）：创建并直接发布
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

        var parsedSendUserId = ParseNullableLong(command.SendUserId);
        if (parsedSendUserId.HasValue)
        {
            var sender = await _userRepository.GetByIdAsync(parsedSendUserId.Value)
                         ?? throw new KeyNotFoundException($"未找到发送用户: {parsedSendUserId.Value}");

            if (resolvedTenantId.HasValue && sender.TenantId != resolvedTenantId.Value)
            {
                throw new BusinessException(message: "发送用户与通知租户不一致");
            }

            resolvedTenantId ??= sender.TenantId;
        }

        IReadOnlyList<long> targetUserIds;
        if (command.IsGlobal)
        {
            targetUserIds = await ResolveAllActiveUserIdsAsync(resolvedTenantId);
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

        var notification = new SysNotification
        {
            TenantId = resolvedTenantId,
            SendUserId = parsedSendUserId,
            NotificationType = command.NotificationType,
            Title = title,
            Content = command.Content,
            Icon = command.Icon,
            Link = command.Link,
            BusinessType = command.BusinessType,
            BusinessId = command.BusinessId,
            SendTime = sendTime,
            ExpireTime = command.ExpireTime,
            IsGlobal = command.IsGlobal,
            NeedConfirm = command.NeedConfirm,
            IsPublished = true,
            Status = YesOrNo.Yes,
            Remark = command.Remark
        };

        var saved = await _notificationRepository.AddAsync(notification);

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

        try
        {
            await PushRealtimeAsync(saved, resolvedTenantId, targetUserIds);
        }
        catch
        {
            // SignalR 推送失败不影响通知发布主流程
        }

        return targetUserIds.Count;
    }

    #endregion

    #region 实体映射

    protected override Task<SysNotification> MapDtoToEntityAsync(NotificationCreateDto createDto)
    {
        var entity = new SysNotification
        {
            TenantId = createDto.TenantId,
            SendUserId = createDto.SendUserId,
            NotificationType = createDto.NotificationType,
            Title = createDto.Title.Trim(),
            Content = createDto.Content,
            Icon = createDto.Icon,
            Link = createDto.Link,
            BusinessType = createDto.BusinessType,
            BusinessId = createDto.BusinessId,
            SendTime = createDto.SendTime,
            ExpireTime = createDto.ExpireTime,
            IsGlobal = createDto.IsGlobal,
            NeedConfirm = createDto.NeedConfirm,
            Status = YesOrNo.Yes,
            Remark = createDto.Remark
        };

        return Task.FromResult(entity);
    }

    protected override Task MapDtoToEntityAsync(NotificationUpdateDto updateDto, SysNotification entity)
    {
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

    #endregion

    #region 私有方法

    private static void ValidateNotificationPayload(
        string title, bool isGlobal, long? recipientUserId,
        DateTimeOffset sendTime, DateTimeOffset? expireTime)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new BusinessException(message: "通知标题不能为空");
        if (!isGlobal && (!recipientUserId.HasValue || recipientUserId <= 0))
            throw new BusinessException(message: "非全员通知必须指定接收用户");
        if (isGlobal && recipientUserId.HasValue)
            throw new BusinessException(message: "全员通知不允许指定接收用户");
        if (expireTime.HasValue && expireTime <= sendTime)
            throw new BusinessException(message: "过期时间必须晚于发送时间");
    }

    private static long? NormalizeTenantId(long? tenantId) =>
        tenantId.HasValue && tenantId.Value > 0 ? tenantId.Value : null;

    private static long? ParseNullableLong(string? value) =>
        !string.IsNullOrWhiteSpace(value) && long.TryParse(value, out var result) && result > 0 ? result : null;

    private async Task<IReadOnlyList<long>> ResolveAllActiveUserIdsAsync(long? tenantId)
    {
        var users = tenantId.HasValue
            ? await _userRepository.GetListAsync(u => u.TenantId == tenantId.Value && u.Status == YesOrNo.Yes)
            : await _userRepository.GetListAsync(u => u.Status == YesOrNo.Yes);
        return users.Select(u => u.BasicId).Distinct().ToArray();
    }

    private async Task EnsureUsersExistsAsync(long? recipientUserId, long? sendUserId, long? tenantId)
    {
        var userIds = new[] { recipientUserId, sendUserId }
            .Where(id => id is > 0).Select(id => id!.Value).Distinct().ToArray();
        if (userIds.Length == 0) return;

        var users = await _userRepository.GetByIdsAsync(userIds);
        if (users.Count != userIds.Length)
            throw new BusinessException(message: "通知用户不存在");
        if (tenantId.HasValue && users.Any(user => user.TenantId != tenantId.Value))
            throw new BusinessException(message: "通知用户与租户不一致");
    }

    private async Task PushRealtimeAsync(SysNotification notification, long? tenantId, IReadOnlyCollection<long> targetUserIds)
    {
        var payload = new
        {
            Type = notification.NotificationType switch
            {
                NotificationType.Warning => "Warning",
                NotificationType.Error => "Error",
                _ => "Info"
            },
            notification.Title,
            Content = notification.Content ?? string.Empty,
            notification.NeedConfirm,
            notification.IsGlobal,
            NotificationType = (int)notification.NotificationType,
            TenantId = tenantId,
            SendTime = DateTimeOffset.UtcNow
        };

        if (notification.IsGlobal && !tenantId.HasValue)
        {
            await _realtimeNotifier.SendToAllAsync(SignalRConstants.ClientMethods.ReceiveNotification, payload);
            return;
        }

        if (targetUserIds.Count == 0) return;

        var userIdStrings = targetUserIds.Select(static id => id.ToString()).Distinct(StringComparer.Ordinal).ToArray();
        await _realtimeNotifier.SendToUsersAsync(userIdStrings, SignalRConstants.ClientMethods.ReceiveNotification, payload);
    }

    #endregion
}
