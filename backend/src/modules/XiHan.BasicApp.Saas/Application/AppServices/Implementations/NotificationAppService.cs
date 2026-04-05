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
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.Uow;
using XiHan.Framework.Uow.Options;

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

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="notificationRepository"></param>
    /// <param name="userRepository"></param>
    /// <param name="queryService"></param>
    /// <param name="messageCacheService"></param>
    /// <param name="unitOfWorkManager"></param>
    public NotificationAppService(
        INotificationRepository notificationRepository,
        IUserRepository userRepository,
        INotificationQueryService queryService,
        IMessageCacheService messageCacheService,
        IUnitOfWorkManager unitOfWorkManager)
        : base(notificationRepository)
    {
        _notificationRepository = notificationRepository;
        _userRepository = userRepository;
        _queryService = queryService;
        _messageCacheService = messageCacheService;
        _unitOfWorkManager = unitOfWorkManager;
    }

    /// <summary>
    /// ID 查询（委托 QueryService，走缓存）
    /// </summary>
    public override async Task<NotificationDto?> GetByIdAsync(long id)
    {
        return await _queryService.GetByIdAsync(id);
    }

    /// <summary>
    /// 获取用户通知列表
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="includeRead"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<NotificationDto>> GetUserNotificationsAsync(long userId, bool includeRead = true, long? tenantId = null)
    {
        if (userId <= 0)
        {
            throw new ArgumentException("用户 ID 无效", nameof(userId));
        }

        var entities = await _notificationRepository.GetUserNotificationsAsync(userId, includeRead, tenantId);
        return entities.Select(static entity => entity.Adapt<NotificationDto>()!).ToArray();
    }

    /// <summary>
    /// 获取用户未读通知数量
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    public async Task<int> GetUnreadCountAsync(long userId, long? tenantId = null)
    {
        if (userId <= 0)
        {
            throw new ArgumentException("用户 ID 无效", nameof(userId));
        }

        return await _messageCacheService.GetUnreadCountAsync(
            userId,
            tenantId,
            token => _notificationRepository.GetUnreadCountAsync(userId, tenantId, token));
    }

    /// <summary>
    /// 标记通知为已读
    /// </summary>
    /// <param name="notificationId"></param>
    /// <param name="userId"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    public async Task<bool> MarkAsReadAsync(long notificationId, long userId, long? tenantId = null)
    {
        if (notificationId <= 0 || userId <= 0)
        {
            throw new ArgumentException("通知 ID 或用户 ID 无效");
        }

        var changed = await _notificationRepository.MarkAsReadAsync(notificationId, userId, tenantId);
        if (changed)
        {
            await _messageCacheService.InvalidateUnreadCountAsync(tenantId);
        }

        return changed;
    }

    /// <summary>
    /// 标记全部通知为已读
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    public async Task<int> MarkAllAsReadAsync(long userId, long? tenantId = null)
    {
        if (userId <= 0)
        {
            throw new ArgumentException("用户 ID 无效", nameof(userId));
        }

        var count = await _notificationRepository.MarkAllAsReadAsync(userId, tenantId);
        if (count > 0)
        {
            await _messageCacheService.InvalidateUnreadCountAsync(tenantId);
        }

        return count;
    }

    /// <summary>
    /// 确认通知
    /// </summary>
    /// <param name="notificationId"></param>
    /// <param name="userId"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    public async Task<bool> ConfirmAsync(long notificationId, long userId, long? tenantId = null)
    {
        if (notificationId <= 0 || userId <= 0)
        {
            throw new ArgumentException("通知 ID 或用户 ID 无效");
        }

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);

        var notification = await _notificationRepository.GetByIdAsync(notificationId);
        if (notification is null)
        {
            return false;
        }

        if (tenantId.HasValue && notification.TenantId != tenantId.Value)
        {
            return false;
        }

        if (!notification.CanAccess(userId)
            || notification.Status != YesOrNo.Yes
            || notification.NotificationStatus == NotificationStatus.Deleted
            || notification.IsExpired())
        {
            return false;
        }

        notification.Confirm();
        await _notificationRepository.UpdateAsync(notification);
        await uow.CompleteAsync();
        await _messageCacheService.InvalidateUnreadCountAsync(notification.TenantId);
        return true;
    }

    /// <summary>
    /// 推送通知
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
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
            .Where(id => id > 0)
            .Distinct()
            .ToArray();

        if (!command.IsGlobal && recipientIds.Length == 0)
        {
            throw new BusinessException(message: "非全员通知必须指定至少一个接收人");
        }

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);

        long? resolvedTenantId = command.TenantId;

        SysUser? sender = null;
        if (command.SendUserId.HasValue)
        {
            sender = await _userRepository.GetByIdAsync(command.SendUserId.Value)
                     ?? throw new KeyNotFoundException($"未找到发送用户: {command.SendUserId.Value}");

            if (resolvedTenantId.HasValue && sender.TenantId != resolvedTenantId.Value)
            {
                throw new BusinessException(message: "发送用户与通知租户不一致");
            }

            resolvedTenantId ??= sender.TenantId;
        }

        var recipients = Array.Empty<SysUser>();
        if (recipientIds.Length > 0)
        {
            var entities = await _userRepository.GetByIdsAsync(recipientIds);
            if (entities.Count != recipientIds.Length)
            {
                throw new BusinessException(message: "存在无效接收用户 ID");
            }

            recipients = entities.ToArray();

            if (resolvedTenantId.HasValue && recipients.Any(user => user.TenantId != resolvedTenantId.Value))
            {
                throw new BusinessException(message: "接收用户与通知租户不一致");
            }

            if (!resolvedTenantId.HasValue)
            {
                var distinctTenantIds = recipients.Select(user => user.TenantId).Distinct().ToArray();
                if (distinctTenantIds.Length > 1)
                {
                    throw new BusinessException(message: "跨租户通知必须显式指定租户");
                }

                resolvedTenantId = distinctTenantIds[0];
            }
        }

        var sendTime = DateTimeOffset.UtcNow;
        if (command.ExpireTime.HasValue && command.ExpireTime <= sendTime)
        {
            throw new BusinessException(message: "过期时间必须晚于当前时间");
        }

        var count = 0;
        if (command.IsGlobal)
        {
            var notification = new SysNotification
            {
                TenantId = resolvedTenantId,
                RecipientUserId = null,
                SendUserId = command.SendUserId,
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
                IsGlobal = true,
                NeedConfirm = command.NeedConfirm,
                Status = YesOrNo.Yes,
                Remark = command.Remark
            };

            await _notificationRepository.AddAsync(notification);
            count = 1;
        }
        else
        {
            foreach (var recipientId in recipientIds)
            {
                var notification = new SysNotification
                {
                    TenantId = resolvedTenantId,
                    RecipientUserId = recipientId,
                    SendUserId = command.SendUserId,
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
                    IsGlobal = false,
                    NeedConfirm = command.NeedConfirm,
                    Status = YesOrNo.Yes,
                    Remark = command.Remark
                };

                await _notificationRepository.AddAsync(notification);
            }

            count = recipientIds.Length;
        }

        await uow.CompleteAsync();
        await _messageCacheService.InvalidateUnreadCountAsync(resolvedTenantId);
        return count;
    }

    /// <summary>
    /// 创建通知
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public override async Task<NotificationDto> CreateAsync(NotificationCreateDto input)
    {
        input.ValidateAnnotations();
        ValidateNotificationPayload(input.Title, input.IsGlobal, input.RecipientUserId, input.SendTime, input.ExpireTime);
        await EnsureUsersExistsAsync(input.RecipientUserId, input.SendUserId, input.TenantId);
        var dto = await base.CreateAsync(input);
        await _messageCacheService.InvalidateUnreadCountAsync(input.TenantId);
        return dto;
    }

    /// <summary>
    /// 更新通知
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
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
    /// <param name="id"></param>
    /// <returns></returns>
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
    /// <param name="createDto"></param>
    /// <returns></returns>
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
    /// 映射更新 DTO 到实体
    /// </summary>
    /// <param name="updateDto"></param>
    /// <param name="entity"></param>
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
        entity.NotificationStatus = updateDto.NotificationStatus;
        entity.ReadTime = updateDto.ReadTime;
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
    /// <param name="title"></param>
    /// <param name="isGlobal"></param>
    /// <param name="recipientUserId"></param>
    /// <param name="sendTime"></param>
    /// <param name="expireTime"></param>
    /// <exception cref="InvalidOperationException"></exception>
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

    /// <summary>
    /// 校验发送用户与接收用户是否存在且租户一致
    /// </summary>
    /// <param name="recipientUserId"></param>
    /// <param name="sendUserId"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
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
}
