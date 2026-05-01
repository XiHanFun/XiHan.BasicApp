#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:NotificationApplicationMapper
// Guid:d16feaa4-ee7e-46f8-b6a3-a60ac4632eaa
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 系统通知应用层映射器
/// </summary>
public static class NotificationApplicationMapper
{
    /// <summary>
    /// 映射系统通知列表项
    /// </summary>
    /// <param name="notification">系统通知实体</param>
    /// <returns>系统通知列表项 DTO</returns>
    public static NotificationListItemDto ToListItemDto(SysNotification notification)
    {
        ArgumentNullException.ThrowIfNull(notification);

        return new NotificationListItemDto
        {
            BasicId = notification.BasicId,
            SendUserId = notification.SendUserId,
            NotificationType = notification.NotificationType,
            Title = notification.Title,
            BusinessType = notification.BusinessType,
            BusinessId = notification.BusinessId,
            SendTime = notification.SendTime,
            ExpireTime = notification.ExpireTime,
            IsBroadcast = notification.IsBroadcast,
            NeedConfirm = notification.NeedConfirm,
            IsPublished = notification.IsPublished,
            HasBody = !string.IsNullOrWhiteSpace(notification.Content),
            HasVisualMark = !string.IsNullOrWhiteSpace(notification.Icon),
            HasAction = !string.IsNullOrWhiteSpace(notification.Link),
            HasNote = !string.IsNullOrWhiteSpace(notification.Remark),
            CreatedTime = notification.CreatedTime,
            ModifiedTime = notification.ModifiedTime
        };
    }

    /// <summary>
    /// 映射系统通知详情
    /// </summary>
    /// <param name="notification">系统通知实体</param>
    /// <returns>系统通知详情 DTO</returns>
    public static NotificationDetailDto ToDetailDto(SysNotification notification)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var item = ToListItemDto(notification);
        return new NotificationDetailDto
        {
            BasicId = item.BasicId,
            SendUserId = item.SendUserId,
            NotificationType = item.NotificationType,
            Title = item.Title,
            BusinessType = item.BusinessType,
            BusinessId = item.BusinessId,
            SendTime = item.SendTime,
            ExpireTime = item.ExpireTime,
            IsBroadcast = item.IsBroadcast,
            NeedConfirm = item.NeedConfirm,
            IsPublished = item.IsPublished,
            HasBody = item.HasBody,
            HasVisualMark = item.HasVisualMark,
            HasAction = item.HasAction,
            HasNote = item.HasNote,
            CreatedTime = item.CreatedTime,
            CreatedId = notification.CreatedId,
            CreatedBy = notification.CreatedBy,
            ModifiedTime = item.ModifiedTime,
            ModifiedId = notification.ModifiedId,
            ModifiedBy = notification.ModifiedBy
        };
    }

    /// <summary>
    /// 映射用户通知列表项
    /// </summary>
    /// <param name="userNotification">用户通知实体</param>
    /// <returns>用户通知列表项 DTO</returns>
    public static UserNotificationListItemDto ToUserListItemDto(SysUserNotification userNotification)
    {
        ArgumentNullException.ThrowIfNull(userNotification);

        return new UserNotificationListItemDto
        {
            BasicId = userNotification.BasicId,
            NotificationId = userNotification.NotificationId,
            UserId = userNotification.UserId,
            NotificationStatus = userNotification.NotificationStatus,
            ReadTime = userNotification.ReadTime,
            ConfirmTime = userNotification.ConfirmTime,
            CreatedTime = userNotification.CreatedTime
        };
    }

    /// <summary>
    /// 映射用户通知详情
    /// </summary>
    /// <param name="userNotification">用户通知实体</param>
    /// <returns>用户通知详情 DTO</returns>
    public static UserNotificationDetailDto ToUserDetailDto(SysUserNotification userNotification)
    {
        ArgumentNullException.ThrowIfNull(userNotification);

        var item = ToUserListItemDto(userNotification);
        return new UserNotificationDetailDto
        {
            BasicId = item.BasicId,
            NotificationId = item.NotificationId,
            UserId = item.UserId,
            NotificationStatus = item.NotificationStatus,
            ReadTime = item.ReadTime,
            ConfirmTime = item.ConfirmTime,
            CreatedTime = item.CreatedTime,
            CreatedId = userNotification.CreatedId,
            CreatedBy = userNotification.CreatedBy
        };
    }
}
