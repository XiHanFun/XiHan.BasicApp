#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:INotificationAppService
// Guid:d67eafab-f2f0-4fdf-b31f-f7cd0a6a7fd2
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 10:50:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.UseCases.Commands;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 通知应用服务
/// </summary>
public interface INotificationAppService
    : ICrudApplicationService<SysNotification, NotificationDto, long, NotificationCreateDto, NotificationUpdateDto, BasicAppPRDto>
{
    /// <summary>
    /// 获取用户通知列表
    /// </summary>
    Task<IReadOnlyList<NotificationDto>> GetUserNotificationsAsync(long userId, bool includeRead = true, long? tenantId = null);

    /// <summary>
    /// 获取用户未读通知数量
    /// </summary>
    Task<int> GetUnreadCountAsync(long userId, long? tenantId = null);

    /// <summary>
    /// 标记通知为已读
    /// </summary>
    Task<bool> MarkAsReadAsync(long notificationId, long userId, long? tenantId = null);

    /// <summary>
    /// 标记全部通知为已读
    /// </summary>
    Task<int> MarkAllAsReadAsync(long userId, long? tenantId = null);

    /// <summary>
    /// 确认通知
    /// </summary>
    Task<bool> ConfirmAsync(long notificationId, long userId, long? tenantId = null);

    /// <summary>
    /// 推送通知
    /// </summary>
    Task<int> PushAsync(PushNotificationCommand command);
}
