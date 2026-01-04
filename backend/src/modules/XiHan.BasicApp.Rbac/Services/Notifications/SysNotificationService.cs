#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysNotificationService
// Guid:b1c2d3e4-f5g6-7890-abcd-ef1234567890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 18:10:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Extensions;
using XiHan.BasicApp.Rbac.Repositories.Notifications;
using XiHan.BasicApp.Rbac.Services.Notifications.Dtos;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Services.Notifications;

/// <summary>
/// 系统通知服务实现
/// </summary>
public class SysNotificationService : CrudApplicationServiceBase<SysNotification, NotificationDto, XiHanBasicAppIdType, CreateNotificationDto, UpdateNotificationDto>, ISysNotificationService
{
    private readonly ISysNotificationRepository _notificationRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysNotificationService(ISysNotificationRepository notificationRepository) : base(notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    #region 业务特定方法

    /// <summary>
    /// 根据用户ID获取通知列表
    /// </summary>
    public async Task<List<NotificationDto>> GetByUserIdAsync(XiHanBasicAppIdType userId)
    {
        var notifications = await _notificationRepository.GetByUserIdAsync(userId);
        return notifications.ToDto();
    }

    /// <summary>
    /// 根据通知类型获取通知列表
    /// </summary>
    public async Task<List<NotificationDto>> GetByTypeAsync(NotificationType notificationType)
    {
        var notifications = await _notificationRepository.GetByTypeAsync(notificationType);
        return notifications.ToDto();
    }

    /// <summary>
    /// 根据通知状态获取通知列表
    /// </summary>
    public async Task<List<NotificationDto>> GetByStatusAsync(NotificationStatus notificationStatus)
    {
        var notifications = await _notificationRepository.GetByStatusAsync(notificationStatus);
        return notifications.ToDto();
    }

    /// <summary>
    /// 根据发送者ID获取通知列表
    /// </summary>
    public async Task<List<NotificationDto>> GetBySenderIdAsync(XiHanBasicAppIdType senderId)
    {
        var notifications = await _notificationRepository.GetBySenderIdAsync(senderId);
        return notifications.ToDto();
    }

    /// <summary>
    /// 获取用户的未读通知数量
    /// </summary>
    public async Task<int> GetUnreadCountAsync(XiHanBasicAppIdType userId)
    {
        return await _notificationRepository.GetUnreadCountAsync(userId);
    }

    /// <summary>
    /// 获取全局通知列表
    /// </summary>
    public async Task<List<NotificationDto>> GetGlobalNotificationsAsync()
    {
        var notifications = await _notificationRepository.GetGlobalNotificationsAsync();
        return notifications.ToDto();
    }

    /// <summary>
    /// 标记通知为已读
    /// </summary>
    public async Task<bool> MarkAsReadAsync(MarkReadDto input)
    {
        foreach (var notificationId in input.NotificationIds)
        {
            var notification = await _notificationRepository.GetByIdAsync(notificationId);
            if (notification != null && notification.UserId == input.UserId)
            {
                notification.NotificationStatus = NotificationStatus.Read;
                notification.ReadTime = DateTimeOffset.Now;
                await _notificationRepository.UpdateAsync(notification);
            }
        }

        return true;
    }

    #endregion 业务特定方法

    #region 映射方法实现

    /// <summary>
    /// 映射实体到DTO
    /// </summary>
    protected override Task<NotificationDto> MapToEntityDtoAsync(SysNotification entity)
    {
        return Task.FromResult(entity.ToDto());
    }

    /// <summary>
    /// 映射 NotificationDto 到实体（基类方法，不推荐直接使用）
    /// </summary>
    protected override Task<SysNotification> MapToEntityAsync(NotificationDto dto)
    {
        var entity = new SysNotification
        {
            TenantId = dto.TenantId,
            UserId = dto.UserId,
            SenderId = dto.SenderId,
            NotificationType = dto.NotificationType,
            Title = dto.Title,
            Content = dto.Content,
            Icon = dto.Icon,
            Link = dto.Link,
            BusinessType = dto.BusinessType,
            BusinessId = dto.BusinessId,
            NotificationStatus = dto.NotificationStatus,
            ReadTime = dto.ReadTime,
            SendTime = dto.SendTime,
            ExpireTime = dto.ExpireTime,
            IsGlobal = dto.IsGlobal,
            NeedConfirm = dto.NeedConfirm,
            Status = dto.Status,
            Remark = dto.Remark
        };

        return Task.FromResult(entity);
    }

    /// <summary>
    /// 映射 NotificationDto 到现有实体（基类方法，不推荐直接使用）
    /// </summary>
    protected override Task MapToEntityAsync(NotificationDto dto, SysNotification entity)
    {
        entity.Title = dto.Title;
        entity.Content = dto.Content;
        entity.Icon = dto.Icon;
        entity.Link = dto.Link;
        entity.NotificationStatus = dto.NotificationStatus;
        entity.ReadTime = dto.ReadTime;
        entity.ExpireTime = dto.ExpireTime;
        entity.Status = dto.Status;
        entity.Remark = dto.Remark;

        return Task.CompletedTask;
    }

    /// <summary>
    /// 映射创建DTO到实体
    /// </summary>
    protected override Task<SysNotification> MapToEntityAsync(CreateNotificationDto createDto)
    {
        var entity = new SysNotification
        {
            TenantId = createDto.TenantId,
            UserId = createDto.UserId,
            SenderId = createDto.SenderId,
            NotificationType = createDto.NotificationType,
            Title = createDto.Title,
            Content = createDto.Content,
            Icon = createDto.Icon,
            Link = createDto.Link,
            BusinessType = createDto.BusinessType,
            BusinessId = createDto.BusinessId,
            SendTime = DateTimeOffset.Now,
            ExpireTime = createDto.ExpireTime,
            IsGlobal = createDto.IsGlobal,
            NeedConfirm = createDto.NeedConfirm,
            Remark = createDto.Remark
        };

        return Task.FromResult(entity);
    }

    /// <summary>
    /// 映射更新DTO到现有实体
    /// </summary>
    protected override Task MapToEntityAsync(UpdateNotificationDto updateDto, SysNotification entity)
    {
        if (updateDto.Title != null) entity.Title = updateDto.Title;
        if (updateDto.Content != null) entity.Content = updateDto.Content;
        if (updateDto.Icon != null) entity.Icon = updateDto.Icon;
        if (updateDto.Link != null) entity.Link = updateDto.Link;
        if (updateDto.NotificationStatus.HasValue) entity.NotificationStatus = updateDto.NotificationStatus.Value;
        if (updateDto.ExpireTime.HasValue) entity.ExpireTime = updateDto.ExpireTime;
        if (updateDto.Status.HasValue) entity.Status = updateDto.Status.Value;
        if (updateDto.Remark != null) entity.Remark = updateDto.Remark;

        return Task.CompletedTask;
    }

    #endregion 映射方法实现
}
