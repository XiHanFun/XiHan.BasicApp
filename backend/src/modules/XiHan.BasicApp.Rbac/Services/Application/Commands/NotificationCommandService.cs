#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:NotificationCommandService
// Guid:e5f6a7b8-c9d0-4e1f-2a3b-4c5d6e7f8a9b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Rbac.Dtos.Base;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Repositories.Abstracts;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Services.Application.Commands;

/// <summary>
/// 通知命令服务（处理通知的写操作）
/// </summary>
public class NotificationCommandService : CrudApplicationServiceBase<SysNotification, RbacDtoBase, long, RbacDtoBase, RbacDtoBase>
{
    private readonly INotificationRepository _notificationRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public NotificationCommandService(INotificationRepository notificationRepository)
        : base(notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    /// <summary>
    /// 发送通知（创建并标记为待发送）
    /// </summary>
    public override async Task<RbacDtoBase> CreateAsync(RbacDtoBase input)
    {
        var notification = input.Adapt<SysNotification>();

        // 设置初始状态为待发送
        notification.Status = Enums.YesOrNo.Yes;
        notification.SendTime = DateTimeOffset.UtcNow;

        notification = await _notificationRepository.AddAsync(notification);

        return await MapToEntityDtoAsync(notification);
    }

    /// <summary>
    /// 标记通知为已读
    /// </summary>
    /// <param name="notificationId">通知ID</param>
    public async Task<bool> MarkAsReadAsync(long notificationId)
    {
        return await _notificationRepository.MarkAsReadAsync(notificationId);
    }

    /// <summary>
    /// 批量标记为已读
    /// </summary>
    /// <param name="notificationIds">通知ID列表</param>
    public async Task<bool> BatchMarkAsReadAsync(List<long> notificationIds)
    {
        return await _notificationRepository.BatchMarkAsReadAsync(notificationIds);
    }

    /// <summary>
    /// 删除通知
    /// </summary>
    /// <param name="notificationId">通知ID</param>
    /// <param name="userId">用户ID</param>
    public async Task<bool> DeleteNotificationAsync(long notificationId, long userId)
    {
        return await _notificationRepository.DeleteNotificationAsync(notificationId, userId);
    }

    /// <summary>
    /// 批量删除过期通知
    /// </summary>
    /// <param name="expirationDate">过期日期</param>
    public async Task<int> DeleteExpiredNotificationsAsync(DateTimeOffset expirationDate)
    {
        return await _notificationRepository.DeleteExpiredNotificationsAsync(expirationDate);
    }
}
