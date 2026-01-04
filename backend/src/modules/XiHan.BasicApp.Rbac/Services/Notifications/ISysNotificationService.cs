#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysNotificationService
// Guid:a1b2c3d4-e5f6-7890-abcd-ef1234567890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 18:05:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Services.Notifications.Dtos;
using XiHan.Framework.Application.Services.Abstracts;

namespace XiHan.BasicApp.Rbac.Services.Notifications;

/// <summary>
/// 系统通知服务接口
/// </summary>
public interface ISysNotificationService : ICrudApplicationService<NotificationDto, XiHanBasicAppIdType, CreateNotificationDto, UpdateNotificationDto>
{
    /// <summary>
    /// 根据用户ID获取通知列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    Task<List<NotificationDto>> GetByUserIdAsync(XiHanBasicAppIdType userId);

    /// <summary>
    /// 根据通知类型获取通知列表
    /// </summary>
    /// <param name="notificationType">通知类型</param>
    /// <returns></returns>
    Task<List<NotificationDto>> GetByTypeAsync(NotificationType notificationType);

    /// <summary>
    /// 根据通知状态获取通知列表
    /// </summary>
    /// <param name="notificationStatus">通知状态</param>
    /// <returns></returns>
    Task<List<NotificationDto>> GetByStatusAsync(NotificationStatus notificationStatus);

    /// <summary>
    /// 根据发送者ID获取通知列表
    /// </summary>
    /// <param name="senderId">发送者ID</param>
    /// <returns></returns>
    Task<List<NotificationDto>> GetBySenderIdAsync(XiHanBasicAppIdType senderId);

    /// <summary>
    /// 获取用户的未读通知数量
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    Task<int> GetUnreadCountAsync(XiHanBasicAppIdType userId);

    /// <summary>
    /// 获取全局通知列表
    /// </summary>
    /// <returns></returns>
    Task<List<NotificationDto>> GetGlobalNotificationsAsync();

    /// <summary>
    /// 标记通知为已读
    /// </summary>
    /// <param name="input">标记已读DTO</param>
    /// <returns></returns>
    Task<bool> MarkAsReadAsync(MarkReadDto input);
}
