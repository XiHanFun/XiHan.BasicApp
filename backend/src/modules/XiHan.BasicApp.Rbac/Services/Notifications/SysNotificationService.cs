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

using Mapster;
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
        return notifications.Adapt<List<NotificationDto>>();
    }

    /// <summary>
    /// 根据通知类型获取通知列表
    /// </summary>
    public async Task<List<NotificationDto>> GetByTypeAsync(NotificationType notificationType)
    {
        var notifications = await _notificationRepository.GetByTypeAsync(notificationType);
        return notifications.Adapt<List<NotificationDto>>();
    }

    /// <summary>
    /// 根据通知状态获取通知列表
    /// </summary>
    public async Task<List<NotificationDto>> GetByStatusAsync(NotificationStatus notificationStatus)
    {
        var notifications = await _notificationRepository.GetByStatusAsync(notificationStatus);
        return notifications.Adapt<List<NotificationDto>>();
    }

    /// <summary>
    /// 根据发送者ID获取通知列表
    /// </summary>
    public async Task<List<NotificationDto>> GetBySenderIdAsync(XiHanBasicAppIdType senderId)
    {
        var notifications = await _notificationRepository.GetBySenderIdAsync(senderId);
        return notifications.Adapt<List<NotificationDto>>();
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
        return notifications.Adapt<List<NotificationDto>>();
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
}
